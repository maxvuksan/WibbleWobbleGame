using UnityEngine;

public class SpringPad : MonoBehaviour
{

    [SerializeField] private float springForce;



    public void OnTriggerEnter2D(Collider2D collision)
    {
        
        if(collision.attachedRigidbody != null)
        {
            GetComponent<Animator>().SetTrigger("Spring");
            collision.attachedRigidbody.AddForce(transform.up * springForce, ForceMode2D.Impulse);
        }
    }
}
