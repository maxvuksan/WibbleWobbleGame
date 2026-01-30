using UnityEngine;

public class AlwaysMaintainRotation : MonoBehaviour
{

    [SerializeField] private float fixedRotation = 0;

    void LateUpdate()
    {
        transform.rotation = Quaternion.Euler(0, 0, fixedRotation);
    }

}
