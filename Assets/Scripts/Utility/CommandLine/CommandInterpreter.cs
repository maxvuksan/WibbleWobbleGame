

using UnityEngine;

public abstract class CommandInterpreter : MonoBehaviour
{
    public CommandLine CommandLine { get => _commandLine;}
    private CommandLine _commandLine;

    /// <summary>
    /// Attaches a Command Line to this interpreter
    /// </summary>
    public void Initalize(CommandLine commandLine)
    {
        _commandLine = commandLine;
    }

    /// <summary>
    /// Splits a command string by spaces
    /// </summary>
    /// <param name="commandString">The command string to split</param>
    /// <returns>Array of arguments split by spaces</returns>
    protected string[] SplitCommand(string commandString)
    {
        return commandString.Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
    }

    /// <summary>
    /// Is called whenever a command is submitted through the command line interface
    /// </summary>
    /// <param name="commandString">The command to interpret (Note: the '/' is already excluded)</param>
    public virtual void InterpretCommand(string commandString)
    {
        
    }
}