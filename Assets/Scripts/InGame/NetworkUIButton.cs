using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Netcode;
using Unity.Services.Relay.Models;
using Unity.Services.Relay;
using QFSW.QC;
using Unity.Networking.Transport.Relay;
using Unity.Netcode.Transports.UTP;

public class NetworkUIButton : MonoBehaviour
{
    [SerializeField] Button startHost;
    [SerializeField] Button startClient;

    private async void Start()
    {

        await UnityServices.InitializeAsync();

        await AuthenticationService.Instance.SignInAnonymouslyAsync();
        Debug.Log("Signed in " + AuthenticationService.Instance.PlayerId);

        startHost.onClick.AddListener(StartHost);
    }

    private async void StartHost() {

        try {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(5);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            Debug.Log(joinCode);

            RelayServerData relayServerData = new RelayServerData(allocation, "dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);
            NetworkManager.Singleton.StartHost();

        } catch (RelayServiceException e) { Debug.Log(e); }



        
    }

    [Command]
    private async void StartClient(string joinCode) {
        try {

            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            RelayServerData relayServerData = new RelayServerData(joinAllocation, "dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);
            NetworkManager.Singleton.StartClient();


        } catch (RelayServiceException e) { Debug.Log(e); }
    }
}
