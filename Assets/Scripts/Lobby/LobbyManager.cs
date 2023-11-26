using System.Collections.Generic;
using UnityEngine;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using QFSW.QC;
using TMPro;
using UnityEngine.UI;

public class LobbyManager : MonoBehaviour
{
    //void RoomCreate()     �� �����
    //void RefreshLobby()   �κ� �� ��������� �ʱ�ȭ
    //void LeaveLobby()     �� ������
    //void StartGame()      ���� ����
    //void SentHeatBeat()   Room �� �������� ��� Heartbeat ������
    //void RefreshLobby()   �濡 ��������(����, ����) ����� �ֽ�ȭ

    #region 1.������ 
    private Lobby hostLobby;
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


    private float heartbeatTimer;
    private float lobbyUpdateTimer;
    #endregion
    #region 2.Start, Update
    private void Update(){
        HandleLobbyHeartbeat();
        HandleLobbyPollForUpdates();
    }

    private void Start(){
        playerName.text = "Hello! " + AuthenticationService.Instance.PlayerName;
        leaveRoomButton.onClick.AddListener(LeaveLobby);
        //callbacks.KickedFromLobby += OnKickedFromLobby;
        //callbacks.LobbyEventConnectionStateChanged += OnLobbyEventConnectionStateChanged;
    }
    #endregion

    #region 3.Lobby
    public async void CreateLobby(){

        try{
            string lobbyName = roomName_input.text;
            CreateLobbyOptions createLobbyOptions = new CreateLobbyOptions      //���߿� ����ٰ� �ɼ� �߰��� �� ����
            {
                IsPrivate = false,
                Player = GetPlayer()
            };

            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers_input.value+2, createLobbyOptions);
            hostLobby = lobby;
            joinedLobby = hostLobby;

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
    public async void RefreshLobby() {
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

    private async void HandleLobbyHeartbeat(){
        if (hostLobby != null)
        {
            heartbeatTimer -= Time.deltaTime;
            if (heartbeatTimer < 0f)
            {
                float heartbeatTimerMax = 15;
                heartbeatTimer = heartbeatTimerMax;

                await LobbyService.Instance.SendHeartbeatPingAsync(hostLobby.Id);
            }
        }
    }

    private async void HandleLobbyPollForUpdates() {
        if (joinedLobby != null)
        {
            lobbyUpdateTimer -= Time.deltaTime;
            if (lobbyUpdateTimer < 0f)
            {
                float lobbyUpdateTimerMax = 1.1f;
                lobbyUpdateTimer = lobbyUpdateTimerMax;

                Lobby lobby = await LobbyService.Instance.GetLobbyAsync(joinedLobby.Id);
                joinedLobby = lobby;


                
            }
        }
    }

    void OnLobbyChanged(ILobbyChanges changes) {
        if (changes.PlayerJoined.Changed || changes.PlayerLeft.Changed) {
            changes.ApplyToLobby(joinedLobby);
            Debug.Log("Player Joined the Room!");
            Debug.Log(joinedLobby.Players.Count);
            foreach (Transform child in playerHolder)
            {
                Destroy(child.gameObject);
            }

            foreach (Player player in joinedLobby.Players)
            {
                
                GameObject playerPanel = Instantiate(playerInRoomPrefab, playerHolder);
                PlayerCreated playerCreated = playerPanel.GetComponent<PlayerCreated>(); 
                playerCreated.SetPlayer(player.Data["PlayerName"].Value);
                playerPanel.name = AuthenticationService.Instance.PlayerName;
            }


        }
    }

    [Command]
    private void PrintPlayers() {
        PrintPlayers(joinedLobby);
    }

    private void PrintPlayers(Lobby lobby){
        Debug.Log("Players in Lobby: ");
        foreach (Player player in lobby.Players)
        {
            Debug.Log(player.Id);
        }
    }

    [Command]
    private async void LeaveLobby() {
        try{
            
            if (hostLobby != null) {
                if(joinedLobby.Players.Count >= 2) MigrateLobbyHost();
            } //���� ȣ��Ʈ ������� �����ϰ� �����ߵ�
            await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, AuthenticationService.Instance.PlayerId);
            roomPanel.SetActive(false);
            hostLobby = null;
            joinedLobby = null;
        }
        catch (LobbyServiceException e)
        {
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
            Debug.Log("Room Joined:");
            roomPanel.SetActive(true);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    private async void MigrateLobbyHost() {
        try {
            hostLobby = await Lobbies.Instance.UpdateLobbyAsync(hostLobby.Id, new UpdateLobbyOptions {
                HostId = joinedLobby.Players[1].Id});
            joinedLobby = hostLobby;
        }
        catch (LobbyServiceException e) {
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
    private void printName() {
        foreach (Player player in joinedLobby.Players)
        {
            Debug.Log(player.Data["PlayerName"].Value);
        }
    }
    #endregion
}
