using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Volatile;


/// <summary>
/// A traps serialized representation
/// </summary>
[System.Serializable]
public struct TrapPlacedData : INetworkSerializable, IEquatable<TrapPlacedData>
{
    public int trapTypeIndex;
    public int positionXHundredths;
    public int positionYHundredths;
    public int rotationHundredths;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer)
        where T : IReaderWriter
    {
        serializer.SerializeValue(ref trapTypeIndex);
        serializer.SerializeValue(ref positionXHundredths);
        serializer.SerializeValue(ref positionYHundredths);
        serializer.SerializeValue(ref rotationHundredths);
    }

    public bool Equals(TrapPlacedData other)
    {
        return trapTypeIndex == other.trapTypeIndex
            && positionXHundredths == other.positionXHundredths
            && positionYHundredths == other.positionYHundredths
            && rotationHundredths == other.rotationHundredths;
    }
}


/// <summary>
/// Manages the traps in the level
/// </summary>
public class TrapPlacementArea : NetworkBehaviour
{

    [SerializeField] private TrapDictionary trapDictionary;
    [SerializeField] private Transform scopedObjectTransform;

    private Transform _trapStaticInstanceParent;
    private Transform _trapBehaviorInstanceParent;

    public NetworkList<TrapPlacedData> NetworkedPlacedTrapDataList { get => _networkedPlacedTrapDataList; }
    private NetworkList<TrapPlacedData> _networkedPlacedTrapDataList; 
    private List<GameObject> _trapInstances; 
    private List<GameObject> _scopedObjectsList;
    private Dictionary<string, int> _trapNameToIndexMap; 

    public static TrapPlacementArea Singleton;


    private void Awake()
    {
        if(Singleton != null)
        {
            Debug.LogError("Cannot add multiple TrapPlacementArea Singletons...");
            Destroy(this.gameObject);
            return;    
        }

        Singleton = this; 

        _networkedPlacedTrapDataList = new NetworkList<TrapPlacedData>();
        _trapInstances = new List<GameObject>();

        _scopedObjectsList = new List<GameObject>();
        _trapNameToIndexMap = new Dictionary<string, int>();

        // create name to index mapping for traps
        for(int i = 0; i < trapDictionary.traps.Length; i++)
        {
            _trapNameToIndexMap.Add(trapDictionary.traps[i].name, i);
        }

        // spawn game objects to hold trap data...

        _trapStaticInstanceParent = new GameObject().transform;
        _trapStaticInstanceParent.name = "[STATIC TRAPS]";
        _trapStaticInstanceParent.parent = this.transform;

        _trapBehaviorInstanceParent = new GameObject().transform;
        _trapBehaviorInstanceParent.name = "[BEHAVIOR TRAPS]";
        _trapBehaviorInstanceParent.parent = this.transform;

        CustomPhysics.OnRecomputeEntityIds += OnRecomputeEntityIds;
    }


    public override void OnNetworkSpawn()
    {
        _networkedPlacedTrapDataList.OnListChanged += OnPlacedTrapsChanged;
        //SpawnAllTrapInstances();
    }

    public override void OnNetworkDespawn()
    {
        _networkedPlacedTrapDataList.OnListChanged -= OnPlacedTrapsChanged;
    }

    public TrapData GetTrapDataByName(string dictionaryKey)
    {
        return trapDictionary.traps[_trapNameToIndexMap[dictionaryKey]];        
    }

    /// <summary>
    /// Reacting to a change in the networked traps list
    /// </summary>
    private void OnPlacedTrapsChanged(NetworkListEvent<TrapPlacedData> changeEvent)
    {
        if(GameStateManager.Singleton.NetworkedState.Value == GameStateManager.GameStateEnum.GameState_SelectingTrap || 
           GameStateManager.Singleton.NetworkedState.Value == GameStateManager.GameStateEnum.GameState_PreviewLevel ||
           GameStateManager.Singleton.NetworkedState.Value == GameStateManager.GameStateEnum.GameState_PlacingTrap ||
           GameStateManager.Singleton.NetworkedState.Value == GameStateManager.GameStateEnum.GameState_CreativeMode
        )
        {
            SpawnAllTrapInstances();
        }
    }

    private void Start()
    {
        PlayerDataManager.Singleton.OnRoundEnd += DestroyAllScopedObjects;
    }

    override public void OnDestroy()
    {
        base.OnDestroy();

        CustomPhysics.OnRecomputeEntityIds -= OnRecomputeEntityIds;
        PlayerDataManager.Singleton.OnRoundEnd -= DestroyAllScopedObjects;
    }

    /// <summary>
    /// Destroys all objects which have their lifetimes scoped to the game round, should be called when a game round ends
    /// </summary>
    public void DestroyAllScopedObjects()
    {
        for (int i = 0; i < _scopedObjectsList.Count; i++)
        {
            Destroy(_scopedObjectsList[i]);
        }
        _scopedObjectsList.Clear();
    }

    /// <summary>
    /// Spawns a game object with a lifetime scoped to the duration of the game round, when the round ends all scoped game objects are destroyed.
    /// </summary>
    public GameObject InstantiateScopedObject(GameObject prefab, Vector2 position, float rotationEulerZ = 0)
    {
        GameObject newObj = Instantiate(
            prefab,
            position,
            Quaternion.Euler(0, 0, rotationEulerZ),
            scopedObjectTransform
        );

        _scopedObjectsList.Add(newObj);

        return newObj;
    }


    /// <summary>
    /// Removes all traps from the placement area, deletes the currently loaded level, and spawns a new level specified by levelIndex
    /// </summary>
    public void ServerClearTraps()
    {
        DestroyAndClearAllTrapInstances();
        _networkedPlacedTrapDataList.Clear();
    }

    public void ServerAddTrap(Vector2 position, float zRotationEuler, string trapName, bool playSound = true)
    {
        ServerAddTrap(position, zRotationEuler, _trapNameToIndexMap[trapName], playSound);
    }

    /// <summary>
    /// Adds a trap to the placement area. 
    /// when GameState == Placement,    traps are represented as their static instances
    /// when GameState == Playing,      traps with behavior are spawned in their static instances place.
    /// </summary>
    public void ServerAddTrap(Vector2 position, float zRotationEuler, int trapTypeIndex, bool playSound = true)
    {
        if(playSound && trapDictionary.traps[trapTypeIndex].soundOnPlace != "")
        {
            AudioManager.Singleton.Play(trapDictionary.traps[trapTypeIndex].soundOnPlace);
        }

        // Since we know this code will always run on the server (the host will always perform this math)
        // We should not need to worry about deterministic problems when converting between floats
        // Any potential error will be treated as the truth, we trust the servers values

        TrapPlacedData data = new TrapPlacedData
        {
            trapTypeIndex = trapTypeIndex,
            positionXHundredths = Mathf.RoundToInt(position.x * 100f),
            positionYHundredths = Mathf.RoundToInt(position.y * 100f),
            rotationHundredths = Mathf.RoundToInt(zRotationEuler * 100f)
        };

        ServerAddTrap(data);
    }

    /// <summary>
    /// Adds a trap using the TrapPlacedData structure
    /// </summary>
    public void ServerAddTrap(TrapPlacedData trapData)
    {
        // if a trap only has a static instance, we can assume it cannot be placed (it is likely a utility: e.g. delete trap)
        if(trapDictionary.traps[trapData.trapTypeIndex].behaviorPrefab == null)
        {
            return;
        }

        _networkedPlacedTrapDataList.Add(trapData);
    }
    
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void AddTrapRpc(Vector2 position, float zRotationEuler, int trapTypeIndex)
    {
        ServerAddTrap(position, zRotationEuler, trapTypeIndex);
    }


    public void OnRecomputeEntityIds()
    {
        if (Configuration.Singleton.DebugMode)
        {
            string msg = "Recomputing EntityIds for trapInstances, number of traps = " + _trapInstances.Count;

            Debug.Log(msg);
            DeterminismLogger.LogExtraInfo(msg);
        }

        ulong currentEntityId = 100ul; // Start at 100

        for(int i = 0; i < _trapInstances.Count; i++)
        {
            // Assign IDs to this trap and all its child physics bodies
            currentEntityId = AssignEntityIdsRecursive(_trapInstances[i], currentEntityId);
        }
    }

    /// <summary>
    /// Iterates over a object finding all the CustomPhysicsBody instances, assigning an id for each body
    /// </summary>
    /// <returns></returns>
    private ulong AssignEntityIdsRecursive(GameObject obj, ulong startId)
    {
        ulong currentId = startId;
        
        // Get all physics bodies in this object and its children
        CustomPhysicsBody[] bodies = obj.GetComponentsInChildren<CustomPhysicsBody>();
        
        foreach(var body in bodies)
        {
            body.SetDesiredEntityId(currentId);
            
            if (Configuration.Singleton.DebugMode)
            {
                DeterminismLogger.LogExtraInfo("RecomputeEntityIds for: " + body.gameObject.name + ", new EntityId: " + currentId);
            }
            
            currentId++;
        }
        
        return currentId; // Return next available ID
    }

    /// <summary>
    /// Spawns every trap in the placement area as their static prefabs
    /// </summary>
    public void SpawnAllTrapInstances()
    {
        DestroyAndClearAllTrapInstances();

        // Sort the network trap placed data list, to ensure we introduce traps to the physics simulation in the same order

        var sortedTrapsToPlace = new List<TrapPlacedData>();
        
        for (int i = 0; i < _networkedPlacedTrapDataList.Count; i++){
            sortedTrapsToPlace.Add(_networkedPlacedTrapDataList[i]);
        }

        sortedTrapsToPlace.Sort((a, b) => {

            int cmp = a.positionXHundredths.CompareTo(b.positionXHundredths);
            if (cmp != 0){ 
                return cmp;
            }
            return a.positionYHundredths.CompareTo(b.positionYHundredths);
        });


        for(int i = 0; i < sortedTrapsToPlace.Count; i++)
        {
            IntHundredth rotationDegrees = new IntHundredth { ValueHundredths = sortedTrapsToPlace[i].rotationHundredths };
            IntHundredth positionX = new IntHundredth { ValueHundredths = sortedTrapsToPlace[i].positionXHundredths };
            IntHundredth positionY = new IntHundredth { ValueHundredths = sortedTrapsToPlace[i].positionYHundredths };

            Debug.Log("Spawning: " + trapDictionary.traps[sortedTrapsToPlace[i].trapTypeIndex].name);
            GameObject newObj = Instantiate(trapDictionary.traps[sortedTrapsToPlace[i].trapTypeIndex].behaviorPrefab, _trapStaticInstanceParent);
            newObj.SetActive(false);

            newObj.transform.position = new Vector2(positionX.AsFloat(), positionY.AsFloat());
            newObj.transform.rotation = Quaternion.Euler(0,0, rotationDegrees.AsFloat());

            // Set the CustomTransform values to the IntHundredth values, to ensure deterministic position and rotation on every client...
            
            CustomTransform customTransform = newObj.GetComponent<CustomTransform>();

            if(customTransform == null)
            {
                customTransform = newObj.GetComponentInChildren<CustomTransform>();
            }
            if(customTransform == null)
            {
                Debug.LogError("No CustomTransform attached to placed trap, SpawnAllTrapInstances()");
            }

            customTransform.SetValues(positionX, positionY, rotationDegrees);
            // TODO: We may need to update the child CustomTransform, will see

            CustomTransform[] bodies = newObj.GetComponentsInChildren<CustomTransform>();
            for(int b = 0; b < bodies.Length; b++)
            {   
                if(!bodies[b].IsWorldSpace)
                {
                    continue;
                }
                bodies[b].SetValues(
                    positionX + bodies[b].PositionXHundredth, 
                    positionY + bodies[b].PositionYHundredth, 
                    rotationDegrees + bodies[b].RotationDegreesHundredth);
            }

            // trigger start to run
            newObj.SetActive(true);

            _trapInstances.Add(newObj);

            // Ensure the trap has the TrapHeader, this script is required by all traps
            TrapHeader trapHeader = newObj.GetComponent<TrapHeader>();

            if(trapHeader == null)
            {
                Debug.LogError("A trap is trying to be placed without a TrapHeader component, please ensure all traps have this for other behaviour to work");
            }
        }


        // // ensure traps children are positioned prior to next draw operation... (before the physics starts)
        // for(int i = 0; i < _trapInstances.Count; i++)
        // {
        //     CustomPhysicsBody[] bodies = _trapInstances[i].GetComponentsInChildren<CustomPhysicsBody>();
        //     for(int b = 0; b < bodies.Length; b++)
        //     {   
        //         if(bodies[b].ParentBody == null)
        //         {
        //             continue;
        //         }
        //         VoltVector2 initalPosition = bodies[b].GetInitalPositionAccountingForParent();
        //         bodies[b].transform.position = new Vector2((float)initalPosition.x, (float)initalPosition.y);
        //     }
        // }

        // TODO: This is the problem, we are recomputing 0 traps...
        Debug.Log("there is X sorted traps = " + sortedTrapsToPlace.Count);
        Debug.Log("there is X traps = " + _trapInstances.Count);
        CustomPhysics.RecomputeEntityIds();
    }

    
    
    /// <summary>
    /// Destroys all the static instances from the placement area
    /// </summary>
    public void DestroyAndClearAllTrapInstances()
    {
        for(int i = 0; i < _trapInstances.Count; i++)
        {
            Destroy(_trapInstances[i]);
        }
        _trapInstances.Clear();
    
    }
}
