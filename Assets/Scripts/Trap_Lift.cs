using FixMath.NET;
using UnityEngine;
using Volatile;

public class Trap_Lift : MonoBehaviour
{
    
    [SerializeField] private SpriteJiggleMultiState _spriteState;

    [SerializeField] private Transform _initalPosition;
    [SerializeField] private Transform _endPosition;

    [SerializeField] private CustomTransform _initalPosCustomTransform;
    [SerializeField] private CustomTransform _endPosCustomTransform;
    private VoltVector2 _initalPos;
    private VoltVector2 _endPos;

    [SerializeField] private CustomPhysicsBody _triggerBody;
    [SerializeField] private CustomPhysicsBody _collisionBody;
    [SerializeField] private SpringData _platformSpringData;
    [SerializeField] private float _speedIncreaseFactor = 0.1f;
    [SerializeField] private float _maxSpeed = 2f;

    private Fix64 _maxSpeedFix64;
    private Fix64 _speedFix64;
    private Fix64 _lerpT; // Current position along the path (0 to 1)
    private bool _activeFlipFlop;


    private void Awake()
    {
        _maxSpeedFix64 = (Fix64)_maxSpeed;
        _speedFix64 = Fix64.Zero;

        _triggerBody.OnTrigger += OnTrigger;
        CustomPhysics.OnPhysicsTick += OnPhysicsTick;
        CustomPhysics.OnStartPhysicsSimulation += OnStartPhysicsSimulation;
    }

    void OnDestroy()
    {
        CustomPhysics.OnPhysicsTick -= OnPhysicsTick;
         CustomPhysics.OnStartPhysicsSimulation -= OnStartPhysicsSimulation;
    }

    private void OnStartPhysicsSimulation()
    {
        CustomTransform parentCustomTransform = GetComponent<CustomTransform>();

        _initalPos = Helpers.TransformLocalPositionByParentTransform(parentCustomTransform, _initalPosCustomTransform.GetPositionFix64());
        _endPos = Helpers.TransformLocalPositionByParentTransform(parentCustomTransform, _endPosCustomTransform.GetPositionFix64());

    }


    public void OnTrigger(CustomPhysicsBody other) 
    {
        if(other.gameObject != this._collisionBody.gameObject)
        {
            _activeFlipFlop = true;
        }
    }

    public void OnPhysicsTick()
    {
        Fix64 speedIncreaseFactorFix64 = (Fix64)_speedIncreaseFactor;
        Fix64 deltaTime = CustomPhysics.TimeBetweenTicks;

        if (!_activeFlipFlop)
        {
            _spriteState.SetState("Idle");
            // Decelerate
            _speedFix64 -=  speedIncreaseFactorFix64 * deltaTime;
        }
        else
        {
            _spriteState.SetState("Pressed");
            // Accelerate
            _speedFix64 += speedIncreaseFactorFix64 * deltaTime;
        }

        // Clamp speed
        if(_speedFix64 > _maxSpeedFix64)
        {
            _speedFix64 = _maxSpeedFix64;
        }
        else if(_speedFix64 < -_maxSpeedFix64)
        {
            _speedFix64 = -_maxSpeedFix64;
        }

        Fix64 totalDistance = VoltVector2.Distance(_initalPos, _endPos);

        Fix64 normalizedSpeed = _speedFix64 / totalDistance;
        _lerpT += normalizedSpeed * deltaTime;

        if (_lerpT > Fix64.One)
        {
            _lerpT = Fix64.One;
            _speedFix64 = Fix64.Zero; // Stop at end
        }
        else if (_lerpT < Fix64.Zero)
        {
            _lerpT = Fix64.Zero;
            _speedFix64 = Fix64.Zero; // Stop at start
        }

        VoltVector2 targetPosition = VoltVector2.Lerp(_initalPos, _endPos, _lerpT);

        VoltVector2 force = Spring.CalculateForce(_collisionBody.Position, targetPosition, _collisionBody.LinearVelocity, _platformSpringData);
        _collisionBody.SetVelocity(force);
        
        _activeFlipFlop = false;
    }
}
