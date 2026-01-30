using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkPlayerSpawner : NetworkBehaviour
{


    [SerializeField] private GameObject _playerDataSetPrefab;


    private void Start()
    {
        DontDestroyOnLoad(this.gameObject);    
    }

    public override void OnNetworkSpawn()
    {
        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnGameSceneLoaded;
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }
    public override void OnNetworkDespawn()
    {
        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnGameSceneLoaded;
        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
    }

    public void OnClientConnected(ulong clientId)
    {
        if (!IsServer)
        {
            return;
        }

        // the player is already present...
        if (NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(clientId) != null){
            return;
        }

        GameObject gameObject = Instantiate(_playerDataSetPrefab);
        gameObject.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);    
         
        print("connect player" + clientId);
    }

    private void OnGameSceneLoaded(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        if (!IsServer || sceneName != "GameScene")
        {
            return;
        }

        for(int i = 0; i < clientsCompleted.Count; i++)
        {
            OnClientConnected(clientsCompleted[i]);
        }

    }
}
