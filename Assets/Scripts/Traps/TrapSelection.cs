using Unity.Netcode;
using UnityEngine;

public class TrapSelection : MonoBehaviour
{
    private ulong _currentPlayerIndex;
    private bool _moveToPlacingState = false;
    public TrapButton[] trapButtons;
    private bool _networkReady = false;

    public static TrapSelection Singleton;

    void Awake()
    {
        if(Singleton != null)
        {
            Debug.LogError("Multiple instances of TrapSelection Singleton are present...");
            return;    
        }

        Singleton = this;
    }



    public void HideButtons()
    {
        for (int i = 0; i < trapButtons.Length; i++)
        {
            // spawn if not spawned already
            trapButtons[i].TriggerHide();
            trapButtons[i].SetTrapIndex(-1);
        }
    }

    public void ShowButtons()
    {
        for (int i = 0; i < trapButtons.Length; i++)
        {
            // spawn if not spawned already
            trapButtons[i].TriggerShow();
            trapButtons[i].SetTrapIndex(i);
        }
    }

    

}
