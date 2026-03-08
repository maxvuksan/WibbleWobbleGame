using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public static class DeterminismLogger
{
    private const int LOG_TICKS = 2000;
    private static System.Text.StringBuilder _log = new();
    private static bool _done = false;

    public static void LogExtraInfo(string extraInfo)
    {
        _log.AppendLine("info: " + extraInfo + "\n");
    }

    public static void ClearLog()
    {
        _log = new();
        _done = false;
    }

    public static void LogTick(long tick, CustomSimulationSnapshot snapshot, List<PlayerInputDriver> drivers)
    {   

        if (_done || tick > LOG_TICKS)
        {
            if (!_done)
            {
                _done = true;
                WriteLog();
            }
            return;
        }

        // Build checksum from all body states
        long checksum = 0;
        int bodyIndex = 0;

        System.Text.StringBuilder bodyDetails = new();
        foreach (var bodyState in snapshot.Bodies)
        {
            long px = bodyState.Position.x.RawValue;
            long py = bodyState.Position.y.RawValue;
            long vx = bodyState.Velocity.x.RawValue;
            long vy = bodyState.Velocity.y.RawValue;
            long angle = bodyState.Angle.RawValue;
            long angularVel = bodyState.AngularVelocity.RawValue;

            checksum ^= (px * 31 + py * 17 + vx * 13 + vy * 7 + angle * 11 + angularVel * 5) * (bodyIndex + 1);

            string bodyName = (bodyState.BodyComponent != null && bodyState.BodyComponent) 
                ? bodyState.BodyComponent.name 
                : "destroyed";

            bodyDetails.Append($"  body[{bodyIndex}] ({bodyName}) pos=({px},{py}) vel=({vx},{vy}) angle={angle} angularVel={angularVel}\n");
            
            bodyIndex++;
        }

        // Log input state per driver
        System.Text.StringBuilder inputDetails = new();
        foreach (var driver in drivers)
        {
            PlayerInputAtTick inputs = driver.GetLatestInputAtTick();
            inputDetails.Append($"  clientId={driver.OwnerClientId} move={inputs.Inputs.InputMoveDirection} jump={inputs.Inputs.InputJump} wasPredicted={inputs.WasPredicted}\n");
        }

        _log.AppendLine($"--- tick={tick} checksum={checksum}");
        _log.Append(bodyDetails);
        _log.Append(inputDetails);
    }

    public static void Reset()
    {
        _log.Clear();
        _done = false;
    }

    private static void WriteLog()
    {
        string path = System.IO.Path.Combine(Application.persistentDataPath, "determinism_log.txt");
        System.IO.File.WriteAllText(path, _log.ToString());
        UnityEngine.Debug.Log($"Determinism log written to: {path}");
    }
}