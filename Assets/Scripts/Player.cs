using System;
using System.Collections;
using System.Collections.Generic;
using FixMath.NET;
using Unity.Netcode;
using UnityEngine;
using Volatile;

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

    [SerializeField] private CustomPhysicsBody _physicsBody;
    [SerializeField] private PlayerInputDriver _driver;


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
    

    private Color _originalColour;

    public NetworkPlayerHeader PlayerHeader {
        get => _playerHeader;
    }
    private NetworkPlayerHeader _playerHeader;    

    // state
    private Vector2 _groundNormal = Vector2.up;
    private Fix64 _groundedTracked;
    private double _jumpInputTracked;
    private double _jumpDisabledTracked;
    private bool _grounded;
    private bool _trueGrounded;
    private float _stepSpriteDurationTracked;
    private bool _isLeftFootForward;
    private float _inAirRotationFactor = 0;
    private int _directionFacing = 1;

    void Awake()
    {
        _playerHeader = GetComponentInParent<NetworkPlayerHeader>();
        _animState = GetComponent<SpriteJiggleMultiState>();
        _inputHandler = FindFirstObjectByType<ControllerInputHandler>();
        _physicsBody = GetComponent<CustomPhysicsBody>();
    }

    void Start()
    {
        CustomPhysics.OnPhysicsTick += OnPhysicsTick;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        CustomPhysics.OnPhysicsTick -= OnPhysicsTick;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        ResetState();
    }


    public void ResetState()
    {
        _originalColour = playerRenderer.color;
        _jumpInputTracked = 0;
        _jumpDisabledTracked = 0;
        _inAirRotationFactor = 0;
        _trueGrounded = false;
        _directionFacing = 1;

        animator.SetBool("HasWon", false);

        _physicsBody.LinearVelocity = VoltVector2.zero;

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

        int moveDirection = _driver.PlayerInputs.GetMoveDirection();
        if(moveDirection != 0)
        {
            _directionFacing = moveDirection;
        }
        
        ReflectSpriteState();
    }


    [Rpc(SendTo.Owner, InvokePermission = RpcInvokePermission.Owner | RpcInvokePermission.Server)]
    public void SetPositionRpc(Vector3 position)
    {
        
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

    public void Jump(bool playSound = true)
    {
        _physicsBody.LinearVelocity = new VoltVector2(_physicsBody.LinearVelocityX, (Fix64)jumpHeight);

        if (playSound)
        {
            //AudioManager.Singleton.Play("Player_Jump");
        }
    }

    public void OnPhysicsTick()
    {
        if (_driver.PlayerInputs.InputJump == PlayerJumpInput.JumpPressed)
        {
            _jumpInputTracked = jumpInputBuffer;
        }

        if (_jumpInputTracked > 0)
        {
            // TODO: Casting to double here may be problematic?
            _jumpInputTracked -= (double)CustomPhysics.TimeBetweenTicks;
        }
        if (_jumpDisabledTracked > 0) {
            _jumpDisabledTracked -= (double)CustomPhysics.TimeBetweenTicks;
        }

        if(_jumpInputTracked > 0)
        {
            Jump(true);
            _jumpInputTracked = 0;
        }

        ApplyPositionWrapAroundInLobby();
        ApplyMovementPhysics();
    }


    public void ApplyMovementPhysics()
    {
        float targetSpeed = _driver.PlayerInputs.GetMoveDirection() * maxSpeed;

        _physicsBody.LinearVelocityX = (Fix64)targetSpeed;
    }


    private void ApplyPositionWrapAroundInLobby()
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
        PlayerSpriteJiggleAnimations localAnimState = PlayerSpriteJiggleAnimations.ANIM_IDLE;

        if(_grounded)
        {
            localAnimState = PlayerSpriteJiggleAnimations.ANIM_IN_AIR;
            _isLeftFootForward = false;
            _stepSpriteDurationTracked = 0;


            if((float)_physicsBody.LinearVelocityY > 0)
            {
                _inAirRotationFactor += inAirRotationSpeed * Time.deltaTime;
            }
            else
            {
                _inAirRotationFactor -= inAirRotationSpeed * Time.deltaTime;
            }

            _inAirRotationFactor = Mathf.Clamp(_inAirRotationFactor, -1, 1);
            playerRenderer.transform.rotation = Quaternion.Euler(0,0,_inAirRotationFactor * stepRotation * _directionFacing);

        }
        else
        {
            _inAirRotationFactor = 0;
            if(_driver.PlayerInputs.InputMoveDirection != PlayerMoveInput.None){

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
                    playerRenderer.transform.rotation = Quaternion.Euler(0,0,stepRotation * _directionFacing);
                    localAnimState = PlayerSpriteJiggleAnimations.ANIM_STEP_LEFT;
                }
                else
                {
                    playerRenderer.transform.rotation = Quaternion.Euler(0,0,-stepRotation * _directionFacing);
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
        playerRenderer.transform.localScale = new Vector3((1 - Mathf.Abs(horizontalCounterStretch * (float)_physicsBody.LinearVelocityY)) * _directionFacing, 1 + Mathf.Abs(verticalStretch * (float)_physicsBody.LinearVelocityY), 1);
    }


    void OwnerHandleGrounding()
    {
        _trueGrounded = true;

        if (_trueGrounded)
        {
            // TODO: Change this..
            _groundedTracked = Fix64.MaxValue;
            //_groundedTracked = groundedBuffer;
        }

        _grounded = false;
        if (_groundedTracked > Fix64.Zero)
        {
            _grounded = true;
            _groundedTracked -= CustomPhysics.TimeBetweenTicks;
        }
    }


}
