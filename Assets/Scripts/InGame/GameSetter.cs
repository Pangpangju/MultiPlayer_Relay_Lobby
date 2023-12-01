using System.Collections.Generic;
using UnityEngine;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;

using Unity.Netcode;



public class GameSetter : NetworkBehaviour
{
    [SerializeField] Transform playerPrefab;

    private GameObject _host;
    private GameObject _client;
    void Start()
    {
        Debug.Log(IsHost);
        Debug.Log(IsServer);
        Debug.Log(IsClient);
        if (IsHost)
        {
            _host = Instantiate(playerPrefab, new Vector3(0, 0, 0), Quaternion.identity).gameObject;
            _host.GetComponent<NetworkObject>().SpawnAsPlayerObject(NetworkManager.Singleton.LocalClientId);
        }
        //else SpawnAllClientsServerRpc(NetworkManager.Singleton.LocalClientId);
    }



    [ServerRpc(RequireOwnership = false)]
    void SpawnAllClientsServerRpc(ulong clientId) {
        _client = Instantiate(playerPrefab, new Vector3(0, 0, 0), Quaternion.identity).gameObject;
        _client.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);
    }
}
