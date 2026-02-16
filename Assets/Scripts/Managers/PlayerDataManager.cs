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
    public ulong index;
    public PlayerInput playerInput;
    public TrapToPlace trapToPlace;
    public GameObject gameObject;
    public Player player;
    public MouseCursor mouseCursor;
    public NetworkPlayerHeader networkedPlayerHeader;
    public PlayerInputDriver playerInputDriver;

    // ________________________________________________

    public DeviceType deviceType;

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
            
            case GameStateManager.GameStateEnum.GameState_Play:
            {
                _playerData[listIndex].mouseCursor.gameObject.SetActive(false);
                _playerData[listIndex].trapToPlace.gameObject.SetActive(false);
                _playerData[listIndex].player.gameObject.SetActive(true);
                break;
            }
            case GameStateManager.GameStateEnum.GameState_SelectingLevel:
            case GameStateManager.GameStateEnum.GameState_SelectingTrap:
            {
                _playerData[listIndex].mouseCursor.gameObject.SetActive(true);
                _playerData[listIndex].trapToPlace.gameObject.SetActive(false);
                _playerData[listIndex].player.gameObject.SetActive(false);
                _playerData[listIndex].mouseCursor.SetRenderLayer(Helpers.Singleton.uiRenderingLayer);
                break;
            }
            case GameStateManager.GameStateEnum.GameState_PlacingTrap:
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
            if (PlayerData[i].networkedPlayerHeader.Alive.Value && 
                !PlayerData[i].networkedPlayerHeader.HasWon.Value)
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

    public void RegisterPlayer(GameObject gameDataSetObject, ulong playerIndex)
    {
        PlayerDataSet dataSet = new PlayerDataSet();

        print("register player" + playerIndex);

        dataSet.gameObject = gameDataSetObject;
        dataSet.player = gameDataSetObject.GetComponentInChildren<Player>();
        dataSet.playerInputDriver = gameDataSetObject.GetComponentInChildren<PlayerInputDriver>();

        dataSet.networkedPlayerHeader = gameDataSetObject.GetComponent<NetworkPlayerHeader>();
        dataSet.index = playerIndex;
        dataSet.mouseCursor = gameDataSetObject.GetComponentInChildren<MouseCursor>(true);
        dataSet.trapToPlace = dataSet.mouseCursor.GetComponentInChildren<TrapToPlace>(true);

        var perIndexStyling = _playerDataPerIndex[(int)playerIndex % _playerDataPerIndex.Length];

        dataSet.player.SetColour(perIndexStyling.colourBody);
        dataSet.mouseCursor.SetColour(perIndexStyling.colourMouse);
        dataSet.mouseCursor.SetOutlineColour(perIndexStyling.colourMouseOutline);

        _playerData.Add(dataSet);
    }

    public NetworkPlayerHeader GetNetworkPlayerHeader(ulong playerIndex)
    {
        for(int i = 0; i < PlayerCount; i++)
        {
            if(_playerData[i].index == playerIndex)
            {
                return _playerData[i].networkedPlayerHeader;
            }
        }
        return null;
    }

    public PlayerDataSet GetPlayer(ulong playerIndex)
    {
        for(int i = 0; i < PlayerCount; i++)
        {
            if(_playerData[i].index == playerIndex)
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
            if(_playerData[i].networkedPlayerHeader.IsOwner)
            {
                list.Add(_playerData[i].networkedPlayerHeader);
            }
        }
        
        return list;
    }

    public void ServerResetAllPlayerStates()
    {
        for(int i = 0; i < PlayerCount; i++)
        {
            PlayerData[i].networkedPlayerHeader.ResetStateRpc();
        }
    }

    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
    public void SetActiveAllPlayersRpc(bool activeState)
    {
        for(int i = 0; i < PlayerCount; i++)
        {
            if (activeState)
            {
                PlayerData[i].networkedPlayerHeader.Alive.Value = false;
                PlayerData[i].networkedPlayerHeader.HasWon.Value = false;
            }

            PlayerData[i].player.enabled = true;
            PlayerData[i].player.gameObject.SetActive(activeState);    
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
                print("Set trap to place for player: " + i + ", Trap is: " + _playerData[i].networkedPlayerHeader.SelectedTrap.Value);
                _playerData[i].trapToPlace.SetTrapType(_playerData[i].networkedPlayerHeader.SelectedTrap.Value);
            }
            else
            {
                _playerData[i].trapToPlace.SetTrapType(-1);
            }
        }
    }



    public void SetActiveAllMouseCursors(bool activeState, bool moveToUILayer)
    {
        for(int i = 0; i < PlayerCount; i++)
        {
            if (moveToUILayer)
            {
                _playerData[i].mouseCursor.SetRenderLayer(Helpers.Singleton.uiRenderingLayer);
            }
            else
            {
                _playerData[i].mouseCursor.SetRenderLayer(Helpers.Singleton.foregroundRenderingLayer);
            }

            _playerData[i].mouseCursor.gameObject.SetActive(activeState);
        }
    }

}
