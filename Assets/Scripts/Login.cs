using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using TMPro;

public class Login : MonoBehaviour
{
    [SerializeField] TMP_InputField loginName;

    public async void LoginWithNickName() {
        await UnityServices.InitializeAsync();  

        await AuthenticationService.Instance.SignInAnonymouslyAsync();
        Debug.Log("Signed in " + AuthenticationService.Instance.PlayerId);
    }
}
