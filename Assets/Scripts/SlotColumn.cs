using System.Collections.Generic;
using UnityEngine;

public class SlotColumn : MonoBehaviour
{

    [System.Serializable]
    public struct SlotCell
    {
        public int index;
        public GameObject gameObject;
    }

    [SerializeField] private TrapDictionary traps;
    [SerializeField] private float spinSpeed;
    [SerializeField] private float timeUntilDecay = 5;
    [SerializeField] private float speedDecay = 0.3f;
    [SerializeField] private float cellHeight;
    [SerializeField] private int count;


    private float _timeUntilDecayTracked = 0;

    private List<SlotCell> cells;    // select index from here

    void OnEnable()
    {
        _timeUntilDecayTracked = 0;
        cells = new List<SlotCell>();

        for(int i = 0; i < count; i++)
        {
            
            SlotCell cell;
            cell.index = Random.Range(0, traps.traps.Length);
            cell.gameObject = Instantiate(traps.traps[cell.index].staticPrefab, transform);
            cell.gameObject.transform.localPosition = Vector3.zero;

            cells.Add(cell);
        }
    }

    void OnDisable()
    {
        for(int i = 0; i < cells.Count; i++)
        {
            Destroy(cells[i].gameObject);
        }
    }

    public void Update()
    {

        if(timeUntilDecay > 0)
        {
            timeUntilDecay -= Time.deltaTime;
        }
        else
        {
            if(spinSpeed > 0)
            {
                spinSpeed -= Time.deltaTime * speedDecay;
            }
            else
            {
                spinSpeed = 0;
            }   
        }

        Spin();
    }

    public void Spin()
    {
        for (int i = 0; i < cells.Count; i++)
        {
            Transform t = cells[i].gameObject.transform;

            // Move downward
            t.localPosition -= new Vector3(0, spinSpeed * Time.deltaTime, 0);

            // if off bottom, wrap to top...
            if (t.localPosition.y < -cellHeight)
            {

                float highestY = GetHighestCellY();

                t.localPosition = new Vector3(
                    0,
                    highestY + cellHeight,
                    0
                );

            }
        }
    }

    float GetHighestCellY()
    {
        float max = float.MinValue;

        foreach (var c in cells)
        {
            float y = c.gameObject.transform.localPosition.y;
            if (y > max) max = y;
        }

        return max;
    }

}
