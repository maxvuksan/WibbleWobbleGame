using UnityEngine;

public class BoltHeader : MonoBehaviour
{
    [SerializeField] private Transform boltPosition;

    private bool tryAttach = false;
    private bool attachDelay = true;

    void OnEnable()
    {
        attachDelay = true;
    }

    public void FixedUpdate() 
    {
        if (tryAttach || attachDelay)
        {
            attachDelay = false;
            return;
        }



        AttemptAttachingToTrap();
    }



    public void AttemptAttachingToTrap()
    {
        tryAttach = true;

        Collider2D hit = Physics2D.OverlapCircle(boltPosition.position, 0.4f);

        if(hit == null)
        {
            Debug.Log("Failed to attach bolt to trap");
            return;
        }

        TrapHeader header = hit.GetComponent<TrapHeader>();

        if (header != null)
        {
            tryAttach = true;
            header.AttachChildTrap(this);    
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (boltPosition == null)
            return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(boltPosition.position, 0.4f);
    }

}
