using FixMath.NET;
using UnityEngine;
using Volatile;



public class CustomTrapLiftTickState : ICustomTickState<CustomTrapLiftTickState>
{
    public Fix64 LerpT; // Current position along the path (0 to 1)
    public Fix64 Speed;
    public bool ActiveFlipFlop;

    public CustomTrapLiftTickState Clone()
    {   
        return (CustomTrapLiftTickState)this.MemberwiseClone();
    }

    public override string ToString()
    {
        return $"";
    }
}

public class Trap_Lift : MonoBehaviour
{
    
    [SerializeField] private SpriteJiggleMultiState _spriteState;

    [SerializeField] private CustomTransform _initalPosCustomTransform;
    [SerializeField] private CustomTransform _endPosCustomTransform;
    private VoltVector2 _initalPos;
    private VoltVector2 _endPos;

    [SerializeField] private CustomPhysicsBody _triggerBody;
    [SerializeField] private CustomPhysicsBody _collisionBody;
    [SerializeField] private SpringData _platformSpringData;
    [SerializeField] private IntHundredth _speedIncreaseFactor = 1;
    [SerializeField] private IntHundredth _maxSpeed = 2;

    private CustomTrapLiftTickState _tickState;
    private bool _activeFlipFlop;


    private void Awake()
    {
        _triggerBody.OnTrigger += OnTrigger;
        CustomPhysics.OnPhysicsTick += OnPhysicsTick;
        CustomPhysics.OnStartPhysicsSimulation += OnStartPhysicsSimulation;
    }

    void OnDestroy()
    {
        if(_triggerBody != null)
        {
            _triggerBody.OnTrigger -= OnTrigger;
        }
        CustomPhysics.OnPhysicsTick -= OnPhysicsTick;
        CustomPhysics.OnStartPhysicsSimulation -= OnStartPhysicsSimulation;
    }

    private void OnStartPhysicsSimulation()
    {
        CustomTransform parentCustomTransform = GetComponent<CustomTransform>();

        _initalPos = Helpers.TransformLocalPositionByParentTransform(parentCustomTransform, _initalPosCustomTransform.GetPositionFix64());
        _endPos = Helpers.TransformLocalPositionByParentTransform(parentCustomTransform, _endPosCustomTransform.GetPositionFix64());

        _tickState = new CustomTrapLiftTickState();
        _collisionBody.CustomState = _tickState;

        _tickState.Speed = Fix64.Zero;
        _tickState.LerpT = Fix64.Zero;
        _tickState.ActiveFlipFlop = false;
    }


    public void OnTrigger(CustomPhysicsBody other) 
    {
        if(other.gameObject != this._collisionBody.gameObject)
        {
            _tickState.ActiveFlipFlop = true;
        }
    }

    public void OnPhysicsTick()
    {
        if(CustomPhysics.Tick == 0)
        {
            _collisionBody.Body.Set(_initalPos, Fix64.Zero);
        }

        // Sync local tickstate with snapshot state...
        _tickState = _collisionBody.CustomState as CustomTrapLiftTickState;

        Fix64 deltaTime = CustomPhysics.TimeBetweenTicks;

        if (!_tickState.ActiveFlipFlop)
        {
            _spriteState.SetState("Idle");
            // Decelerate
            _tickState.Speed -=  _speedIncreaseFactor * deltaTime;
        }
        else
        {
            _spriteState.SetState("Pressed");
            // Accelerate
            _tickState.Speed += _speedIncreaseFactor * deltaTime;
        }
        _tickState.ActiveFlipFlop = false;

        // if (Configuration.Singleton.DebugMode && CustomPhysics.Tick % 10 == 0)
        // {
        //     DeterminismLogger.LogExtraInfo(
        //         $"Trap {gameObject.name} T{CustomPhysics.Tick}:\n" +
        //         $"  LerpT={_tickState.LerpT.RawValue}\n" +
        //         $"  Speed={_tickState.Speed.RawValue}\n" +
        //         $"  Active={_activeFlipFlop}\n" +
        //         $"  BodyPos={_collisionBody.Position.x.RawValue},{_collisionBody.Position.y.RawValue}\n" +
        //         $"  BodyVel={_collisionBody.LinearVelocity.x.RawValue},{_collisionBody.LinearVelocity.y.RawValue}\n" +
        //         $"  InitPos={_initalPos.x.RawValue},{_initalPos.y.RawValue}\n" +
        //         $"  EndPos={_endPos.x.RawValue},{_endPos.y.RawValue}"
        //     );
        // }

        // Clamp speed
        if(_tickState.Speed > _maxSpeed)
        {
            _tickState.Speed = _maxSpeed;
        }
        else if(_tickState.Speed < -(Fix64)_maxSpeed)
        {
            _tickState.Speed = -(Fix64)_maxSpeed;
        }

        Fix64 totalDistance = VoltVector2.Distance(_initalPos, _endPos);
        
        Fix64 normalizedSpeed = _tickState.Speed / totalDistance;
        _tickState.LerpT += normalizedSpeed * deltaTime;

        if (_tickState.LerpT > Fix64.One)
        {
            _tickState.LerpT = Fix64.One;
            _tickState.Speed = Fix64.Zero; // Stop at end
        }
        else if (_tickState.LerpT < Fix64.Zero)
        {
            _tickState.LerpT = Fix64.Zero;
            _tickState.Speed = Fix64.Zero; // Stop at start
        }

        VoltVector2 targetPosition = VoltVector2.Lerp(_initalPos, _endPos, _tickState.LerpT);

        VoltVector2 force = Spring.CalculateForce(_collisionBody.Position, targetPosition, _collisionBody.LinearVelocity, _platformSpringData);
        _collisionBody.SetVelocity(force);
        
    }
}
