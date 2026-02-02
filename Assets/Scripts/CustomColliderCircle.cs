using FixMath.NET;
using UnityEngine;
using Volatile;

public class CustomColliderCircle : MonoBehaviour
{

    [Header("Configuration")]
    [SerializeField] private float _radius;
    private Fix64 _radiusFix64;    
    VoltCircle _circle;

    void Awake()
    {
        _radiusFix64 = (Fix64)_radius; 
        
        //_circle.InitializeFromWorldSpace(VoltVector2.zero, _radiusFix64, (Fix64)0, (Fix64)0, (Fix64)0);
    }

}
