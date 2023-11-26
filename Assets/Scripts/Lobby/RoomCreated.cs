using UnityEngine;
using TMPro;
using Unity.Services.Lobbies;

public class RoomCreated : MonoBehaviour
{
    //방이 만들어졌을 때 Instatiate 된 버튼에 달려 있을 스크립트이다
    //방의 이름, MaxPlayer 등을 설정하기 위한 메서드가 달려있음

    private string roomName;
    private int maxPlayers;
    private string roomID;

    [SerializeField] TMP_Text roomNameShow;
    [SerializeField] TMP_Text roomPlayerShow;

    public void SetRoom(string roomNameInput, int maxPlayersInput, string roomIDInput) {
        roomName = roomNameInput;
        maxPlayers = maxPlayersInput;
        roomID = roomIDInput;

        roomNameShow.text = roomNameInput;
        roomPlayerShow.text = "1/" + maxPlayers.ToString();
    }


}
