using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Volatile;

public class GameStateManager : NetworkBehaviour
{


    public enum GameStateEnum
    {

        GameState_SelectingLevel,
        GameState_PreviewLevel,
        GameState_Play,
        GameState_ShowingRoundResults,
        GameState_SelectingTrap,
        GameState_PlacingTrap,
        GameState_CreativeMode,
        GameState_NUMBER_OF_STATES
    }

    [System.Serializable]
    public class GameEnviromentalVariables
    {

        public float rigidBodyGravityScale = 4.5f;
    }
    


    public NetworkVariable<GameStateEnum> NetworkedState = new(GameStateEnum.GameState_SelectingLevel, readPerm: NetworkVariableReadPermission.Everyone);

    [SerializeField] private GameObject[] onlyWhenSelectingLevel;
    [SerializeField] private GameObject[] onlyWhenPlaying;  // is disabled in other modes, enabled when state == GameState_Play
    [SerializeField] private GameObject[] onlyWhenSelectingTrap; 
    [SerializeField] private GameObject[] onlyWhenPlacingTrap;  
    [SerializeField] private GameObject[] onlyWhenShowingRoundResults;
    [SerializeField] private GameObject[] onlyWhenCreativeMode;

    public GameEnviromentalVariables enviromentalVariables;

    public static GameStateManager Singleton;

    [SerializeField] private float hostPreviewHoldSeconds = 3;
    private float _hostPreviewTimerTracked;

    private void Awake()
    {
        Singleton = this;
        Cursor.visible = false;
    }


    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        ApplyGameState(NetworkedState.Value);
        StartCoroutine(ApplyInitialStateWhenReady());

    }

    IEnumerator ApplyInitialStateWhenReady()
    {
        yield return new WaitUntil(() => 
            PlayerDataManager.Singleton != null &&
            LevelManager.Singleton != null &&
            TrapPlacementArea.Singleton != null
        );
        
        ApplyGameState(NetworkedState.Value);
    }

    public void ServerSchedulePhysicsForEnable()
    {
        for(int i = 0; i < PlayerDataManager.Singleton.PlayerCount; i++)
        {
            PlayerDataManager.Singleton.PlayerData[i].playerInputDriver.SyncClockThenStartPhysics();
        }
    }

    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
    private void OnGameStateChangedRpc(GameStateEnum newState)
    {
        ApplyGameState(newState);
    }

    private void ResetState()
    {
        enviromentalVariables.rigidBodyGravityScale = 4.5f;
        ServerSetGameState(GameStateEnum.GameState_SelectingLevel);

    }

    private void Start()
    {
        if (IsHost)
        {
            PlayerDataManager.Singleton.OnRoundEnd += NetworkedOnRoundEnd;
            ResetState();
        }
    }

    private void OnDestroy()
    {
        base.OnDestroy();

        if (IsHost)
        {
            PlayerDataManager.Singleton.OnRoundEnd -= NetworkedOnRoundEnd;
        }
    }

    public void NetworkedOnRoundEnd()
    {
        ServerSetGameState(GameStateEnum.GameState_SelectingTrap);
    }

    public void Update()
    {
        if (!IsServer)
        {
            return;
        }

        // if the host has set the state to preview, create a timer to switch to the next state...

        if(NetworkedState.Value == GameStateEnum.GameState_PreviewLevel)
        {
            _hostPreviewTimerTracked -= Time.deltaTime;
            if(_hostPreviewTimerTracked <= 0)
            {
                ServerSetGameState(GameStateEnum.GameState_SelectingTrap);
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
            if(PlayerDataManager.Singleton.PlayerData[i].networkedPlayerHeader.SelectedTrap.Value == -1)
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
        if(NetworkedState.Value == GameStateEnum.GameState_SelectingTrap)
        {
            ServerSetGameState(GameStateEnum.GameState_PlacingTrap);
        }
    }

    /// <summary>
    /// Is called after the server changes the game state. Makes local state changes and performs networked state changes if IsServer.
    /// </summary>
    /// <param name="_state">The new game state we are transition to</param>
    public void ApplyGameState(GameStateEnum _state)
    {
        
        switch (_state)
        {
            case GameStateEnum.GameState_CreativeMode:
            {
                LoopingAudioManager.Singleton.SwitchProfile("Light1");

                CustomPhysics.TurnOffSimulation();
                TrapPlacementArea.Singleton.DestroyAllScopedObjects();
                TrapPlacementArea.Singleton.SpawnAllTrapInstances();

                // reset placed trap
                List<NetworkPlayerHeader> headers = PlayerDataManager.Singleton.GetOwnedNetworkPlayerHeaders();
                for(int i = 0; i < headers.Count; i++)
                {
                    headers[i].PlacedTrap.Value = false;
                }

                PlayerDataManager.Singleton.SetActiveAllMouseCursors(true);

                Helpers.SetActiveGameObjectArray(onlyWhenSelectingLevel, false);
                Helpers.SetActiveGameObjectArray(onlyWhenPlaying, false);
                Helpers.SetActiveGameObjectArray(onlyWhenSelectingTrap, false);
                Helpers.SetActiveGameObjectArray(onlyWhenShowingRoundResults, false);
                Helpers.SetActiveGameObjectArray(onlyWhenPlacingTrap, false);
                
                Helpers.SetActiveGameObjectArray(onlyWhenCreativeMode, true);


                if(IsServer){
                    PlayerDataManager.Singleton.ServerSetActiveAllTrapToPlaceRpc(true);
                }

                TrapSelection.Singleton.HideButtons();

                break;
            }

            case GameStateEnum.GameState_SelectingLevel:
            {
                LoopingAudioManager.Singleton.SwitchProfile("Light1");

                CustomPhysics.TurnOffSimulation();

                for(int i = 0; i < PlayerDataManager.Singleton.PlayerCount; i++){
                    
                    PlayerDataManager.Singleton.PlayerData[i].player.SetPosition(new IntHundredthVector2(0,0));
                }

                TrapPlacementArea.Singleton.DestroyAllScopedObjects();
                TrapPlacementArea.Singleton.DestroyAndClearAllTrapInstances();

                //PlayerDataManager.Singleton.SetActiveAllPlayers(false);
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


                Helpers.SetActiveGameObjectArray(onlyWhenPlacingTrap, false);
                Helpers.SetActiveGameObjectArray(onlyWhenSelectingTrap, false);
                Helpers.SetActiveGameObjectArray(onlyWhenPlaying, false);
                Helpers.SetActiveGameObjectArray(onlyWhenShowingRoundResults, false);
                Helpers.SetActiveGameObjectArray(onlyWhenCreativeMode, false);

                Helpers.SetActiveGameObjectArray(onlyWhenSelectingLevel, true);

                break;
            }
            case GameStateEnum.GameState_PreviewLevel:
            {
                LoopingAudioManager.Singleton.SwitchProfile("Light1");

                CustomPhysics.TurnOffSimulation();
                PlayerDataManager.Singleton.SetActiveAllMouseCursors(false);

                if (IsServer)
                {                    
                    for(int i = 0; i < PlayerDataManager.Singleton.PlayerCount; i++){
                            
                        PlayerDataManager.Singleton.PlayerData[i].networkedPlayerHeader.SetPlayerExistsInWorldRpc(false);  
                    }
                }
                
                Helpers.SetActiveGameObjectArray(onlyWhenPlacingTrap, false);
                Helpers.SetActiveGameObjectArray(onlyWhenSelectingTrap, false);
                Helpers.SetActiveGameObjectArray(onlyWhenPlaying, false);
                Helpers.SetActiveGameObjectArray(onlyWhenShowingRoundResults, false);
                Helpers.SetActiveGameObjectArray(onlyWhenSelectingLevel, false);
                Helpers.SetActiveGameObjectArray(onlyWhenCreativeMode, false);


                // this variable only has an effect for the host, creates a timer to switch to next game state...
                _hostPreviewTimerTracked = hostPreviewHoldSeconds;

                break;
            }
            case GameStateEnum.GameState_SelectingTrap:
            {
                LoopingAudioManager.Singleton.SwitchProfile("Light1");

                CustomPhysics.TurnOffSimulation();
                TrapPlacementArea.Singleton.SpawnAllTrapInstances();
                //PlayerDataManager.Singleton.SetActiveAllPlayers(false);
                PlayerDataManager.Singleton.SetActiveAllMouseCursors(true);

                Helpers.SetActiveGameObjectArray(onlyWhenSelectingLevel, false);
                Helpers.SetActiveGameObjectArray(onlyWhenPlacingTrap, false);
                Helpers.SetActiveGameObjectArray(onlyWhenPlaying, false);
                Helpers.SetActiveGameObjectArray(onlyWhenShowingRoundResults, false);
                Helpers.SetActiveGameObjectArray(onlyWhenCreativeMode, false);

                Helpers.SetActiveGameObjectArray(onlyWhenSelectingTrap, true);

                PlayerRoundOverScreen.Singleton.Clear();
                
                if(IsServer){

                    PlayerDataManager.Singleton.ServerSetActiveAllTrapToPlaceRpc(false);
                    TrapPlacementArea.Singleton.DestroyAndClearAllTrapInstances();
                    
                    // reset trap flags on all players
                    for(int i = 0; i < PlayerDataManager.Singleton.PlayerCount; i++){
                        
                        PlayerDataManager.Singleton.PlayerData[i].networkedPlayerHeader.SetSelectedTrapRpc(-1);  
                        PlayerDataManager.Singleton.PlayerData[i].networkedPlayerHeader.SetPlacedTrapRpc(false);
                    }
                }

                TrapSelection.Singleton.ShowButtons();

                break;
            }

            case GameStateEnum.GameState_PlacingTrap:
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

                Helpers.SetActiveGameObjectArray(onlyWhenSelectingLevel, false);
                Helpers.SetActiveGameObjectArray(onlyWhenPlaying, false);
                Helpers.SetActiveGameObjectArray(onlyWhenSelectingTrap, false);
                Helpers.SetActiveGameObjectArray(onlyWhenShowingRoundResults, false);
                Helpers.SetActiveGameObjectArray(onlyWhenCreativeMode, false);

                Helpers.SetActiveGameObjectArray(onlyWhenPlacingTrap, true);
                
                if(IsServer){
                    PlayerDataManager.Singleton.ServerSetActiveAllTrapToPlaceRpc(true);
                }

                TrapSelection.Singleton.HideButtons();

                break;
            }
            case GameStateEnum.GameState_Play:
            {
                LoopingAudioManager.Singleton.SwitchProfile("Dark1");

                CustomPhysics.TurnOffSimulation();
                TrapPlacementArea.Singleton.SpawnAllTrapInstances();

                for(int i = 0; i < PlayerDataManager.Singleton.PlayerCount; i++){
                    
                    // TODO: Im not sure if the PlayerData list will be the same order for each player. we should check back on this
                    PlayerDataManager.Singleton.PlayerData[i].player.SetPosition(LevelManager.Singleton.LoadedLevel.GetSpawnpoint(i));
                }

                if(IsServer){
                    
                    ServerSchedulePhysicsForEnable();

                    PlayerDataManager.Singleton.ServerSetActiveAllTrapToPlaceRpc(false);

                    // make all players alive again
                    for(int i = 0; i < PlayerDataManager.Singleton.PlayerCount; i++){
                        
                        PlayerDataManager.Singleton.PlayerData[i].networkedPlayerHeader.SetPlayerExistsInWorldRpc(true);  
                        PlayerDataManager.Singleton.PlayerData[i].networkedPlayerHeader.SetPlayerAliveRpc(true);
                    }
                    
                }

                //PlayerDataManager.Singleton.SetActiveAllPlayers(true);

                Helpers.SetActiveGameObjectArray(onlyWhenSelectingLevel, false);
                Helpers.SetActiveGameObjectArray(onlyWhenPlacingTrap, false);
                Helpers.SetActiveGameObjectArray(onlyWhenSelectingTrap, false);
                Helpers.SetActiveGameObjectArray(onlyWhenShowingRoundResults, false);
                Helpers.SetActiveGameObjectArray(onlyWhenCreativeMode, false);

                Helpers.SetActiveGameObjectArray(onlyWhenPlaying, true);
                
                break;
            }
            case GameStateEnum.GameState_ShowingRoundResults:
            {
                CustomPhysics.TurnOffSimulation();
                //TrapPlacementArea.Singleton.DestroyAndClearAllTrapInstances();

                PlayerRoundOverScreen.Singleton.Populate();
                //TrapPlacementArea.Singleton.SpawnAllStaticInstances();
                //PlayerDataManager.Singleton.SetActiveAllPlayers(false);

                Helpers.SetActiveGameObjectArray(onlyWhenSelectingLevel, false);
                Helpers.SetActiveGameObjectArray(onlyWhenPlacingTrap, false);
                Helpers.SetActiveGameObjectArray(onlyWhenSelectingTrap, false);
                Helpers.SetActiveGameObjectArray(onlyWhenPlaying, false);
                Helpers.SetActiveGameObjectArray(onlyWhenCreativeMode, false);

                Helpers.SetActiveGameObjectArray(onlyWhenShowingRoundResults, true);

                if(IsServer){
                    StartCoroutine("SwitchToSelectionModeFromRoundResults");
                }

                break;
            }

        }

        ComputeLevelCameraBounds();
    }

    private void ComputeLevelCameraBounds()
    {
        (float min, float max) = TrapPlacementArea.Singleton.ComputeHorizontalBoundsOfPlacedTraps();
        CameraMovement cameraMovement = FindFirstObjectByType<CameraMovement>();
        cameraMovement.MinXPosition = min;
        cameraMovement.MaxXPosition = max;
    }
    
    public IEnumerator SwitchToSelectionModeFromRoundResults()
    {
        yield return new WaitForSeconds(5.0f);
        if(NetworkedState.Value == GameStateEnum.GameState_ShowingRoundResults)
        {
            ServerSetGameState(GameStateEnum.GameState_CreativeMode);
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
            if (!PlayerDataManager.Singleton.PlayerData[i].networkedPlayerHeader.PlacedTrap.Value)
            {
                return;
            }
        }
        
        ServerSetGameState(GameStateEnum.GameState_Play);
    }

    /// <summary>
    /// Determines if all players have died or have won (no longer in play), if so we transition to ShowingRoundResults state
    /// </summary>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void EvaluateIfAllPlayersAreFinishedPlayingRpc()
    {
        for(int i = 0; i < PlayerDataManager.Singleton.PlayerCount; i++)
        {
            NetworkPlayerHeader header = PlayerDataManager.Singleton.PlayerData[i].networkedPlayerHeader;

            if ((header.Alive.Value && !header.HasWon.Value) || header.PlayerExistsInWorld.Value)
            {
                return;
            }
        }
        
        ServerSetGameState(GameStateEnum.GameState_ShowingRoundResults);
    }

}
