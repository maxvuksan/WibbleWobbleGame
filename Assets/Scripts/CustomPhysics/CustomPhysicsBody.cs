using System;
using System.Collections.Generic;
using FixMath.NET;
using UnityEngine;
using Volatile;


public enum CustomBodyType : byte
{
    Static,
    Dynamic,
    Kinematic,
}


public class CustomPhysicsBody : MonoBehaviour
{
    
    /// <summary>
    /// Determines whether the physics body can move and rotate in response to forces
    /// </summary>
    public CustomBodyType BodyType = CustomBodyType.Static;

    public Fix64 Angle
    {
        get => Body.Angle;
        set => Body.Set(Body.Position, value);
    }
    public Fix64 AngularVelocity
    {
        get => Body.AngularVelocity;
    }

    public VoltVector2 Position
    {
        get => Body.Position;
        set => Body.Set(value, Body.Angle);
    }

    public VoltVector2 LinearVelocity
    {
        get => Body.LinearVelocity;
        set => SetVelocity(value);
    }
    public Fix64 LinearVelocityX
    {   
        get => Body.LinearVelocity.x;
        set => SetVelocityX(value);
    }
    public Fix64 LinearVelocityY
    {
        get => Body.LinearVelocity.y;
        set => SetVelocityY(value);
    }

    public VoltBody Body { get; private set; }


    [Header("Configuration")]
    // the fields should not be modified via the inspector checkbox, rather use IsStatic public field via code
    [SerializeField] private float _gravity = 1;
    [SerializeField] private float _restitution = 0.05f;
    [SerializeField] private float _friction = 0;
    [SerializeField] private float _mass = 1;

    [Header("Constraints")]
    [SerializeField] private bool _constrainRotation = false;
    [SerializeField] private bool _constrainXPosition = false;
    [SerializeField] private bool _constrainYPosition = false;

    private List<CustomCollider> _colliderList = new();
    private Fix64 _radiansZFix64;
    private VoltVector2 _positionFix64;


    private void Start()
    {
        _radiansZFix64 = (Fix64)(transform.eulerAngles.z * Mathf.Deg2Rad);
        _positionFix64 = new VoltVector2((Fix64)transform.position.x, (Fix64)transform.position.y);

        List<VoltShape> shapes = new();

        foreach(var collider in _colliderList)
        {
            collider.ConstructShape();
            shapes.Add(collider.GetShape());
        }

        foreach(var shape in shapes)
        {
            shape.Restitution = (Fix64)_restitution;
            shape.Friction = (Fix64)_friction;
        }

        Fix64 mass = (Fix64)_mass;

        if (BodyType == CustomBodyType.Static)
        {
            Body = CustomPhysicsSpace.Singleton.SimulationSpace.CreateStaticBody(_positionFix64, _radiansZFix64, shapes.ToArray());
        }
        else if(BodyType == CustomBodyType.Dynamic || BodyType == CustomBodyType.Kinematic)
        {
            Body = CustomPhysicsSpace.Singleton.SimulationSpace.CreateDynamicBody(_positionFix64, _radiansZFix64, shapes.ToArray());

            if(BodyType == CustomBodyType.Kinematic)
            {
                mass = Fix64.MaxValue;
            }
        }

        Body.Mass = mass;
        Body.IsFixedAngle = _constrainRotation;
        Body.IsFixedPositionX = _constrainXPosition;
        Body.IsFixedPositionY = _constrainYPosition;
        Body.Gravity = new VoltVector2(Fix64.Zero, -(Fix64)_gravity);

        Body.Set(_positionFix64, _radiansZFix64);

        CustomPhysicsSpace.Singleton.AddBody(this);
    }

    private void OnDestroy()
    {
        CustomPhysicsSpace.Singleton.RemoveBody(this);
    }

    /// <summary>
    /// Associates a collision shape (collider) with this body
    /// </summary>
    /// <param name="colliderShape"></param>
    public void AddCollider(CustomCollider colliderShape)
    {
        _colliderList.Add(colliderShape);
    }


    /// <summary>
    /// Is called after the physics space has performed a step of the simulation. This function will update
    /// the transform position of the gameobject
    /// </summary>
    public void ApplySimulationToGameObject()
    {
        // TODO: Update transform position of gameobject
        // TODO: Create enum for interpolation (default should be to true), this will allow simulation to run at specific rate, but interpolation to make it smooth
        
        transform.position = new Vector2((float)Body.Position.x, (float)Body.Position.y);
        transform.rotation = Quaternion.Euler(0, 0, Mathf.Rad2Deg * (float)Body.Angle);

        for(int i = 0; i < Body.shapes.Length; i++)
        {
            VoltVector2 shapePos = Body.shapes[i].bodySpaceAABB.Center;
            _colliderList[i].Offset = new Vector2((float)shapePos.x, (float)shapePos.y);
        }
    }

    public void AddForce(VoltVector2 force)
    {
        Body.AddForce(force);
    }
    public void AddForceX(Fix64 force)
    {
        Body.AddForce(new VoltVector2(force, Fix64.Zero));
    }
    public void AddForceY(Fix64 force)
    {
        Body.AddForce(new VoltVector2(Fix64.Zero, force));
    }
    public void AddTorque(Fix64 torque)
    {
        Body.AddTorque(torque);
    }
    public void SetVelocity(VoltVector2 velocity)
    {
        Body.LinearVelocity = velocity;
    }
    public void SetVelocityX(Fix64 velocityX)
    {
        Body.LinearVelocity = new VoltVector2(velocityX, Body.LinearVelocity.y);
    }
    public void SetVelocityY(Fix64 velocityY)
    {
        Body.LinearVelocity = new VoltVector2(Body.LinearVelocity.x, velocityY);
    }
     

}
