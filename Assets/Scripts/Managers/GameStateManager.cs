using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Volatile;

public class GameStateManager : NetworkBehaviour
{


    public enum GameStateEnum
    {

        LobbyPlay,
        PreviewLevel,
        Play,
        ShowingRoundResults,
        SelectingTrap,
        PlacingTrap,
        CreativeMode,
        NUMBER_OF_STATES
    }



    public NetworkVariable<GameStateEnum> NetworkedState = new(GameStateEnum.LobbyPlay, readPerm: NetworkVariableReadPermission.Everyone);

    [SerializeField] private GameObject[] onlyWhenSelectingLevel;
    [SerializeField] private GameObject[] onlyWhenPlaying;  // is disabled in other modes, enabled when state == Play
    [SerializeField] private GameObject[] onlyWhenSelectingTrap; 
    [SerializeField] private GameObject[] onlyWhenPlacingTrap;  
    [SerializeField] private GameObject[] onlyWhenShowingRoundResults;
    [SerializeField] private GameObject[] onlyWhenCreativeMode;
    [SerializeField] private GameObject[] onlyWhenPreviewLevel;

    private GameObject[][] allOnlyWhenArrays;

    public static GameStateManager Singleton;

    [SerializeField] private float hostPreviewHoldSeconds = 3;
    private float _hostPreviewTimerTracked;

    private void Awake()
    {
        allOnlyWhenArrays = new GameObject[][]
        {
            onlyWhenSelectingLevel,
            onlyWhenPlacingTrap,
            onlyWhenSelectingTrap,
            onlyWhenPlaying,
            onlyWhenCreativeMode,
            onlyWhenPreviewLevel,
            onlyWhenShowingRoundResults
        };

        SetActiveAppropriateObjects(null);

        Singleton = this;
        Cursor.visible = false;
    }


    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
    }

    public void ServerSchedulePhysicsForEnable()
    {
        CustomPhysics.TurnOffSimulation();

        for(int i = 0; i < PlayerDataManager.Singleton.PlayerCount; i++)
        {
            PlayerDataManager.Singleton.PlayerData[i].InputDriver.SyncClockThenStartPhysics();
        }
    }

    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
    private void OnGameStateChangedRpc(GameStateEnum newState)
    {
        ApplyGameState(newState);
    }


    private void Start()
    {
        if (IsHost)
        {
            PlayerDataManager.Singleton.OnRoundEnd += ServerOnRoundEnd;
        }
    }

    public override void OnDestroy()
    {
        base.OnDestroy();

        if (IsHost)
        {
            PlayerDataManager.Singleton.OnRoundEnd -= ServerOnRoundEnd;
        }
    }

    public void ServerOnRoundEnd()
    {
        ServerSetGameState(GameStateEnum.SelectingTrap);
    }

    public void Update()
    {
        if (!IsServer)
        {
            return;
        }

        // if the host has set the state to preview, create a timer to switch to the next state...

        if(NetworkedState.Value == GameStateEnum.PreviewLevel)
        {
            _hostPreviewTimerTracked -= Time.deltaTime;
            if(_hostPreviewTimerTracked <= 0)
            {
                ServerSetGameState(GameStateEnum.Play);
            }            
        }
    }

    public void ServerSetGameState(GameStateEnum state)
    {   
        // only the network host can change the game state
        if (IsServer)
        {
            NetworkedState.Value = state;
            OnGameStateChangedRpc(state);
        }
    }

    
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void SuggestFinishSelectingTrapRpc()
    {
        bool allSelected = true;

        for(int i = 0; i < PlayerDataManager.Singleton.PlayerCount; i++)
        {
            if(PlayerDataManager.Singleton.PlayerData[i].NetworkedPlayerHeader.SelectedTrap.Value == -1)
            {
                allSelected = false;
            }
        }

        // players are still selected... do not transition state yet
        if (!allSelected)
        {
            return;
        }


        // if we are still in selecting trap state, move to placing state
        if(NetworkedState.Value == GameStateEnum.SelectingTrap)
        {
            ServerSetGameState(GameStateEnum.PlacingTrap);
        }
    }

    /// <summary>
    /// Is called after the server changes the game state. Makes local state changes and performs networked state changes if IsServer.
    /// </summary>
    /// <param name="_state">The new game state we are transition to</param>
    public void ApplyGameState(GameStateEnum _state)
    {        
        TrapPlacementArea.Singleton.DestroyAllScopedObjects();

        switch (_state)
        {
            case GameStateEnum.CreativeMode:
            {
                LoopingAudioManager.Singleton.SwitchProfile("Light1");

                CustomPhysics.TurnOffSimulation();
                TrapPlacementArea.Singleton.SpawnAllTrapInstances();

                // reset placed trap
                List<NetworkPlayerHeader> headers = PlayerDataManager.Singleton.GetOwnedNetworkPlayerHeaders();
                for(int i = 0; i < headers.Count; i++)
                {
                    headers[i].PlacedTrap.Value = false;
                }

                PlayerDataManager.Singleton.SetActiveAllMouseCursors(true);

                SetActiveAppropriateObjects(onlyWhenCreativeMode);

                if(IsServer){
                    PlayerDataManager.Singleton.ServerSetActiveAllTrapToPlaceRpc(true);
                }

                TrapPlacementArea.Singleton.UpdateCameraBounds();

                TrapSelection.Singleton.HideButtons();

                break;
            }

            case GameStateEnum.LobbyPlay:
            {
                LoopingAudioManager.Singleton.SwitchProfile("Light1");

                CustomPhysics.TurnOffSimulation();

                for(int i = 0; i < PlayerDataManager.Singleton.PlayerCount; i++){
                    
                    PlayerDataManager.Singleton.PlayerData[i].Player.SetPosition(new IntHundredthVector2(0,0));
                }

                TrapPlacementArea.Singleton.DestroyAndClearAllTrapInstances();

                PlayerDataManager.Singleton.SetActiveAllMouseCursors(true);

                PlayerRoundOverScreen.Singleton.Clear();

                if (IsServer)
                {
                    if (PlayerDataManager.Singleton.IsSpawned) // TO DO: The additional is spawned check is to ensure PlayerDataManager has been spawned
                    // ideally we should have an object which coordinates the order of spawning
                    {
                        PlayerDataManager.Singleton.ServerSetActiveAllTrapToPlaceRpc(false);
                    }

                    LevelManager.Singleton.LoadLobbyRpc();

                    ServerSchedulePhysicsForEnable();
                }

                SetActiveAppropriateObjects(onlyWhenSelectingLevel);

                TrapPlacementArea.Singleton.UpdateCameraBounds();

                break;
            }
            case GameStateEnum.PreviewLevel:
            {
                LoopingAudioManager.Singleton.SwitchProfile("Light1");

                CustomPhysics.TurnOffSimulation();
                PlayerDataManager.Singleton.SetActiveAllMouseCursors(false);
                PlayerDataManager.Singleton.SetActiveAllPlayers(false);

                LevelPreviewManager.Singleton.TypeOutLevelText(LevelManager.Singleton.LoadedLevelHeader.Name);
                
                SetActiveAppropriateObjects(onlyWhenPreviewLevel);

                // this variable only has an effect for the host, creates a timer to switch to next game state...
                _hostPreviewTimerTracked = hostPreviewHoldSeconds;

                break;
            }
            case GameStateEnum.SelectingTrap:
            {
                LoopingAudioManager.Singleton.SwitchProfile("Light1");

                CustomPhysics.TurnOffSimulation();
                TrapPlacementArea.Singleton.SpawnAllTrapInstances();
                //PlayerDataManager.Singleton.SetActiveAllPlayers(false);
                PlayerDataManager.Singleton.SetActiveAllMouseCursors(true);

                SetActiveAppropriateObjects(onlyWhenSelectingTrap);

                PlayerRoundOverScreen.Singleton.Clear();
                TrapPlacementArea.Singleton.DestroyAndClearAllTrapInstances();

                if(IsServer){

                    PlayerDataManager.Singleton.ServerSetActiveAllTrapToPlaceRpc(false);
                    
                    // reset trap flags on all players
                    for(int i = 0; i < PlayerDataManager.Singleton.PlayerCount; i++){
                        
                        PlayerDataManager.Singleton.PlayerData[i].NetworkedPlayerHeader.SetSelectedTrapRpc(-1);  
                        PlayerDataManager.Singleton.PlayerData[i].NetworkedPlayerHeader.SetPlacedTrapRpc(false);
                    }
                }

                TrapSelection.Singleton.ShowButtons();

                break;
            }

            case GameStateEnum.PlacingTrap:
            {
                LoopingAudioManager.Singleton.SwitchProfile("Light1");

                CustomPhysics.TurnOffSimulation();
                TrapPlacementArea.Singleton.SpawnAllTrapInstances();

                // reset placed trap
                List<NetworkPlayerHeader> headers = PlayerDataManager.Singleton.GetOwnedNetworkPlayerHeaders();
                for(int i = 0; i < headers.Count; i++)
                {
                    headers[i].PlacedTrap.Value = false;
                }

                PlayerDataManager.Singleton.SetActiveAllMouseCursors(true);

                SetActiveAppropriateObjects(onlyWhenPlacingTrap);
                
                if(IsServer){
                    PlayerDataManager.Singleton.ServerSetActiveAllTrapToPlaceRpc(true);
                }

                TrapSelection.Singleton.HideButtons();

                break;
            }
            case GameStateEnum.Play:
            {
                LoopingAudioManager.Singleton.SwitchProfile("Dark1");

                CustomPhysics.TurnOffSimulation();
                TrapPlacementArea.Singleton.SpawnAllTrapInstances();

                for(int i = 0; i < PlayerDataManager.Singleton.PlayerCount; i++){
                    
                    // TODO: Im not sure if the PlayerData list will be the same order for each player. we should check back on this
                    PlayerDataManager.Singleton.PlayerData[i].Player.SetPosition(LevelManager.Singleton.GetSpawnpoint(i));
                }

                if(IsServer){
                    
                    ServerSchedulePhysicsForEnable();

                    PlayerDataManager.Singleton.ServerSetActiveAllTrapToPlaceRpc(false);

                    // make all players alive again
                    for(int i = 0; i < PlayerDataManager.Singleton.PlayerCount; i++){
                        
                        PlayerDataManager.Singleton.PlayerData[i].NetworkedPlayerHeader.SetPlayerExistsInWorldRpc(true);  
                        PlayerDataManager.Singleton.PlayerData[i].NetworkedPlayerHeader.SetPlayerAliveRpc(true);
                    }
                    
                }

                CameraMovement.SceneSingleton.MinXPosition = LevelManager.Singleton.LoadedLevelRuntimeData.CameraXMin;
                CameraMovement.SceneSingleton.MaxXPosition = LevelManager.Singleton.LoadedLevelRuntimeData.CameraXMax;

                SetActiveAppropriateObjects(onlyWhenPlaying);
                break;
            }
            case GameStateEnum.ShowingRoundResults:
            {
                CustomPhysics.TurnOffSimulation();

                PlayerRoundOverScreen.Singleton.Populate();

                SetActiveAppropriateObjects(onlyWhenShowingRoundResults);

                if(IsServer){
                    StartCoroutine("SwitchToSelectionModeFromRoundResults");
                }

                break;
            }

        }
    }

    /// <summary>
    /// Iterates over each onlyX array enabling if it is equal to the provided array argument, disabled otherwise
    /// </summary>
    /// <param name="array">The array to set active, set to null to disable all</param>
    private void SetActiveAppropriateObjects(GameObject[] activeArray)
    {
        foreach (var array in allOnlyWhenArrays)
        {
            bool shouldBeActive = array == activeArray;
            Helpers.SetActiveGameObjectArray(array, shouldBeActive);
        }
    }

    
    public IEnumerator SwitchToSelectionModeFromRoundResults()
    {
        yield return new WaitForSeconds(3.0f);
        if(NetworkedState.Value == GameStateEnum.ShowingRoundResults)
        {
            ServerSetGameState(GameStateEnum.CreativeMode);
        }
    }

    public IEnumerator SwitchToRoundResultsFromPlayMode()
    {
        yield return new WaitForSeconds(3.0f);
        if(NetworkedState.Value == GameStateEnum.Play)
        {
            ServerSetGameState(GameStateEnum.ShowingRoundResults);
        }
    }

    /// <summary>
    /// Determines if all players have placed a trap, if so we transition to Play state
    /// </summary>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void EvaluateIfAllPlayersHavePlacedTrapRpc()
    {
        for(int i = 0; i < PlayerDataManager.Singleton.PlayerCount; i++)
        {
            if (!PlayerDataManager.Singleton.PlayerData[i].NetworkedPlayerHeader.PlacedTrap.Value)
            {
                return;
            }
        }
        
        ServerSetGameState(GameStateEnum.Play);
    }

    /// <summary>
    /// Determines if all players have died or have won (no longer in play), if so we transition to ShowingRoundResults state
    /// </summary>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void EvaluateIfAllPlayersAreFinishedPlayingRpc()
    {
        for(int i = 0; i < PlayerDataManager.Singleton.PlayerCount; i++)
        {
            NetworkPlayerHeader header = PlayerDataManager.Singleton.PlayerData[i].NetworkedPlayerHeader;

            if ((header.Alive.Value && !header.HasWon.Value) || header.PlayerExistsInWorld.Value)
            {
                return;
            }
        }
        
        StartCoroutine("SwitchToRoundResultsFromPlayMode");
    }


}
