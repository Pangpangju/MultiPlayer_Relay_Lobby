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
        


    }
    [Command]
    private void CreatePlayers()
    { 
        if (IsHost)
        {
           
            foreach (var Clients in NetworkManager.Singleton.ConnectedClientsList)
            {
                GameObject playerAvatar;
                playerAvatar = Instantiate(_client, new Vector3(Clients.ClientId, 0, 0), Quaternion.identity);
                playerAvatar.name = "Player " + Clients.ClientId;
                playerAvatar.GetComponent<NetworkObject>().SpawnWithOwnership(Clients.ClientId);
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
