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
    //void RoomCreate()     �� �����
    //void RefreshLobby()   �κ� �� ��������� �ʱ�ȭ
    //void LeaveLobby()     �� ������
    //void StartGame()      ���� ����
    //void SentHeatBeat()   Room �� �������� ��� Heartbeat ������
    //void RefreshLobby()   �濡 ��������(����, ����) ����� �ֽ�ȭ

    #region 1.������ 
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
            CreateLobbyOptions createLobbyOptions = new CreateLobbyOptions      //���߿� ����ٰ� �ɼ� �߰��� �� ����
            {
                IsPrivate = false,
                Player = GetPlayer(),
                Data = new Dictionary<string, DataObject> {
                    { "START_GAME", new DataObject(DataObject.VisibilityOptions.Member, "0")}       //���� ������ �̰ɷ� �ص�
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
            PlayerCreated playerCreated = playerPanel.GetComponent<PlayerCreated>();        //�游�鶧�� �̺�Ʈ ���� ���޾Ƽ� ���� Panel ���������� �� �ķδ� �ڵ�
            playerCreated.SetPlayer(AuthenticationService.Instance.PlayerName);
            playerPanel.name = AuthenticationService.Instance.PlayerName;
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }



    }
    [Command]
    public async void RefreshLobby() {          //�κ� ����� Query���� �޾ƿ� �ֽ�ȭ ������
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

    private async void HandleLobbyHeartbeat(){      //�κ�� �����ð����� Heartbeat�̶�� ��ȣ�� �������� ������ �ڵ����� ������
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

    private async void HandleLobbyPollForUpdates() {    //�κ� ������ �ִ� ���¸� �������� �κ��� ������Ȳ(�ɼ� ���� ��)�� ��� �ʱ�ȭ ������ߵ�
        if (joinedLobby != null)
        {
            lobbyUpdateTimer -= Time.deltaTime;
            if (lobbyUpdateTimer < 0f)
            {
                float lobbyUpdateTimerMax = 1.1f;
                lobbyUpdateTimer = lobbyUpdateTimerMax;

                Lobby lobby = await LobbyService.Instance.GetLobbyAsync(joinedLobby.Id);
                joinedLobby = lobby;

                if (joinedLobby.Data["START_GAME"].Value != "0") { //���� ����
                    if (!isLobbyHost) {
                        JoinRelay(joinedLobby.Data["START_GAME"].Value);
                    }
                }

                
            }
        }
    }

    void OnLobbyChanged(ILobbyChanges changes) {            //�̺�Ʈ �ڵ鷯 (�÷��̾� ����, ����� �� �ֽ�ȭ)
        if (changes.PlayerJoined.Changed || changes.PlayerLeft.Changed) {
            changes.ApplyToLobby(joinedLobby);
            RoomRefresh(joinedLobby.Players);
        }
    }


    void RoomRefresh(List<Player> players) {                            //�� UI �ֽ�ȭ �ϴ� �Լ�
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
            //���� ȣ��Ʈ ������� �����ϰ� �����ߵ�
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
    private async void StartGame() {  //Ŭ���̾�Ʈ���� ���� �����ϴ� �޼���
        if (isLobbyHost) {
            try {
                Allocation allocation = await Relay.Instance.CreateAllocationAsync(2);      //�ϴ� 2��
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
