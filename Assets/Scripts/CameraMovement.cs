using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    public static CameraMovement SceneSingleton = null;

    public bool FollowXAxis = true;
    public bool FollowYAxis = false;

    public Vector3 TargetPosition;

    public static int AbsoluteXMin { get => -9999; }
    public static int AbsoluteXMax { get => 9999; }
    public float MinXPosition = AbsoluteXMin; // the minimum edge of the camera position
    public float MaxXPosition = AbsoluteXMax; // the maximum edge of the camera position
    public float XPositionMidpoint
    {
        get => (MinXPosition + MaxXPosition) / 2.0f;
    }

    [SerializeField] private float _boundsPadding = 6.0f;
    [SerializeField] private Camera _referenceCamera;
    [SerializeField] private float _smoothTime = 3.0f;
    private Vector2 _smoothVelocity;


    public void Awake()
    {
        SceneSingleton = this;
        TargetPosition = new Vector2(0,0);
    }

    /// <summary>
    /// Snaps the camera (teleports) to the target position
    /// </summary>
    public void SnapToTarget() 
    {
        transform.position = ComputeTargetPosition();    
        _smoothVelocity.Set(0,0);
    }


    /// <summary>
    /// Returns the target position adjusted by the set constraints 
    /// </summary>
    private Vector2 ComputeTargetPosition()
    {
        Vector2 _targetThisFrame = TargetPosition;
        if (!FollowXAxis)
        {
            _targetThisFrame.x = transform.position.x;
        }
        if (!FollowYAxis)
        {
            _targetThisFrame.y = transform.position.y;
        }

        // clamp
        float halfCameraWidth = _referenceCamera.orthographicSize * _referenceCamera.aspect;
        float minShifted = MinXPosition + halfCameraWidth - _boundsPadding;
        float maxShifted = MaxXPosition - halfCameraWidth + _boundsPadding;
        

        float minMaxDiff = Mathf.Abs(MaxXPosition - MinXPosition);
        if(minMaxDiff > halfCameraWidth * 2)
        {
            _targetThisFrame.x = Mathf.Clamp(
            _targetThisFrame.x,
            minShifted,
            maxShifted);
        }
        else
        {
            // go between min and max
            _targetThisFrame.x = Mathf.Lerp(minShifted, maxShifted, 0.5f);
        }

        return _targetThisFrame;
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            TargetPosition.x -= 5f * Time.deltaTime;
        }
        else if (Input.GetKey(KeyCode.RightArrow))
        {
            TargetPosition.x += 5f * Time.deltaTime;
        }

        Vector2 _targetThisFrame = ComputeTargetPosition();

        Vector2 newPosition = Vector2.SmoothDamp(transform.position, _targetThisFrame, ref _smoothVelocity, _smoothTime);
        transform.position = newPosition;
    }

}
