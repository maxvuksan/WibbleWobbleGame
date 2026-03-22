using System.Text;
using Volatile;
using FixMath.NET;

public static class VoltWorldDumper
{
    public static string DumpCompleteState(VoltWorld world)
    {
        StringBuilder sb = new StringBuilder();
        
        sb.AppendLine("=== VOLTWORLD STATE DUMP ===\n");
        
        // World-level properties
        sb.AppendLine("--- World Properties ---");
        sb.AppendLine($"DeltaTime: {world.DeltaTime.RawValue}");
        sb.AppendLine($"IterationCount: {world.IterationCount}");
        sb.AppendLine($"Elasticity: {world.Elasticity.RawValue}");
        sb.AppendLine($"LinearDamping: {world.LinearDamping.RawValue}");
        sb.AppendLine($"AngularDamping: {world.AngularDamping.RawValue}");
        sb.AppendLine($"Gravity: ({world.Gravity.x.RawValue}, {world.Gravity.y.RawValue})");
        sb.AppendLine();
        
        // All bodies
        sb.AppendLine("--- Bodies ---");
        int bodyIndex = 0;
        foreach (var body in world.Bodies)
        {
            sb.AppendLine($"Body[{bodyIndex}] EntityId={body.EntityId}:");
            sb.AppendLine($"  Position: ({body.Position.x.RawValue}, {body.Position.y.RawValue})");
            sb.AppendLine($"  Velocity: ({body.LinearVelocity.x.RawValue}, {body.LinearVelocity.y.RawValue})");
            sb.AppendLine($"  Angle: {body.Angle.RawValue}");
            sb.AppendLine($"  AngularVelocity: {body.AngularVelocity.RawValue}");
            sb.AppendLine($"  Force: ({body.Force.x.RawValue}, {body.Force.y.RawValue})");
            sb.AppendLine($"  Torque: {body.Torque.RawValue}");
            sb.AppendLine($"  BiasVelocity: ({body.BiasVelocity.x.RawValue}, {body.BiasVelocity.y.RawValue})");
            sb.AppendLine($"  BiasRotation: {body.BiasRotation.RawValue}");
            sb.AppendLine($"  Mass: {body.Mass.RawValue}");
            sb.AppendLine($"  Inertia: {body.Inertia.RawValue}");
            sb.AppendLine($"  IsStatic: {body.IsStatic}");
            sb.AppendLine($"  IsEnabled: {body.IsEnabled}");
            sb.AppendLine($"  IsFixedAngle: {body.IsFixedAngle}");
            sb.AppendLine($"  IsFixedPositionX: {body.IsFixedPositionX}");
            sb.AppendLine($"  IsFixedPositionY: {body.IsFixedPositionY}");
            sb.AppendLine($"  LinearDamping: {body.LinearDamping.RawValue}");
            sb.AppendLine($"  AngularDamping: {body.AngularDamping.RawValue}");
            sb.AppendLine($"  AABB: T={body.AABB.Top.RawValue} B={body.AABB.Bottom.RawValue} L={body.AABB.Left.RawValue} R={body.AABB.Right.RawValue}");
            sb.AppendLine($"  ShapeCount: {body.shapeCount}");
            
            // Shapes
            int shapeIdx = 0;
            foreach (var shape in body.shapes)
            {
                sb.AppendLine($"    Shape[{shapeIdx}]: Type={shape.GetType().Name}");
                sb.AppendLine($"      AABB: T={shape.AABB.Top.RawValue} B={shape.AABB.Bottom.RawValue} L={shape.AABB.Left.RawValue} R={shape.AABB.Right.RawValue}");
                shapeIdx++;
            }
            
            bodyIndex++;
            sb.AppendLine();
        }
        
        // Manifolds (if accessible)
        try
        {
            sb.AppendLine("--- Manifolds ---");
            var manifoldsField = typeof(VoltWorld).GetField("manifolds", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (manifoldsField != null)
            {
                var manifolds = manifoldsField.GetValue(world) as System.Collections.IList;
                if (manifolds != null)
                {
                    sb.AppendLine($"Manifold Count: {manifolds.Count}");
                    
                    for (int i = 0; i < manifolds.Count; i++)
                    {
                        var manifold = manifolds[i];
                        var manifoldType = manifold.GetType();
                        
                        var shapeA = manifoldType.GetProperty("ShapeA")?.GetValue(manifold);
                        var shapeB = manifoldType.GetProperty("ShapeB")?.GetValue(manifold);
                        
                        if (shapeA != null && shapeB != null)
                        {
                            var bodyA = shapeA.GetType().GetProperty("Body")?.GetValue(shapeA) as VoltBody;
                            var bodyB = shapeB.GetType().GetProperty("Body")?.GetValue(shapeB) as VoltBody;
                            
                            sb.AppendLine($"  Manifold[{i}]: BodyA={bodyA?.EntityId} BodyB={bodyB?.EntityId}");
                        }
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            sb.AppendLine($"Could not access manifolds: {e.Message}");
        }
        
        sb.AppendLine();
        sb.AppendLine("=== END STATE DUMP ===");
        
        return sb.ToString();
    }
    
    public static string ComputeStateHash(VoltWorld world)
    {
        long hash = 0;
        
        foreach (var body in world.Bodies)
        {
            hash ^= body.Position.x.RawValue * 31;
            hash ^= body.Position.y.RawValue * 37;
            hash ^= body.LinearVelocity.x.RawValue * 41;
            hash ^= body.LinearVelocity.y.RawValue * 43;
            hash ^= body.Angle.RawValue * 47;
            hash ^= body.AngularVelocity.RawValue * 53;
            hash ^= body.Force.x.RawValue * 59;
            hash ^= body.Force.y.RawValue * 61;
            hash ^= body.BiasVelocity.x.RawValue * 67;
            hash ^= body.BiasVelocity.y.RawValue * 71;
            hash ^= body.BiasRotation.RawValue * 73;
        }
        
        return hash.ToString();
    }
}