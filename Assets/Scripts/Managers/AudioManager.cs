using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{

    [HideInInspector]
    public static AudioManager Singleton = null;


    [SerializeField] private SoundGroup[] soundGroups;
    private Dictionary<string, (int groupIndex, int soundIndex)> soundLookup;
    private int inital_sound_pool_size = 15;
    private int sound_pool_size = 0;
    private List<AudioSource> sound_source_pool;
    private int current_sound_index = 0;

    [SerializeField] private AudioMixerGroup audioMixerGroup;


    void Awake()
    {
        if (Singleton != null)
        {
            Destroy(this);
            return;
        }

        Singleton = this;
        DontDestroyOnLoad(this);

        sound_source_pool = new List<AudioSource>();

        for (int i = 0; i < inital_sound_pool_size; i++)
        {
            AddPoolObject();
        }

        ConstructSoundDictionary();
    }
    
    public void ConstructSoundDictionary()
    {
        soundLookup = new Dictionary<string, (int groupIndex, int soundIndex)>();
        
        for (int g = 0; g < soundGroups.Length; g++)
        {
            SoundGroup group = soundGroups[g];

            for (int s = 0; s < group.sounds.Length; s++)
            {
                string soundName = group.sounds[s].name;

                if (!soundLookup.ContainsKey(soundName))
                {
                    soundLookup.Add(soundName, (g, s));
                }
                else
                {
                    Debug.LogWarning($"Duplicate sound name '{soundName}' found in AudioManager — ignoring duplicate.");
                }
            }
        }
    }
    
    // returns the new index
    int AddPoolObject()
    {
        GameObject sound_obj = new GameObject();
        sound_obj.transform.parent = transform;

        AudioSource new_source = sound_obj.AddComponent<AudioSource>();
        new_source.outputAudioMixerGroup = audioMixerGroup;

        sound_source_pool.Add(new_source);
        sound_pool_size++;

        return sound_pool_size - 1;
    }



    public void Play(string name, Vector3 position = default, float volumeMultiplier = 1, float pitchMultiplier = 1)
    {
        if (!soundLookup.TryGetValue(name, out var indices))
        {
            Debug.LogWarning($"Could not find sound '{name}'");
            return;
        }

        Sound targetSound = soundGroups[indices.groupIndex].sounds[indices.soundIndex];


        // find a slot that isn't taken
        int items_cycled_through = 0;
        while (sound_source_pool[current_sound_index].isPlaying)
        {
            items_cycled_through++;

            current_sound_index++;
            current_sound_index %= sound_pool_size;

            if (items_cycled_through >= sound_pool_size)
            {
                // expand pool, all are taken
                current_sound_index = AddPoolObject();
                break;
            }
        }


        // choose random clip
        sound_source_pool[current_sound_index].clip = targetSound.clips[UnityEngine.Random.Range(0, targetSound.clips.Length)];

        sound_source_pool[current_sound_index].pitch = targetSound.pitch * pitchMultiplier + UnityEngine.Random.Range(-targetSound.pitch_variation, targetSound.pitch_variation);
        sound_source_pool[current_sound_index].volume = targetSound.volume * volumeMultiplier;
        sound_source_pool[current_sound_index].transform.position = position;
        sound_source_pool[current_sound_index].spatialBlend = 0.0f;

        sound_source_pool[current_sound_index].Play();


        // loops back around the sound pool
        current_sound_index++;
        current_sound_index %= sound_pool_size;

    }

}


