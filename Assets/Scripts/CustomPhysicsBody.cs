using FixMath.NET;
using UnityEngine;
using Volatile;

public class CustomPhysicsBody : MonoBehaviour
{
    
    /// <summary>
    /// Determines whether the physics body can move and rotate in response to forces
    /// </summary>
    public bool IsStatic
    {
        get => _isStatic;
        set
        {
            SetIsStatic(value);
        }
    }
    
    [Header("Configuration")]
    // is _isStatic should not be modified via the inspector checkbox, rather use IsStatic public field via code
    [SerializeField] private bool _isStatic = false;

    private VoltCircle _shape;
    private VoltBody _body;
    private Fix64 _radiansZFix64;
    private VoltVector2 _positionFix64;

     private void Start()
    {
        _shape = new VoltCircle();
        _shape.InitializeFromWorldSpace(VoltVector2.zero, (Fix64)1, (Fix64)1, (Fix64)1, (Fix64)1);

        _radiansZFix64 = (Fix64)transform.rotation.z;
        _positionFix64 = new VoltVector2((Fix64)transform.position.x, (Fix64)transform.position.y);

        if (_isStatic)
        {
            _body = CustomPhysicsSpace.Singleton.SimulationSpace.CreateStaticBody(_positionFix64, _radiansZFix64, _shape);
        }
        else
        {
            _body = CustomPhysicsSpace.Singleton.SimulationSpace.CreateDynamicBody(_positionFix64, _radiansZFix64, _shape);
        }


    }

    private void Update()
    {
        _body.AddForce(new VoltVector2(Fix64.Zero, (Fix64)(-0.1f)));
    }

    private void SetIsStatic(bool state)
    {
        _isStatic = state;

        // do some things to change the bodies properties...
    }

    private void OnDrawGizmos()
    {
        if(_shape == null || _body == null)
        {
            return;
        }

        Gizmos.color = Color.softYellow;
        Gizmos.DrawWireSphere(new Vector3((float)_body.Position.x, (float)_body.Position.y, 0), (float)_shape.Radius);

    }

}
