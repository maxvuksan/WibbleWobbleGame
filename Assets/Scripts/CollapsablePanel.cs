using UnityEngine;

public class CollapsablePanel : MonoBehaviour
{
    public Vector2 ClosedLocalPosition;
    public Vector2 OpenLocalPosition;
    public bool StartOpen = false;
    public float SmoothTime = 1.0f;

    private Vector2 _velocity;
    private bool _isOpen;
    private Vector2 _desiredPosition;
    private CollapsablePanelTab _openCloseTab;

    void Awake()
    {
        _openCloseTab = GetComponentInChildren<CollapsablePanelTab>();
        SetOpen(StartOpen);
        transform.position = _desiredPosition;
    }

    public void TogglePanelOpen()
    {
        SetOpen(!_isOpen);
    }

    void Update()
    {
        transform.position = Vector2.SmoothDamp(transform.position, _desiredPosition, ref _velocity, SmoothTime);
    }

    /// <summary>
    /// Finds all instances of collapsable panel and closes them
    /// </summary>
    public static void CloseAllPanels()
    {
        foreach(var panel in FindObjectsByType<CollapsablePanel>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            panel.SetOpen(false);
        }
    }

    public void SetOpen(bool state)
    {
        _isOpen = state;

        if (_isOpen)
        {
            _desiredPosition = OpenLocalPosition;
        }
        else
        {
            _desiredPosition = ClosedLocalPosition;
        }
        _openCloseTab.SetOpen(state);
    }
}
