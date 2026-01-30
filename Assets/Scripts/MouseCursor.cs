using UnityEngine;

public class MouseCursor : MonoBehaviour
{

    [SerializeField] private float _controllerSpeed;
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private SpriteRenderer _outlineSpriteRenderer;

    private ControllerInputHandler _input;
    private NetworkPlayerHeader _playerHeader;

    void Awake()
    {
        _playerHeader = GetComponentInParent<NetworkPlayerHeader>();
        _input = FindFirstObjectByType<ControllerInputHandler>();
    }

    public void SetOutlineColour(Color colour)
    {
        _outlineSpriteRenderer.color = colour;
    }

    public void SetColour(Color color)
    {
        _spriteRenderer.color = color;
    }

    public void SetRenderLayer(int renderLayer)
    {
        _spriteRenderer.gameObject.layer = renderLayer;
        _outlineSpriteRenderer.gameObject.layer = renderLayer;

        print("set render layer, " + renderLayer);
    }


    void Update()
    {
        // TO DO: Currently Assumes WE ARE ALWAYS ON MOUSE AND KEYBOARD...

        //if(PlayerDataManager.Singleton.PlayerData[_playerHeader.Index].deviceType == DeviceType.Device_MouseKeyboard)
        //{
            transform.position = Helpers.Singleton.PixelScreenToWorld(new Vector2(Input.mousePosition.x, Input.mousePosition.y));
        //}
        //else 
        //{
            //transform.Translate(new Vector3(_controllerInput.Input.mouseCursorVelocity.x * _controllerSpeed * Time.deltaTime, _controllerInput.Input.mouseCursorVelocity.y * _controllerSpeed * Time.deltaTime, 0));
        //}

        DetectMouseUIButtons();     
    }



    void DetectMouseUIButtons()
    {
        Vector2 pixelMousePos = new Vector2(transform.position.x, transform.position.y);
        Collider2D hit = Physics2D.OverlapPoint(pixelMousePos, Helpers.Singleton.layerWorldUi);

        // we have not hit anything, do not continue...
        if(hit == null)
        {
            return;
        }

        WorldUIButton button = hit.GetComponent<WorldUIButton>();
        button.Hover();

        if (_input.Input.mainButtonIsPressed)
        {
            button.Press(_playerHeader.PlayerIndex.Value);
            _input.ClearMainButtonIsPressed();
        }
    }
}
