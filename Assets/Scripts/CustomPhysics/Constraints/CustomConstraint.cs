
using FixMath.NET;
using UnityEngine;

/// <summary>
/// Base class for constraint types (e.g. springs and joints)
/// </summary>
public class CustomConstraint : MonoBehaviour
{

    public CustomPhysicsBody bodyA;
    public CustomPhysicsBody bodyB;
    public IntHundredthVector2 bodyAAttachmentOffset;
    public IntHundredthVector2 bodyBAttachmentOffset;

    virtual public void Awake()
    {
        CustomConstraintSolver.AddConstraint(this);
    }
    
    virtual public void OnDestroy()
    {
        CustomConstraintSolver.RemoveConstraint(this);
    }

    /// <summary>
    /// Is called N many times per physics tick, to resolve 
    /// </summary>
    virtual public void ApplySubStep(Fix64 stepScaler)
    {
        // populate in derived class...
    }
    
}