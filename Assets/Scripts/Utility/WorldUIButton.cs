using System;
using UnityEngine;
using UnityEngine.Events;

public class WorldUIButton : MonoBehaviour
{
    
    [System.Serializable]
    public struct GraphicTarget{

        public SpriteRenderer spriteRenderer;
        public Color baseColour;
        public Color hoverColour;
    }

    [SerializeField] private GraphicTarget[] _targets;
    public UnityEvent OnPress;
    [HideInInspector] public Action OnPressAction;
    
    private bool _hovering = false;

    // can be accessed to determine which player index pressed the last button
    public static ulong PlayerIndexWhoPressedButton
    {
        get => _playerWhoPressedButton;
    }
    private static ulong _playerWhoPressedButton = 0;

    public bool DoesScaleOnHover = true;


    void Awake()
    {
        _hovering = false;
    }

    public void Hover()
    {
        if(_targets != null)
        {
            for (int i = 0; i < _targets.Length; i++)
            {
                _targets[i].spriteRenderer.color = _targets[i].hoverColour;
            }   
        }

        ScaleTransform(1);
        _hovering = true;
    }

    public void ScaleTransform(float direction)
    {
        if (!DoesScaleOnHover)
        {
            return;
        }

        float scale = transform.localScale.x + direction * Time.deltaTime * 2;

        if(scale < 1)
        {
            scale = 1;
        }
        if(scale > 1.1f)
        {
            scale = 1.1f;
        }
        transform.localScale = new Vector3(scale,scale, 1);
    }

    public void Press(ulong playerWhoPressedButton)
    {
        _playerWhoPressedButton = playerWhoPressedButton;
        OnPress?.Invoke(); 
        OnPressAction?.Invoke();
        _playerWhoPressedButton = 0;

        AudioManager.Singleton.Play("Click");
    }

    public void Update()
    {
        // no longer hovering, bring to base colour...
        if (!_hovering)
        {
            ScaleTransform(-1);

            if(_targets != null)
            {
                for (int i = 0; i < _targets.Length; i++)
                {
                    _targets[i].spriteRenderer.color = _targets[i].baseColour;
                }                
            }
        }

        // mark hovering to stop next frame...
        if (_hovering)
        {
            _hovering = false;
        }
    }
}
