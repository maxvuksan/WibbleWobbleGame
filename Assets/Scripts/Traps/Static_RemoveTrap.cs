using UnityEngine;

public class Static_RemoveTrap : StaticTrap
{
    [SerializeField] private float radius;

    Collider2D[] _trapsToRemove;

    public void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(transform.position, radius);
    }

    public void FixedUpdate() 
    {
        _trapsToRemove = Physics2D.OverlapCircleAll(transform.position, radius);
    }

    public override void OnTrapPlace(Vector2 placePosition)
    {
        print("remove traps.....");
        
        for(int i = 0; i < _trapsToRemove.Length; i++)
        {
            print(i);
            StaticTrap staticTrap = _trapsToRemove[i].GetComponent<StaticTrap>();

            if(staticTrap != null)
            {
                print("remove " + i);
                staticTrap.RemoveTrap();
            }
        }
    }
}
