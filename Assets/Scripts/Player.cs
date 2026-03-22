using System;
using System.Collections;
using System.Collections.Generic;
using FixMath.NET;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using Volatile;




public class CustomPlayerTickState : ICustomTickState<CustomPlayerTickState>
{
    public long _lastJumpPhysicsTick = int.MinValue;
    public long _lastJumpInputPhysicsTick = int.MinValue;
    public long _lastGroundedPhysicsTick = int.MinValue;
    public bool _trueGrounded = false;
    public bool _grounded;
    public int _directionFacing = 1;

    public CustomPlayerTickState Clone()
    {
        return new CustomPlayerTickState
        {
            _lastJumpPhysicsTick = this._lastJumpPhysicsTick,
            _lastJumpInputPhysicsTick = this._lastJumpInputPhysicsTick,
            _lastGroundedPhysicsTick = this._lastGroundedPhysicsTick,
            _trueGrounded = this._trueGrounded,
            _grounded = this._grounded,
            _directionFacing = this._directionFacing,
        };
    }

    public override string ToString()
    {
        return $"jump:{_lastJumpPhysicsTick}\n jumpInput:{_lastJumpInputPhysicsTick} \n" +
               $"lastGrounded:{_lastGroundedPhysicsTick}\n trueGrounded:{_trueGrounded} \n" +
               $"grounded:{_grounded} facing:{_directionFacing}\n\n";
    }
}


public class Player : NetworkBehaviour
{

    [Header("Ground Checking")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Transform groundCheckPosition;
    [SerializeField] private Vector2 groundCheckSize;
    [SerializeField] private Vector2 wallCheckSize;
 
    [Header("Movement")]
    [SerializeField] private int _jumpTickDisableBuffer = 25; // minimum allowed time between jumps (prevents spamming)
    [SerializeField] private int _jumpTickInputBuffer = 30; // holds onto jump inputs for this amount of time, will then jump when grounded next
    [SerializeField] private int _groundedTickBuffer = 20;
    
    // some of these variables are stored as var/10 to allow us to store as integer for determinism restriction
    [SerializeField] private int _jumpHeightTenths = 160;      // 16.0
    [SerializeField] private int _maxSpeedTenths = 80;         // 8.0
    [SerializeField] private int _groundRayLengthTenths = 2;   // 0.2
    [SerializeField] private int _groundRayYOffsetTenths = 15; // 1.5

    private Fix64 _jumpHeight => (Fix64)_jumpHeightTenths / (Fix64)10;
    private Fix64 _maxSpeed => (Fix64)_maxSpeedTenths / (Fix64)10;
    private Fix64 _groundRayLength => (Fix64)_groundRayLengthTenths / (Fix64)10;
    private Fix64 _groundRayYOffset => (Fix64)_groundRayYOffsetTenths / (Fix64)10;


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

    private CustomPlayerTickState _tickState = new();


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
    private CustomPhysicsRayResult _groundedRaycastResult;
    private float _inAirRotationFactor = 0;
    private float _stepSpriteDurationTracked;
    private bool _isLeftFootForward;

    void Awake()
    {
        _playerHeader = GetComponentInParent<NetworkPlayerHeader>();
        _animState = GetComponent<SpriteJiggleMultiState>();
        _inputHandler = FindFirstObjectByType<ControllerInputHandler>();
        _physicsBody = GetComponent<CustomPhysicsBody>();


        _physicsBody.CustomState = _tickState;
    }

    private void OnTurnOffPhysicsSimulation()
    {
        _tickState = new CustomPlayerTickState();
        _physicsBody.CustomState = _tickState;

        ResetState();
    }

    void Start()
    {
        CustomPhysics.OnPhysicsTick += OnPhysicsTick;
        CustomPhysics.OnTurnOffPhysicsSimulation += OnTurnOffPhysicsSimulation;
        CustomPhysics.OnPostPhysicsTick += OnPostPhysicsTick;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        CustomPhysics.OnPhysicsTick -= OnPhysicsTick;
        CustomPhysics.OnTurnOffPhysicsSimulation -= OnTurnOffPhysicsSimulation;
        CustomPhysics.OnPostPhysicsTick -= OnPostPhysicsTick;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        ResetState();
    }


    public void ResetState()
    {
        _originalColour = playerRenderer.color;
        _tickState._trueGrounded = false;
        _stepSpriteDurationTracked = 0;

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
        if (!_tickState._grounded || footstepPrefab == null)
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

        // Don't update visuals during resimulation
        if (CustomPhysics.Resimulating){
            return;
        }

        // sync with body
        _tickState = _physicsBody.CustomState as CustomPlayerTickState;
        
        ReflectSpriteState();
    }


    public void SetPosition(IntHundredthVector2 position)
    {
        _physicsBody.SetPosition(position);
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
        _physicsBody.LinearVelocity = new VoltVector2(_physicsBody.LinearVelocityX, _jumpHeight);

        if (playSound)
        {
            //AudioManager.Singleton.Play("Player_Jump");
        }
    }

    public void OnPhysicsTick()
    {
        // Sync local tickstate with snapshot state...
        _tickState = _physicsBody.CustomState as CustomPlayerTickState;

        if (_driver.PlayerInputs.InputJump == PlayerJumpInput.JumpPressed)
        {
             _tickState._lastJumpInputPhysicsTick = CustomPhysics.Tick;
        }

        PerformGroundedCheck();

        int moveDirection = _driver.PlayerInputs.GetMoveDirection();
        if(moveDirection != 0)
        {
            _tickState._directionFacing = moveDirection;
        }


        bool canJump = false;
        if(_tickState._lastJumpInputPhysicsTick >= CustomPhysics.Tick - _jumpTickInputBuffer &&
            _tickState._lastJumpPhysicsTick < CustomPhysics.Tick - _jumpTickDisableBuffer
        )
        {
            canJump = true;
        }
        
        if(canJump && _tickState._grounded) // & _jumpInputTracked > 0
        {
            Jump(true);

            _tickState._lastJumpPhysicsTick = CustomPhysics.Tick;
            _tickState._lastGroundedPhysicsTick = int.MinValue; // ensure we are no longer considered grounded
            _tickState._lastJumpInputPhysicsTick = int.MinValue;
        }

        ApplyMovementPhysics();
        ApplyPositionWrapAroundInLobby();    

    
        // reapply custom data
        _physicsBody.CustomState = _tickState;
    }

    public void OnPostPhysicsTick()
    {
        
    }


    public void ApplyMovementPhysics()
    {
        Fix64 targetSpeed = (Fix64)_driver.PlayerInputs.GetMoveDirection() * _maxSpeed;

        _physicsBody.LinearVelocityX = targetSpeed;
    }


    private void ApplyPositionWrapAroundInLobby()
    {
        if(GameStateManager.Singleton.NetworkedState.Value != GameStateManager.GameStateEnum.GameState_SelectingLevel)
        {
             //return;
        }

        Fix64 minY = (Fix64)(-25);
        Fix64 minX = (Fix64)(-41);
        Fix64 maxY = (Fix64)25;
        Fix64 maxX = (Fix64)41;

        // Vertical wrap
        if (_physicsBody.Position.y < minY)
        {
            _physicsBody.PositionY = maxY;
        }
        else if (_physicsBody.Position.y > maxY)
        {
            _physicsBody.PositionY = minY;
        }

        // Horizontal wrap 
        if (_physicsBody.Position.x < minX)
        {
            _physicsBody.PositionX = maxX;
        }
        else if (_physicsBody.Position.x > maxX)
        {
            _physicsBody.PositionX = minX;
        }
    }


    void ReflectSpriteState()
    {
        PlayerSpriteJiggleAnimations localAnimState = PlayerSpriteJiggleAnimations.ANIM_IDLE;

        if(!_tickState._trueGrounded)
        {
            _isLeftFootForward = false;
            _stepSpriteDurationTracked = 0;
            localAnimState = PlayerSpriteJiggleAnimations.ANIM_IN_AIR;

            if((float)_physicsBody.LinearVelocityY > 0)
            {
                _inAirRotationFactor += inAirRotationSpeed * Time.deltaTime;
            }
            else
            {
                _inAirRotationFactor -= inAirRotationSpeed * Time.deltaTime;
            }

            _inAirRotationFactor = Mathf.Clamp(_inAirRotationFactor, -1, 1);
            playerRenderer.transform.rotation = Quaternion.Euler(0,0,_inAirRotationFactor * stepRotation * _tickState._directionFacing);

        }
        else
        {
            _inAirRotationFactor = 0;
        
            // I dont mind doing floating point math here because the result is not stored + its for visuals not actual simulation
            bool isMoving = Mathf.Abs((float)_physicsBody.LinearVelocityX) > 1.0f;
            
            if(isMoving) {
                
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
                    playerRenderer.transform.rotation = Quaternion.Euler(0,0,stepRotation * _tickState._directionFacing);
                    localAnimState = PlayerSpriteJiggleAnimations.ANIM_STEP_LEFT;
                }
                else
                {
                    playerRenderer.transform.rotation = Quaternion.Euler(0,0,-stepRotation * _tickState._directionFacing);
                    localAnimState = PlayerSpriteJiggleAnimations.ANIM_STEP_RIGHT;
                }
            }
            else
            {
                playerRenderer.transform.rotation = Quaternion.Euler(0,0,0);
                //_stepSpriteDurationTracked = 0;
                localAnimState = PlayerSpriteJiggleAnimations.ANIM_IDLE;
            }
        }

        _animState.SetState(_spriteJiggleAnimationMappings[(int)localAnimState]);
        playerRenderer.transform.localScale = new Vector3((1 - Mathf.Abs(horizontalCounterStretch * (float)_physicsBody.LinearVelocityY)) * _tickState._directionFacing, 1 + Mathf.Abs(verticalStretch * (float)_physicsBody.LinearVelocityY), 1);
    }


    void PerformGroundedCheck()
    {
        _groundedRaycastResult = CustomPhysics.Raycast(
            _physicsBody.Position + new VoltVector2(Fix64.Zero, -(Fix64)_groundRayYOffset), 
            new VoltVector2(Fix64.Zero, -Fix64.One), 
            (Fix64)_groundRayLength);

        
        _tickState._trueGrounded = _groundedRaycastResult.Hit;
        if (_tickState._trueGrounded)
        {
            _tickState._lastGroundedPhysicsTick = CustomPhysics.Tick;
        }

        _tickState._grounded = false;
        if (_tickState._lastGroundedPhysicsTick >= CustomPhysics.Tick - _groundedTickBuffer)//_groundedTracked > Fix64.Zero)
        {
            _tickState._grounded = true;
        }
    }

    void OnDrawGizmos()
    {  
        Gizmos.color = _groundedRaycastResult.Hit ? Color.red : Color.green;

        Vector3 start = new Vector3(
            (float)_groundedRaycastResult.Origin.x,
            (float)_groundedRaycastResult.Origin.y,
            0);

        Vector3 end = new Vector3(
            (float)_groundedRaycastResult.Destination.x,
            (float)_groundedRaycastResult.Destination.y,
            0);

        Gizmos.DrawLine(start, end);
    }


}
