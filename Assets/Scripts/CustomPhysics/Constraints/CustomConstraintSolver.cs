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
        ulong idA1 = GetBodyID(a.bodyA);
        ulong idA2 = GetBodyID(a.bodyB);
        ulong idA_min = idA1 < idA2 ? idA1 : idA2;
        ulong idA_max = idA1 > idA2 ? idA1 : idA2;
        
        ulong idB1 = GetBodyID(b.bodyA);
        ulong idB2 = GetBodyID(b.bodyB);
        ulong idB_min = idB1 < idB2 ? idB1 : idB2;
        ulong idB_max = idB1 > idB2 ? idB1 : idB2;
        
        // Primary: Compare min IDs
        if (idA_min != idB_min)
            return idA_min < idB_min ? -1 : 1;
        
        // Secondary: Compare max IDs
        if (idA_max != idB_max)
            return idA_max < idB_max ? -1 : 1;
        
        // Tie breaker if both springs have the same connected bodies

        if (a.bodyAAttachmentOffset.X.ValueHundredths != b.bodyAAttachmentOffset.X.ValueHundredths)
            return a.bodyAAttachmentOffset.X.ValueHundredths.CompareTo(b.bodyAAttachmentOffset.X.ValueHundredths);
        
        if (a.bodyAAttachmentOffset.Y.ValueHundredths != b.bodyAAttachmentOffset.Y.ValueHundredths)
            return a.bodyAAttachmentOffset.Y.ValueHundredths.CompareTo(b.bodyAAttachmentOffset.Y.ValueHundredths);
        
        if (a.bodyBAttachmentOffset.X.ValueHundredths != b.bodyBAttachmentOffset.X.ValueHundredths)
            return a.bodyBAttachmentOffset.X.ValueHundredths.CompareTo(b.bodyBAttachmentOffset.X.ValueHundredths);
        
        if (a.bodyBAttachmentOffset.Y.ValueHundredths != b.bodyBAttachmentOffset.Y.ValueHundredths)
            return a.bodyBAttachmentOffset.Y.ValueHundredths.CompareTo(b.bodyBAttachmentOffset.Y.ValueHundredths);
        
        // Otherwise... completely identical constraints

        return 0;
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