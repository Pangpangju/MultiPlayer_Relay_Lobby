using System.Collections.Generic;
using UnityEngine;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using QFSW.QC;
using TMPro;
using UnityEngine.UI;
using Unity.Services.Relay;
using Unity.Networking.Transport.Relay;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Relay.Models;
using UnityEngine.SceneManagement;


public class LobbyManager : MonoBehaviour
{
    //void RoomCreate()     방 만들기
    //void RefreshLobby()   로비에 방 만들어지면 초기화
    //void LeaveLobby()     방 떠나기
    //void StartGame()      게임 시작
    //void SentHeatBeat()   Room 안 없어지게 계속 Heartbeat 보내기
    //void RefreshLobby()   방에 변동사항(입장, 퇴장) 생기면 최신화

    #region 1.변수들 
    private Lobby joinedLobby;


    [SerializeField] TMP_InputField roomName_input;
    [SerializeField] TMP_Dropdown maxPlayers_input;
    [SerializeField] TMP_Text playerName;
    [SerializeField] Transform roomHolder;
    [SerializeField] Transform playerHolder;
    [SerializeField] GameObject roomPrefab;
    [SerializeField] GameObject playerInRoomPrefab;
    [SerializeField] Button leaveRoomButton;
    [SerializeField] GameObject roomPanel;
    [SerializeField] Button startGameButton;

    [SerializeField] GameObject playerPrefab;

    private float heartbeatTimer;
    private float lobbyUpdateTimer;

    private bool isLobbyHost;

    #endregion
    #region 2.Start, Update
    private void Update(){
        HandleLobbyHeartbeat();
        HandleLobbyPollForUpdates();
    }

    private void Start(){
        playerName.text = "Hello! " + AuthenticationService.Instance.PlayerName;
        leaveRoomButton.onClick.AddListener(LeaveLobby);
    }

    #endregion
    #region 3.Lobby
    public async void CreateLobby(){

        try{
            string lobbyName = roomName_input.text;
            CreateLobbyOptions createLobbyOptions = new CreateLobbyOptions      //나중에 여기다가 옵션 추가할 수 있음
            {
                IsPrivate = false,
                Player = GetPlayer(),
                Data = new Dictionary<string, DataObject> {
                    { "START_GAME", new DataObject(DataObject.VisibilityOptions.Member, "0")}       //게임 시작을 이걸로 해둠
                }
            };

            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers_input.value+2, createLobbyOptions);
            isLobbyHost = true;
            joinedLobby = lobby;

            var callbacks = new LobbyEventCallbacks();
            callbacks.LobbyChanged += OnLobbyChanged;

            await Lobbies.Instance.SubscribeToLobbyEventsAsync(lobby.Id, callbacks);
            Debug.Log("Created Lobby! " + lobby.Name + " " + lobby.MaxPlayers + " " + lobby.LobbyCode);
            roomPanel.SetActive(true);



            GameObject playerPanel = Instantiate(playerInRoomPrefab, playerHolder);
            PlayerCreated playerCreated = playerPanel.GetComponent<PlayerCreated>();        //방만들때는 이벤트 아직 못받아서 직접 Panel 만들어줘야함 그 후로는 자동
            playerCreated.SetPlayer(AuthenticationService.Instance.PlayerName);
            playerPanel.name = AuthenticationService.Instance.PlayerName;
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }



    }
    [Command]
    public async void RefreshLobby() {          //로비 목록을 Query에서 받아와 최신화 시켜줌
        try { 
            QueryResponse queryResponse = await Lobbies.Instance.QueryLobbiesAsync();
            Debug.Log("Lobbies Found: " + queryResponse.Results.Count);
            foreach (Transform child in roomHolder){
                Destroy(child.gameObject);
            }

            foreach (Lobby lobby in queryResponse.Results){
                Button newRoomButton = Instantiate(roomPrefab, roomHolder).GetComponent<Button>();
                newRoomButton.onClick.AddListener(() => JoinRoom(lobby.Id));

                newRoomButton.name = lobby.Name;
                RoomCreated setRoominit = newRoomButton.GetComponent<RoomCreated>();
                setRoominit.SetRoom(lobby.Name, lobby.MaxPlayers, lobby.Id);
            }
        }
        catch (LobbyServiceException e){
            Debug.Log(e);
        }
    }

    private async void HandleLobbyHeartbeat(){      //로비는 일정시간마다 Heartbeat이라는 신호를 보내주지 않으면 자동으로 삭제됨
        if (isLobbyHost)
        {
            heartbeatTimer -= Time.deltaTime;
            if (heartbeatTimer < 0f)
            {
                float heartbeatTimerMax = 15;
                heartbeatTimer = heartbeatTimerMax;

                await LobbyService.Instance.SendHeartbeatPingAsync(joinedLobby.Id);
            }
        }
    }

    private async void HandleLobbyPollForUpdates() {    //로비에 접속해 있는 상태면 서버에서 로비의 변동상황(옵션 변동 등)을 계속 초기화 시켜줘야됨
        if (joinedLobby != null)
        {
            lobbyUpdateTimer -= Time.deltaTime;
            if (lobbyUpdateTimer < 0f)
            {
                float lobbyUpdateTimerMax = 1.1f;
                lobbyUpdateTimer = lobbyUpdateTimerMax;

                Lobby lobby = await LobbyService.Instance.GetLobbyAsync(joinedLobby.Id);
                joinedLobby = lobby;

                if (joinedLobby.Data["START_GAME"].Value != "0") { //게임 시작
                    if (!isLobbyHost) {
                        JoinRelay(joinedLobby.Data["START_GAME"].Value);
                    }
                }

                
            }
        }
    }

    void OnLobbyChanged(ILobbyChanges changes) {            //이벤트 핸들러 (플레이어 입장, 퇴장시 방 최신화)
        if (changes.PlayerJoined.Changed || changes.PlayerLeft.Changed) {
            changes.ApplyToLobby(joinedLobby);
            RoomRefresh(joinedLobby.Players);
        }
    }


    void RoomRefresh(List<Player> players) {                            //방 UI 최신화 하는 함수
        foreach (Transform child in playerHolder)
        {
            Destroy(child.gameObject);
        }
        foreach (Player player in players)
        {
            GameObject playerPanel = Instantiate(playerInRoomPrefab, playerHolder);
            PlayerCreated playerCreated = playerPanel.GetComponent<PlayerCreated>();
            playerCreated.SetPlayer(player.Data["PlayerName"].Value);
            playerPanel.name = AuthenticationService.Instance.PlayerName;
        }
    }

    [Command]
    private async void LeaveLobby() {
        try{
            
            if(joinedLobby.Players.Count >= 2 && isLobbyHost) MigrateLobbyHost();
            //만약 호스트 유저라면 변경하고 나가야됨
            await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, AuthenticationService.Instance.PlayerId);
            roomPanel.SetActive(false);
            isLobbyHost = false;
            joinedLobby = null;
        } catch (LobbyServiceException e) {
            Debug.Log(e);
        }
    }
    

    private async void JoinRoom(string roomID) {
        try
        {
            JoinLobbyByIdOptions joinLobbyByIdOptions = new JoinLobbyByIdOptions
            {
                Player = GetPlayer()
            };

            joinedLobby = await LobbyService.Instance.JoinLobbyByIdAsync(roomID, joinLobbyByIdOptions);
            var callbacks = new LobbyEventCallbacks();
            await Lobbies.Instance.SubscribeToLobbyEventsAsync(roomID, callbacks);

            callbacks.LobbyChanged += OnLobbyChanged;
            roomPanel.SetActive(true);
            RoomRefresh(joinedLobby.Players);
        } catch (LobbyServiceException e) {
            Debug.Log(e);
        }
    }
    [Command]
    private async void MigrateLobbyHost() {
        try {
            joinedLobby = await Lobbies.Instance.UpdateLobbyAsync(joinedLobby.Id, new UpdateLobbyOptions {
                HostId = joinedLobby.Players[1].Id});
        } catch (LobbyServiceException e) {
            Debug.Log(e);
        }
    }

    private Player GetPlayer() {
        return new Player {
            Data = new Dictionary<string, PlayerDataObject> {
                        {"PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, AuthenticationService.Instance.PlayerName) }
            }
        };
    }

    [Command]
    private void PrintName() {
        foreach (Player player in joinedLobby.Players)
        {
            Debug.Log(player.Data["PlayerName"].Value);
        }
    }


    [Command]
    private async void StartGame() {  //클라이언트들이 게임 시작하는 메서드
        if (isLobbyHost) {
            try {
                Allocation allocation = await Relay.Instance.CreateAllocationAsync(2);      //일단 2로
                string joinCode = await Relay.Instance.GetJoinCodeAsync(allocation.AllocationId);
                Debug.Log(joinCode);


                Lobby lobby = await Lobbies.Instance.UpdateLobbyAsync(joinedLobby.Id, new UpdateLobbyOptions { 
                    Data = new Dictionary<string, DataObject> {
                        { "START_GAME", new DataObject(DataObject.VisibilityOptions.Member, joinCode) }
                    }
                });
                RelayServerData relayServerData = new RelayServerData(allocation, "dtls");
                NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);
                NetworkManager.Singleton.StartHost();
                NetworkManager.Singleton.SceneManager.LoadScene("InGameScreen",LoadSceneMode.Single);
            }
            catch (RelayServiceException e) { Debug.Log(e); }
        }
    }
    #endregion
    #region 4.Relay
    [Command]
    private async void JoinRelay(string joinCode)
    {
        try
        {
            Debug.Log("Joining Relay with:" + joinCode);
            
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            RelayServerData relayServerData = new RelayServerData(joinAllocation, "dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);
            NetworkManager.Singleton.StartClient();
            NetworkManager.Singleton.SceneManager.LoadScene("InGameScreen", LoadSceneMode.Single);
        }
        catch (RelayServiceException e) { Debug.Log(e); }
    }
    #endregion
}
