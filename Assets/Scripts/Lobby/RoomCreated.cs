using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;


public class RoomCreated : MonoBehaviour
{
    //방이 만들어졌을 때 Instatiate 된 버튼에 달려 있을 스크립트이다
    //만들어진 방을 눌렀을 때 접속하는 로직을 가지고 있음
    //동적으로 생성될 Prefab 이기 때문에 동적으로 데이터를 할당해주는 로직이 필요함

    private string roomName;
    private int maxPlayers;
    private string roomID;

    [SerializeField] TMP_Text roomNameShow;
    [SerializeField] TMP_Text roomPlayerShow;

    public void OnRoomClicked() {
        Debug.Log("Room Clicked!");
    }

    public void SetRoom(string roomNameInput, int maxPlayersInput, string roomIDInput) {
        roomName = roomNameInput;
        maxPlayers = maxPlayersInput;
        roomID = roomIDInput;

        roomNameShow.text = roomNameInput;
        roomPlayerShow.text = "1/" + maxPlayers.ToString();
    }

}
