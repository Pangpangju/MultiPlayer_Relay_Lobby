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


public class LobbyManager : NetworkBehaviour
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
    [SerializeField] GameObject loadingPanel;


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
                        //JoinRelay(joinedLobby.Data["START_GAME"].Value);
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

        if (changes.Data.Changed) {
            changes.ApplyToLobby(joinedLobby);
            JoinRelay(joinedLobby.Data["START_GAME"].Value);
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
                loadingPanel.SetActive(true);
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
                NetworkManager.Singleton.SceneManager.OnSceneEvent += SceneManager_OnSceneEvent;

                NetworkManager.Singleton.SceneManager.LoadScene("InGameTestSceen",LoadSceneMode.Single);
                
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
            loadingPanel.SetActive(true);
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            RelayServerData relayServerData = new RelayServerData(joinAllocation, "dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);
            NetworkManager.Singleton.StartClient();
            NetworkManager.Singleton.SceneManager.OnSceneEvent += SceneManager_OnSceneEvent;
        }
        catch (RelayServiceException e) { Debug.Log(e); }
    }

    private void SceneMovementHandler() {
        
        //이벤트 등록


        //로딩 창 띄우기
        //전부 씬에 등록되었는지 이벤트로 확인
        //이벤트로 확인 되면 로딩창에 progress bar 로 표
        //씬에 전부 등록되었으면 전부 씬으로 이동
    }

    private void SceneManager_OnSceneEvent(SceneEvent sceneEvent) {
        switch (sceneEvent.SceneEventType)
        {
            // Handle server to client Load Notifications
            case SceneEventType.Load:
                {
                    // This event provides you with the associated AsyncOperation
                    // AsyncOperation.progress can be used to determine scene loading progression
                    var asyncOperation = sceneEvent.AsyncOperation;
                    // Since the server "initiates" the event we can simply just check if we are the server here
                    
                    if (IsHost)
                    {
                        Debug.Log("Load...by Server");
                        // Handle server side load event related tasks here
                    }
                    else
                    {
                        Debug.Log("Load...by Client");
                        // Handle client side load event related tasks here
                    }
                    break;
                }
            // Handle server to client unload notifications
            case SceneEventType.Unload:
                {
                    // You can use the same pattern above under SceneEventType.Load here
                    break;
                }
            // Handle client to server LoadComplete notifications
            case SceneEventType.LoadComplete:
                {
                    
                    // This will let you know when a load is completed
                    // Server Side: receives this notification for both itself and all clients
                    if (IsHost)
                    {
                        Debug.Log("Load Complete! by Server");
                        if (sceneEvent.ClientId == NetworkManager.LocalClientId)
                        {
                            // Handle server side LoadComplete related tasks here
                        }
                        else
                        {
                            // Handle client LoadComplete **server-side** notifications here
                        }
                    }
                    else // Clients generate this notification locally
                    {
                        Debug.Log("Load Complete! by Client");
                        // Handle client side LoadComplete related tasks here
                    }

                    // So you can use sceneEvent.ClientId to also track when clients are finished loading a scene
                    break;
                }
            // Handle Client to Server Unload Complete Notification(s)
            case SceneEventType.UnloadComplete:
                {
                    // This will let you know when an unload is completed
                    // You can follow the same pattern above as SceneEventType.LoadComplete here

                    // Server Side: receives thisn'tification for both itself and all clients
                    // Client Side: receives thisn'tification for itself

                    // So you can use sceneEvent.ClientId to also track when clients are finished unloading a scene
                    break;
                }
            // Handle Server to Client Load Complete (all clients finished loading notification)
            case SceneEventType.LoadEventCompleted:
                {
                    
                    // This will let you know when all clients have finished loading a scene
                    // Received on both server and clients
                    foreach (var clientId in sceneEvent.ClientsThatCompleted)
                    {
                        // Example of parsing through the clients that completed list
                        if (IsHost)
                        {
                            Debug.Log("Load Event Complete! by Server");
                            // Handle any server-side tasks here
                        }
                        else
                        {
                            Debug.Log("Load Event Complete! by Client");
                            // Handle any client-side tasks here
                        }
                    }
                    break;
                }
            // Handle Server to Client unload Complete (all clients finished unloading notification)
            case SceneEventType.UnloadEventCompleted:
                {
                    // This will let you know when all clients have finished unloading a scene
                    // Received on both server and clients
                    foreach (var clientId in sceneEvent.ClientsThatCompleted)
                    {
                        // Example of parsing through the clients that completed list
                        if (IsServer)
                        {
                            // Handle any server-side tasks here
                        }
                        else
                        {
                            // Handle any client-side tasks here
                        }
                    }
                    break;
                }
        }
    }

    #endregion
}
