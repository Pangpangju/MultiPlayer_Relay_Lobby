using UnityEngine;
using TMPro;

public class PlayerCreated : MonoBehaviour
{
    [SerializeField] TMP_Text playerNameShow;


    public void SetPlayer(string playerNameInput) {
        playerNameShow.text = playerNameInput;
    }
}
