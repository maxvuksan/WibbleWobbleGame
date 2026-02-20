using System.Collections.Generic;
using Steamworks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;


// A snapshot of the players input at a specific physics tick
public class PlayerInputAtTick
{
    public PlayerClientInputs Inputs;
    public long Tick;
    public bool WasPredicted; // was the input actually recieved, or was it predicted

    public PlayerInputAtTick()
    {
        Inputs = new();
        Tick = 0;
        WasPredicted = false;
    }

    public PlayerInputAtTick(PlayerInputAtTick other)
    {
        Inputs = other.Inputs;
        Tick = other.Tick;
        WasPredicted = other.WasPredicted;
    }
}


/// <summary>
/// Is responsible for controller a player instance
/// </summary>
public class PlayerInputDriver : NetworkBehaviour
{

    public static readonly int InputHistoryLength = CustomPhysics.HistoryLength;

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
    private PlayerClientInputs _playerInputs = new();


    [SerializeField] private NetworkPlayerHeader _playerHeader;

    /// <summary>
    /// We fill the first ticks of the input buffer with empty inputs, to account for the drivers input latency (InputBufferTickDelay)
    /// </summary>
    private bool _hasPrefilledBuffer;
    
    // TODO: When actually in game, it may be wise to dynamically increase this input ring buffer so that 
    private List<PlayerInputAtTick> _inputHistoryRingBuffer = new();

    private ControllerInputHandler _input;
    

    void Awake()
    {   
        // resize the ring buffer to match the history length
        _inputHistoryRingBuffer = new List<PlayerInputAtTick>();
        for(int i = 0; i < InputHistoryLength; i++)
        {
            _inputHistoryRingBuffer.Add(null);
        }

        _input = FindFirstObjectByType<ControllerInputHandler>();

        CustomPhysics.OnPrePhysicsTick += OnPrePhysicsTick;

        _hasPrefilledBuffer = true;
        _playerInputs = new();
    }

    public override void OnDestroy()
    {
        base.OnDestroy();

        CustomPhysics.OnPrePhysicsTick -= OnPrePhysicsTick;
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
                defaultInput.WasPredicted = true;
                defaultInput.Tick = tick;

                RecordInputToHistory(defaultInput);

                BroadcastInputsForTickRpc(defaultInput.Inputs, tick);
            }
        }
    }

    /// <summary>
    /// Adds an input to the history ring buffer, the position in the buffer is relative to how far the provided tick value is from the current tick 
    /// </summary>
    public void RecordInputToHistory(PlayerInputAtTick inputs) 
    { 
        long tickDifference = CustomPhysics.Tick - inputs.Tick; 
        
        if(Mathf.Abs(tickDifference) > InputHistoryLength) { 
            Debug.LogError("trying to call RecordInputToHistory() but the provided tick is too far in the past or future"); 
            return; 
        } 
        
        long bufferIndex = CustomPhysics.Tick - tickDifference; 
        bufferIndex %= InputHistoryLength; 
        
        _inputHistoryRingBuffer[(int)bufferIndex] = inputs; 
    }

    public void Rollback(long previousTick)
    {
        long tickDifference = CustomPhysics.Tick - previousTick;

        if(Mathf.Abs(tickDifference) > InputHistoryLength)
        {
            Debug.LogError("trying to call RecordInputToHistory() but the provided tick is too far in the past or future");
            return;
        }

        CustomPhysics.Rollback(previousTick);
    }

    /// <summary>
    /// For the current tick, returns the buffer index
    /// </summary>
    public int GetCurrentBufferIndex(long offset = 0)
    {
        return (int)(CustomPhysics.Tick + offset) % InputHistoryLength;
    }

    void OnPrePhysicsTick()
    {   
        if (!CustomPhysics.Resimulating && IsOwner)
        {
            // store current input in buffer, offset by out input delay

            long tickOffset = InputBufferTickDelay + 1;
            long tick = tickOffset + CustomPhysics.Tick;

            PlayerInputAtTick inputAtTick = new();
            inputAtTick.Tick = tick;
            inputAtTick.WasPredicted = false;
            inputAtTick.Inputs = CalculatePlayerInput(); 

            RecordInputToHistory(inputAtTick);

            BroadcastInputsForTickRpc(_inputHistoryRingBuffer[GetCurrentBufferIndex(tickOffset)].Inputs, tick);
        }

        AssignPlayerInputs();
    }

    private void AssignPlayerInputs()
    {
        PlayerClientInputs inputs = new();
        
        if(_inputHistoryRingBuffer[GetCurrentBufferIndex()]?.Tick == CustomPhysics.Tick)
        {
            inputs = _inputHistoryRingBuffer[GetCurrentBufferIndex()].Inputs;
        }
        else
        {
            inputs = PredictInputForTick(CustomPhysics.Tick);
        }

        _playerInputs.InputJump = inputs.InputJump;
        _playerInputs.InputMoveDirection = inputs.InputMoveDirection;
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
    [Rpc(SendTo.NotOwner, InvokePermission = RpcInvokePermission.Owner)]
    void BroadcastInputsForTickRpc(PlayerClientInputs inputs, long tick)
    {
        PlayerInputAtTick inputAtTick = new();
        inputAtTick.Inputs = inputs;
        inputAtTick.WasPredicted = false;
        inputAtTick.Tick = tick; 

        // no rollback required

        RecordInputToHistory(inputAtTick);
        
        long currentTick = CustomPhysics.Tick;

        if(currentTick > tick)
        {
            Rollback(tick);
            CustomPhysics.SimulateFuture(currentTick);
        }
    }
}
