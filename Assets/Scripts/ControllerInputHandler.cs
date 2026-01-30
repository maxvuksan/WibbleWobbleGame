using UnityEngine;
using UnityEngine.InputSystem;


[System.Serializable]
public struct InputState
{
    // press refers to the initial moment a input is pressed.
    // held refers to the sustained duration an input is held.

    public bool mainButtonIsPressed;
    public bool rotateForwardButtonIsHeld;
    public bool rotateBackButtonIsHeld;
    public Vector2 mouseCursorVelocity;
}


public class ControllerInputHandler : MonoBehaviour
{

    public InputState Input{
        
        get => _input;
    }
    private InputState _input;



    void Awake()
    {
    }


    public void LateUpdate()
    {
        // reset momentary input states (e.g. buttons and triggers)...

        ClearMainButtonIsPressed();
    }

    /// <summary>
    /// Resets the state (turns off) the mainButtonIsPressed flag
    /// </summary>
    public void ClearMainButtonIsPressed()
    {
        _input.mainButtonIsPressed = false;
    }

    public void Input_MouseButton(InputAction.CallbackContext ctx)
    {
        if(ctx.phase == InputActionPhase.Started || ctx.phase == InputActionPhase.Performed){
            _input.mainButtonIsPressed = true;
        }
    }
    public void Input_RotateForwardButton(InputAction.CallbackContext ctx)
    {
        if(ctx.phase == InputActionPhase.Started || ctx.phase == InputActionPhase.Performed){
            _input.rotateForwardButtonIsHeld = true;
        }
        else if(ctx.phase == InputActionPhase.Canceled)
        {
            _input.rotateForwardButtonIsHeld = false;
        }
    }
    public void Input_RotateBackwardButton(InputAction.CallbackContext ctx)
    {
        if(ctx.phase == InputActionPhase.Started || ctx.phase == InputActionPhase.Performed){
            _input.rotateBackButtonIsHeld = true;
        }
        else if(ctx.phase == InputActionPhase.Canceled)
        {
            _input.rotateBackButtonIsHeld = false;
        }
    }
    
    public void Input_MouseCursor(InputAction.CallbackContext ctx)
    {
        print(_input.mouseCursorVelocity);
        _input.mouseCursorVelocity = ctx.ReadValue<Vector2>();
    }
}
