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
    }
    public Fix64 InverseInertia
    {
        get => Body.InvInertia;
    }
    public Fix64 InverseMass
    {
        get => Body.InvMass;
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


    /// <summary>
    /// Determines whether the physics body can move and rotate in response to forces
    /// </summary>
    public CustomBodyType BodyType = CustomBodyType.Static;
    public ICustomTickState CustomState;

    public bool IsTrigger { get => _isTrigger; }
    [SerializeField] private bool _isTrigger = false;

    public CustomPhysicsBody ParentBody { get=> _parentBody; }
    [SerializeField] private CustomPhysicsBody _parentBody; // if is Trigger, we perform a search for a parent body to move said trigger, we may not always find a parent
    [SerializeField] private bool _ignoreParentBodyRotation = false;
    [SerializeField] private VoltVector2 _parentBodyPositionOffset;

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
    public Action<CustomPhysicsBody> OnCollide;

    private List<CustomCollider> _colliderList = new();
    private Fix64 _radiansZFix64;
    private VoltVector2 _positionFix64;
    private bool _constructed = false;
    public CustomTransform CustomTransform { get; private set; }

    /// <summary>
    /// A flag used to disable interpolation for a single physics tick update, this should be used when we want to manually set the position of the body 
    /// </summary>
    private bool _skipInterpolation = false;
    private bool _turnOnInterpolatioNextTick = false;

    /// <summary>
    /// Used to perform visual interpolation of the transform to smooth out the physics simulation. 
    /// Note: we can use floats here because this is purley driving visuals not the actual simulation
    /// </summary>
    private Vector2 _previousPositionForLerp;
    private Vector2 _currentPositionForLerp;
    private float _previousAngleForLerp;
    private float _currentAngleForLerp;


    void Awake()
    {
        AssignCustomTransform();
    }

    private void AssignCustomTransform()
    {
        CustomTransform = GetComponent<CustomTransform>();

        if(CustomTransform == null)
        {
            Debug.LogError("No CustomTransform attached to physics body, this is a requirement, Object: " + gameObject.name);
            return;
        }
    }

    /// <summary>
    /// Updates the parent body offset if this body has a parent, this is calculated at editor time, not runtime
    /// </summary>
    private void OnValidate()
    {
        // Only run in editor mode
        if (Application.isPlaying)
            return;
            
        AssignCustomTransform();

        // Calculate offset from parent if parent is assigned
        if (_parentBody != null && CustomTransform != null)
        {
            // Get parent's CustomTransform
            var parentTransform = _parentBody.GetComponent<CustomTransform>();
            
            if (parentTransform != null)
            {
                // Calculate offset in editor
                VoltVector2 myPosition = CustomTransform.GetPositionFix64();
                VoltVector2 parentPosition = parentTransform.GetPositionFix64();
                _parentBodyPositionOffset = myPosition - parentPosition;
                Debug.Log("Setting parent body position: " + _parentBodyPositionOffset.y + " on object of name " + gameObject.name);

            }
        }
    }

    void Update()
    {
        InterpolateTransform();
    }

    public void SetDesiredEntityId(ulong id)
    {
        _desiredEntityId = id;
        
        if (_constructed)
        {
            Body.EntityId = id;    
        }
    }

    public ulong GetDesiredEntityId()
    {
        return _desiredEntityId;
    }

    private void SortColliderList()
    {
        _colliderList.Sort((a, b) => 
        {
            // Get world positions: body position + collider offset
            VoltVector2 posA = _positionFix64 + a.Offset;
            VoltVector2 posB = _positionFix64 + b.Offset;
            
            // Sort by X coordinate first
            int xCompare = posA.x.CompareTo(posB.x);
            if (xCompare != 0) 
                return xCompare;
            
            // Then by Y coordinate
            int yCompare = posA.y.CompareTo(posB.y);
            if (yCompare != 0)
                return yCompare;
            
            // If positions are identical (shouldn't happen), sort by name as tiebreaker
            return a.GetInstanceID().CompareTo(b.GetInstanceID());
        });
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
        
        _radiansZFix64 = CustomTransform.GetRotationRadiansFix64();
        _positionFix64 = CustomTransform.GetPositionFix64();

        // shapes must be sorted by position, to ensure when a body is created on different devices the shapes are in the same order
        SortColliderList();

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
        Body.EntityId = _desiredEntityId;
        Body.Gravity = new VoltVector2(Fix64.Zero, -(Fix64)_gravity);

        if (_isTrigger)
        {
            Body.IsTrigger = true;
            Body.IgnoreRaycasts = true;

            foreach(VoltShape shape in Body.shapes)
            {
                shape.IsTrigger = true;
                Body.IgnoreRaycasts = true;
            }
        }

        Body.OnCollision += OnInternalCollision;

        Body.Set(_positionFix64, _radiansZFix64);

        CustomPhysicsSpace.Singleton.AddBody(this);

        _constructed = true;

        ApplySimulationToGameObject();

    }

    /// <summary>
    /// Logs the order of the attached colliders
    /// </summary>
    public void LogShapeOrder()
    {
        int i = 0;
        string info = "";
        foreach(var collider in _colliderList)
        {
            info += collider.GetType().ToString() + " with index: " + i + " and OffsetX: " + collider.Offset.x + "and OffsetY: " + collider.Offset.y + "\n";
            i++;
        }
        DeterminismLogger.LogExtraInfo(info);
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
        if(!_constructed || !enabled)
        {
            return;
        }

        ApplySimulationToBody();
        ApplySimulationInterpolation();
    }


    public VoltVector2 GetInitalPositionAccountingForParent()
    {
        Fix64 cos = Fix64.Cos(_parentBody.GetComponent<CustomTransform>().GetRotationRadiansFix64());
        Fix64 sin = Fix64.Sin(_parentBody.GetComponent<CustomTransform>().GetRotationRadiansFix64());

        VoltVector2 rotatedOffset = new VoltVector2(
                cos * CustomTransform.GetPositionFix64().x - sin * CustomTransform.GetPositionFix64().y,
                sin * CustomTransform.GetPositionFix64().x + cos * CustomTransform.GetPositionFix64().y
        );

        return _parentBody.GetComponent<CustomTransform>().GetPositionFix64() + rotatedOffset;
    }

    public void ApplySimulationToBody()
    {

        // make body follow parent if parent is assigned
        if(_parentBody != null)
        {
            Fix64 cos = Fix64.Cos(_parentBody.Angle);
            Fix64 sin = Fix64.Sin(_parentBody.Angle);

            VoltVector2 rotatedOffset = new VoltVector2(
                 cos * CustomTransform.GetPositionFix64().x - sin * CustomTransform.GetPositionFix64().y,
                 sin * CustomTransform.GetPositionFix64().x + cos * CustomTransform.GetPositionFix64().y
            );
            
            Fix64 angle = Body.Angle;

            // on first tick configure angle...
            if(CustomPhysics.Tick == 0)
            {
                if (!_ignoreParentBodyRotation)
                {
                    angle += _parentBody.Angle;
                }
            }

            Body.Set(_parentBody.Position + rotatedOffset, angle);
        }
        for(int i = 0; i < Body.shapes.Length; i++)
        {
            VoltVector2 shapePos = Body.shapes[i].bodySpaceAABB.Center;
            _colliderList[i].Offset = shapePos;
        }
    }

    /// <summary>
    /// Applies the current simulation state to our internal interpolation variables
    /// </summary>
    public void ApplySimulationInterpolation()
    {
        if (_turnOnInterpolatioNextTick)
        {
            _skipInterpolation = false;
            _turnOnInterpolatioNextTick = false;
        }


        if (!ShouldInterpolate())
        {
            // Store previous frame's current as new previous
            _previousPositionForLerp = _currentPositionForLerp;
            _previousAngleForLerp = _currentAngleForLerp;
            
            // Store new current position from physics
            _currentPositionForLerp = new Vector2((float)Body.Position.x, (float)Body.Position.y);
            _currentAngleForLerp = Mathf.Rad2Deg * (float)Body.Angle;
        }
        else
        {
            // No interpolation; direct update
            transform.position = new Vector2((float)Body.Position.x, (float)Body.Position.y);
            transform.rotation = Quaternion.Euler(0, 0, Mathf.Rad2Deg * (float)Body.Angle);

            _turnOnInterpolatioNextTick = true;
        }
    }

    /// <summary>
    /// Interpolates the transform position and rotation between the last physics tick and the current physics tick 
    /// </summary>
    public void InterpolateTransform()
    {
        if (!_constructed || Body.IsStatic)
        {
            return;
        }


        if (!ShouldInterpolate())
        {
            float t = CalculateInterpolationTValue();
            
            transform.position = Vector2.Lerp(_previousPositionForLerp, _currentPositionForLerp, t);
            
            float interpolatedAngle = Mathf.LerpAngle(_previousAngleForLerp, _currentAngleForLerp, t);
            transform.rotation = Quaternion.Euler(0, 0, interpolatedAngle);
        }
    }

    public bool ShouldInterpolate()
    {
        return !_skipInterpolation && CustomPhysicsSpace.Singleton.VisualInterpolation && CustomPhysics.Tick > 0;
    }

    /// <summary>
    /// Calculates the value t fraction for lerping between the previous physics tick and current physics tick
    /// </summary>
    private float CalculateInterpolationTValue()
    {
        double timeSinceLastTick = Time.realtimeSinceStartupAsDouble - CustomPhysics.LastPhysicsTickTime;
        float t = (float)(timeSinceLastTick / (double)CustomPhysics.TimeBetweenTicks);
        
        // Clamp to [0, 1] to prevent extrapolation
        return Mathf.Clamp01(t);
    }

    /// <summary>
    /// Sets the position of the body aswell as the internal CustomTransform. This call will also disable visaul interpolation for the next tick
    /// </summary>
    /// <param name="position">The position to set</param>
    public void TeleportPosition(IntHundredthVector2 position)
    {
        CustomTransform.SetValues(position.X, position.Y, CustomTransform.RotationDegreesHundredth);

        if(Body != null)
        {
            Body.Set(new VoltVector2((Fix64)position.X, (Fix64)position.Y), Body.Angle);
        }

        SetSkipInterpolationNextFrame(true);
    }

    public void SetSkipInterpolationNextFrame(bool skipInterpolationNextFrame)
    {
        if (skipInterpolationNextFrame)
        {
            transform.position = new Vector2((float)Body.Position.x, (float)Body.Position.y);

            _previousPositionForLerp = transform.position;
            _currentPositionForLerp = transform.position;

            _skipInterpolation = true;
            _turnOnInterpolatioNextTick = false;
        }
        else
        {
            _skipInterpolation = false;
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

        if (IsTrigger)
        {
            OnTrigger?.Invoke(otherBody);
        }
        {
            OnCollide?.Invoke(otherBody);
        }
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
