using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Player : NetworkBehaviour
{



    [Header("Ground Checking")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Transform groundCheckPosition;
    [SerializeField] private Vector2 groundCheckSize;
    [SerializeField] private Vector2 wallCheckSize;

 
    [Header("Movement")]
    [SerializeField] private float moveSpeed;
    [SerializeField] private float jumpDisableBuffer; // minimum allowed time between jumps (prevents spamming)
    [SerializeField] private float jumpInputBuffer; // holds onto jump inputs for this amount of time, will then jump when grounded next
    [SerializeField] private float groundedBuffer; // holds onto being grounded for this amount of time, allows jumping in this period
    [SerializeField] private float jumpHeight;
    [SerializeField] private bool canMove = true;
    [SerializeField] private float takeHitKnockbackForce = 100;
    [SerializeField] private float velocityCeiling = 40;

    [SerializeField] private float maxSpeed = 8f;
    [SerializeField] private float groundAcceleration = 60f;
    [SerializeField] private float groundDeceleration = 80f;
    [SerializeField] private float airAcceleration = 30f;
    [SerializeField] private float airDeceleration = 20f;
    [SerializeField] private float maxSlopeAngle = 45f;
    [SerializeField] private float slopeStickForce = 50f;

    [Header("Ground Rays")]
    [SerializeField] private float groundRayLength = 0.2f;
    [SerializeField] private float groundRayInset = 0.02f; // pulls side rays slightly inward

    [Header("Visuals")]

    [SerializeField] private float inAirRotationSpeed = 1.0f;
    [SerializeField] private float verticalStretch = 1.0f;
    
    [SerializeField] private float horizontalCounterStretch = 0.5f;
    [SerializeField] private float stepRotation = 15.0f;
    [SerializeField] private float stepSpriteDuration = 0.2f;
    [SerializeField] private GameObject footstepPrefab;    
    [SerializeField] private SpriteRenderer playerRenderer;
    [SerializeField] private SpriteRenderer eyeRenderer;
    [SerializeField] private Animator animator;


    [SerializeField] private Transform _physicalPlayer; // the simulated body
    [SerializeField] private Transform _visualPlayer; // the player graphics
    [SerializeField] private Rigidbody2D _serverRigidBody;
    [SerializeField] private Rigidbody2D _visualRigidBody;


    enum PlayerSpriteJiggleAnimations
    {
        ANIM_IDLE,
        ANIM_STEP_LEFT,
        ANIM_STEP_RIGHT,
        ANIM_IN_AIR,

        ANIM_NUMBER_OF_ANIMATIONS
    }

    private static string[] _spriteJiggleAnimationMappings = { "Idle", "StepLeft", "StepRight", "InAir"};


    private ControllerInputHandler _inputHandler;
    private SpriteJiggleMultiState _animState;
    private NetworkVariable<PlayerSpriteJiggleAnimations> _ownerSpriteJiggleAnimatorState =

        new NetworkVariable<PlayerSpriteJiggleAnimations>(
            PlayerSpriteJiggleAnimations.ANIM_IDLE, 
            NetworkVariableReadPermission.Everyone, 
            NetworkVariableWritePermission.Owner);
    
    private NetworkVariable<int> _ownerDirectionFacing = 
        new NetworkVariable<int>(
            1, 
            NetworkVariableReadPermission.Everyone, 
            NetworkVariableWritePermission.Owner);

    private Color _originalColour;
    private NetworkPlayerHeader _playerHeader;
    public NetworkPlayerHeader PlayerHeader {
        get => _playerHeader;
    }
    
    // state
    private Vector2 _groundNormal = Vector2.up;
    private bool _shouldStickToSlope;
    private float _groundAngle;
    private float _groundedTracked;
    private float _jumpInputTracked;
    private float _jumpDisabledTracked;
    private bool _grounded;
    private bool _moving;
    private bool _trueGrounded;
    private int _directionFacing;
    private float _stepSpriteDurationTracked;
    private bool _isLeftFootForward;
    private float _inAirRotationFactor = 0;
    public int directionFacing => _directionFacing;

    // server state


    [System.Serializable]
    public class PlayerOwnerToServerInput
    {
        public float inputMoveX;
        public bool inputJump;
        public bool grounded;
        public int tick = 0;
    }


    [System.Serializable]
    struct ServerSnapshot : INetworkSerializable
    {
        public Vector2 position;
        public Vector2 velocity;
        public bool grounded;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer)
        where T : IReaderWriter
        {
            serializer.SerializeValue(ref position);
            serializer.SerializeValue(ref velocity);
            serializer.SerializeValue(ref grounded);
        }
    }



    private PlayerOwnerToServerInput _clientInputs;



    /// <summary>
    /// For owners syncing position with server snapshots...
    /// </summary>
    /// private Vector2 _reconcileTargetPos;
    [Header("Server Reconcile")]
     [SerializeField] private float reconcileDuration = 0.08f;
    [SerializeField] private float softError = 0.05f;
    [SerializeField] private float hardError = 0.5f;
    private Vector2 _reconcileTargetPos;
    private Vector2 _reconcileStartPos;
    private float _reconcileTimer;
    private bool _isReconciling;



    void Awake()
    {
        _clientInputs = new PlayerOwnerToServerInput();
        _playerHeader = GetComponentInParent<NetworkPlayerHeader>();
        _animState = GetComponent<SpriteJiggleMultiState>();
        _inputHandler = FindFirstObjectByType<ControllerInputHandler>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        ResetState();
    }


    public void ResetState()
    {
        _originalColour = playerRenderer.color;
        _directionFacing = 1;
        _jumpInputTracked = 0;
        _jumpDisabledTracked = 0;
        _groundedTracked = 0;
        _inAirRotationFactor = 0;
        _trueGrounded = false;
        _moving = false;


        if (IsOwner)
        {
            _visualRigidBody.linearVelocity = Vector2.zero;
            _visualRigidBody.gravityScale = GameStateManager.Singleton.enviromentalVariables.rigidBodyGravityScale;
        }

        if (IsServer)
        {
            _clientInputs.inputMoveX = 0;
            _clientInputs.grounded = false;
            _clientInputs.inputJump = false;
            _serverRigidBody.linearVelocity = Vector2.zero;
            _serverRigidBody.gravityScale = GameStateManager.Singleton.enviromentalVariables.rigidBodyGravityScale;
        }

        animator.SetBool("HasWon", false);
    }

    /// <summary>
    /// Enables or disables the player locally 
    /// </summary>
    /// <param name="doesExist">Does the player exist or not</param>
    public void ApplyExistsInWorldLocally(bool doesExist)
    {
        if (doesExist)
        {
            playerRenderer.enabled = true;
            eyeRenderer.enabled = true;
        }
        else
        {
            eyeRenderer.enabled = false;
            playerRenderer.enabled = false;
        }
        transform.localScale = new Vector3(1,1,1);
    }
    

    public void ReachEnd()
    {
        //PlayerDataManager.Singleton.PlayerData[_playerHeader.Index].hasWon = true;
        animator.SetBool("HasWon", true);

        //PlayerDataManager.Singleton.Callback_OnPlayerCompleteRound(_playerHeader.PlayerIndex.Value);
        
        //GetComponent<Collider2D>().enabled = false;

    }

    public void SetColour(Color color)
    {
        _originalColour = playerRenderer.color;
        playerRenderer.color = color;   
    }

    public void SpawnFootstepPrefab()
    {
        if (!_grounded || footstepPrefab == null)
        {
            return;
        }

        Instantiate(footstepPrefab, groundCheckPosition.position, Quaternion.identity);
    }

    public void Update()
    {
        if (!_playerHeader.Alive.Value || _playerHeader.HasWon.Value || !_playerHeader.PlayerExistsInWorld.Value)
        {
            return;
        }

        if (IsOwner)
        {
            HandleGrounding(); // both client and server do ground checks

            if (_clientInputs.inputJump && _grounded)
            {
                _jumpInputTracked = jumpInputBuffer;
            }

            if (_jumpInputTracked > 0)
            {
                _jumpInputTracked -= Time.deltaTime;
            }
            if (_jumpDisabledTracked > 0) {
                _jumpDisabledTracked -= Time.deltaTime;
            }

            OwnerDetectMovement();

        }
        else if (IsServer)
        {
            if (!IsOwner)
            {
                ReflectSpriteState(_clientInputs.inputMoveX, !_grounded);
            }
        }

    }


    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner | RpcInvokePermission.Server)]
    public void SetPositionRpc(Vector3 position)
    {
        _visualPlayer.position = position;
        _physicalPlayer.position = position;
    }

    public void HitTrap(Vector2 directionToApplyForce)
    {
        // allow owners to trigger there own death
        if (!IsOwner)
        {
            return;            
        }

        if (!_playerHeader.Alive.Value)
        {
            return;
        }

        _playerHeader.SetPlayerAliveRpc(false);

        //rb.AddForce(directionToApplyForce, ForceMode2D.Impulse);
        //PlayerDataManager.Singleton.Callback_OnPlayerDeath(_playerHeader.PlayerIndex.Value);

        OnPlayerDeath();
    }

    public void OnPlayerDeath()
    {
        //GetComponent<Collider2D>().enabled = false;
        AudioManager.Singleton.Play("Player_Death");
    }

    public void Jump(Rigidbody2D rb, bool playSound = true)
    {
        rb.linearVelocity = new Vector2(rb.linearVelocityX, jumpHeight);

        if (playSound)
        {
            AudioManager.Singleton.Play("Player_Jump");
        }
    }

    public void FixedUpdate()
    {
        if(IsOwner)
        {
            ClientPhysicsPredict();
        }

        if (IsServer)
        {
            //ServerDetermineIfShouldStickToSlope();
            //ServerHandleSlopeStick();
            ServerApplyPositionWrapAroundInLobby();
            ServerPhysicsCalculate();
            //ServerApplyMovement();
        
            ServerSnapshot s;
            s.grounded = _clientInputs.grounded;
            s.position = _physicalPlayer.transform.position;
            s.velocity = _serverRigidBody.linearVelocity;

            SendSnapshotOwnerRpc(s);

            if (!_playerHeader.Alive.Value || _playerHeader.HasWon.Value || !_playerHeader.PlayerExistsInWorld.Value)
            {
                //_serverRigidBody.linearVelocity = Vector3.zero;
                return;
            }
        }
        
        ReconcilePositionWithServer();
        
        
    }

    public void ReconcilePositionWithServer()
    {
        if (!_isReconciling)
        {
            return;
        }

        _reconcileTimer += Time.fixedDeltaTime;

        float t = _reconcileTimer / reconcileDuration;
        t = Mathf.Clamp01(t);

        // convert t value to smoothstep, this feels better than linear
        //t = t * t * (3f - 2f * t);

        Vector2 pos = Vector2.Lerp(
            _reconcileStartPos,
            _reconcileTargetPos,
            t
        );

        _visualRigidBody.MovePosition(pos);

        if (t >= 1f){
            _isReconciling = false;
        }
    }

    /// <summary>
    /// Predicts the players movement locally on the client, this is done to prevent latency from server physics updates
    /// </summary>
    public void ClientPhysicsPredict()
    {
       ApplyMovementPhysics(_visualRigidBody);
    }


    /// <summary>
    /// Simulates the players physics on the server, this is done on the server to allow the clients to interact with physics objects over the network.
    /// </summary>
    public void ServerPhysicsCalculate()
    {
        ApplyMovementPhysics(_serverRigidBody);
    }


    public void ApplyMovementPhysics(Rigidbody2D rb)
    {
        float targetSpeed = _clientInputs.inputMoveX * maxSpeed;
        float speedDiff = targetSpeed - rb.linearVelocity.x;

        float accelRate;
        accelRate = Mathf.Abs(targetSpeed) > 0.01f ? airAcceleration : airDeceleration;

        // if (_grounded)
        //     accelRate = Mathf.Abs(targetSpeed) > 0.01f ? groundAcceleration : groundDeceleration;
        // else
        //     accelRate = Mathf.Abs(targetSpeed) > 0.01f ? airAcceleration : airDeceleration;

        float movement = accelRate * speedDiff;

        rb.AddForce(new Vector2(Vector2.right.x * movement * Time.fixedDeltaTime,
                            -GameStateManager.Singleton.enviromentalVariables.rigidBodyGravityScale * Time.fixedDeltaTime));

        rb.linearVelocity = ClampVelocity(rb.linearVelocity);
    }


    [Rpc(SendTo.Owner, InvokePermission = RpcInvokePermission.Server)]
    void SendSnapshotOwnerRpc(ServerSnapshot snapshot)
    {
        OnServerSnapshot(snapshot);
    }

    /// <summary>
    /// Reconcile local player position as the player may have been predicting the position locally.
    /// </summary>
    /// <param name="snapshot">A snapshot of the actual physics state for this player</param>
    void OnServerSnapshot(ServerSnapshot s)
    {
        Vector2 error = new Vector2(s.position.x - _visualPlayer.position.x, s.position.y - _visualPlayer.position.y);

        if (error.magnitude < softError)
        {
            return;
        }
        // hard correction if huge
        if (error.magnitude > hardError)
        {
            _visualPlayer.transform.position = s.position;
            _visualRigidBody.linearVelocity = s.velocity;
            _isReconciling = false;
            return;
        }
        else if(error.magnitude > softError)
        {
            // reconcile smoothly
            _reconcileStartPos = _visualRigidBody.transform.position;
            _reconcileTargetPos = s.position;
            _visualRigidBody.linearVelocity = s.velocity;
            _reconcileTimer = 0f;
            _isReconciling = true;
        }
    }


    /// <summary>
    /// Clamps the provided velocity input and returns it
    /// </summary>
    /// <param name="velocity">input to clamp</param>
    /// <returns>velocity input clamped to (-velocityCeiling, velocityCeiling) parameters</returns>
    private Vector2 ClampVelocity(Vector2 velocity)
    {
        // Clamp horizontal speed
        velocity.x = Mathf.Clamp(velocity.x, -velocityCeiling, velocityCeiling);

        // Optional: clamp vertical too
        velocity.y = Mathf.Clamp(velocity.y, -velocityCeiling, velocityCeiling);

        return velocity;
    }

    private void ServerApplyPositionWrapAroundInLobby()
    {
        // we are not in lobby...
        if(GameStateManager.Singleton.NetworkedState.Value != GameStateManager.GameStateEnum.GameState_SelectingLevel)
        {
            return;
        }

        Bounds bounds = Camera.main.GetComponent<BoxCollider2D>().bounds;

        Vector3 pos = _physicalPlayer.transform.position;

        float padding = 0.1f; // small buffer so we don't jitter on the edge

        // Vertical wrap
        if (pos.y < bounds.min.y - padding)
        {
            pos.y = bounds.max.y + padding;
        }
        else if (pos.y > bounds.max.y + padding)
        {
            pos.y = bounds.min.y - padding;
        }

        // Horizontal wrap 
        if (pos.x < bounds.min.x - padding)
        {
            pos.x = bounds.max.x + padding;
        }
        else if (pos.x > bounds.max.x + padding)
        {
            pos.x = bounds.min.x - padding;
        }

        _serverRigidBody.position = pos;
    }


    void ReflectSpriteState(float moveX, bool inAir)
    {
        PlayerSpriteJiggleAnimations localAnimState = PlayerSpriteJiggleAnimations.ANIM_IDLE;

        if(inAir)
        {
            localAnimState = PlayerSpriteJiggleAnimations.ANIM_IN_AIR;
            _isLeftFootForward = false;
            _stepSpriteDurationTracked = 0;


            if(_visualRigidBody.linearVelocity.y > 0)
            {
                _inAirRotationFactor += inAirRotationSpeed * Time.deltaTime;
            }
            else
            {
                _inAirRotationFactor -= inAirRotationSpeed * Time.deltaTime;
            }

            _inAirRotationFactor = Mathf.Clamp(_inAirRotationFactor, -1, 1);
            playerRenderer.transform.rotation = Quaternion.Euler(0,0,_inAirRotationFactor * stepRotation * _ownerDirectionFacing.Value);

        }
        else
        {
            _inAirRotationFactor = 0;
            if(moveX != 0){

                _stepSpriteDurationTracked += Time.deltaTime;
                if(_stepSpriteDurationTracked > stepSpriteDuration)
                {
                    _stepSpriteDurationTracked = 0;
                    _isLeftFootForward = !_isLeftFootForward;

                    if (_isLeftFootForward)
                    {
                        AudioManager.Singleton.Play("Player_Step", Vector3.zero, 1, 0.9f);
                    }   
                    else
                    {
                        AudioManager.Singleton.Play("Player_Step");
                    }

                    SpawnFootstepPrefab();
                }

                if (_isLeftFootForward)
                {
                    playerRenderer.transform.rotation = Quaternion.Euler(0,0,stepRotation * _ownerDirectionFacing.Value);
                    localAnimState = PlayerSpriteJiggleAnimations.ANIM_STEP_LEFT;
                }
                else
                {
                    playerRenderer.transform.rotation = Quaternion.Euler(0,0,-stepRotation * _ownerDirectionFacing.Value);
                    localAnimState = PlayerSpriteJiggleAnimations.ANIM_STEP_RIGHT;
                }
            }
            else
            {
                playerRenderer.transform.rotation = Quaternion.Euler(0,0,0);
                _stepSpriteDurationTracked = 0;
                localAnimState = PlayerSpriteJiggleAnimations.ANIM_IDLE;
            }
        }

        _animState.SetState(_spriteJiggleAnimationMappings[(int)localAnimState]);
        playerRenderer.transform.localScale = new Vector3((1 - Mathf.Abs(horizontalCounterStretch * _visualRigidBody.linearVelocityY)) * _ownerDirectionFacing.Value, 1 + Mathf.Abs(verticalStretch * _visualRigidBody.linearVelocityY), 1);
    }


    void OwnerDetectMovement()
    {
        float moveX = 0;
        if(_inputHandler.Input.mouseCursorVelocity.x > 0.1f)
        {
            moveX = 1;
        }
        else if(_inputHandler.Input.mouseCursorVelocity.x < -0.1f)
        {
            moveX = -1;
        }
        if (!canMove)
        {
            moveX = 0;
        }

        // Facing sprite & moving state
        _moving = moveX != 0;
        if (_moving)
        {
            _ownerDirectionFacing.Value = moveX > 0 ? 1 : -1;
        }

        bool shouldJump = false;

        if (_inputHandler.Input.mainButtonIsPressed)
        {
            _jumpInputTracked = jumpInputBuffer;
        }
        if (_jumpInputTracked > 0)
        {
            _jumpInputTracked -= Time.deltaTime;
        }
        if (_jumpDisabledTracked > 0) {
            _jumpDisabledTracked -= Time.deltaTime;
        }

        if(_grounded && _jumpInputTracked > 0 && _jumpDisabledTracked <= 0){
            shouldJump = true;
            _jumpInputTracked = 0;
            _jumpDisabledTracked = jumpDisableBuffer;
            Jump(_visualRigidBody);
        }

        ReflectSpriteState(moveX, !_grounded || (_jumpDisabledTracked > 0));
        SetInputRpc(moveX, shouldJump, _grounded, _clientInputs.tick);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    void SetInputRpc(float moveX, bool jump, bool grounded, int tick)
    {
        _clientInputs.inputMoveX = moveX;
        _clientInputs.inputJump = jump;
        _clientInputs.grounded = grounded;
        _clientInputs.tick = tick;

        if (jump)
        {
            // if isOwner, no need to play sound again
            Jump(_serverRigidBody, !IsOwner);
        }
    }

    void HandleGrounding()
    {
        Bounds bounds = _visualRigidBody.GetComponent<Collider2D>().bounds;

        Vector2 center = new Vector2(bounds.center.x, bounds.min.y);
        Vector2 left   = new Vector2(bounds.min.x + groundRayInset, bounds.min.y);
        Vector2 right  = new Vector2(bounds.max.x - groundRayInset, bounds.min.y);

        RaycastHit2D hitCenter = Physics2D.Raycast(center, Vector2.down, groundRayLength, groundLayer);
        RaycastHit2D hitLeft   = Physics2D.Raycast(left,   Vector2.down, groundRayLength, groundLayer);
        RaycastHit2D hitRight  = Physics2D.Raycast(right,  Vector2.down, groundRayLength, groundLayer);

        RaycastHit2D bestHit = default;
        bool hasHit = false;

        // Prefer center hit
        if (hitCenter.collider != null)
        {
            bestHit = hitCenter;
            hasHit = true;
        }

        if (hitLeft.collider != null && (!hasHit || hitLeft.normal.y > bestHit.normal.y)){
            bestHit = hitLeft;
            hasHit = true;
        }

        if (hitRight.collider != null && (!hasHit || hitRight.normal.y > bestHit.normal.y)){
            bestHit = hitRight;
            hasHit = true;
        }

        _trueGrounded = hasHit;

        if (_trueGrounded)
        {
            _groundNormal = bestHit.normal;
            _groundAngle = Vector2.Angle(_groundNormal, Vector2.up);
            _groundedTracked = groundedBuffer;
        }

        _grounded = false;
        if (_groundedTracked > 0)
        {
            _grounded = true;
            _groundedTracked -= Time.deltaTime;
        }
    }

    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        Bounds bounds = _physicalPlayer.GetComponent<Collider2D>().bounds;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(
            new Vector2(bounds.center.x, bounds.min.y),
            new Vector2(bounds.center.x, bounds.min.y - groundRayLength)
        );

        Gizmos.DrawLine(
            new Vector2(bounds.min.x + groundRayInset, bounds.min.y),
            new Vector2(bounds.min.x + groundRayInset, bounds.min.y - groundRayLength)
        );

        Gizmos.DrawLine(
            new Vector2(bounds.max.x - groundRayInset, bounds.min.y),
            new Vector2(bounds.max.x - groundRayInset, bounds.min.y - groundRayLength)
        );
    }


}
