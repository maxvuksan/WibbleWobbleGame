using UnityEngine;

public class Destroy : MonoBehaviour
{
    public float timeToDestroy;

    void Start()
    {
        Invoke("DoDestroy()", timeToDestroy);
    }

    public void DoDestroy()
    {
        Destroy(this.gameObject);
    }
}
