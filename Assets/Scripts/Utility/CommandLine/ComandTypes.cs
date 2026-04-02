


using UnityEngine;

/// <summary>
/// A line of text to be pushed to the submission history
/// </summary>
[System.Serializable]
public struct CommandLineSubmission
{
    public string Message;
    public CommandLineMessageType MessageType;
}

/// <summary>
/// The message type of a sbmission, this dictates the colour of a submission
/// </summary>
public enum CommandLineMessageType : byte
{
    Print,
    Warning,
    Success,
    Error,
}

public class CommandColours {
    
    public Color[] Colours =
    {
        new Color(0.36f, 0.36f, 0.36f),
        new Color(0.93f, 0.66f, 0.31f),
        new Color(0.4f, 0.92f, 0.48f),
        new Color(0.99f, 0.2f, 0.2f)
    };
}
