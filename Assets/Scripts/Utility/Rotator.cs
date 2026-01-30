using Unity.Netcode;
using UnityEngine;

public class Rotator : NetworkBehaviour
{
    [SerializeField] private float speed;

    public void FixedUpdate()
    {
        if (!IsServer)
        {
            return;
        }

        transform.Rotate(new Vector3(0,0, speed * Time.deltaTime));
    }
}
