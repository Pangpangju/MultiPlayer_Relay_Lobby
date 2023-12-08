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
                        //JoinRelay(joinedLobby.Data["START_GAME"].Value);
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

        if (changes.Data.Changed) {
            changes.ApplyToLobby(joinedLobby);
            JoinRelay(joinedLobby.Data["START_GAME"].Value);
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
                loadingPanel.SetActive(true);
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
        
        //�̺�Ʈ ���


        //�ε� â ����
        //���� ���� ��ϵǾ����� �̺�Ʈ�� Ȯ��
        //�̺�Ʈ�� Ȯ�� �Ǹ� �ε�â�� progress bar �� ǥ
        //���� ���� ��ϵǾ����� ���� ������ �̵�
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
