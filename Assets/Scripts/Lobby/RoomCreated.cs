using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomCreated : MonoBehaviour
{
    //���� ��������� �� Instatiate �� ��ư�� �޷� ���� ��ũ��Ʈ�̴�
    //������� ���� ������ �� �����ϴ� ������ ������ ����
    //�������� ������ Prefab �̱� ������ �������� �����͸� �Ҵ����ִ� ������ �ʿ���

    private string roomName;
    private int maxPlayers;
    private string roomId;

    public void OnRoomClicked() {
        Debug.Log("Room Clicked!");
    }

    void SetRoom(string roomNameInput, int maxPlayersInput, string roomIDInput) { 
        
    }
}
