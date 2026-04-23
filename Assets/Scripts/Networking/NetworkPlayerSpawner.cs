using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkPlayerSpawner : NetworkBehaviour
{

    [SerializeField] private GameObject _playerDataSetPrefab;

    public override void OnNetworkSpawn()
    {
        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnGameSceneLoaded;
    }
    public override void OnNetworkDespawn()
    {
        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnGameSceneLoaded;
    }

    private void OnGameSceneLoaded(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        if (!IsServer || sceneName != "GameScene") return;

        foreach (ulong clientId in clientsCompleted)
        {
            if (NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(clientId) != null){
                continue;
            }
            
            GameObject gameobject = Instantiate(_playerDataSetPrefab);
            gameobject.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);
        }
    }

    
}
