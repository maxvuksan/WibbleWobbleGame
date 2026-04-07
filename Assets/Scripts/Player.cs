using System;
using System.Collections;
using System.Collections.Generic;
using FixMath.NET;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using Volatile;




public class CustomPlayerTickState : ICustomTickState<CustomPlayerTickState>
{
    // when was the jump input last pressed
    public long _lastJumpInputPhysicsTick = int.MinValue;

    // when was a jump operation last performed...
    public long _lastJumpPhysicsTick = int.MinValue;
    public long _lastWallLeftJumpPhysicsTick = int.MinValue;
    public long _lastWallRightJumpPhysicsTick = int.MinValue;

    public PlayerMoveInput _playerGroundedDirection; // none if the main ground ray hits
    public Fix64 _horizontalOverrideVelocityX;
    public Fix64 _horizontalControlFactor; // if 0 we have control, otherwise this value approaches zero

    // when did we last touch a surface...
    public long _lastGroundedPhysicsTick = int.MinValue;
    public long _lastWallLeftTouchPhysicsTick = int.MinValue;
    public long _lastWallRightTouchPhysicsTick = int.MinValue;
    

    public ulong? _groundedBodyEntityId = 0;
    public VoltVector2 _groundedNormalDirection;
    public bool _trueGrounded = false;
    
    public bool _grounded;
    public bool _touchingLeftWall;
    public bool _touchingRightWall;

    public int _directionFacing = 1;
    public bool _playerAliveLocally = true;

    public CustomPlayerTickState Clone()
    {   
        // TODO: We should ensure no reference types are cloned (no reference types should be present in TickState)
        return (CustomPlayerTickState)this.MemberwiseClone();
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
    [SerializeField] private IntHundredth _horizontalControlDuration;
    [SerializeField] private IntHundredth _wallJumpHeight;
    [SerializeField] private IntHundredth _wallJumpXVelocity;
    
    // some of these variables are stored as var/10 to allow us to store as integer for determinism restriction
    [SerializeField] private IntHundredth _jumpHeight = 160;      // 16.0
    [SerializeField] private IntHundredth _maxSpeed = 80;         // 8.0
    [SerializeField] private int _groundRayLengthTenths = 2;   // 0.2
    [SerializeField] private int _groundRayYOffsetTenths = 15; // 1.5
    [SerializeField] private IntHundredth _wallRayXOffset;
    [SerializeField] private IntHundredth _wallRayYOffset;
    [SerializeField] private IntHundredth _wallRayLength;
    [SerializeField] private IntHundredth _groundRaySidesXOffset;
    [SerializeField] private IntHundredth _groundRaySidesYOffset;
    [SerializeField] private IntHundredth _groundRaySidesLength;

    // TODO: Use new IntHundreth type
    private Fix64 _groundRayLength => (Fix64)_groundRayLengthTenths / (Fix64)10;
    private Fix64 _groundRayYOffset => (Fix64)_groundRayYOffsetTenths / (Fix64)10;


    [Header("Visuals")]
    [SerializeField] private float inAirRotationSpeed = 1.0f;
    [SerializeField] private float verticalStretch = 1.0f;
    [SerializeField] private float horizontalCounterStretch = 0.5f;
    [SerializeField] private float stepRotation = 15.0f;
    [SerializeField] private float stepSpriteDuration = 0.2f;
    [SerializeField] private GameObject _footstepPrefab;    
    [SerializeField] private SpriteRenderer _playerRenderer;
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
    private CustomPhysicsRayResult _groundedLeftRaycastResult;
    private CustomPhysicsRayResult _groundedRightRaycastResult;

    private CustomPhysicsRayResult _topLeftRaycastResult;
    private CustomPhysicsRayResult _topRightRaycastResult;
    private CustomPhysicsRayResult _bottomLeftRaycastResult;
    private CustomPhysicsRayResult _bottomRightRaycastResult;
    
    private float _inAirRotationFactor = 0;
    private float _stepSpriteDurationTracked;
    private bool _isLeftFootForward;

    void Awake()
    {
        _playerHeader = GetComponentInParent<NetworkPlayerHeader>();
        _animState = GetComponent<SpriteJiggleMultiState>();
        _inputHandler = FindFirstObjectByType<ControllerInputHandler>();
        _physicsBody = GetComponent<CustomPhysicsBody>();

    }

    private void OnTurnOffPhysicsSimulation()
    {
        _tickState = new CustomPlayerTickState();

        ResetState();

        _tickState._playerAliveLocally = false;
        _physicsBody.CustomState = _tickState;
    }

    void Start()
    {
        CustomPhysics.OnPhysicsTick += OnPhysicsTick;
        CustomPhysics.OnTurnOffPhysicsSimulation += OnTurnOffPhysicsSimulation;
        CustomPhysics.OnStartPhysicsSimulation += ResetState;
        CustomPhysics.OnPrePhysicsTick += OnPrePhysicsTick;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        CustomPhysics.OnPhysicsTick -= OnPhysicsTick;
        CustomPhysics.OnTurnOffPhysicsSimulation -= OnTurnOffPhysicsSimulation;
        CustomPhysics.OnStartPhysicsSimulation -= ResetState;
        CustomPhysics.OnPrePhysicsTick -= OnPrePhysicsTick;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        OnTurnOffPhysicsSimulation();
    }

    public void ResetState()
    {
        _originalColour = _playerRenderer.color;
        _tickState._trueGrounded = false;
        _stepSpriteDurationTracked = 0;

        _tickState._playerAliveLocally = true;
        
        _tickState._horizontalOverrideVelocityX = Fix64.Zero;
        _tickState._horizontalControlFactor = Fix64.Zero;
        _tickState._lastJumpInputPhysicsTick = int.MinValue;
        _tickState._lastJumpPhysicsTick = int.MinValue;
        _tickState._lastGroundedPhysicsTick = int.MinValue;

        _tickState._lastWallLeftTouchPhysicsTick = int.MinValue;
        _tickState._lastWallLeftJumpPhysicsTick = int.MinValue;
        _tickState._lastWallLeftJumpPhysicsTick = int.MinValue;
        _tickState._lastWallRightJumpPhysicsTick = int.MinValue;

        _physicsBody.CustomState = _tickState;
        _physicsBody.LinearVelocity = VoltVector2.zero;

        _playerHeader.SetHasWonRpc(false);
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
            _playerRenderer.enabled = true;
            eyeRenderer.enabled = true;
        }
        else
        {
            eyeRenderer.enabled = false;
            _playerRenderer.enabled = false;
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
        _originalColour = _playerRenderer.color;
        _playerRenderer.color = color;   
    }

    public void SpawnFootstepPrefab()
    {
        if (!_tickState._grounded || _footstepPrefab == null)
        {
            return;
        }

        Instantiate(_footstepPrefab, groundCheckPosition.position, Quaternion.identity);
    }

    public void Update()
    {
        // sync with body
        _tickState = _physicsBody.CustomState as CustomPlayerTickState;

        if (!_tickState._playerAliveLocally)
        {
            _playerRenderer.gameObject.SetActive(false);
            return;
        }
        else
        {
            _playerRenderer.gameObject.SetActive(true);
        }

        ReflectSpriteState();
    }

    public void SetPosition(IntHundredthVector2 position)
    {
        _physicsBody.TeleportPosition(position);
    }

    public void HitTrap(Vector2 directionToApplyForce)
    {
        if (!_tickState._playerAliveLocally)
        {
            return;            
        }

        _tickState._playerAliveLocally = false;
        _physicsBody.Body.IsEnabled = _tickState._playerAliveLocally;
        //_playerHeader.SetPlayerAliveRpc(false);

        //rb.AddForce(directionToApplyForce, ForceMode2D.Impulse);
        //PlayerDataManager.Singleton.Callback_OnPlayerDeath(_playerHeader.PlayerIndex.Value);

        OnPlayerDeath();
    }

    public void OnPlayerDeath()
    {
        AudioManager.Singleton.Play("Player_Death");
    }

    public void Jump(PlayerMoveInput wallJumpDirection = PlayerMoveInput.None)
    {   

        _tickState._lastJumpInputPhysicsTick = CustomPhysics.Tick;

        switch(wallJumpDirection){

            // regular jump
            case PlayerMoveInput.None:
                _physicsBody.LinearVelocity = new VoltVector2(_physicsBody.LinearVelocityX, _jumpHeight);
                _tickState._lastJumpPhysicsTick = CustomPhysics.Tick;
                break;

            // wall jumps...
            case PlayerMoveInput.LeftPressed:
                
                _physicsBody.LinearVelocity = new VoltVector2(_wallJumpXVelocity, _wallJumpHeight);
                _tickState._lastWallLeftJumpPhysicsTick = CustomPhysics.Tick;
                _tickState._directionFacing = 1;
                _tickState._horizontalControlFactor = _horizontalControlDuration;
                _tickState._horizontalOverrideVelocityX = _wallJumpXVelocity;
                break;

            case PlayerMoveInput.RightPressed:
                _physicsBody.LinearVelocity = new VoltVector2(-_wallJumpXVelocity.AsFix64(), _wallJumpHeight);
                _tickState._lastWallRightJumpPhysicsTick = CustomPhysics.Tick;
                _tickState._directionFacing = -1;
                _tickState._horizontalControlFactor = _horizontalControlDuration;
                _tickState._horizontalOverrideVelocityX = -_wallJumpXVelocity.AsFix64();
                break;
        }


        if (!CustomPhysics.Resimulating)
        {
            AudioManager.Singleton.Play("Player_Jump");
        }
    }

    public void OnPrePhysicsTick()
    {
        // Sync local tickstate with snapshot state...
        _tickState = _physicsBody.CustomState as CustomPlayerTickState;
    }

    public void OnPhysicsTick()
    {
        _physicsBody.Body.IsEnabled = _tickState._playerAliveLocally;

        if (!_tickState._playerAliveLocally)
        {
            return;
        }

        if (_driver.PlayerInputs.InputJump == PlayerJumpInput.JumpPressed)
        {
            _tickState._lastJumpInputPhysicsTick = CustomPhysics.Tick;
        }

        PerformGroundedCheck();
        PerformWallChecks();


        if(_tickState._horizontalControlFactor > Fix64.Zero)
        {
            _tickState._horizontalControlFactor -= CustomPhysics.TimeBetweenTicks;
        }
        else
        {
            _tickState._horizontalControlFactor = Fix64.Zero;
        }
        Fix64 horizontalControl_t = _tickState._horizontalControlFactor / _horizontalControlDuration.AsFix64();

        // we can move, and we also are more than 50% in control horizontally
        int moveDirection = _driver.PlayerInputs.GetMoveDirection();
        if(
            moveDirection != 0 && 
            ((horizontalControl_t * (Fix64)10) < (Fix64)5))
        {
            _tickState._directionFacing = moveDirection;
        }

        // determine if we are within specified buffer/grace periods
        bool jumpInput = _tickState._lastJumpInputPhysicsTick >= CustomPhysics.Tick - _jumpTickInputBuffer;
        bool canVerticalJump = _tickState._lastJumpPhysicsTick < CustomPhysics.Tick - _jumpTickDisableBuffer;
        bool canWallJumpLeft = _tickState._lastWallLeftJumpPhysicsTick < CustomPhysics.Tick - _jumpTickDisableBuffer;
        bool canWallJumpRight = _tickState._lastWallRightJumpPhysicsTick < CustomPhysics.Tick - _jumpTickDisableBuffer;

        ApplyMovementPhysics(horizontalControl_t);

        if(jumpInput) // & _jumpInputTracked > 0
        {
            if (canVerticalJump && _tickState._grounded)
            {
                Jump(_tickState._playerGroundedDirection);
            }
            else if (canWallJumpLeft && _tickState._touchingLeftWall)
            {
                Jump(PlayerMoveInput.LeftPressed);
            }
            else if (canWallJumpRight && _tickState._touchingRightWall)
            {
                Jump(PlayerMoveInput.RightPressed);
            }

            _tickState._lastJumpInputPhysicsTick = int.MinValue;
        }

        ApplyPositionWrapAroundInLobby();    

    
        // reapply custom data
        _physicsBody.CustomState = _tickState;
    }

    public void ApplyMovementPhysics(Fix64 horizontalControl_t)
    {
        Fix64 targetSpeed = (Fix64)_driver.PlayerInputs.GetMoveDirection() * _maxSpeed;
        targetSpeed = Fix64.Lerp(targetSpeed, _tickState._horizontalOverrideVelocityX, horizontalControl_t);

        if(targetSpeed < Fix64.Zero){

            // wall is on left, dont push into wall
            if(_topLeftRaycastResult.Hit && _topLeftRaycastResult.Body.BodyType == CustomBodyType.Static)
            {
                targetSpeed = Fix64.Zero;
            }
            if(_bottomLeftRaycastResult.Hit && _bottomLeftRaycastResult.Body.BodyType == CustomBodyType.Static)
            {
                targetSpeed = Fix64.Zero;
            }
        }

        if(targetSpeed > Fix64.Zero){
            
            // wall is on right, dont push into wall
            if(_topRightRaycastResult.Hit && _topRightRaycastResult.Body.BodyType == CustomBodyType.Static)
            {
                targetSpeed = Fix64.Zero;
            }
            if(_bottomRightRaycastResult.Hit && _bottomRightRaycastResult.Body.BodyType == CustomBodyType.Static)
            {
                targetSpeed = Fix64.Zero;
            }
        }


        if (_tickState._trueGrounded)
        {   
            _physicsBody.LinearVelocityX = targetSpeed;

            // only make body follow if we are not moving
            if(_tickState._groundedBodyEntityId != null && targetSpeed == Fix64.Zero)
            {
                CustomPhysicsBody otherBody = CustomPhysicsSpace.Singleton.GetBody(_tickState._groundedBodyEntityId.Value);
                _physicsBody.LinearVelocityX = targetSpeed + otherBody.LinearVelocityX;
            }
        }
        else
        {
            _physicsBody.LinearVelocityX = targetSpeed;
        }


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

        bool setPosOccured = false;
        
        // Vertical wrap
        if (_physicsBody.Position.y < minY)
        {
            _physicsBody.PositionY = maxY;
            setPosOccured = true;
        }
        else if (_physicsBody.Position.y > maxY)
        {
            _physicsBody.PositionY = minY;
            setPosOccured = true;
        }

        // Horizontal wrap 
        if (_physicsBody.Position.x < minX)
        {
            _physicsBody.PositionX = maxX;
            setPosOccured = true;
        }
        else if (_physicsBody.Position.x > maxX)
        {
            _physicsBody.PositionX = minX;
            setPosOccured = true;
        }

        if (setPosOccured)
        {
            _physicsBody.SetSkipInterpolationNextFrame(true);
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
            _playerRenderer.transform.rotation = Quaternion.Euler(0,0,_inAirRotationFactor * stepRotation * _tickState._directionFacing);

        }
        else
        {
            _inAirRotationFactor = 0;
        
            // I dont mind doing floating point math here because the result is not stored + its for visuals not actual simulation
            bool isMoving = _driver.PlayerInputs.InputMoveDirection != PlayerMoveInput.None; 
            
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
                    _playerRenderer.transform.rotation = Quaternion.Euler(0,0,stepRotation * _tickState._directionFacing);
                    localAnimState = PlayerSpriteJiggleAnimations.ANIM_STEP_LEFT;
                }
                else
                {
                    _playerRenderer.transform.rotation = Quaternion.Euler(0,0,-stepRotation * _tickState._directionFacing);
                    localAnimState = PlayerSpriteJiggleAnimations.ANIM_STEP_RIGHT;
                }
            }
            else
            {
                _playerRenderer.transform.rotation = Quaternion.Euler(0,0,0);
                //_stepSpriteDurationTracked = 0;
                localAnimState = PlayerSpriteJiggleAnimations.ANIM_IDLE;
            }
        }

        _animState.SetState(_spriteJiggleAnimationMappings[(int)localAnimState]);
        _playerRenderer.transform.localScale = new Vector3((1 - Mathf.Abs(horizontalCounterStretch * (float)_physicsBody.LinearVelocityY)) * _tickState._directionFacing, 1 + Mathf.Abs(verticalStretch * (float)_physicsBody.LinearVelocityY), 1);
    }


    bool NormalIsValidForGrounding(VoltVector2 normal)
    {   
        // we assume the normal is already normalized

        // convert to larger number for comparision
        normal *= (Fix64)10;

        // 10 is perfect floor, 0 is wall
        if(normal.y > (Fix64)6)
        {
            return true;
        }
        return false;
    }

    void SetCurrentGroundedBody(CustomPhysicsRayResult raycast, ulong? groundedBodyEntityId)
    {
        _tickState._groundedBodyEntityId = groundedBodyEntityId;
        _tickState._groundedNormalDirection = _groundedRaycastResult.Normal;
        _tickState._trueGrounded = true;
        _tickState._lastGroundedPhysicsTick = CustomPhysics.Tick;
    }

    void PerformGroundedCheck()
    {
        _groundedRaycastResult = CustomPhysics.Raycast(
            _physicsBody.Position + new VoltVector2(Fix64.Zero, -(Fix64)_groundRayYOffset), 
            new VoltVector2(Fix64.Zero, -Fix64.One), 
            (Fix64)_groundRayLength);

        _groundedLeftRaycastResult = CustomPhysics.Raycast(
            _physicsBody.Position + new VoltVector2(-(Fix64)_groundRaySidesXOffset, -(Fix64)_groundRaySidesYOffset), 
            new VoltVector2(Fix64.Zero, -Fix64.One), 
            (Fix64)_groundRaySidesLength);

        _groundedRightRaycastResult = CustomPhysics.Raycast(
            _physicsBody.Position + new VoltVector2(_groundRaySidesXOffset, -(Fix64)_groundRaySidesYOffset), 
            new VoltVector2(Fix64.Zero, -Fix64.One), 
            (Fix64)_groundRaySidesLength);
        
        if (_groundedRaycastResult.Hit)
        {
            SetCurrentGroundedBody(_groundedRaycastResult, _groundedRaycastResult.Body.GetDesiredEntityId());
            _tickState._playerGroundedDirection = PlayerMoveInput.None;
        }
        else if (_groundedLeftRaycastResult.Hit && NormalIsValidForGrounding(_groundedLeftRaycastResult.Normal))
        {
            SetCurrentGroundedBody(_groundedLeftRaycastResult, null);
            _tickState._playerGroundedDirection = PlayerMoveInput.LeftPressed;
        }
        else if (_groundedRightRaycastResult.Hit && NormalIsValidForGrounding(_groundedRightRaycastResult.Normal))
        {
            SetCurrentGroundedBody(_groundedRightRaycastResult, null);
            _tickState._playerGroundedDirection = PlayerMoveInput.RightPressed;
        }
        else
        {
            _tickState._trueGrounded = false;
        }

        _tickState._grounded = false;
        if (_tickState._lastGroundedPhysicsTick >= CustomPhysics.Tick - _groundedTickBuffer)//_groundedTracked > Fix64.Zero)
        {
            _tickState._grounded = true;
        }
    }

    void PerformWallChecks()
    {
        _topLeftRaycastResult = CustomPhysics.Raycast(
            _physicsBody.Position + new VoltVector2(-_wallRayXOffset.AsFix64(), (Fix64)_wallRayYOffset.AsFix64()), 
            new VoltVector2(-Fix64.One, Fix64.Zero), 
            (Fix64)_wallRayLength);

        _bottomLeftRaycastResult = CustomPhysics.Raycast(
            _physicsBody.Position + new VoltVector2(-_wallRayXOffset.AsFix64(), -(Fix64)_wallRayYOffset.AsFix64()), 
            new VoltVector2(-Fix64.One, Fix64.Zero), 
            (Fix64)_wallRayLength);

        if(_topLeftRaycastResult.Hit || _bottomLeftRaycastResult.Hit)
        {
            _tickState._lastWallLeftTouchPhysicsTick = CustomPhysics.Tick;
        }

        _topRightRaycastResult = CustomPhysics.Raycast(
            _physicsBody.Position + new VoltVector2(_wallRayXOffset.AsFix64(), (Fix64)_wallRayYOffset.AsFix64()), 
            new VoltVector2(Fix64.One, Fix64.Zero), 
            (Fix64)_wallRayLength);

        _bottomRightRaycastResult = CustomPhysics.Raycast(
            _physicsBody.Position + new VoltVector2(_wallRayXOffset.AsFix64(), -(Fix64)_wallRayYOffset.AsFix64()), 
            new VoltVector2(Fix64.One, Fix64.Zero), 
            (Fix64)_wallRayLength);

        if(_topRightRaycastResult.Hit || _bottomRightRaycastResult.Hit)
        {
            _tickState._lastWallRightTouchPhysicsTick = CustomPhysics.Tick;
        }

        _tickState._touchingLeftWall = _tickState._lastWallLeftTouchPhysicsTick >= CustomPhysics.Tick - _groundedTickBuffer;
        _tickState._touchingRightWall = _tickState._lastWallRightTouchPhysicsTick >= CustomPhysics.Tick - _groundedTickBuffer;

    }

    void OnDrawGizmos()
    {  
        DrawRaycastResultGizmo(_groundedRaycastResult);
        DrawRaycastResultGizmo(_groundedLeftRaycastResult);
        DrawRaycastResultGizmo(_groundedRightRaycastResult);
        
        DrawRaycastResultGizmo(_topLeftRaycastResult);
        DrawRaycastResultGizmo(_topRightRaycastResult);
        DrawRaycastResultGizmo(_bottomLeftRaycastResult);
        DrawRaycastResultGizmo(_bottomRightRaycastResult);
    }

    void DrawRaycastResultGizmo(CustomPhysicsRayResult rayResult)
    {
        Gizmos.color = rayResult.Hit ? Color.red : Color.green;

        Vector3 start = new Vector3(
            (float)rayResult.Origin.x,
            (float)rayResult.Origin.y,
            0);

        Vector3 end = new Vector3(
            (float)rayResult.Destination.x,
            (float)rayResult.Destination.y,
            0);

        Gizmos.DrawLine(start, end);
    }


}
