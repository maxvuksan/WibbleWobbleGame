using UnityEngine;

public class DeathPit : MonoBehaviour
{

    void OnTriggerEnter2D(Collider2D collision)
    {
        Player player = collision.gameObject.GetComponent<Player>();

        if(player != null)
        {
            player.HitTrap(Vector2.down);
        }
    }
}
