using UnityEngine;

public class CameraPanByMousePosition : MonoBehaviour
{
    [SerializeField] private Transform _childToPan;
    [SerializeField] private float _smoothTime = 3.0f;
    public Transform MousePosition;   
    public float PanStrength;

    private Vector3 _smoothVelocity;



    void Update()
    {
        Vector3 difference = transform.position - MousePosition.position;

        Vector3 newPosition = Vector3.SmoothDamp(_childToPan.transform.localPosition, difference * PanStrength, ref _smoothVelocity, _smoothTime);
        _childToPan.transform.localPosition = newPosition;
    }
}
