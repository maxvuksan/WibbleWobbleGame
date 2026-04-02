using System.Collections.Generic;
using FixMath.NET;
using UnityEngine;
using Volatile;

/// <summary>
/// Allows constraint based objects (eg. springs and joints) to be solved through multiple sub steps (iterations) for stability
/// </summary>
public static class CustomConstraintSolver
{
    private static bool _isInitialized = false;

    public static void Initialize()
    {
        if (_isInitialized) return;
        
        CustomPhysics.OnPostRecomputeEntityIds += OnPostRecomputeEntityIds;
        _isInitialized = true;
    }

    public static void Cleanup()
    {
        if (!_isInitialized) return;
        
        CustomPhysics.OnPostRecomputeEntityIds -= OnPostRecomputeEntityIds;
        _isInitialized = false;
    }

    private static List<CustomConstraint> _constraints = new List<CustomConstraint>();

    public static void OnPostRecomputeEntityIds()
    {
        SortConstraints();
    }

    public static void SortConstraints()
    {
        _constraints.Sort((a, b) => 
        {
            // Normalize each constraint's body pair (smaller ID first)
            ulong idA_min = System.Math.Min(GetBodyID(a.bodyA), GetBodyID(a.bodyB));
            ulong idA_max = System.Math.Max(GetBodyID(a.bodyA), GetBodyID(a.bodyB));
            
            ulong idB_min = System.Math.Min(GetBodyID(b.bodyA), GetBodyID(b.bodyB));
            ulong idB_max = System.Math.Max(GetBodyID(b.bodyA), GetBodyID(b.bodyB));
            
            // Compare
            int minCompare = idA_min.CompareTo(idB_min);
            if (minCompare != 0) return minCompare;
            
            return idA_max.CompareTo(idB_max);
        });
    }

    private static ulong GetBodyID(CustomPhysicsBody body)
    {
        return body != null ? body.GetDesiredEntityId() : ulong.MaxValue;
    }
     
    public static void AddConstraint(CustomConstraint constraint)
    {
        _constraints.Add(constraint);
        SortConstraints();
    }

    public static void RemoveConstraint(CustomConstraint constraint)
    {
        _constraints.Remove(constraint);
        SortConstraints();
    }

    public static void ClearConstraints()
    {
        _constraints.Clear();
    }

    public static void SolveAllConstraints()
    {
        int iterations = CustomPhysicsSpace.Singleton.ConstraintIterations; 
        Fix64 stepStepScaler = Fix64.One / (Fix64)iterations;
        
        for (int i = 0; i < iterations; i++)
        {
            foreach (var constraint in _constraints)
            {
                constraint.ApplySubStep(stepStepScaler * CustomPhysics.TimeBetweenTicks);
            }
        }
    }
}