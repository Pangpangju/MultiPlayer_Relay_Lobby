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
    //void RefreshLobby()    �濡 ��������(����, ����) ����� �ֽ�ȭ


    private Lobby hostLobby;
    private Lobby joinedLobby;


    [SerializeField] TMP_InputField roomName_input;
    [SerializeField] Button createRoom;
    [SerializeField] TMP_Dropdown maxPlayers_input;
    [SerializeField] TMP_Text playerName;
    [SerializeField] Transform roomHolder;
    [SerializeField] GameObject roomPrefab;


    private void Start()
    {
        playerName.text = "Hello! "+ AuthenticationService.Instance.PlayerName;
    }
    public async void CreateLobby()
    {
        try
        {
            string lobbyName = roomName_input.text;
            CreateLobbyOptions createLobbyOptions = new CreateLobbyOptions
            {
                IsPrivate = false
            };

            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers_input.value+2, createLobbyOptions);

            hostLobby = lobby;
            joinedLobby = hostLobby;

            Debug.Log("Created Lobby! " + lobby.Name + " " + lobby.MaxPlayers + " " + lobby.LobbyCode);

        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }



    }
    [Command]
    private async void RefreshLobby() {
        try
        {
            QueryResponse queryResponse = await Lobbies.Instance.QueryLobbiesAsync();
            Debug.Log("Lobbies Found: " + queryResponse.Results.Count);
            foreach (Transform child in roomHolder) //���ΰ�ħ
            {
                Destroy(child.gameObject);
            }

            foreach (Lobby lobby in queryResponse.Results)
            {
                GameObject newRoomButton = Instantiate(roomPrefab, roomHolder);
                newRoomButton.name = lobby.Name;
                RoomCreated setRoominit = newRoomButton.GetComponent<RoomCreated>();
                setRoominit.SetRoom(lobby.Name, lobby.MaxPlayers, lobby.Id);
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }
    
}
