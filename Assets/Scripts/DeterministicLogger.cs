using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public static class DeterminismLogger
{
    private const int LOG_TICKS = 5000;
    private static System.Text.StringBuilder _log = new();
    private static bool _done = false;
    private static int _logIndex = 0;

    public static void LogExtraInfo(string extraInfo)
    {
        _log.AppendLine("info: " + extraInfo + "\n");
    }

    public static void ClearLog()
    {
        _log = new();
        _done = false;
    }

    public static void LogTick(long tick, CustomSimulationSnapshot snapshot, List<PlayerInputDriver> drivers, string title="")
    {
        if (!Configuration.Singleton.DebugMode || !Configuration.Singleton.GenerateLogFilesInDebugMode)
        {
            return;
        }

        if(tick <= 1)
        {
            _done = false;
        }

        if (_done)
        {
            return;
        }

        if(CustomPhysics.SimulateFutureAtRegularTickRate){
            if(tick > CustomPhysics.SimuluateFutureAtRegularTickRateStartTick){

                Debug.Log("Write log: " + tick);
                WriteLog();
                return;
            }
        }
        else{

            if (tick > LOG_TICKS)
            {
                WriteLog();
                return;
            }
        }

        // Build checksum from all body states
        long checksum = 0;
        int bodyIndex = 0;

        System.Text.StringBuilder bodyDetails = new();
        
        _log.AppendLine($"{title} tick={tick} -------------------");

        if(Configuration.Singleton.LogsShouldIncludeClientInputs){
            // Log input state per driver
            System.Text.StringBuilder inputDetails = new();
            foreach (var driver in drivers)
            {
                PlayerClientInputs inputs = driver?.PlayerInputs;
                if(inputs != null)
                {
                    inputDetails.Append($"  clientId={driver.OwnerClientId} move={inputs.InputMoveDirection} jump={inputs.InputJump}\n");
                }
            }
            _log.Append(inputDetails);
            
        }
        

        
        // CHANGED: Get bodies from the actual Volatile world in simulation order
        foreach (var voltBody in CustomPhysicsSpace.Singleton.SimulationSpace.Bodies)
        {
            // Find the corresponding CustomPhysicsBody
            CustomPhysicsBody bodyComponent = CustomPhysicsSpace.Singleton.GetBody(voltBody.EntityId);
            
            long px = voltBody.Position.x.RawValue;
            long py = voltBody.Position.y.RawValue;
            long vx = voltBody.LinearVelocity.x.RawValue;
            long vy = voltBody.LinearVelocity.y.RawValue;
            long angle = voltBody.Angle.RawValue;
            long angularVel = voltBody.AngularVelocity.RawValue;

            checksum ^= (px * 31 + py * 17 + vx * 13 + vy * 7 + angle * 11 + angularVel * 5) * (bodyIndex + 1);

            if(Configuration.Singleton.LogsShouldIncludePhysicsBodyState){

                if (!voltBody.IsStatic)
                {
                    string bodyName = (bodyComponent != null) 
                    ? bodyComponent.name 
                    : "destroyed";

                    bodyDetails.Append($"  entityId[{voltBody.EntityId}] body[{bodyIndex}] ({bodyName}) pos=({px},{py}) vel=({vx},{vy}) angle={angle} angularVel={angularVel}\n");   
                }
        
            }

            bodyIndex++;
        }

        if (Configuration.Singleton.LogsShouldIncludeCustomDataFromBodies)
        {
            foreach(var body in CustomPhysicsSpace.Singleton.Bodies){
                
                if(body.Value.CustomState != null)
                {
                    bodyDetails.Append("Custom Body State: \n");
                    bodyDetails.Append(body.Value.CustomState.ToString() + "\n");
                    
                }
            }
        }

        if (Configuration.Singleton.LogsShouldIncludeClientInputs || Configuration.Singleton.LogsShouldIncludePhysicsBodyState)
        {
            _log.AppendLine($"physics checksum={checksum}");
        }

        _log.Append(bodyDetails);
    }

    public static void WriteLog()
    {
        _done = true;
        string clientNumber = "";
        if(NetworkManager.Singleton != null)
        {
            clientNumber += ", ClientId," + NetworkManager.Singleton.LocalClientId;
        }
        string name = "determinism_log_" + _logIndex + clientNumber + ".txt";
        string path = System.IO.Path.Combine(Application.persistentDataPath, name);
        System.IO.File.WriteAllText(path, _log.ToString());
        UnityEngine.Debug.Log($"Determinism log written to: {path}");
        _logIndex++;
    }
}