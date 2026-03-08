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
    public ICustomTickState CustomState;

    public bool IsTrigger { get => _isTrigger; }
    [SerializeField] private bool _isTrigger = false;
    public CustomPhysicsBody ParentBody { get=> _parentBody; }
    [SerializeField] private CustomPhysicsBody _parentBody; // if is Trigger, we perform a search for a parent body to move said trigger, we may not always find a parent
    private VoltVector2 _parentBodyPositionOffset;

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
    public Fix64 PositionX
    {
        get => Body.Position.x;
        set => Body.Set(new VoltVector2(value, Body.Position.y), Body.Angle);
    }
    public Fix64 PositionY
    {
        get => Body.Position.y;
        set => Body.Set(new VoltVector2(Body.Position.x, value), Body.Angle);
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

    public VoltBody Body { get; private set; } = new();
    private ulong _desiredEntityId = 0;

    [Header("Configuration")]
    // the fields should not be modified via the inspector checkbox, rather use IsStatic public field via code
    [SerializeField] private IntHundredth _gravity = 0;
    [SerializeField] private IntHundredth _restitution = 0;
    [SerializeField] private IntHundredth _friction = 0;
    [SerializeField] private IntHundredth _mass = 1;

    [Header("Constraints")]
    [SerializeField] private bool _constrainRotation = false;
    [SerializeField] private bool _constrainXPosition = false;
    [SerializeField] private bool _constrainYPosition = false;

    public Action<CustomPhysicsBody> OnTrigger;

    private List<CustomCollider> _colliderList = new();
    private Fix64 _radiansZFix64;
    private VoltVector2 _positionFix64;
    private bool _constructed = false;
    public CustomTransform CustomTransform { get; private set; }

    void Awake()
    {
        CustomTransform = GetComponent<CustomTransform>();

        if(CustomTransform == null)
        {
            Debug.LogError("No CustomTransform attached to physics body, this is a requirement, Object: " + gameObject.name);
            return;
        }
    }

    public void SetEntityId(ulong id)
    {
        _desiredEntityId = id;
        
        if (_constructed)
        {
            Body.EntityId = id;    
        }
    }

    public void Construct()
    {
        if (!enabled)
        {
            return;
        }

        // remove if the body already exists
        if (_constructed)
        {   
            Remove();
        }

        // if this is a trigger, look for a parent body
        if (_parentBody != null)
        {
            _parentBodyPositionOffset =
                new VoltVector2(CustomTransform.GetPositionFix64().x, CustomTransform.GetPositionFix64().y) - 
                new VoltVector2((Fix64)_parentBody.CustomTransform.GetPositionFix64().x, (Fix64)_parentBody.CustomTransform.GetPositionFix64().y);
        }
        
        _radiansZFix64 = CustomTransform.GetRotationRadiansFix64();
        _positionFix64 = CustomTransform.GetPositionFix64();

        List<VoltShape> shapes = new();

        print("create with mass: " + _mass.AsFloat());

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
        Body.EntityId = _desiredEntityId;
        Body.Gravity = new VoltVector2(Fix64.Zero, -(Fix64)_gravity);

        if (_isTrigger)
        {
            Body.IsTrigger = true;

            foreach(VoltShape shape in Body.shapes)
            {
                shape.IsTrigger = true;
            }

            Body.OnCollision += OnInternalCollision;
        }

        Body.Set(_positionFix64, _radiansZFix64);

        CustomPhysicsSpace.Singleton.AddBody(this);

        _constructed = true;

    }

    /// <summary>
    /// Is called if this body is a trigger and another body has overlapped said trigger. Should be extended with .OnTrigger Action
    /// </summary>
    private void OnTriggerCallback(CustomPhysicsBody otherBody)
    {
        OnTrigger?.Invoke(otherBody);
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
        // make body follow parent if parent is assigned
        if(_parentBody != null)
        {
            Fix64 cos = Fix64.Cos(Fix64.Zero);
            Fix64 sin = Fix64.Sin(Fix64.Zero);

            VoltVector2 rotatedOffset = new VoltVector2(
                cos * _parentBodyPositionOffset.x - sin * _parentBodyPositionOffset.y,
                sin * _parentBodyPositionOffset.x + cos * _parentBodyPositionOffset.y
            );
            Body.Set(rotatedOffset + _parentBody.Position, Fix64.Zero);
        }

        // TODO: Update transform position of gameobject
        // TODO: Create enum for interpolation (default should be to true), this will allow simulation to run at specific rate, but interpolation to make it smooth
        
        transform.position = new Vector2((float)Body.Position.x, (float)Body.Position.y);
        transform.rotation = Quaternion.Euler(0, 0, Mathf.Rad2Deg * (float)Body.Angle);

        for(int i = 0; i < Body.shapes.Length; i++)
        {
            VoltVector2 shapePos = Body.shapes[i].bodySpaceAABB.Center;
            _colliderList[i].Offset = shapePos;
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
     

    private void OnInternalCollision(VoltBody bodyA, VoltBody bodyB, VoltVector2 position, VoltVector2 normal, Fix64 penetration)
    {
        var otherVoltBody = bodyA == Body ? bodyB : bodyA;
        var otherBody = CustomPhysicsSpace.Singleton.GetBody(otherVoltBody.EntityId);
        OnTriggerCallback(otherBody);
    }

    private void OnDestroy()
    {
        Remove();
    }
    private void OnDisable()
    {
        Remove();
    }

    private void Remove()
    {
        if (!_constructed)
        {
            return;
        }
        
        
        CustomPhysicsSpace.Singleton.SimulationSpace.RemoveBody(Body);
        CustomPhysicsSpace.Singleton.RemoveBody(this);
        _constructed = false;
    }

}
