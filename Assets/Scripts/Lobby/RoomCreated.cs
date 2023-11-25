using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;


public class RoomCreated : MonoBehaviour
{
    //���� ��������� �� Instatiate �� ��ư�� �޷� ���� ��ũ��Ʈ�̴�
    //������� ���� ������ �� �����ϴ� ������ ������ ����
    //�������� ������ Prefab �̱� ������ �������� �����͸� �Ҵ����ִ� ������ �ʿ���

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
