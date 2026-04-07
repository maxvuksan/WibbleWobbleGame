using FixMath.NET;
using UnityEngine;

public class Bird : MonoBehaviour
{
    public enum BirdState
    {
        Idle, // stand still
        Pekking, // pick the ground
        Hopping, // move to a nearby location
    }

    private Animator _animator;
    private SpriteRenderer _spriteRenderer; 
    private BirdState _state;
    private Fix64 _timeInState;
    private Fix64 _timeToNextAction;


    void Awake()
    {
        _animator = GetComponent<Animator>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        CustomPhysics.OnPhysicsTick += OnPhysicsTick;

        RefreshState(BirdState.Idle);
    }

    void OnDestroy()
    {
        CustomPhysics.OnPhysicsTick -= OnPhysicsTick;
    }

    void OnPhysicsTick()
    {
        switch (_state)
        {
            case BirdState.Idle: {
                
                if(_timeInState <= _timeToNextAction)
                {
                    break;
                }

                int randResult = Random.Range(0, 3);

                switch (randResult)
                {
                    case 0:
                    {
                        _spriteRenderer.flipX = !_spriteRenderer.flipX;
                        _animator.SetTrigger("Hop");
                        RefreshState(BirdState.Idle);
                        break;
                    }
                    case 1:
                    case 2:
                    {
                        RefreshState(BirdState.Pekking);
                        break;
                    }
                }
                

                break;
            }

            case BirdState.Pekking: {

                _animator.SetTrigger("Pek");
                RefreshState(BirdState.Idle);
                break;
            }

            case BirdState.Hopping: {

                break;
            }
        }

        _timeInState += CustomPhysics.TimeBetweenTicks;
    }

    void RefreshState(BirdState newState)
    {
        _state = newState;
        _timeInState = Fix64.Zero;
        _timeToNextAction = (Fix64)(Random.Range(7, 75)) / (Fix64)10;
    }
}
