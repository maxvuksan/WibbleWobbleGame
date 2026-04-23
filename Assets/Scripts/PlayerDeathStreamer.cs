using System;
using FixMath.NET;
using UnityEngine;
using Volatile;

public class PlayerDeathStreamer : MonoBehaviour
{
    [SerializeField] private float _radius;
    [SerializeField] private float _initalSpeed;
    [SerializeField] private float _gravityForce = 1;
    
    [Range(0, 1)]
    [SerializeField] private float _speedScalePerBounce = 0.7f;
    private Vector2 _velocity;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        CustomPhysics.OnPostPhysicsTick += OnPostPhysicsTick;
    
        _velocity = new Vector2(CustomRandom.Float(-_initalSpeed, _initalSpeed), CustomRandom.Float(-_initalSpeed, _initalSpeed));
    }

    void OnDestroy()
    {
        CustomPhysics.OnPostPhysicsTick -= OnPostPhysicsTick;
    }

    public void SetColour(Color colour)
    {
        GetComponent<TrailRenderer>().startColor = colour;
        GetComponent<TrailRenderer>().endColor = colour;
    }

    void OnPostPhysicsTick()
    {
        VoltVector2 velocity = new VoltVector2((Fix64)_velocity.x, (Fix64)_velocity.y);
        VoltVector2 position = new VoltVector2((Fix64)transform.position.x, (Fix64)transform.position.y);
        
        var raycastResult = CustomPhysics.Raycast(
            position, 
            velocity.normalized,(Fix64)(_radius));

        if (raycastResult.Hit)
        {
            VoltVector2 reflectVelocity = CustomMaths.Reflect(velocity, raycastResult.Normal);

            float mag = _velocity.magnitude;
            _velocity = new Vector2((float)reflectVelocity.x * mag * _speedScalePerBounce, (float)reflectVelocity.y * mag * _speedScalePerBounce);
        }

        _velocity.y -= _gravityForce * (float)CustomPhysics.TimeBetweenTicks;
    }

    void Update()
    {
        transform.position += new Vector3(_velocity.x, _velocity.y, 0) * Time.deltaTime;
    }

    void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(transform.position, _radius);
    }
}
