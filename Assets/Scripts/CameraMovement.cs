using UnityEngine;

public class CameraMovement : MonoBehaviour
{

    public bool FollowXAxis = true;
    public bool FollowYAxis = false;

    [SerializeField] private Vector3 _targetPosition;
    [SerializeField] private float _smoothTime = 3.0f;
    private Vector3 _smoothVelocity;

    void Update()
    {

        if (Input.GetKey(KeyCode.LeftArrow))
        {
            _targetPosition.x -= 5f * Time.deltaTime;
        }
        else if (Input.GetKey(KeyCode.RightArrow))
        {
            _targetPosition.x += 5f * Time.deltaTime;
        }

        Vector3 newPosition = Vector3.SmoothDamp(transform.position, _targetPosition, ref _smoothVelocity, _smoothTime);
        transform.position = newPosition;
    }

}
