using UnityEngine;

public class CollapsablePanelTab : MonoBehaviour
{
    [SerializeField] private Transform _arrow;

    public void SetOpen(bool isOpen)
    {
        int xScale = 1;
        if (isOpen)
        {
            xScale = -1;
        }   

        _arrow.localScale = new Vector3(xScale, 1, 1);
    
    }
}
