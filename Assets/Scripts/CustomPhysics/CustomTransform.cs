using UnityEngine;
using FixMath.NET;
using Volatile;
using System.ComponentModel;

[ExecuteAlways]
public class CustomTransform : MonoBehaviour
{
    [Header("Deterministic Values (Read-Only)")]
    [Tooltip("X position in hundredths. Auto-synced from Unity transform.")]
    [ReadOnly(true)] 
    [SerializeField] private IntHundredth _positionX;
    
    [Tooltip("Y position in hundredths. Auto-synced from Unity transform.")]
    [ReadOnly(true)] 
    [SerializeField] private IntHundredth _positionY;
    
    [Tooltip("Rotation in one tenth degrees. Auto-synced from Unity transform.")]
    [ReadOnly(true)] 
    [SerializeField] private IntHundredth _rotationDegrees;

    public IntHundredth RotationDegreesHundredth { get => _rotationDegrees; }
    public IntHundredth PositionXHundredth { get => _positionX; }
    public IntHundredth PositionYHundredth { get => _positionY; }

    /// <summary>
    /// Set this flag if the transform should ignore the parent positioning
    /// </summary>
    public bool IsWorldSpace => _isWorldSpace;
    [SerializeField] private bool _isWorldSpace = false;

    // Cache the Fix64 values
    private VoltVector2? _cachedPositionFix64;
    private Fix64? _cachedRotationRadians;

    // Track last transform values to detect changes
    private Vector3 _lastUnityPosition;
    private float _lastUnityRotation;


    public void SetValues(IntHundredth positionX, IntHundredth positionY, IntHundredth rotationDegrees)
    {
        _positionX = positionX;
        _positionY = positionY;
        _rotationDegrees = rotationDegrees;

        _cachedPositionFix64 = null;
        _cachedRotationRadians = null;
    }

    public VoltVector2 GetPositionFix64()
    {
        if (!_cachedPositionFix64.HasValue)
        {
            _cachedPositionFix64 = new VoltVector2(_positionX, _positionY);
        }
        return _cachedPositionFix64.Value;
    }

    public Fix64 GetRotationRadiansFix64()
    {
        if (!_cachedRotationRadians.HasValue)
        {
            _cachedRotationRadians = (Fix64)_rotationDegrees * Fix64.Pi / (Fix64)180;
        }
        return _cachedRotationRadians.Value;
    }

    private void SyncFromUnityTransform()
    {
        _positionX.ValueHundredths = Mathf.RoundToInt(transform.position.x * 100f);
        _positionY.ValueHundredths = Mathf.RoundToInt(transform.position.y * 100f);
        _rotationDegrees.ValueHundredths = Mathf.RoundToInt(transform.eulerAngles.z * 100f);
        
        _cachedPositionFix64 = null;
        _cachedRotationRadians = null;
        
        _lastUnityPosition = transform.position;
        _lastUnityRotation = transform.eulerAngles.z;
    }


    private void OnEnable()
    {
        if (Application.isPlaying)
        {
            _cachedPositionFix64 = null;
            _cachedRotationRadians = null;
            return;
        }

        // We want values to be serialized and stored in the unity editor (computed before build not runtime)
        // This is to ensure all clients have the same values
        SyncFromUnityTransform();
    }

    private void Update()
    {
        if (!Application.isPlaying)
        {
            UpdateInEditor();
        }
    }

    private void UpdateInEditor()
    {
        bool unityTransformChanged = 
            Vector3.Distance(transform.position, _lastUnityPosition) > 0.001f ||
            Mathf.Abs(transform.eulerAngles.z - _lastUnityRotation) > 0.1f;

        if (unityTransformChanged)
        {
            SyncFromUnityTransform();
        }
    }

    private void OnValidate()
    {
        // Don't sync during multi-select or when values aren't meant to be edited

        #if UNITY_EDITOR
        if (UnityEditor.Selection.objects.Length > 1)
        {
            return;
        }
        #endif

        SyncFromUnityTransform();
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        float x = _positionX.AsFloat();
        float y = _positionY.AsFloat();
        Vector3 fixedPos = new Vector3(x, y, 0);
        
        Gizmos.color = Color.green;
        Gizmos.DrawLine(fixedPos + Vector3.left * 0.2f, fixedPos + Vector3.right * 0.2f);
        Gizmos.DrawLine(fixedPos + Vector3.up * 0.2f, fixedPos + Vector3.down * 0.2f);
        Gizmos.DrawWireSphere(fixedPos, 0.15f);
        
        float rotRad = _rotationDegrees.AsFloat() * Mathf.Deg2Rad;
        Vector3 rotDir = new Vector3(Mathf.Cos(rotRad), Mathf.Sin(rotRad), 0) * 0.5f;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(fixedPos, fixedPos + rotDir);
        
        UnityEditor.Handles.Label(
            fixedPos + Vector3.up * 0.4f, 
            $"({_positionX.AsFloat():F2}, {_positionY.AsFloat():F2}) {_rotationDegrees.AsFloat()}°"
        );
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0, 1, 0, 0.3f);
        Gizmos.DrawSphere(transform.position, 0.1f);
    }
#endif
}