using UnityEngine;

public class HurtBox : MonoBehaviour
{

    private void OnTriggerEnter2D(Collider2D other) {

        Player player = other.GetComponent<Player>();

        if(player == null)
        {
            return;
        }    

        player.HitTrap(other.transform.position - transform.position);
    }

    private void OnCollisionEnter2D(Collision2D other) 
    {
        Player player = other.collider.GetComponent<Player>();

        if(player == null)
        {
            return;
        }    

        player.HitTrap(other.transform.position - transform.position);
    }
}
