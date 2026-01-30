using UnityEngine;

public class Trap_Clamp : MonoBehaviour
{

    [SerializeField] private LineRenderer lr;
    [SerializeField] private Transform lrStart;
    [SerializeField] private Transform lrEnd;
    
    [SerializeField] private Rigidbody2D hookEndRb; 
    [SerializeField] private SpriteJiggleMultiState hookEndJiggle;
    [SerializeField] private float hookSpeed;
    [SerializeField] private float hookSpeedIncrease;
    [SerializeField] private LayerMask trapLayer;

    private bool _hookAttached;



    void Start()
    {
        _hookAttached = false;
        hookEndRb.linearVelocityY = -hookSpeed;
    }

    void FixedUpdate()
    {
        if (_hookAttached)
        {
            return;
        }

        hookEndRb.linearVelocityY -= hookSpeedIncrease;

        Vector2 vel = hookEndRb.linearVelocity;

        Vector2 dir = vel.normalized;
        float dist = vel.magnitude * Time.fixedDeltaTime; 

        RaycastHit2D hit = Physics2D.Raycast(hookEndRb.position, dir, dist, trapLayer);
        
        if (hit)
        {

            AudioManager.Singleton.Play("MetalClampLock");

            hookEndRb.bodyType = RigidbodyType2D.Dynamic;

            Rigidbody2D rb = hit.collider.gameObject.GetComponent<Rigidbody2D>();
            rb.constraints = RigidbodyConstraints2D.None;
            rb.gravityScale = 1;

            DistanceJoint2D joint = hookEndRb.gameObject.AddComponent<DistanceJoint2D>();
            joint.connectedBody = rb;

            Vector3 hitPoint = new Vector3(hit.point.x, hit.point.y);

            hookEndRb.transform.position = hitPoint ;
            joint.connectedAnchor = rb.transform.InverseTransformPoint(hitPoint);
            joint.autoConfigureDistance = false;
            joint.distance = 0;

            hookEndJiggle.SetState("Closed");
            hookEndRb.linearVelocityY = 0;

            _hookAttached = true;
        }
    }

    void Update()
    {
        lr.positionCount = 2;
        lr.SetPosition(0, lrStart.transform.position);
        lr.SetPosition(1, lrEnd.transform.position);
    }
}
