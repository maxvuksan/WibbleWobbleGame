using FixMath.NET;
using UnityEngine;
using Volatile;
using static CustomMaths;

public class CustomSpring : CustomConstraint
{   
    [SerializeField] private bool _useImpulseSolver = true; // this makes it behave like a joint, rather than a spring
    [SerializeField] private bool _calculateRestLengthAtStart = true;
    private bool _restLengthCalculated = false;

    public SpringData configuration;    
    [SerializeField] private IntHundredthVector2 bodyAAttachmentOffset;
    [SerializeField] private IntHundredthVector2 bodyBAttachmentOffset;


    override public void Awake()
    {
        base.Awake();       
        CustomPhysics.OnPhysicsTick += OnPhysicsTick;
        CustomPhysics.OnStartPhysicsSimulation += OnStartPhysicsSimulation;
    }
    override public void OnDestroy()
    {
        base.OnDestroy();    
        CustomPhysics.OnPhysicsTick -= OnPhysicsTick;
        CustomPhysics.OnStartPhysicsSimulation -= OnStartPhysicsSimulation;
    }

    private void OnStartPhysicsSimulation()
    {
        _restLengthCalculated = false;
    }

    private void OnPhysicsTick()
    {
        if(bodyA != null && bodyB != null)
        {
            if(_calculateRestLengthAtStart && CustomPhysics.Tick == 1)
            {
                Fix64 distance = VoltVector2.Distance(GetStartAnchorPosition(), GetEndAnchorPosition());
                configuration.restLength = new IntHundredth(distance);
                _restLengthCalculated = true;
            }
        }
    }

    override public void ApplySubStep(Fix64 deltaTime)
    {

        if(bodyA == null || bodyB == null || !_restLengthCalculated)
        {
            return;
        }

        VoltVector2 p1 = GetStartAnchorPosition();
        VoltVector2 p2 = GetEndAnchorPosition();

        VoltVector2 r1 = p1 - bodyA.Position;
        VoltVector2 r2 = p2 - bodyB.Position;

        VoltVector2 delta = p2 - p1;
        Fix64 distance = delta.magnitude;
        if (distance < (Fix64)0.001f) return;

        VoltVector2 direction = new VoltVector2(delta.x / distance, delta.y / distance);

        Fix64 distError = distance - (Fix64)configuration.restLength;
        
        Fix64 r1CrossN = CrossProduct(r1, direction);
        Fix64 r2CrossN = CrossProduct(r2, direction);
        
        Fix64 effectiveMass = bodyA.InverseMass + bodyB.InverseMass + 
                            r1CrossN * r1CrossN * bodyA.InverseInertia + 
                            r2CrossN * r2CrossN * bodyB.InverseInertia;
        
        if (effectiveMass < (Fix64)0.0001f) return;

        // position correction
        Fix64 baumgarte = (Fix64)0.1f;
        Fix64 lambda = -distError * baumgarte / effectiveMass;

        VoltVector2 impulse = direction * lambda;

        bodyA.Body.ApplyImpulse(-impulse, r1);
        bodyB.Body.ApplyImpulse(impulse, r2);
    }

    public VoltVector2 GetStartAnchorPosition()
    {
        return RotatePoint(bodyAAttachmentOffset, bodyA.Angle) + bodyA.Position;
    }

    public VoltVector2 GetEndAnchorPosition()
    {
        return RotatePoint(bodyBAttachmentOffset, bodyB.Angle) + bodyB.Position;
    }

    private void OnDrawGizmos()
    {
        if (bodyA == null || bodyB == null) return;

        Vector2 pointA = bodyA.transform.TransformPoint(bodyAAttachmentOffset.AsVector2());
        Vector2 pointB = bodyB.transform.TransformPoint(bodyBAttachmentOffset.AsVector2());
         
        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(pointA, (Vector2)bodyA.transform.position);
        Gizmos.DrawLine(pointB, (Vector2)bodyB.transform.position);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(pointA, 0.1f);
        Gizmos.DrawWireSphere(pointB, 0.1f);
        Gizmos.DrawLine(pointA, pointB);
    }
}