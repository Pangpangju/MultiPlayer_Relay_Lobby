using System.Collections.Generic;
using UnityEngine;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using QFSW.QC;
using TMPro;

public class LobbyManager : MonoBehaviour
{
    //void RoomCreate()     �� �����
    //void RefreshLobby()   �κ� �� ��������� �ʱ�ȭ
    //void LeaveLobby()     �� ������
    //void StartGame()      ���� ����
    //void SentHeatBeat()   Room �� �������� ��� Heartbeat ������
    //void RefreshRoom()    �濡 ��������(����, ����) ����� �ֽ�ȭ


    private Lobby hostLobby;
    private Lobby joinedLobby;
    [SerializeField] TMP_InputField roomName_input;
    [SerializeField] TMP_InputField maxPlayers_input;


    private string playerName;

    public async void CreateLobby()
    {
        try
        {
            string lobbyName = roomName_input.text;
            int maxPlayers = int.Parse(maxPlayers_input.text);
            CreateLobbyOptions createLobbyOptions = new CreateLobbyOptions
            {
                IsPrivate = false,
                Player = GetPlayer()
            };

            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, createLobbyOptions);

            hostLobby = lobby;
            joinedLobby = hostLobby;

            Debug.Log("Created Lobby! " + lobby.Name + " " + lobby.MaxPlayers + " " + lobby.LobbyCode);

        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }



    }

    private Player GetPlayer()          //���� �ٸ� �Լ����� Player �� �Ű������� �䱸�� �� �ʿ��� return �Լ� Signed In �޼��尡 �ƴϱ� ������ Custom �ϰ� �г����� ������ ����
    {
        return new Player
        {
            Data = new Dictionary<string, PlayerDataObject> {
                        {"PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, playerName) }
            }
        };
    }

}
