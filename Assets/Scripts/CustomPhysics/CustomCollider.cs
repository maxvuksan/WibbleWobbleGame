using FixMath.NET;
using UnityEngine;
using Volatile;

public abstract class CustomCollider : MonoBehaviour
{
    [Header("Configuration")]

    public Vector2 Offset;

    public void Awake()
    {
        RegisterWithCustomPhysicsBody();
    }
    /// <summary>
    /// Initalize the VoltShape, this is called right before the physics body is added to the simulation
    /// </summary>
    public abstract void ConstructShape();

    /// <returns>The VoltShape derived class of the derived collider</returns>
    public abstract VoltShape GetShape();

    /// <summary>
    /// Should be called in awake, before the physics body start method has ran
    /// </summary>
    public void RegisterWithCustomPhysicsBody()
    {
        CustomPhysicsBody body = GetComponent<CustomPhysicsBody>();

        if(body == null)
        {
            body = GetComponentInParent<CustomPhysicsBody>();
        }

        if(body == null)
        {
            Debug.LogWarning("A CustomCollider has been added to a object with no physics body. a physics body is being added dynamically");
            return;
        }

        body.AddCollider(this);
    }

    /// <summary>
    /// Draws additional gizmos for colliders to show position of shapes and self
    /// </summary>
    protected void DrawPositionDots()
    {
        Gizmos.matrix = transform.localToWorldMatrix;

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(Vector2.zero, 0.1f);
        Gizmos.DrawSphere(new Vector3(Offset.x, Offset.y, 0), 0.1f);

        Gizmos.matrix = Matrix4x4.zero;
    }
}





