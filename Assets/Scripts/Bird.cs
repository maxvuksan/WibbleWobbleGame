using System;
using FixMath.NET;
using UnityEngine;

[System.Serializable]
public class BirdConfig
{
    public float ScareDistance = 5.0f;
    public float ScareByAnotherBirdDistance = 2.0f;
    public float ScareHorizontalMilage = 20.0f; // how far the bird flys away horizontally
    public float ScareHorizontalMilageVariance = 15.0f; // random error around the milage value
    public float TimeOffscreen = 3.0f;
    public float DissapearHeight = 30.0f; // how high the bird flys to dissapear
    public float FlySpeedMin = 25.0f; 
    public float FlySpeedMax = 80.0f; 
    public float FlyLandingSpeed = 20.0f; 
    public float FlySpeedVariance = 3.0f; 
    public float FlySpeedIncreaseRate = 100.0f;
    public float FlySpeedIncreaseRateVariance = 20.0f;
    public float WaveAmplitude = 2.0f; // How high/low the waves are
    public float WaveFrequency = 2.0f; // How many waves per second
}

public class Bird : MonoBehaviour
{
    public enum BirdState
    {
        Idle, // stand still
        Pekking, // pick the ground
        FlyingAway, // move to a nearby location
        FlyingToLand, 
        Offscreen, // the bird has flown off screen
    }

    [HideInInspector] public BirdConfig BirdConfig;

    private Animator _animator;
    private SpriteRenderer _spriteRenderer; 
    private BirdState _state;
    private float _timeInState;
    private float _timeToNextAction;

    private Vector2 _flyPosition;
    private float _flySpeed;
    private float _flySpeedIncreaseRate;
    private float _waveTime; 
    private float _timeOffscreen;

    private static Action<Vector2> s_OnAnotherBirdTakesOff;

    void Awake()
    {
        _animator = GetComponentInChildren<Animator>();
        _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        _spriteRenderer.flipX = false;
        CustomPhysics.OnPhysicsTick += OnPhysicsTick;

        s_OnAnotherBirdTakesOff += OnAnotherBirdTakesOff;

        RefreshState(BirdState.Idle);
    }

    void OnDestroy()
    {
        CustomPhysics.OnPhysicsTick -= OnPhysicsTick;
        s_OnAnotherBirdTakesOff -= OnAnotherBirdTakesOff;
    }

    void OnAnotherBirdTakesOff(Vector2 birdPosition)
    {   
        if(!CanBeScared())
        {
            return;    
        }

        if(Vector2.Distance(transform.position, birdPosition) < BirdConfig.ScareByAnotherBirdDistance)
        {
            // make propogated birdsd less loud
            ScareAway(birdPosition, 0.5f);  
        }
    }

    private bool CanBeScared()
    {
        return _state == BirdState.Idle || _state == BirdState.Pekking;
    }

    void DetectScareAway()
    {
        if(!CanBeScared())
        {
            return;
        }

        var result = CustomPhysics.OverlapCircle(new Volatile.VoltVector2((Fix64)transform.position.x, (Fix64)transform.position.y), (Fix64)BirdConfig.ScareDistance);
    
        foreach(var body in result.Bodies)
        {
            if(body.BodyType == CustomBodyType.Dynamic)
            {
                ScareAway(body.transform.position);
                break;
            }
        }
    }

    void ScareAway(Vector2 bodyPosition, float volumeScaler = 1.0f)
    {
        if(_state == BirdState.FlyingAway)
        {
            return;
        }

        AudioManager.Singleton.Play("BirdFlyAway", transform.position, volumeScaler);

        RefreshState(BirdState.FlyingAway);

        // fly offscreen

        float randomXOffset = CustomRandom.Float(-BirdConfig.ScareHorizontalMilageVariance, BirdConfig.ScareHorizontalMilageVariance);
        randomXOffset += BirdConfig.ScareHorizontalMilage;

        if(bodyPosition.x < transform.position.x)
        {
            _flyPosition.x = randomXOffset;
        }
        else
        {
            _flyPosition.x = -randomXOffset;
        }
        _flyPosition.y = BirdConfig.DissapearHeight;
        _flyPosition.x += transform.position.x;

        if(CustomRandom.Float(0, 1) > 0.75f){
            _flyPosition.x = -_flyPosition.x;
        }

        FlipSpriteDependingOnFlyPosition();

        _flySpeed = CustomRandom.Float(-BirdConfig.FlySpeedVariance, BirdConfig.FlySpeedVariance);
        _flySpeed += BirdConfig.FlySpeedMin;

        _flySpeedIncreaseRate = CustomRandom.Float(-BirdConfig.FlySpeedIncreaseRateVariance, BirdConfig.FlySpeedIncreaseRateVariance);
        _flySpeedIncreaseRate += BirdConfig.FlySpeedIncreaseRate;

        _waveTime = 0f; // Reset wave time

    }

    void OnPhysicsTick()
    {
        DetectScareAway();
    }

    void Update()
    {

        switch (_state)
        {
            case BirdState.Idle: {
                
                if(_timeInState <= _timeToNextAction)
                {
                    break;
                }

                int randResult = CustomRandom.Int(0, 3);

                switch (randResult)
                {
                    case 0:
                    {
                        SetSpriteFlipped(-_spriteRenderer.transform.localScale.x);
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

            case BirdState.FlyingAway: {
                
                _animator.SetBool("Flying", true);
                
                Vector2 straightLinePos = Vector2.MoveTowards(transform.position, _flyPosition, _flySpeed * Time.deltaTime);
                transform.position = straightLinePos;
                if(straightLinePos == _flyPosition)
                {
                    RefreshState(BirdState.Offscreen);
                    break;
                }

                ApplySinWaveToPosition();

                _flySpeed += _flySpeedIncreaseRate * Time.deltaTime;
                if(_flySpeed > BirdConfig.FlySpeedMax)
                {
                    _flySpeed = BirdConfig.FlySpeedMax;
                }

                break;
            }
            case BirdState.FlyingToLand: {

                _animator.SetBool("Flying", true);

                float distance = Vector2.Distance(transform.position, _flyPosition);
                    
                if(distance < BirdConfig.ScareDistance)
                {
                    _flySpeed = Mathf.Lerp(3.0f, BirdConfig.FlyLandingSpeed, distance / BirdConfig.ScareDistance);
                }
                else{
                    _flySpeed = BirdConfig.FlyLandingSpeed;
                }

                Vector2 straightLinePos = Vector2.MoveTowards(transform.position, _flyPosition, _flySpeed * Time.deltaTime);
                transform.position = straightLinePos;
                if(straightLinePos == _flyPosition)
                {
                    RefreshState(BirdState.Idle);
                    break;
                }

                ApplySinWaveToPosition();

                break;
            }
            case BirdState.Offscreen:
            {
                if(_timeInState <= _timeOffscreen)
                {
                    break;
                }

                // lets find a place to land
                Vector2 landingPosition = BirdManager.Singleton.FindNewPerchSpot(gameObject, false);
                if(landingPosition.y != BirdConfig.DissapearHeight)
                {
                    _flyPosition = landingPosition;
                    FlipSpriteDependingOnFlyPosition();
                    RefreshState(BirdState.FlyingToLand);
                }

                // reset timer if couldn't find a place
                _timeInState = 0;

                break;
            }
        }

        _timeInState += Time.deltaTime;
    }

    void FixedUpdate()
    {
        // no need to scare other burds every update frame, can do this in a fixed manner
        if(_state == BirdState.FlyingAway)
        {
            s_OnAnotherBirdTakesOff?.Invoke(transform.position);
        }
    }

    void FlipSpriteDependingOnFlyPosition()
    {
        if(transform.position.x > _flyPosition.x)
        {
            SetSpriteFlipped(-1);
        }
        else
        {
            SetSpriteFlipped(1);
        }
    }

    void SetSpriteFlipped(float xScale)
    {
        _spriteRenderer.transform.localScale = new Vector3(xScale, 1, 1);
    }

    void ApplySinWaveToPosition()
    {
        _waveTime += Time.deltaTime;
        float waveOffset = Mathf.Sin(_waveTime * BirdConfig.WaveFrequency) * BirdConfig.WaveAmplitude;
        transform.position = new Vector2(transform.position.x, transform.position.y + waveOffset);

    }

    void RefreshState(BirdState newState)
    {
        if(_state != BirdState.FlyingAway || _state != BirdState.FlyingToLand)
        {
            _animator.SetBool("Flying", false);
        }

        _timeOffscreen = CustomRandom.Float(BirdConfig.TimeOffscreen, BirdConfig.TimeOffscreen * 2);
        _waveTime = 0;
        _state = newState;
        _timeInState = 0;
        _timeToNextAction = CustomRandom.Int(7, 75) / 10.0f;
    }
}
