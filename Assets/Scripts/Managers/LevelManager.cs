using Unity.Netcode;
using UnityEngine;

public class LevelManager : NetworkBehaviour
{

    [SerializeField] private GameObject[] _levelPrefabs;
    [SerializeField] private Transform _levelParent;
    private Level _loadedLevel;


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

        Level spawnedLevel = Instantiate(_levelPrefabs[levelIndex], _levelParent).GetComponent<Level>();
        this._loadedLevel = spawnedLevel;

        StaticTrap[] children = spawnedLevel.transform.GetComponentsInChildren<StaticTrap>();

        for(int i = 0; i < children.Length; i++)
        {
            if (IsServer)
            {
                TrapPlacementArea.Singleton.ServerAddTrap(children[i].transform.position, children[i].transform.eulerAngles.z, children[i].TrapName, false);
            }

            Destroy(children[i].gameObject);
        }

        if (IsServer)
        {
            GameStateManager.Singleton.ServerSetGameState(GameStateManager.GameStateEnum.GameState_PreviewLevel);
        }    
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
