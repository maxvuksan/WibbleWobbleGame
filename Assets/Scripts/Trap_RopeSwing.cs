using FixMath.NET;
using Steamworks;
using UnityEngine;
using Volatile;

public class Trap_RopeSwing : TrapHeader
{
    [SerializeField] private SpringData springData;
    [SerializeField] private RopeVisual ropeVisual;

    [SerializeField] private CustomTransform startAnchor;
    [SerializeField] private CustomTransform endAnchor;

    private CustomPhysicsBody bodyA;
    private CustomPhysicsBody bodyB;

    void Awake()
    {
        CustomPhysics.OnPrePhysicsTick += OnPrePhysicsTick;
        CustomPhysics.OnPhysicsTick += OnPhysicsTick;
    }
    void OnDestroy()
    {
        CustomPhysics.OnPrePhysicsTick -= OnPrePhysicsTick;
        CustomPhysics.OnPhysicsTick -= OnPhysicsTick;
    }

    void OnPrePhysicsTick()
    {
        // on first tick, attach to bodies
        if(CustomPhysics.Tick == 1)
        {
            VoltVector2 pointA = startAnchor.GetPositionFix64();
            VoltVector2 pointB = endAnchor.GetPositionFix64();

            // spring length is 85% of distance
            springData.restLength.ValueHundredths = (int)Fix64.Round(VoltVector2.Distance(pointA, pointB) * (Fix64)85); 
        
            var resultA = CustomPhysics.Raycast(pointA, new VoltVector2(Fix64.Zero, Fix64.One), (Fix64)0.1f);
            var resultB = CustomPhysics.Raycast(pointB, new VoltVector2(Fix64.Zero, Fix64.One), (Fix64)0.1f);

            bodyA = resultA.Body;
            bodyB = resultB.Body;
        }
    }

    void OnPhysicsTick()
    {
        if(bodyA == null || bodyB == null)
        {
            return;
        }

        bodyA.LinearVelocity = Spring.CalculateForce(bodyA.Position, bodyB.Position, bodyA.LinearVelocity, bodyB.LinearVelocity, springData);
        bodyB.LinearVelocity = Spring.CalculateForce(bodyB.Position, bodyA.Position, bodyB.LinearVelocity, bodyA.LinearVelocity, springData);
    }

    void Update()
    {
        if(bodyA == null || bodyB == null)
        {
            return;
        }

        ropeVisual.SetPoint(0, bodyA.Position);
        ropeVisual.SetPoint(1, bodyB.Position);
    }

}
