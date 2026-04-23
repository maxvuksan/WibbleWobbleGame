using UnityEngine;

public class PageToggler : MonoBehaviour
{
    [SerializeField] private GameObject[] _pages;
    public int CurrentPage = 0;

    void Awake()
    {
        MoveToNextPage(0);
    }

    public void NextPage()
    {
        MoveToNextPage(1);
    }
    public void BackPage()
    {
        MoveToNextPage(-1);
    }

    public void MoveToNextPage(int direction)
    {
        CurrentPage += direction;
        CurrentPage %= _pages.Length;

        if(CurrentPage < 0)
        {
            CurrentPage += _pages.Length;
        }
         
        for(int i = 0; i < _pages.Length; i++)
        {
            if(i == CurrentPage)
            {
                _pages[i].SetActive(true);    
            }
            else
            {
                _pages[i].SetActive(false);    
            }
        }
    }
}
