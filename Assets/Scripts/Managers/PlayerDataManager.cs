using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using System.Linq;
using UnityEngine.InputSystem.LowLevel;
using System;
using Unity.Netcode;
using System.Net;






public enum DeviceType
{
    Device_MouseKeyboard,
    Device_Controller,
}


/// <summary>
/// A structure to encapsulate all elements of a player. 
/// </summary>
[System.Serializable]
public class PlayerDataSet
{
    public ulong Index;
    public PlayerInput Input;
    public TrapToPlace TrapToPlace;
    public GameObject GameObject;
    public Player Player;
    public MouseCursor MouseCursor;
    public NetworkPlayerHeader NetworkedPlayerHeader;
    public PlayerInputDriver InputDriver;

    // ________________________________________________

    public DeviceType DeviceType;

    //__________________________________________________
}


/// <summary>
/// Specific data to apply depending on the players index (e.g. each player index should be represented with a different colour)
/// </summary>
[System.Serializable]
public struct PlayerDataPerIndex
{
    public Color colourBody;
    public Color colourMouse;
    public Color colourMouseOutline;    
}


public class PlayerDataManager : NetworkBehaviour
{
    
    private List<PlayerDataSet> _playerData;
    private bool _keyboardMouseJoined = false;

    [SerializeField] private PlayerDataPerIndex[] _playerDataPerIndex;


    public int PlayerCount
    {
        get 
        {
            if(_playerData == null)
            {
                return 0;
            }
            
            return _playerData.Count;
        }
    }

    public List<PlayerDataSet> PlayerData
    {
        get => _playerData;
    }
    public PlayerDataPerIndex[] PlayerDataPerIndex
    {
        get => _playerDataPerIndex;
    }

    public static PlayerDataManager Singleton;

    // callbacks ________________________________________________
    
    public Action OnRegisterPlayer;
    public Action<ulong> OnPlayerDeath;   
    public Action<ulong> OnPlayerCompleteRound;
    public Action OnRoundEnd;   

    public void Awake()
    {
        if(Singleton != null)
        {
            Debug.LogError("Cannot have multiple PlayerDataManagers...");
            return;
        }

        Singleton = this;
        _playerData = new List<PlayerDataSet>();

        _keyboardMouseJoined = false;

        CustomPhysics.OnRecomputeEntityIds += OnRecomputeEntityIds;
    }

    void OnDestroy()
    {
        CustomPhysics.OnRecomputeEntityIds -= OnRecomputeEntityIds;
    }

    private void OnRecomputeEntityIds()
    {
        for(int i = 0; i < PlayerCount; i++)
        {
            var body = PlayerData[i].Player.GetComponent<CustomPhysicsBody>();
            var networkObject = PlayerData[i].NetworkedPlayerHeader.GetComponent<NetworkObject>();
            
            body.SetDesiredEntityId(networkObject.NetworkObjectId);

            if (Configuration.Singleton.DebugMode)
            {
                DeterminismLogger.LogExtraInfo($"RecomputeEntityIds for player index={PlayerData[i].Index} networkObjectId={networkObject.NetworkObjectId} entityId={body.Body.EntityId}");
            }
        }
    }

    private HashSet<InputDevice> joinedDevices = new HashSet<InputDevice>();

    /*
    public void RegisterPlayer(PlayerInput input)
    {
        // LOCK control scheme permanently
        input.neverAutoSwitchControlSchemes = true;

        PlayerDataSet data = new PlayerDataSet();

        data.playerInput = input;
        data.gameObject = input.gameObject;
        data.index = _playerData.Count;

        // Determine device type via control scheme
        data.deviceType =
            input.currentControlScheme == "Gamepad"
                ? DeviceType.Device_Controller
                : DeviceType.Device_MouseKeyboard;


        // Cache components
        data.player = data.gameObject.GetComponentInChildren<Player>(true);
        data.mouseCursor = data.gameObject.GetComponentInChildren<MouseCursor>(true);
        data.trapToPlace = data.mouseCursor.GetComponentInChildren<TrapToPlace>(true);

        data.score = 0;
        data.alive = true;
        data.hasWon = false;

        Debug.Log(
            $"Player object: {input.gameObject.name}, " +
            $"scene: {input.gameObject.scene.name}"
        );

        Debug.Log($"Registered Player {data.index} using {input.currentControlScheme}");

        // Apply per-player visuals
        var perIndex = _playerDataPerIndex[data.index % _playerDataPerIndex.Length];

        data.player.SetColour(perIndex.colourBody);
        data.mouseCursor.SetColour(perIndex.colourMouse);
        data.mouseCursor.SetOutlineColour(perIndex.colourMouseOutline);

        // Set index on header AFTER playerIndex is assigned
        input.GetComponent<PlayerHeader>().SetIndex(data.index);

        _playerData.Add(data);

        DetermineCorrectActiveStateForSpecificPlayer(_playerData.Count - 1);
    }

    public void UnregisterPlayer(PlayerInput input)
    {
        print("PLAYER UNREGISTERED");
    }

    public void DetermineCorrectActiveState()
    {
        for(int i = 0; i < PlayerCount; i++)
        {
            DetermineCorrectActiveStateForSpecificPlayer(i);
        }
    }

    public void DetermineCorrectActiveStateForSpecificPlayer(int listIndex)
    {
        switch(GameStateManager.Singleton.GetState()){
            
            case GameStateManager.GameStateEnum.Play:
            {
                _playerData[listIndex].mouseCursor.gameObject.SetActive(false);
                _playerData[listIndex].trapToPlace.gameObject.SetActive(false);
                _playerData[listIndex].player.gameObject.SetActive(true);
                break;
            }
            case GameStateManager.GameStateEnum.LobbyPlay:
            case GameStateManager.GameStateEnum.SelectingTrap:
            {
                _playerData[listIndex].mouseCursor.gameObject.SetActive(true);
                _playerData[listIndex].trapToPlace.gameObject.SetActive(false);
                _playerData[listIndex].player.gameObject.SetActive(false);
                _playerData[listIndex].mouseCursor.SetRenderLayer(Helpers.Singleton.uiRenderingLayer);
                break;
            }
            case GameStateManager.GameStateEnum.PlacingTrap:
            {
                _playerData[listIndex].mouseCursor.gameObject.SetActive(true);
                _playerData[listIndex].trapToPlace.gameObject.SetActive(true);
                _playerData[listIndex].player.gameObject.SetActive(false);
                _playerData[listIndex].mouseCursor.SetRenderLayer(Helpers.Singleton.foregroundRenderingLayer);
                break;
            }
        }
    }

    public PlayerDataSet GetPlayerByIndex(int index)
    {
        return _playerData[index];
    }




    */

    public void Callback_OnPlayerDeath(ulong playerIndex)
    {
        OnPlayerDeath?.Invoke(playerIndex);

        // for(int i = 0; i < PlayerCount; i++)
        // {
        //     if(playerIndex == _playerData[i].index)
        //     {
        //         _playerData[i].networkedPlayerHeader.Alive.Value = false;
        //     }
        // }
        
        TryOnRoundEnd();
    }

    public void Callback_OnPlayerCompleteRound(ulong playerIndex)
    {
        OnPlayerCompleteRound?.Invoke(playerIndex);
        // for(int i = 0; i < PlayerCount; i++)
        // {
        //     if(playerIndex == _playerData[i].index)
        //     {
        //         _playerData[i].networkedPlayerHeader.HasWon.Value = false;
        //     }
        // }
        
        TryOnRoundEnd();
    }

    public void TryOnRoundEnd()
    {
        for(int i = 0; i < PlayerData.Count; i++)
        {
            if (PlayerData[i].NetworkedPlayerHeader.Alive.Value && 
                !PlayerData[i].NetworkedPlayerHeader.HasWon.Value)
            {
                return;
            }
        }

        Callback_OnRoundEnd();
    }


    public void Callback_OnRoundEnd()
    {
        OnRoundEnd?.Invoke();
    }


    /// <summary>
    /// Registers a player in the PlayerData structure,
    /// </summary>
    /// <param name="gameDataSetObject">The prefab to instantiate</param>
    /// <param name="playerIndex">The network index of the client owner</param>
    public void RegisterPlayer(GameObject gameDataSetObject, ulong playerIndex)
    {
        PlayerDataSet dataSet = new PlayerDataSet();

        dataSet.GameObject = gameDataSetObject;
        dataSet.Player = gameDataSetObject.GetComponentInChildren<Player>();
        dataSet.InputDriver = gameDataSetObject.GetComponentInChildren<PlayerInputDriver>();

        dataSet.NetworkedPlayerHeader = gameDataSetObject.GetComponent<NetworkPlayerHeader>();
        dataSet.Index = playerIndex;
        dataSet.MouseCursor = gameDataSetObject.GetComponentInChildren<MouseCursor>(true);
        dataSet.TrapToPlace = dataSet.MouseCursor.GetComponentInChildren<TrapToPlace>(true);

        var perIndexStyling = _playerDataPerIndex[(int)playerIndex % _playerDataPerIndex.Length];

        dataSet.Player.SetColour(perIndexStyling.colourBody);
        dataSet.MouseCursor.SetColour(perIndexStyling.colourMouse);
        dataSet.MouseCursor.SetOutlineColour(perIndexStyling.colourMouseOutline);



        _playerData.Add(dataSet);

        OnRegisterPlayer?.Invoke();
    }

    public NetworkPlayerHeader GetNetworkPlayerHeader(ulong playerIndex)
    {
        for(int i = 0; i < PlayerCount; i++)
        {
            if(_playerData[i].Index == playerIndex)
            {
                return _playerData[i].NetworkedPlayerHeader;
            }
        }
        return null;
    }

    public PlayerDataSet GetPlayer(ulong playerIndex)
    {
        for(int i = 0; i < PlayerCount; i++)
        {
            if(_playerData[i].Index == playerIndex)
            {
                return _playerData[i];
            }
        }
        return null;
    }

    /// <summary>
    /// Access the owned network player headers
    /// </summary>
    /// <returns>A list of all the network player headers this client owns (for multiplayer this will probably be 1), for local this would be many</returns>
    public List<NetworkPlayerHeader> GetOwnedNetworkPlayerHeaders()
    {   
        List<NetworkPlayerHeader> list = new List<NetworkPlayerHeader>();

        for(int i = 0; i < PlayerCount; i++)
        {
            if(_playerData[i].NetworkedPlayerHeader.IsOwner)
            {
                list.Add(_playerData[i].NetworkedPlayerHeader);
            }
        }
        
        return list;
    }


    public void SetActiveAllPlayers(bool activeState)
    {
        for(int i = 0; i < PlayerCount; i++)
        {
            if (activeState)
            {
                //PlayerData[i].NetworkedPlayerHeader.Alive.Value = false;
                //PlayerData[i].NetworkedPlayerHeader.HasWon.Value = false;
            }

            PlayerData[i].Player.SetPlayerEnabled(activeState);
        }
    }



    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)] 
    public void ServerSetActiveAllTrapToPlaceRpc(bool activeState)
    {
        print("Call trapToPlace Rpc: " + PlayerCount + " players present");
        for(int i = 0; i < PlayerCount; i++)
        {
            if (activeState)
            {
                print("Set trap to place for player: " + i + ", Trap is: " + _playerData[i].NetworkedPlayerHeader.SelectedTrap.Value);
                _playerData[i].TrapToPlace.SetTrapType(_playerData[i].NetworkedPlayerHeader.SelectedTrap.Value);
            }
            else
            {
                _playerData[i].TrapToPlace.SetTrapType(-1);
            }
        }
    }



    public void SetActiveAllMouseCursors(bool activeState)
    {
        for(int i = 0; i < PlayerCount; i++)
        {
            _playerData[i].MouseCursor.gameObject.SetActive(activeState);
        }
    }

}
