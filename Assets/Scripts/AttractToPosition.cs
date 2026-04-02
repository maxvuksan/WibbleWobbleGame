using FixMath.NET;
using UnityEngine;
using Volatile;

public class AttractToPosition : MonoBehaviour
{
    public Vector2 AttractPosition;
    [SerializeField] private SpringData _springData;
    private Rigidbody2D _rigidBody;

    void Start()
    {
        _rigidBody = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate() 
    {
        VoltVector2 current = new((Fix64)transform.position.x, (Fix64)transform.position.y);
        VoltVector2 target = new((Fix64)AttractPosition.x, (Fix64)AttractPosition.y);
        VoltVector2 v1 = new ((Fix64)_rigidBody.linearVelocityX, (Fix64)_rigidBody.linearVelocityY);
        
        v1 = Spring.CalculateForce(current, target, v1, _springData);
    
        _rigidBody.linearVelocity = new Vector2((float)v1.x, (float)v1.y);
    }


    private void LateUpdate()
    {
        transform.parent.position = transform.position;
    }
}
