using System;
using Unity.Netcode;
using UnityEngine;

public class NetworkPlayerHeader : NetworkBehaviour
{
    public NetworkVariable<ulong> PlayerIndex           = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner); 
    public NetworkVariable<bool> Alive                  = new(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner); 
    public NetworkVariable<bool> HasWon                 = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner); 
    public NetworkVariable<int> Score                   = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner); 
    public NetworkVariable<int> SelectedTrap            = new(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner); 
    public NetworkVariable<float> TrapPlacementRotation = new(0.0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner); 
    public NetworkVariable<bool> PlacedTrap             = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner); 
    public NetworkVariable<bool> PlayerExistsInWorld    = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner); 

    [SerializeField] private TrapToPlace _trapToPlace;


    public override void OnNetworkSpawn()
    {
        SelectedTrap.OnValueChanged += OnSelectedTrapChanged;
        PlacedTrap.OnValueChanged += OnPlacedTrapChanged;
        PlayerExistsInWorld.OnValueChanged += OnPlayerExistsInWorld;

        PlayerDataManager.Singleton.RegisterPlayer(this.gameObject, OwnerClientId);

        if (IsOwner)
        {
            PlayerIndex.Value = OwnerClientId;
            ResetStateRpc();
        }
    }

    private void OnSelectedTrapChanged(int oldValue, int newValue)
    {
        if(GameStateManager.Singleton.NetworkedState.Value == GameStateManager.GameStateEnum.GameState_SelectingTrap)
        {
            GameStateManager.Singleton.SuggestFinishSelectingTrapRpc();
        }
        else if(GameStateManager.Singleton.NetworkedState.Value == GameStateManager.GameStateEnum.GameState_PlacingTrap ||
                GameStateManager.Singleton.NetworkedState.Value == GameStateManager.GameStateEnum.GameState_CreativeMode 
        )
        {
            _trapToPlace.SetTrapType(newValue);
        }
    }
    private void OnPlacedTrapChanged(bool oldValue, bool newValue)
    {
        // hide trapToPlace if we placed trap
        if (newValue)
        {
            _trapToPlace.SetTrapType(-1); 
            GameStateManager.Singleton.EvaluateIfAllPlayersHavePlacedTrapRpc();
        }
    }

    private void OnPlayerExistsInWorld(bool oldState, bool newState)
    {
        PlayerDataManager.Singleton.PlayerData[(int)PlayerIndex.Value].player.ApplyExistsInWorldLocally(newState);         
    }

    [Rpc(SendTo.Owner, InvokePermission = RpcInvokePermission.Everyone)]
    public void SetSelectedTrapRpc(int trapIndex)
    {
        SelectedTrap.Value = trapIndex;
    }

    [Rpc(SendTo.Owner, InvokePermission = RpcInvokePermission.Everyone)]
    public void SetPlacedTrapRpc(bool hasPlacedTrap)
    {
        PlacedTrap.Value = hasPlacedTrap;
    }

    [Rpc(SendTo.Owner, InvokePermission = RpcInvokePermission.Everyone)]
    public void SetPlayerAliveRpc(bool alive)
    {
        Alive.Value = alive;

        if (!alive)
        {
            // player should not exist in world after dying
            SetPlayerExistsInWorldRpc(false);   
        }
    }

    [Rpc(SendTo.Owner, InvokePermission = RpcInvokePermission.Everyone)]
    public void SetHasWonRpc(bool hasWon)
    {
        HasWon.Value = hasWon;

        if (hasWon)
        {
            // player should not exist in world after dying
            SetPlayerExistsInWorldRpc(false);   
        }
    }

    [Rpc(SendTo.Owner, InvokePermission = RpcInvokePermission.Everyone)]
    public void SetScoreRpc(int score)
    {
        Score.Value = score;
    }

    [Rpc(SendTo.Owner, InvokePermission = RpcInvokePermission.Everyone)]
    public void SetPlayerExistsInWorldRpc(bool exists)
    {
        PlayerExistsInWorld.Value = exists;

        if (!exists)
        {
            GameStateManager.Singleton.EvaluateIfAllPlayersAreFinishedPlayingRpc();
        }
    }


    [Rpc(SendTo.Owner, InvokePermission = RpcInvokePermission.Owner | RpcInvokePermission.Server)]
    public void ResetStateRpc()
    {
        Alive.Value = true;
        HasWon.Value = false;
        Score.Value = 0;
        SelectedTrap.Value = -1;
        PlayerExistsInWorld.Value = true;
        PlayerDataManager.Singleton.PlayerData[(int)PlayerIndex.Value].player.SetPositionRpc(Vector3.zero);
        PlayerDataManager.Singleton.PlayerData[(int)PlayerIndex.Value].player.ResetState();
    }

    
}

