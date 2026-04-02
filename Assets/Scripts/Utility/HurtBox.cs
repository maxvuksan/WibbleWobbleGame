using UnityEngine;

public class HurtBox : MonoBehaviour
{
    [SerializeField] private CustomPhysicsBody _body;

    void OnEnable()
    {
        _body.OnTrigger += OnTrigger;
    }
    void OnDisable()
    {
        _body.OnTrigger -= OnTrigger;
    }


    private void OnTrigger(CustomPhysicsBody other) {

        Player player = other.GetComponent<Player>();

        if(player == null)
        {
            return;
        }    

        player.HitTrap(other.transform.position - transform.position);
    }
}
