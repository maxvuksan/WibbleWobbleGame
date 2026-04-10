using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    public bool FollowXAxis = true;
    public bool FollowYAxis = false;

    public Vector3 TargetPosition;
    public float MinXPosition = float.MinValue; // the minimum edge of the camera position
    public float MaxXPosition = float.MaxValue; // the maximum edge of the camera position

    [SerializeField] private float _boundsPadding = 6.0f;
    [SerializeField] private Camera _referenceCamera;
    [SerializeField] private float _smoothTime = 3.0f;
    private Vector3 _smoothVelocity;


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

        Vector3 _targetThisFrame = TargetPosition;
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
   
        Vector3 newPosition = Vector3.SmoothDamp(transform.position, _targetThisFrame, ref _smoothVelocity, _smoothTime);
        transform.position = newPosition;
    }

}
