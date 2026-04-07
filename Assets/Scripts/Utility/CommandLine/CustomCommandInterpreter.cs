

using System.IO;
using System.Linq;
using UnityEngine;

public class CustomCommandInterpreter : CommandInterpreter
{
    public override void InterpretCommand(string commandString)
    {

        string[] args = SplitCommand(commandString);
        
        if(args.Length == 0)
        {
            CommandLine.SetOpen(false);
            return;
        }

        CommandLineSubmission submission = new();

        if(args[0] == "help")
        {
            submission.MessageType = CommandLineMessageType.Print;
            submission.Message = "/help, /clear, /level, /game, /colour, /debug";
            CommandLine.PushLineToHistory(submission);
        }
        else if(args[0] == "debug")
        {
            Configuration.Singleton.DebugMode = !Configuration.Singleton.DebugMode;
            CommandLine.SetOpen(false);
        }
        else if(args[0] == "colour")
        {
            if(args.Length == 1)
            {
                submission.MessageType = CommandLineMessageType.Print;
                submission.Message = "/colour [colourPaletteIndex]";
                CommandLine.PushLineToHistory(submission);
            }
            else if(int.TryParse(args[1], out int colourPaletteIndex))
            {
                // Successfully parsed the color palette index
                ColourPaletteManager.Singleton.LoadPalette(colourPaletteIndex);
                CommandLine.SetOpen(false);
            }
            else
            {
                submission.MessageType = CommandLineMessageType.Error;
                submission.Message = "Invalid color palette index. Must be a number.";
                CommandLine.PushLineToHistory(submission);
            }
        }
        else if(args[0] == "clear")
        {
            CommandLine.ClearHistory(); 
        }
        else if(args[0] == "level")
        {
            if(args.Length == 1)
            {
                submission.MessageType = CommandLineMessageType.Print;
                submission.Message = "/level [save / load / list]";
                CommandLine.PushLineToHistory(submission);
            }
            else
            {
                if(args[1] == "save")
                {
                    if(args.Length == 3)
                    {
                        LevelManager.Singleton.SaveLevelToFile(args[2]);
                        CommandLine.SetOpen(false);
                    }
                    else
                    {
                        submission.MessageType = CommandLineMessageType.Print;
                        submission.Message = "/level save [relativeSavePath]";
                        CommandLine.PushLineToHistory(submission);
                    }
                }
                if(args[1] == "load")
                {
                    if(args.Length == 3)
                    {
                        bool result = LevelManager.Singleton.ServerLoadLevelFromFile(args[2]);

                        if (result)
                        {
                            GameStateManager.Singleton.ServerSetGameState(GameStateManager.GameStateEnum.GameState_Play);
                            CommandLine.SetOpen(false);
                        }
                        else // failed to load
                        {
                            submission.MessageType = CommandLineMessageType.Error;
                            submission.Message = "LoadLevelFromFile() failed";
                            CommandLine.PushLineToHistory(submission);
                        }
                    }
                    else
                    {
                        submission.MessageType = CommandLineMessageType.Print;
                        submission.Message = "/level load [relativeSavePath]";
                        CommandLine.PushLineToHistory(submission);
                    }
                }
                if(args[1] == "list")
                {
                    ListLevels();
                }
            }
        }
        else if(args[0] == "game")
        {
            if(args.Length == 1)
            {
                submission.MessageType = CommandLineMessageType.Print;
                submission.Message = "/game [lobby / creative / play / trapSelect]";
                CommandLine.PushLineToHistory(submission);
            }
            else
            {
                if(args[1] == "lobby")
                {
                    GameStateManager.Singleton.ServerSetGameState(GameStateManager.GameStateEnum.GameState_SelectingLevel);
                }
                if(args[1] == "creative")
                {
                    GameStateManager.Singleton.ServerSetGameState(GameStateManager.GameStateEnum.GameState_CreativeMode);
                }
                if(args[1] == "play")
                {
                    GameStateManager.Singleton.ServerSetGameState(GameStateManager.GameStateEnum.GameState_Play);
                }
                if(args[1] == "trapSelect")
                {
                    GameStateManager.Singleton.ServerSetGameState(GameStateManager.GameStateEnum.GameState_SelectingTrap);
                }

                CommandLine.SetOpen(false);
            }
        }
        else
        {
            submission.MessageType = CommandLineMessageType.Error;
            submission.Message = "'" + args[0] + "' Command not found, view /help";
            CommandLine.PushLineToHistory(submission);
        }
    }

    private void ListLevels()
    {
        // Adjust this path to match where LevelManager saves levels

        // Get all level files (adjust extension as needed - could be .json, .dat, .level, etc.)
        string[] levelFiles = Directory.GetFiles(Application.persistentDataPath, "*.*")
            .Where(f => f.EndsWith(".level")) // Filter out Unity meta files
            .ToArray();

        if (levelFiles.Length == 0)
        {
            CommandLineSubmission submission = new();
            submission.MessageType = CommandLineMessageType.Print;
            submission.Message = "No levels found";
            CommandLine.PushLineToHistory(submission);
            return;
        }

        // Extract just the filenames without paths and extensions
        string[] levelNames = levelFiles.Select(f => Path.GetFileNameWithoutExtension(f)).ToArray();

        // Group into lines of 6 names each
        for (int i = 0; i < levelNames.Length; i += 6)
        {
            string[] lineNames = levelNames.Skip(i).Take(6).ToArray();
            string line = string.Join(", ", lineNames);

            CommandLineSubmission submission = new();
            submission.MessageType = CommandLineMessageType.Success;
            submission.Message = line;
            CommandLine.PushLineToHistory(submission);
        }
    }
}