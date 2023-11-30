using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using TMPro;
using UnityEngine.SceneManagement;


public class Login : MonoBehaviour
{
    [SerializeField] TMP_InputField loginName;
    
    public async void LoginWithNickName() {
        await UnityServices.InitializeAsync();  

        await AuthenticationService.Instance.SignInAnonymouslyAsync();
        await AuthenticationService.Instance.UpdatePlayerNameAsync(loginName.text);
        Debug.Log("Signed in " + AuthenticationService.Instance.PlayerName);
        SceneManager.LoadSceneAsync("Lobby");
    }


    
}
