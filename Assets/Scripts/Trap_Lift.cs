using UnityEngine;

public class Trap_Lift : MonoBehaviour
{
    
    [SerializeField] private Rigidbody2D _baseRb;
    [SerializeField] private SpriteJiggleMultiState _spriteState;

    [SerializeField] private Transform _initalPosition;
    [SerializeField] private Transform _endPosition;

    [SerializeField] private float _speedIncreaseFactor;
    [SerializeField] private float _maxSpeed;

    private float _speed;
    private float _lerpTracked;
    private bool _activeFlipFlop;


    private void Awake()
    {
        _speed = 0;
    }


    public void ReactToOnTriggerStay(Collider2D other) 
    {
        if(other.gameObject != this._baseRb.gameObject)
        {
            print(other.gameObject.name);
            _activeFlipFlop = true;
        }

    }

    public void FixedUpdate()
    {
        if (!_activeFlipFlop)
        {
            _spriteState.SetState("Idle");
            _speed -= _speedIncreaseFactor * Time.fixedDeltaTime;
        }
        else
        {
            _spriteState.SetState("Pressed");
            _speed += _speedIncreaseFactor * Time.fixedDeltaTime;
        }

        _speed = Mathf.Clamp(_speed, -_maxSpeed, _maxSpeed);
        _lerpTracked += _speed * Time.fixedDeltaTime;
        _lerpTracked = Mathf.Clamp01(_lerpTracked);

        if(_lerpTracked == 0 || _lerpTracked == 1)
        {
            _speed = 0;
        }

        _baseRb.MovePosition(Vector2.Lerp(_initalPosition.position, _endPosition.position, _lerpTracked));

        _activeFlipFlop = false;
    }
}
