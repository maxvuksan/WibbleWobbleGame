using System;
using Unity.Netcode;
using UnityEngine;

public class LevelManager : NetworkBehaviour
{

    [SerializeField] private GameObject _lobbyLevelPrefab;
    [SerializeField] private GameObject[] _levelPrefabs;
    [SerializeField] private Transform _levelParent;
    private Level _loadedLevel;
    public Level LoadedLevel { get => _loadedLevel; }


    public Action OnLevelLoad;

    public static LevelManager Singleton;

    private void Awake()
    {
        if(Singleton != null)
        {
            Destroy(this.gameObject);
        }

        Singleton = this;
    }


    public void UnloadLevel()
    {
        if(_loadedLevel != null)
        {
            Destroy(_loadedLevel.gameObject);
        }
        if (IsServer)
        {
            TrapPlacementArea.Singleton.ServerClearTraps();
        }
    }

    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
    public void LoadLevelRpc(int levelIndex)
    {
        UnloadLevel();

        LoadLevel(_levelPrefabs[levelIndex]);

        if (IsServer)
        {
            //TrapPlacementArea.Singleton.ServerSignalTrapsLoaded();
            GameStateManager.Singleton.ServerSetGameState(GameStateManager.GameStateEnum.GameState_PreviewLevel);
        }    
    }

    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
    public void LoadLobbyRpc()
    {
        UnloadLevel();
        LoadLevel(_lobbyLevelPrefab);
        print("loaded lobby");
    }

    public void LoadLevel(GameObject prefabToLoad)
    {
        Level spawnedLevel = Instantiate(prefabToLoad, _levelParent).GetComponent<Level>();
        this._loadedLevel = spawnedLevel;

        TrapHeader[] children = spawnedLevel.transform.GetComponentsInChildren<TrapHeader>();

        for(int i = 0; i < children.Length; i++)
        {
            if (IsServer)
            {
                TrapPlacementArea.Singleton.ServerAddTrap(children[i].transform.position, children[i].transform.eulerAngles.z, children[i].TrapName, false);
            }

            Destroy(children[i].gameObject);
        }
        TrapPlacementArea.Singleton.SpawnAllTrapInstances();
    }


    /// <summary>
    /// Spawns the level, Iterates over the level contents, registering the traps with the TrapPlacementArea
    /// </summary>
    public void ServerLoadLevel(int levelIndex)
    {
        if (!IsServer)
        {
            Debug.LogWarning("ServerLoadLevel cannot be called by non server (!Server)");
            return;
        }

        LoadLevelRpc(levelIndex);
    }

}
