using System.Collections.Generic;
using Steamworks;
using Unity.Netcode;
using Unity.VisualScripting;
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
    public const int InputBufferTickDelay = 3;//3; //2; TODO: This should not be set to zero for multiplayer, we need some delay because infomation over the network is not instant

    /// <summary>
    /// Encapsulates the players input (is the player pressing move or jump?)
    /// </summary>
    public PlayerClientInputs PlayerInputs {
        get => _playerInputs;
        private set => _playerInputs = value;
    }
    private PlayerClientInputs _playerInputs = new();

    [SerializeField] private NetworkPlayerHeader _playerHeader;

    // TODO: When actually in game, it may be wise to dynamically increase this input ring buffer so that 
    private List<PlayerInputAtTick> _inputHistoryRingBuffer = new();

    private ControllerInputHandler _input;

    private double _clockOffset = 0;
    

    void Awake()
    {   
        _input = FindFirstObjectByType<ControllerInputHandler>();

        CustomPhysics.OnPrePhysicsTick += OnPrePhysicsTick;
        CustomPhysics.OnTurnOffPhysicsSimulation += OnTurnOffPhysicsSimulation;

        _playerInputs = new();

        OnTurnOffPhysicsSimulation();
    }


    public override void OnDestroy()
    {
        base.OnDestroy();

        CustomPhysics.OnPrePhysicsTick -= OnPrePhysicsTick;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
    }

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.U))
        {
            CustomPhysics.BeginRollbackDebug();
            RequestRollbackAndResimulate(1);
        }
    }


    public void SyncClockThenStartPhysics()
    {
        if (IsServer)
        {
            // Host has no offset
            _clockOffset = 0;
            // wait for ping pong to start simulation... MAY NEED REFACTOR FOR MULTIPLE CLIENTS

            if(NetworkManager.ConnectedClients.Count == 1)
            {
                ServerBeginPhysicsSimulation(0.0d);
                print("starting physics");
            }
            else
            {
                RequestClientsToSyncRpc();
            }
        }
    }

    void ServerBeginPhysicsSimulation(double startDelay = 2.0d)
    {
        // tell other clicks when this t
        double startTime = Time.realtimeSinceStartupAsDouble + startDelay; //start delay so the message has time to travel
        ScheduleSimulationRpc(startTime);   
    }

    [Rpc(SendTo.NotServer, InvokePermission = RpcInvokePermission.Server)]
    void RequestClientsToSyncRpc()
    {
        PingServerRpc(Time.realtimeSinceStartupAsDouble);
    }

    /// <summary>
    /// Ensures nobody ticks before the everyone has loaded in
    /// </summary>
    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
    void ScheduleSimulationRpc(double serverStartTime)
    {
        double localStartTime = serverStartTime - _clockOffset;
        CustomPhysics.ScheduleStart(localStartTime);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    void PingServerRpc(double clientSendTime)
    {
        PongClientRpc(clientSendTime, Time.realtimeSinceStartupAsDouble);
        ServerBeginPhysicsSimulation();
    }

    [Rpc(SendTo.NotServer)]
    void PongClientRpc(double clientSendTime, double serverReceiveTime)
    {
        double clientReceiveTime = Time.realtimeSinceStartupAsDouble;
        double rtt = clientReceiveTime - clientSendTime;
        double oneWayLatency = rtt / 2.0;

        // What is the server's clock right now, from my perspective
        double estimatedServerNow = serverReceiveTime + oneWayLatency;
        _clockOffset = estimatedServerNow - clientReceiveTime;
    }


    // public override void OnNetworkSpawn()
    // {
    //     base.OnNetworkSpawn();
        
    //     if (IsOwner)
    //     { 
    //         Debug.Log($"Player {_playerHeader.PlayerIndex} pre-filling buffer from tick 0 to {InputBufferTickDelay}");
            
    //         // Pre-fill buffer with default inputs so simulation can start
    //         for (int i = 0; i <= InputBufferTickDelay; i++)
    //         {
    //             long tick = i;
    //             PlayerInputAtTick defaultInput = new();
    //             defaultInput.WasPredicted = true;
    //             defaultInput.Tick = tick;

    //             RecordInputToHistory(defaultInput);

    //             BroadcastInputsForTickRpc(defaultInput.Inputs, tick);
    //         }
    //     }
    // }


    public int GetHistoryBufferIndex(long tick)
    {
        return (int)((tick % InputHistoryLength) + InputHistoryLength) % InputHistoryLength;
    }

    /// <summary>
    /// Adds an input to the history ring buffer, the position in the buffer is relative to how far the provided tick value is from the current tick 
    /// </summary>
    public void RecordInputToHistory(PlayerInputAtTick inputs) 
    { 
        long tickDifference = CustomPhysics.Tick - inputs.Tick; 
        
        if(tickDifference > InputHistoryLength || tickDifference < -InputHistoryLength) { 
            Debug.LogError("trying to call RecordInputToHistory() but the provided tick is too far in the past or future"); 
            return; 
        } 
        
        long bufferIndex = GetHistoryBufferIndex(inputs.Tick);
        
        _inputHistoryRingBuffer[(int)bufferIndex] = inputs; 
    }

    public void RequestRollbackAndResimulate(long previousTick)
    {
        long tickDifference = CustomPhysics.Tick - previousTick;

        if(Mathf.Abs(tickDifference) > InputHistoryLength)
        {
            Debug.LogError("trying to call RecordInputToHistory() but the provided tick is too far in the past or future");
            return;
        }

        CustomPhysics.RequestRollbackAndResimulate(previousTick);
    }

    /// <summary>
    /// For the current tick, returns the buffer index
    /// </summary>
    public int GetCurrentBufferIndex(long offset = 0)
    {
        long tick = CustomPhysics.Tick + offset;
        return GetHistoryBufferIndex(tick);
    }

    void ClearHistoryRingBuffer()
    {
        _inputHistoryRingBuffer = new List<PlayerInputAtTick>();
        for(int i = 0; i < InputHistoryLength; i++)
        {
            _inputHistoryRingBuffer.Add(null);
        }
    }

    void OnTurnOffPhysicsSimulation()
    {
        // clear input ring buffer
        ClearHistoryRingBuffer();
        _playerInputs.InputJump = PlayerJumpInput.None;
        _playerInputs.InputMoveDirection = PlayerMoveInput.None;
    }

    void OnPrePhysicsTick()
    {   
        if (!CustomPhysics.SimulateFutureAtRegularTickRate && !CustomPhysics.Resimulating && IsOwner)
        {
            // store current input in buffer, offset by out input delay

            long tickOffset = InputBufferTickDelay;
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

        // We will use a stored input, which is NOT predicted        
        if(_inputHistoryRingBuffer[GetCurrentBufferIndex()]?.Tick == CustomPhysics.Tick
        && !_inputHistoryRingBuffer[GetCurrentBufferIndex()].WasPredicted)
        {
            inputs = new PlayerClientInputs(_inputHistoryRingBuffer[GetCurrentBufferIndex()].Inputs);
        }
        else
        {
            inputs = PredictInputForTick();
            
            // Store the prediction...

            PlayerInputAtTick predicted = new();
            predicted.Inputs = inputs;
            predicted.Tick = CustomPhysics.Tick;
            predicted.WasPredicted = true; 

            RecordInputToHistory(predicted);
        }

        _playerInputs.InputJump = inputs.InputJump;
        _playerInputs.InputMoveDirection = inputs.InputMoveDirection;
    }

    /// <summary>
    /// Predict input for CustomPhysics.Tick using the previous tick as reference
    /// </summary>
    /// <returns></returns>
    private PlayerClientInputs PredictInputForTick()
    {
        PlayerClientInputs inputs = new();

        // get the previous tick if it exists
        if(_inputHistoryRingBuffer[GetCurrentBufferIndex(-1)]?.Tick == CustomPhysics.Tick - 1)
        {
            inputs = new PlayerClientInputs(_inputHistoryRingBuffer[GetCurrentBufferIndex(-1)].Inputs);
        }

        // we assume jump is a momentary input, only lasts 1 tick 
        inputs.InputJump = PlayerJumpInput.None;

        return inputs;
    }

    /// <summary>
    /// Translates inputs from the ControllerInputHandler to the appropriate data structure for driving the player, returns said structure
    /// </summary>
    private PlayerClientInputs CalculatePlayerInput()
    {
        PlayerClientInputs playerInput = new();

        // Jump
        playerInput.InputJump = PlayerJumpInput.None;
        if (_input.Input.jumpButtonIsPressed)
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
    /// TODO: Rpc should be set to NotOwner, set to everyone for everyone
    /// 
    [Rpc(SendTo.NotOwner, InvokePermission = RpcInvokePermission.Owner)]
    void BroadcastInputsForTickRpc(PlayerClientInputs inputs, long tick)
    {
        RecieveBroadcastInputsForTick(inputs, tick);
    }

    private void RecieveBroadcastInputsForTick(PlayerClientInputs inputs, long tick, bool forceRollbackForTesting = false)
    {
        PlayerInputAtTick inputAtTick = new();
        inputAtTick.Inputs = inputs;
        inputAtTick.WasPredicted = false;
        inputAtTick.Tick = tick; 
    
        if(CustomPhysics.Tick > tick)
        {
            int bufferIndex = GetHistoryBufferIndex(tick);
            PlayerInputAtTick predicted = _inputHistoryRingBuffer[bufferIndex];
            
            // Only rollback if we actually HAD a prediction that was wrong
            bool predictionWrong = predicted != null 
                && predicted.Tick == tick
                && predicted.WasPredicted  // ← Only if it was actually predicted
                && (predicted.Inputs.InputMoveDirection != inputs.InputMoveDirection
                    || predicted.Inputs.InputJump != inputs.InputJump)
                || forceRollbackForTesting;

            RecordInputToHistory(inputAtTick);

            if(inputs.InputJump == PlayerJumpInput.JumpPressed)
            {
                string predictedInfo = predicted == null 
                    ? "predicted=NULL" 
                    : $"predicted.Tick={predicted.Tick}, predicted.Jump={predicted.Inputs.InputJump}, predicted.Move={predicted.Inputs.InputMoveDirection}";

                Debug.Log($"Jump Pressed | tick={tick} currentTick={CustomPhysics.Tick} predictionWrong={predictionWrong} | actual.Jump={inputs.InputJump} actual.Move={inputs.InputMoveDirection} | {predictedInfo}");
            }

            if (predictionWrong)
            {
                RequestRollbackAndResimulate(tick - 1);
            }
        }
        else
        {
            RecordInputToHistory(inputAtTick);
        }
    }
}
