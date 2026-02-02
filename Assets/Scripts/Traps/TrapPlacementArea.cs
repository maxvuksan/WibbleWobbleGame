using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;



[System.Serializable]
public struct NetworkedTrapPlacedData : INetworkSerializable, IEquatable<NetworkedTrapPlacedData>{

    public int trapTypeIndex;
    public Vector2 position;
    public float rotationEuler;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer)
        where T : IReaderWriter
    {
        serializer.SerializeValue(ref trapTypeIndex);
        serializer.SerializeValue(ref position);
        serializer.SerializeValue(ref rotationEuler);
    }

    public bool Equals(NetworkedTrapPlacedData other)
    {
        return trapTypeIndex == other.trapTypeIndex && position == other.position && rotationEuler == other.rotationEuler;
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

    private NetworkList<NetworkedTrapPlacedData> _networkedPlacedTrapDataList; 
    private List<GameObject> _staticTrapInstances; 
    private List<NetworkObject> _behaviouralTrapInstances;
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

        _networkedPlacedTrapDataList = new NetworkList<NetworkedTrapPlacedData>();
        _staticTrapInstances = new List<GameObject>();
        _behaviouralTrapInstances = new List<NetworkObject>();

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
    }


    public override void OnNetworkSpawn()
    {
        _networkedPlacedTrapDataList.OnListChanged += OnPlacedTrapsChanged;

        SpawnAllStaticInstances();
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
    /// Applies the active colour palette to this trap object (could be either static or behaviour instance)
    /// </summary>
    /// <param name="trapName">The name of the trap</param>
    /// <param name="trapObject">The instantiated instance of the trap</param>
    public void ApplyColourPaletteToTrap(string trapName, GameObject trapObject)
    {
        if(trapName == "")
        {
            Debug.LogError("The TrapName field has not been set on a trap prefab instance");
            return;
        }

        TrapData data = TrapPlacementArea.Singleton.GetTrapDataByName(trapName);


        if(data.colourTarget == ColourTarget.COLOUR_UNCHANGED)
        {
            return;
        }

        Color colour = ColourPaletteManager.Singleton.GetColour(data.colourTarget, data.colourTargetIndexOffset);

        SpriteRenderer sr = trapObject.GetComponent<SpriteRenderer>();
        if(sr != null)
        {
            sr.color = colour;
        }

        // apply colour to children as well if possible

        for(int i = 0; i < transform.childCount; i++)
        {
            sr = trapObject.GetComponent<SpriteRenderer>();
            if(sr != null)
            {
                sr.color = colour;
            }
        }
    }


    /// <summary>
    /// Reacting to a change in the networked traps list
    /// </summary>
    private void OnPlacedTrapsChanged(NetworkListEvent<NetworkedTrapPlacedData> changeEvent)
    {
        if(GameStateManager.Singleton.NetworkedState.Value == GameStateManager.GameStateEnum.GameState_SelectingTrap || 
           GameStateManager.Singleton.NetworkedState.Value == GameStateManager.GameStateEnum.GameState_PreviewLevel ||
           GameStateManager.Singleton.NetworkedState.Value == GameStateManager.GameStateEnum.GameState_PlacingTrap ||
           GameStateManager.Singleton.NetworkedState.Value == GameStateManager.GameStateEnum.GameState_CreativeMode
        )
        {
            print("NUM OF STATIC: " + _staticTrapInstances.Count);

            SpawnAllStaticInstances();
        }
    }

    private void Start()
    {
        PlayerDataManager.Singleton.OnRoundEnd += DestroyAllScopedObjects;
    }

    private void OnDestroy()
    {
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
    public GameObject InstantiateScopedObject(GameObject prefab, Vector3 position, float rotationEulerZ = 0)
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
        DestroyAndClearAllStaticInstances();
        NetworkedDestroyAndClearAllBehavioralInstances();

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

        // if a trap only has a static instance, we can assume it cannot be placed (it is likely a utility: e.g. delete trap)
        if(trapDictionary.traps[trapTypeIndex].behaviorPrefab == null)
        {
            return;
        }

        NetworkedTrapPlacedData data = new NetworkedTrapPlacedData();
        data.position = position;
        data.trapTypeIndex = trapTypeIndex;
        data.rotationEuler = zRotationEuler;

        _networkedPlacedTrapDataList.Add(data);

    }
    
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void AddTrapRpc(Vector2 position, float zRotationEuler, int trapTypeIndex)
    {
        ServerAddTrap(position, zRotationEuler, trapTypeIndex);
    }

    /// <summary>
    /// Permanently removes a trap from the placement area using the static instance as a reference for what to remove
    /// </summary>
    public void RemoveTrapThroughStaticInstance(GameObject staticInstance)
    {
        return;
        // for(int i = 0; i < _placedTrapsList.Count; i++)
        // {
        //     if(staticInstance == _placedTrapsList[i].staticInstance)
        //     {
        //         DestroyStaticInstance(_placedTrapsList, i);

        //         _placedTrapsList.RemoveAt(i);
        //         return;
        //     }
        // }

    }

    /// <summary>
    /// Spawns every trap in the placement area as their static prefabs
    /// </summary>
    public void SpawnAllStaticInstances()
    {
        DestroyAndClearAllStaticInstances();

        for(int i = 0; i < _networkedPlacedTrapDataList.Count; i++)
        {
            GameObject newObj = Instantiate(trapDictionary.traps[_networkedPlacedTrapDataList[i].trapTypeIndex].staticPrefab, _trapStaticInstanceParent);
            newObj.transform.position = _networkedPlacedTrapDataList[i].position;
            newObj.transform.rotation = Quaternion.Euler(0,0,_networkedPlacedTrapDataList[i].rotationEuler);

            _staticTrapInstances.Add(newObj);

            // ensure static trap has the correct component, this is necassary for all static traps
            StaticTrap staticTrap = newObj.GetComponent<StaticTrap>();

            if(staticTrap == null)
            {
                Debug.LogError("A trap is trying to be placed without a StaticTrap component, please ensure all traps have this for other behaviour to work");
            }
        }
    }

    
    /// <summary>
    /// Spawns every trap in the placement area as their behavioural prefabs (this is the actual trap)
    /// </summary>
    
    public void NetworkedSpawnBehaviorInstances()
    {
        NetworkedDestroyAndClearAllBehavioralInstances();

        for(int i = 0; i < _networkedPlacedTrapDataList.Count; i++)
        {
            GameObject newObj = Instantiate(trapDictionary.traps[_networkedPlacedTrapDataList[i].trapTypeIndex].behaviorPrefab, _networkedPlacedTrapDataList[i].position, Quaternion.Euler(0,0,_networkedPlacedTrapDataList[i].rotationEuler));
            NetworkObject networkObj = newObj.GetComponent<NetworkObject>();
            networkObj.Spawn();

            _behaviouralTrapInstances.Add(networkObj);

            Rigidbody2D rb = networkObj.gameObject.GetComponent<Rigidbody2D>();
            if(rb != null)
            {
                rb.gravityScale = GameStateManager.Singleton.enviromentalVariables.rigidBodyGravityScale;
            }
        }
    }
    
    
    /// <summary>
    /// Destroys all the static instances from the placement area
    /// </summary>
    public void DestroyAndClearAllStaticInstances()
    {
        print("static before: " + _staticTrapInstances.Count);
        for(int i = 0; i < _staticTrapInstances.Count; i++)
        {
            Destroy(_staticTrapInstances[i]);
        }
        _staticTrapInstances.Clear();
        print("static after: " + _staticTrapInstances.Count);
    
    }

    /// <summary>
    /// Destroys all the behavioural instances and clears the behavioural trap list
    /// </summary>
    public void NetworkedDestroyAndClearAllBehavioralInstances()
    {
        for(int i = 0; i < _behaviouralTrapInstances.Count; i++)
        {
            _behaviouralTrapInstances[i].Despawn(true);
        }
        _behaviouralTrapInstances.Clear();
    }



}
