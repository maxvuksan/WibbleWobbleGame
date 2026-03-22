using FixMath.NET;
using UnityEngine;
using Volatile;

public abstract class CustomCollider : MonoBehaviour
{
    [Header("Configuration")]

    [SerializeField] protected IntHundredth OffsetX = 0;
    [SerializeField] protected IntHundredth OffsetY = 0;
    public VoltVector2 Offset { get; set; }

    virtual public void Awake()
    {
        Offset = new VoltVector2(
            (Fix64)OffsetX, 
            (Fix64)OffsetY
        );
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
        CustomPhysicsBody body = GetAssociatedBody();

        if(body == null)
        {
            Debug.LogWarning("A CustomCollider has been added to a object with no physics body. a physics body is being added dynamically");
            return;
        }

        body.AddCollider(this);
    }

    public CustomTransform GetCustomTransform()
    {
        CustomTransform customTransform = GetComponent<CustomTransform>();
        if(customTransform == null)
        {
            Debug.LogWarning("No CustomTransform on collider " + gameObject.name + ", looking at parent...");
            customTransform = GetComponentInParent<CustomTransform>();

            if(customTransform == null)
            {
                Debug.LogError("No CustomTransform found for CustomColliderCircle, this is a requirement");
            }
        }

        return customTransform;
    }

    protected CustomPhysicsBody GetAssociatedBody()
    {
        CustomPhysicsBody body = GetComponent<CustomPhysicsBody>();

        if(body == null)
        {
            body = GetComponentInParent<CustomPhysicsBody>();
        }

        return body;
    }

    protected void SetGizmoColourDependingIfTriggerOrNot()
    {
        CustomPhysicsBody body = GetAssociatedBody();

        if(body == null)
        {
            Gizmos.color = Color.red;
            return;
        }

        if (body.IsTrigger)
        {
            Gizmos.color = Color.green;
        }
        else
        {
            Gizmos.color = Color.yellow;
        }
    }

    /// <summary>
    /// Draws additional gizmos for colliders to show position of shapes and self
    /// </summary>
    protected void DrawPositionDots()
    {
        Gizmos.matrix = transform.localToWorldMatrix;

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(Vector2.zero, 0.1f);
        Gizmos.DrawSphere(new Vector3((float)Offset.x, (float)Offset.y, 0), 0.1f);

        Gizmos.matrix = Matrix4x4.zero;
    }
}





