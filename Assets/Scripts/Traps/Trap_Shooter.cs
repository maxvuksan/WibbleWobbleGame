using UnityEngine;

public class Trap_Shooter : MonoBehaviour
{
    [SerializeField] private GameObject projectileToShoot;
    [SerializeField] private Transform projectileSpawn;
    [SerializeField] private float projectileSpeed = 1;

    [SerializeField] private float delayBetweenShots;
    private float _delayBetweenShotsTracked;

    void Awake()
    {
        _delayBetweenShotsTracked = 0;
    }

    void Update()
    {
        _delayBetweenShotsTracked += Time.deltaTime;

        if(_delayBetweenShotsTracked > delayBetweenShots)
        {
            Shoot();
            _delayBetweenShotsTracked = 0;
        }
    }

    private void Shoot()
    {
        GameObject projectile = TrapPlacementArea.Singleton.InstantiateScopedObject(projectileToShoot, projectileSpawn.position, projectileToShoot.transform.eulerAngles.z);
        
        Vector3 vel = -projectileSpawn.up * projectileSpeed;
        
        projectile.GetComponent<Rigidbody2D>().linearVelocity = vel;
        GetComponent<Rigidbody2D>().linearVelocity = -vel;
    }
}
