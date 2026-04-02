using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Provides functionality for typing and submitting commands through an command line interface
/// </summary>
public class CommandLine : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _textLine;
    [SerializeField] private int _historyLineCount;
    [SerializeField] private RectTransform _historyParent;
    [SerializeField] private int _linePadding = 50;
    [SerializeField] private GameObject _enableOnOpen;
    [SerializeField] private CommandInterpreter _interpreter;

    private CommandColours _colours;
    private string _previousInput;
    private List<TextMeshProUGUI> _history;
    private bool _isOpen;


    void Awake()
    {
        _textLine.text = "";
        _previousInput = "";
        _history = new();
        _colours = new();
        _interpreter.Initalize(this);
     
        for(int i = 0; i < _historyLineCount; i++)
        {
            GameObject newLine = Instantiate(_textLine.gameObject, _historyParent);
            _history.Add(newLine.GetComponent<TextMeshProUGUI>());
            
            _history[i].transform.position = _textLine.transform.position + new Vector3(0, _linePadding * i + _linePadding, 0);
            _history[i].text = "";
        }

        SetOpen(false);
    }

    private void LateUpdate() {
        if (_isOpen)
        {
            ProcessInput();
        
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                SetOpen(false);
            }
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                _textLine.text = _previousInput;
            }
            
        }

        if (Input.GetKeyDown(KeyCode.Period))
        {
            SetOpen(!_isOpen);
        }
    } 

    void ProcessInput()
    {
        // Get all character input this frame
        string input = Input.inputString;
        
        foreach (char c in input)
        {
            if(c == '.')
            {
                continue;
            }

            if (c == '\b') // Backspace
            {
                if (_textLine.text.Length > 0)
                {
                    _textLine.text = _textLine.text.Substring(0, _textLine.text.Length - 1);
                }
            }
            else if (c == '\n' || c == '\r') // Enter/Return
            {
                SubmitInput();
            }
            else
            {
                // Add the character to the input string
                _textLine.text += c;
            }
        }
    }

    void SubmitInput()
    {
        // nothing to submit
        if(_textLine.text.Length == 0)
        {
            return;
        }

        if(_textLine.text[0] == '/')
        {
            SubmitCommand(_textLine.text.Substring(1, _textLine.text.Length - 1));
            _previousInput = _textLine.text;
            _textLine.text = "/";
        }
        else
        {
            CommandLineSubmission submission = new();
            submission.Message = _textLine.text;
            submission.MessageType = CommandLineMessageType.Print;

            PushLineToHistory(submission);
            _previousInput = _textLine.text;
            _textLine.text = "";
        }

    }

    private void SubmitCommand(string command)
    {
        // no command to submit
        if(_textLine.text.Length == 0)
        {
            return;
        }

        _interpreter.InterpretCommand(command);
    }
    
    public void PushLineToHistory(CommandLineSubmission submission)
    {
        for(int i = _history.Count - 1; i > 0; i--)
        {
            _history[i].text = _history[i - 1].text;
            _history[i].color = _history[i - 1].color;
        }

        _history[0].text = submission.Message;
        _history[0].color = _colours.Colours[(int)submission.MessageType];
    }

    public void ClearHistory()
    {
        for(int i = 0; i < _history.Count; i++)
        {
            _history[i].text = "";
        }
    }

    public void SetOpen(bool state)
    {
        _isOpen = state;
        if (state)
        {
            _textLine.text = "/";
        }
        _enableOnOpen.SetActive(_isOpen);
    }
}
