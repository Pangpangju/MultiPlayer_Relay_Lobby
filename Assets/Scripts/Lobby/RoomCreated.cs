using UnityEngine;
using TMPro;
using Unity.Services.Lobbies;

public class RoomCreated : MonoBehaviour
{
    //���� ��������� �� Instatiate �� ��ư�� �޷� ���� ��ũ��Ʈ�̴�
    //���� �̸�, MaxPlayer ���� �����ϱ� ���� �޼��尡 �޷�����

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
