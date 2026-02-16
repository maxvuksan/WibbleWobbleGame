using System.Collections.Generic;
using Steamworks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;


// A snapshot of the players input at a specific physics tick
public class PlayerInputAtTick
{
    public PlayerClientInputs Inputs;
    public bool wasPredicted = false; // was the input actually recieved, or was it predicted
}


/// <summary>
/// Is responsible for controller a player instance
/// </summary>
public class PlayerInputDriver : NetworkBehaviour
{

    /// <summary>
    /// Determines the number of ticks an input is buffered for
    /// </summary>
    public const int InputBufferTickDelay = 2;

    /// <summary>
    /// Encapsulates the players input (is the player pressing move or jump?)
    /// </summary>
    public PlayerClientInputs PlayerInputs {
        get => _playerInputs;
        private set => _playerInputs = value;
    }
    private PlayerClientInputs _playerInputs;


    [SerializeField] private NetworkPlayerHeader _playerHeader;

    private bool _hasPrefilledBuffer;
    private Dictionary<long, PlayerInputAtTick> _inputBuffer = new();
    private ControllerInputHandler _input;
    

    void Awake()
    {
        _input = FindFirstObjectByType<ControllerInputHandler>();

        CustomPhysics.OnPhysicsTick += OnPhysicsTick;
        CustomPhysics.OnPostPhysicsTick += OnPostPhysicsTick;

        _hasPrefilledBuffer = true;
        _playerInputs = new();
    }

    public override void OnDestroy()
    {
        base.OnDestroy();

        CustomPhysics.OnPhysicsTick -= OnPhysicsTick;
        CustomPhysics.OnPostPhysicsTick -= OnPostPhysicsTick;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        if (IsOwner)
        {
            Debug.Log($"Player {_playerHeader.PlayerIndex} pre-filling buffer from tick 0 to {InputBufferTickDelay}");
            
            // Pre-fill buffer with default inputs so simulation can start
            for (int i = 0; i <= InputBufferTickDelay; i++)
            {
                long tick = i;
                PlayerInputAtTick defaultInput = new();
                _inputBuffer[tick] = defaultInput;

                BroadcastInputsForTickRpc(defaultInput.Inputs, tick);
            }
        }
    }

    void OnPhysicsTick()
    {
        if (IsOwner)
        {
            // store current input in buffer, offset by out input delay

            long tick = CustomPhysics.Tick + InputBufferTickDelay + 1;

            PlayerInputAtTick inputAtTick = new();
            _inputBuffer[tick] = inputAtTick;
            _inputBuffer[tick].Inputs = CalculatePlayerInput(); 

            BroadcastInputsForTickRpc(_inputBuffer[tick].Inputs, tick);
        }

        _playerInputs= CalculatePlayerInput(); 
    }

    void OnPostPhysicsTick()
    {
        EvaluateIfWeCanAdvanceNextPhysicsTick();
        PopCurrentInputTicksInBuffer();
    }


    /// Gets the input at the current CustomPhysics.Tick setting it as the active input, then removes its key value pair from the buffer
    /// </summary>
    private void PopCurrentInputTicksInBuffer()
    {
        if (_inputBuffer.TryGetValue(CustomPhysics.Tick, out PlayerInputAtTick bufferedInputs))
        {
            _playerInputs = bufferedInputs.Inputs;

            // TODO: NEED A WAY TO CLEAN UP OLD INPUTS
            /*
                Maybe introduce a holdInputsForXTicks variable, any inputs that are X ticks in the past we discard,
                this should be sigificantly large, so in the case of a rollback we have inputs to go back to.

                if a rollback is super large, throw an ingame error (saying you have been disconnected from the game/round)
            */
            _inputBuffer.Remove(CustomPhysics.Tick); // Clean up old inputs
        }
        else
        {
            // we dont have said input... predict?
            //_playerInputs = PredictInputForTick(CustomPhysics.Tick);
        }
    }

    private PlayerClientInputs PredictInputForTick(long tick)
    {
        return new PlayerClientInputs();
    }

    /// <summary>
    /// Translates inputs from the ControllerInputHandler to the appropriate data structure for driving the player, returns said structure
    /// </summary>
    private PlayerClientInputs CalculatePlayerInput()
    {
        PlayerClientInputs playerInput = new();

        // Jump
        playerInput.InputJump = PlayerJumpInput.None;
        if (_input.Input.mainButtonIsPressed)
        {
            playerInput.InputJump = PlayerJumpInput.JumpPressed;
        }

        // Horizontal movement
        playerInput.InputMoveDirection = PlayerMoveInput.None;
        if(_input.Input.mouseCursorVelocity.x > 0.1)
        {
            playerInput.InputMoveDirection = PlayerMoveInput.RightPressed;
        }
        else if(_input.Input.mouseCursorVelocity.x < -0.1)
        {
            playerInput.InputMoveDirection = PlayerMoveInput.LeftPressed;
        }

        return playerInput;
    }

    /// <summary>
    /// Sends the players input from the owner to each other client
    /// </summary>
    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Owner)]
    void BroadcastInputsForTickRpc(PlayerClientInputs inputs, long tick)
    {
        PlayerInputAtTick inputAtTick = new PlayerInputAtTick();
        inputAtTick.Inputs = inputs;
        inputAtTick.wasPredicted = false;

        // no rollback required
        if(CustomPhysics.Tick < tick)
        {
            _inputBuffer[tick] = inputAtTick;
            return;
        }

        // if the tick is in the past, we must rollback to said tick, 
        // if input at that tick does not match the current input in the buffer, 

        _inputBuffer[tick] = inputAtTick;
    }


    /// <summary>
    /// Should be called after a player input is received for a specific tick. Determines if we have received inputs from each player for that tick
    /// </summary>
    private void EvaluateIfWeCanAdvanceNextPhysicsTick()
    {
        foreach(PlayerDataSet player in PlayerDataManager.Singleton.PlayerData)
        {   
            bool hasInput = player.playerInputDriver._inputBuffer.ContainsKey(CustomPhysics.Tick);

            if (!hasInput)
            {
                Debug.Log($"No input at {CustomPhysics.Tick} for player {player.networkedPlayerHeader.PlayerIndex.Value}, will predict input until recieved");
                break;
            }
        }
    }


}
