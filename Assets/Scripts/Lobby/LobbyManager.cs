using System.Collections.Generic;
using UnityEngine;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using QFSW.QC;
using TMPro;

public class LobbyManager : MonoBehaviour
{
    //void RoomCreate()     방 만들기
    //void RefreshLobby()   로비에 방 만들어지면 초기화
    //void LeaveLobby()     방 떠나기
    //void StartGame()      게임 시작
    //void SentHeatBeat()   Room 안 없어지게 계속 Heartbeat 보내기
    //void RefreshRoom()    방에 변동사항(입장, 퇴장) 생기면 최신화


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

    private Player GetPlayer()          //만약 다른 함수에서 Player 을 매개변수로 요구할 때 필요한 return 함수 Signed In 메서드가 아니기 때문에 Custom 하게 닉네임을 선언한 것임
    {
        return new Player
        {
            Data = new Dictionary<string, PlayerDataObject> {
                        {"PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, playerName) }
            }
        };
    }

}
