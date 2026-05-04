using System;
using System.IO;
using Unity.Netcode;
using UnityEngine;

public class LevelManager : NetworkBehaviour
{
    [SerializeField] private GameObject _lobbyLevelPrefab;
    [SerializeField] private GameObject[] _levelPrefabs;
    [SerializeField] private Transform _levelParent;

    public LevelHeader LoadedLevelHeader { get => _loadedLevelHeader; }
    public Level LoadedLevel { get => _loadedLevel; }
    public LevelRuntimeData LoadedLevelRuntimeData { get => _loadedLevelRuntimeData; }

    private LevelHeader _loadedLevelHeader;
    private Level _loadedLevel;
    private LevelRuntimeData _loadedLevelRuntimeData;    
    private IntHundredthVector2 _playerSpawnpoint;
    

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
    public void ServerUnloadLevelRpc()
    {
        UnloadLevel();
    }


    public IntHundredthVector2 GetSpawnpoint(int playerIndex)
    {   
        // TODO: It would be nice if this spaced players out? rather than all spawning at the same spot
        return _playerSpawnpoint;
    }    

    /// <summary>
    /// Unloads the currently level, then loads the lobby
    /// </summary>
    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
    public void LoadLobbyRpc()
    {
        UnloadLevel();
        LoadLevel(_lobbyLevelPrefab);
    }

    /// <summary>
    /// Loads a level through a prefab, this is primarily done for the lobby. Other levels are saved as files
    /// </summary>
    /// <param name="prefabToLoad">The level prefab to load</param>
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

        if (IsServer)
        {
            TrapPlacementArea.Singleton.ServerSpawnAllTrapInstancesRpc();

            var bounds = TrapPlacementArea.Singleton.ComputeHorizontalBoundsOfPlacedTraps();
            TrapPlacementArea.Singleton.SetCameraBoundsRpc(bounds.Min, bounds.Max);
        }

        OnLevelLoad?.Invoke();
    }

    /// <summary>
    /// Sends the loaded level data meta data to all clients
    /// </summary>
    /// <param name="header">The header loaded from the level save file</param>
    /// <param name="runtimeData">Additional data computed at runtime</param>
    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
    public void AssignLoadedLevelHeaderRpc(LevelHeader header, LevelRuntimeData runtimeData)
    {
        _loadedLevelHeader = header;
        _loadedLevelRuntimeData = runtimeData;

        ColourPaletteManager.Singleton.ActivePaletteIndex = _loadedLevelHeader.ColourPalette;
        _playerSpawnpoint = IntHundredthVector2.CreateFromHundrethValues(runtimeData.PlayerSpawnpointXHundreth, runtimeData.PlayerSpawnpointYHundreth); 

        OnLevelLoad?.Invoke();
    }

    /// <summary>
    /// Saves the current scenes level state to a file
    /// </summary>
    /// <param name="relativeDataPath">The file location relative to the Application.persistantDataPath, omit the file extension</param>
    public void SaveLevelToFile(string relativeDataPath)
    {
        LevelSaveData saveData = new();

        saveData.Header.Name = Path.GetFileNameWithoutExtension(
            relativeDataPath.TrimEnd('/', '\\')
        );

        saveData.Header.ColourPalette = ColourPaletteManager.Singleton.ActivePaletteIndex;

        bool spawnFound = false;
        bool goalFound = false;

        foreach(var trap in TrapPlacementArea.Singleton.NetworkedPlacedTrapDataList)
        {
            saveData.Traps.Add(trap);

            var trapName = TrapPlacementArea.Singleton.GetTrapDataByIndex(trap.trapTypeIndex).name;

            if (trapName == "PlayerSpawn")
            {
                spawnFound = true;
            }
            if(trapName == "PlayerGoal")
            {
                goalFound = true;
            }
        }

        if(!spawnFound || !goalFound)
        {
            CommandLineSubmission submission = new();
            submission.Message = "Could not save level, a PlayerSpawn or PlayerWin trap is missing";
            submission.MessageType = CommandLineMessageType.Error;

            FindFirstObjectByType<CommandLine>().PushLineToHistory(submission);
            return;
        }

        DataSerializer.SaveObjectToFile(saveData, relativeDataPath + ".level");
    }

    /// <summary>
    /// Loads a level from a file
    /// </summary>
    /// <param name="relativeLevelName">The file location relative to the Application.persistantDataPath, omit the file extension</param>
    /// <returns>True if the load was successful, false otherwise</returns>
    public bool ServerLoadLevelFromFile(string relativeLevelName)
    {
        if (!IsServer)
        {
            Debug.LogWarning("ServerLoadLevel cannot be called by non server (!Server)");
            return false;
        }

        ServerUnloadLevelRpc();

        LevelSaveData saveData = DataSerializer.LoadObjectFromFile<LevelSaveData>(relativeLevelName + ".level");
        IntHundredthVector2? _playerSpawn = null;
        foreach(var trap in saveData.Traps)
        {
            TrapPlacementArea.Singleton.ServerAddTrap(trap);

            var trapName = TrapPlacementArea.Singleton.GetTrapDataByIndex(trap.trapTypeIndex).name;

            if(trapName == "PlayerSpawn")
            {
                _playerSpawn = IntHundredthVector2.CreateFromHundrethValues(trap.positionXHundredths, trap.positionYHundredths);
            }
        }

        if (_playerSpawn == null)
        {
            Debug.LogError("Cannot load level which does not a have a defined spawn point, ensure a PlayerSpawn trap is placed");
            return false;
        }

        var bounds = TrapPlacementArea.Singleton.ComputeHorizontalBoundsOfPlacedTraps();
        TrapPlacementArea.Singleton.SetCameraBoundsRpc(bounds.Min, bounds.Max);

        LevelRuntimeData runtimeData = new();
        runtimeData.CameraXMin = bounds.Min;
        runtimeData.CameraXMax = bounds.Max;
        runtimeData.PlayerSpawnpointXHundreth = _playerSpawn.Value.X.ValueHundredths;
        runtimeData.PlayerSpawnpointYHundreth = _playerSpawn.Value.Y.ValueHundredths;

        AssignLoadedLevelHeaderRpc(saveData.Header, runtimeData);

        return true;
    }
}
