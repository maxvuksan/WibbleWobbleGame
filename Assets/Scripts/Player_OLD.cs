using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Player_OLD : NetworkBehaviour
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
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Animator animator;



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
    }

    [System.Serializable]
    public class ServerToClientVerification
    {
        public Vector2 serverVelocity;
    }

    private Vector2 _predictedVelocity;
    private Vector2 _predictedPosition;



    private PlayerOwnerToServerInput _clientInputs;



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
        _predictedVelocity = Vector2.zero;
        _predictedPosition = Vector2.zero;
        _originalColour = playerRenderer.color;
        _directionFacing = 1;
        _jumpInputTracked = 0;
        _jumpDisabledTracked = 0;
        _groundedTracked = 0;
        _inAirRotationFactor = 0;
        _trueGrounded = false;
        _moving = false;

        if (IsServer)
        {
            _clientInputs.inputMoveX = 0;
            _clientInputs.grounded = false;
            _clientInputs.inputJump = false;
            rb.linearVelocity = Vector2.zero;
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

        if(IsServer)
        {
            HandleGrounding();

            if (_clientInputs.inputJump)
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

            if(_jumpInputTracked > 0 && _jumpDisabledTracked <= 0){
                ServerJump();
            }
        }

        if (IsOwner)
        {
            HandleGrounding();

            HandleGrounding(); // both client and server do ground checks
            OwnerHandleSpriteState();
            OwnerDetectMovement();
        }

        ReflectSpriteState();
    }


    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner | RpcInvokePermission.Server)]
    public void SetPositionRpc(Vector3 position)
    {
        transform.position = position;
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

        rb.AddForce(directionToApplyForce, ForceMode2D.Impulse);
        //PlayerDataManager.Singleton.Callback_OnPlayerDeath(_playerHeader.PlayerIndex.Value);

        OnPlayerDeath();
    }

    public void OnPlayerDeath()
    {
        GetComponent<Collider2D>().enabled = false;
        AudioManager.Singleton.Play("Player_Death");
    }

    public void ServerJump()
    {
        // check both server grounding and owner grounding
        if (!_grounded && !_clientInputs.grounded)
        {
            return;
        }

        _jumpInputTracked = 0;
        _jumpDisabledTracked = jumpDisableBuffer;

        if (!canMove)
        {
            return;
        }

        rb.linearVelocity = new Vector2(rb.linearVelocityX, jumpHeight);

        AudioManager.Singleton.Play("Player_Jump");
    }

    public void FixedUpdate()
    {
        if (IsServer)
        {
            ServerClampVelocity();
            ServerDetermineIfShouldStickToSlope();
            ServerHandleSlopeStick();
            ServerApplyPositionWrapAroundInLobby();
            //ServerApplyMovement();
        
            if (!_playerHeader.Alive.Value || _playerHeader.HasWon.Value || !_playerHeader.PlayerExistsInWorld.Value)
            {
                rb.linearVelocity = Vector3.zero;
                return;
            }
        }
        else
        {
            ClientPhysicsPredict();
        }
        
    }

    /// <summary>
    /// Predicts the players movement locally on the client, this is done to prevent latency from server physics updates
    /// </summary>
    public void ClientPhysicsPredict()
    {
        float targetSpeed = _clientInputs.inputMoveX * maxSpeed;
        float accel = _grounded ? groundAcceleration : airAcceleration;

        _predictedVelocity.x = Mathf.MoveTowards(
            _predictedVelocity.x,
            targetSpeed,
            accel * Helpers.Singleton.networkPhysicsTickRate
        );

        _predictedVelocity.y += 1 * Helpers.Singleton.networkPhysicsTickRate;
        _predictedPosition += _predictedVelocity * Helpers.Singleton.networkPhysicsTickRate;
    }


    /// <summary>
    /// Simulates the players physics on the server, this is done on the server to allow the clients to interact with physics objects over the network.
    /// </summary>
    public void ServerPhysicsCalculate()
    {
        float targetSpeed = _clientInputs.inputMoveX * maxSpeed;
        float speedDiff = targetSpeed - rb.linearVelocity.x;

        float accelRate;
        if (_grounded)
            accelRate = Mathf.Abs(targetSpeed) > 0.01f ? groundAcceleration : groundDeceleration;
        else
            accelRate = Mathf.Abs(targetSpeed) > 0.01f ? airAcceleration : airDeceleration;

        float movement = accelRate * speedDiff;

        rb.AddForce(Vector2.right * movement * Time.fixedDeltaTime);
    }



    private void ServerClampVelocity()
    {
        Vector2 v = rb.linearVelocity;

        // Clamp horizontal speed
        v.x = Mathf.Clamp(v.x, -velocityCeiling, velocityCeiling);

        // Optional: clamp vertical too
        v.y = Mathf.Clamp(v.y, -velocityCeiling, velocityCeiling);

        rb.linearVelocity = v; 
    }

    private void ServerApplyPositionWrapAroundInLobby()
    {
        // we are not in lobby...
        if(GameStateManager.Singleton.NetworkedState.Value != GameStateManager.GameStateEnum.GameState_SelectingLevel)
        {
            return;
        }

        Bounds bounds = Camera.main.GetComponent<BoxCollider2D>().bounds;

        Vector3 pos = transform.position;

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

        transform.position = pos;
    }


    void ReflectSpriteState()
    {
        _animState.SetState(_spriteJiggleAnimationMappings[(int)_ownerSpriteJiggleAnimatorState.Value]);
        transform.localScale = new Vector3(_ownerDirectionFacing.Value, 1, 1);
        playerRenderer.transform.localScale = new Vector3(1 - Mathf.Abs(horizontalCounterStretch * rb.linearVelocityY), 1 + Mathf.Abs(verticalStretch * rb.linearVelocityY), 1);
    }

    void OwnerHandleSpriteState()
    {
        // scale by velocity

        if (!_grounded || _jumpDisabledTracked > 0)
        {
            _ownerSpriteJiggleAnimatorState.Value = PlayerSpriteJiggleAnimations.ANIM_IN_AIR;
            _isLeftFootForward = false;
            _stepSpriteDurationTracked = 0;


            if(rb.linearVelocity.y > 0)
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
            if(_clientInputs.inputMoveX != 0){

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
                    _ownerSpriteJiggleAnimatorState.Value = PlayerSpriteJiggleAnimations.ANIM_STEP_LEFT;
                }
                else
                {
                    playerRenderer.transform.rotation = Quaternion.Euler(0,0,-stepRotation * _ownerDirectionFacing.Value);
                    _ownerSpriteJiggleAnimatorState.Value = PlayerSpriteJiggleAnimations.ANIM_STEP_RIGHT;
                }
            }
            else
            {
                playerRenderer.transform.rotation = Quaternion.Euler(0,0,0);
                _stepSpriteDurationTracked = 0;
                _ownerSpriteJiggleAnimatorState.Value = PlayerSpriteJiggleAnimations.ANIM_IDLE;
            }

        }
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

        if(_jumpInputTracked > 0 && _jumpDisabledTracked <= 0){
            shouldJump = true;
        }

        SetInputRpc(moveX, shouldJump, _grounded);
    }

    // void ServerApplyMovement()
    // {
    //     float targetSpeed = _playerToServerInputs.inputMoveX * maxSpeed;
    //     float speedDiff = targetSpeed - rb.linearVelocity.x;

    //     float accelRate;
    //     if (_grounded)
    //         accelRate = Mathf.Abs(targetSpeed) > 0.01f ? groundAcceleration : groundDeceleration;
    //     else
    //         accelRate = Mathf.Abs(targetSpeed) > 0.01f ? airAcceleration : airDeceleration;

    //     float movement = accelRate * speedDiff;

    //     Vector2 surfaceVector = Vector2.right;
    //     if (_shouldStickToSlope)
    //     {
    //         surfaceVector = Vector2.Perpendicular(_groundNormal).normalized;
    //     }

    //     surfaceVector = Vector2.right;

    //     rb.AddForce(surfaceVector.normalized * movement * Time.fixedDeltaTime);
    // }


    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Everyone)]
    void SetInputRpc(float moveX, bool jump, bool grounded)
    {
        _clientInputs.inputMoveX = moveX;
        _clientInputs.inputJump = jump;
        _clientInputs.grounded = grounded;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    void RigidbodySetVelocityRpc(Vector2 velocity)
    {
        rb.linearVelocity = velocity;
    }
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    void RigidbodySetXVelocityRpc(float x)
    {
        rb.linearVelocity = new Vector2(x, rb.linearVelocity.y);
    }
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    void RigidbodySetYVelocityRpc(float y)
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, y);
    }


    void HandleGrounding()
    {
        Bounds bounds = GetComponent<Collider2D>().bounds;

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

    void ServerDetermineIfShouldStickToSlope()
    {
        _shouldStickToSlope = false;

        if (!_grounded)
            return;

        if (_groundAngle > maxSlopeAngle)
            return;

        // Don't stick if player is trying to move
        if (_moving)
            return;

        // Don't stick during or right after jump
        if (_jumpDisabledTracked > 0)
            return;

        // Don't stick if moving upward (jumping / knockback)
        if (rb.linearVelocity.y > 0.05f)
            return;

        // If player is not trying to move, stick to slope
        if (!_moving)
        {
            _shouldStickToSlope = true;
        }
    }
    public void ServerHandleSlopeStick()
    {
        if (_shouldStickToSlope)
        {
            // TO DO: dynamically changing the gravity is probably not a good idea
            Vector2 slopeDirection = Vector2.Perpendicular(_groundNormal).normalized;
            //rb.gravityScale = 0;
        }
        else
        {
            rb.gravityScale = GameStateManager.Singleton.enviromentalVariables.rigidBodyGravityScale;
        }

    }

    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        Bounds bounds = GetComponent<Collider2D>().bounds;

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
