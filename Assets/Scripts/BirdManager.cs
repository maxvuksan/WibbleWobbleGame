using System.Collections.Generic;
using FixMath.NET;
using UnityEngine;
using Volatile;


public class BirdManager : MonoBehaviour
{
    [SerializeField] private int _birdCount = 15;
    [SerializeField] private GameObject _birdPrefab;
    [SerializeField] private BirdConfig _birdConfig;

    private List<GameObject> _birds;
    private float _levelXMin;
    private float _levelXMax;

    public static BirdManager Singleton;

    public void Awake()
    {
        LevelManager.Singleton.OnLevelLoad += OnLevelLoad;
        CustomPhysics.OnPhysicsTick += OnPrePhysicsTick;
        
        _birds = new List<GameObject>();
    

        if(Singleton != null)
        {
            Debug.LogError("Cannot have multiple BirdManager singletons");
            return;
        }
        Singleton = this;
    }
    public void OnDestroy()
    {
        LevelManager.Singleton.OnLevelLoad -= OnLevelLoad;
        CustomPhysics.OnPhysicsTick -= OnPrePhysicsTick;
        Singleton = null;
    }

    private void OnLevelLoad()
    {
        print("SPAWNING BIRDS");
        (_levelXMin, _levelXMax) = TrapPlacementArea.Singleton.ComputeHorizontalBoundsOfPlacedTraps();
   
        SpawnBirds();
    }

    public void SpawnBirds()
    {
        DespawnBirds();
        for(int i = 0; i < _birdCount; i++)
        {
            _birds.Add(Instantiate(_birdPrefab));
            _birds[i].GetComponent<Bird>().BirdConfig = _birdConfig;
            _birds[i].transform.position = new Vector3(0, _birdConfig.DissapearHeight, 0);
        }
    }

    private void OnPrePhysicsTick()
    {
        if(CustomPhysics.Tick == 0)
        {
            for(int i = 0; i < _birds.Count; i++)
            {
                _birds[i].transform.position = FindNewPerchSpot(_birds[i]);
                if(_birds[i].transform.position.y == _birdConfig.DissapearHeight)
                {
                    _birds[i].GetComponent<Bird>().RefreshState(Bird.BirdState.Offscreen);
                }
                else
                {
                    _birds[i].GetComponent<Bird>().RefreshState(Bird.BirdState.Idle);
                }
            }
        }
    }

    public Vector2 FindNewPerchSpot(GameObject bird, bool enforceDistanceRequirement = false)
    {
        float x = CustomRandom.Float(_levelXMin, _levelXMax);
        VoltVector2 rayOrigin = new VoltVector2((Fix64)x, (Fix64)bird.transform.position.y);

        var result = CustomPhysics.Raycast(rayOrigin, new VoltVector2(Fix64.Zero, (Fix64)(-1)), (Fix64)999 + (Fix64)_birdConfig.DissapearHeight);

        // can only perch on static body, and the surface must face upwards
        if (
            result.Hit && 
            result.Body.BodyType == CustomBodyType.Static && 
            (float)result.Normal.y > 0.6f)
        {
            var newPosition = new Vector2((float)result.HitPoint.x, (float)result.HitPoint.y);

            // the new perch spot cannot be near the bird
            if(enforceDistanceRequirement)
            {
                if(Vector2.Distance(bird.transform.position, newPosition) > _birdConfig.ScareDistance * 3)
                {
                    return newPosition;
                }
            }
            else
            {
                return newPosition;
            }
        }
        
        return new Vector2(x, _birdConfig.DissapearHeight);
    }

    public void DespawnBirds()
    {
        for(int i = 0; i < _birds.Count; i++)
        {
            Destroy(_birds[i]);
        }
        _birds.Clear();
    }
}
