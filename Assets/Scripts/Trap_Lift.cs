using FixMath.NET;
using UnityEngine;
using Volatile;

public class Trap_Lift : MonoBehaviour
{
    
    [SerializeField] private SpriteJiggleMultiState _spriteState;

    [SerializeField] private Transform _initalPosition;
    [SerializeField] private Transform _endPosition;
    private VoltVector2 _initalPosVolt;
    private VoltVector2 _endPosVolt;

    [SerializeField] private CustomPhysicsBody _triggerBody;
    [SerializeField] private CustomPhysicsBody _collisionBody;

    [SerializeField] private float _speedIncreaseFactor;
    [SerializeField] private float _maxSpeed;

    private Fix64 _maxSpeedFix64;
    private Fix64 _speedFix64;
    private float _lerpTracked;
    private bool _activeFlipFlop;


    private void Awake()
    {
        _initalPosVolt = new VoltVector2((Fix64)_initalPosition.position.x, (Fix64)_initalPosition.position.y);
        _endPosVolt = new VoltVector2((Fix64)_endPosition.position.x, (Fix64)_endPosition.position.y);

        _maxSpeedFix64 = (Fix64)_maxSpeed;
        _speedFix64 = Fix64.Zero;

        _triggerBody.OnTrigger += OnTrigger;
        CustomPhysics.OnPhysicsTick += OnPhysicsTick;
    }

    void OnDestroy()
    {
        CustomPhysics.OnPhysicsTick -= OnPhysicsTick;
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
        if (!_activeFlipFlop)
        {
            _spriteState.SetState("Idle");
            _speedFix64 -= _speedFix64 * CustomPhysics.TimeBetweenTicks;
        }
        else
        {
            _spriteState.SetState("Pressed");
            _speedFix64 += _speedFix64 * CustomPhysics.TimeBetweenTicks;
        }


        if(_speedFix64 > _maxSpeedFix64)
        {
            _speedFix64 = _maxSpeedFix64;
        }
        else if(_speedFix64 < -_maxSpeedFix64)
        {
            _speedFix64 = -_maxSpeedFix64;
        }

       // VoltVector2 velocity = 


        // _collisionBody.SetVelocity(velocity);

        // _speedFix64 = Mathf.Clamp(_speedFix64, -_speed, _maxSpeed);
        // //_lerpTracked += _speed * Time.fixedDeltaTime;
        // //_lerpTracked = Mathf.Clamp01(_lerpTracked);

        // if(_lerpTracked == 0 || _lerpTracked == 1)
        // {
        //    _speed = 0;
        // }

        // //_baseRb.MovePosition(Vector2.Lerp(_initalPosition.position, _endPosition.position, _lerpTracked));


        _activeFlipFlop = false;
    }
}
