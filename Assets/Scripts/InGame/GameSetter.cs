using System.Collections.Generic;
using UnityEngine;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using QFSW.QC;
using Unity.Netcode;

public class GameSetter : NetworkBehaviour
{

    [SerializeField] private GameObject _host;
    [SerializeField] private GameObject _client;
    void Start()
    {
        
        if (IsHost)
        {
            
            Instantiate(_host, new Vector3(0, 0, 0), Quaternion.identity).GetComponent<NetworkObject>().SpawnAsPlayerObject(NetworkManager.Singleton.LocalClientId);
            Debug.Log("Spawned as Host");
            foreach (var Clients in NetworkManager.Singleton.ConnectedClientsList)
            {
                if(Clients.ClientId > 0)        //호스트만
                Instantiate(_client, new Vector3(Clients.ClientId, 0, 0), Quaternion.identity).GetComponent<NetworkObject>().SpawnAsPlayerObject(Clients.ClientId);
            }
        }

    }
    [Command]
    private void PrintPlayers() {
        Debug.Log("Connected Players: " + NetworkManager.Singleton.ConnectedClientsList.Count);
        foreach (var Clients in NetworkManager.Singleton.ConnectedClientsList)
        {

            Debug.Log("Connected PlayerId: " + Clients.ClientId);
        }
    }
}
