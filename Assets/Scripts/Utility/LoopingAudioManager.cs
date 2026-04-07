using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Audio;

/*
    manages constant looping audio tracks, e.g. ambient sound
*/
public class LoopingAudioManager : MonoBehaviour
{
    public static LoopingAudioManager Singleton = null;
    public AudioMixerGroup AudioMix;
    public float DefaultFadeInTime = 10;

    [SerializeField] private LoopingSoundGroup _soundGroup;
    [SerializeField] private LoopingProfiles _profiles;
    private Dictionary<string, LoopingProfile> _profilesDictionary;

    private List<AudioSource> _loopingSourcePool;
    private LoopingProfile _activeProfile = null;

    void Awake()
    {
        
        if (Singleton != null)
        {
            Destroy(this);
            return;
        }

        Singleton = this;
        DontDestroyOnLoad(this);

        _profilesDictionary = new();
        // construct profile dictionary...
        foreach(LoopingProfile profile in _profiles.Profiles)
        {
            _profilesDictionary.Add(profile.Name, profile);
        }

        _loopingSourcePool = new List<AudioSource>();

        for (int i = 0; i < _soundGroup.sounds.Length; i++)
        {
            AddPoolObject(i);
        }

    }
    
    void AddPoolObject(int soundIndex)
    {
        GameObject sound_obj = new GameObject();
        sound_obj.name = "[LOOP] " + _soundGroup.sounds[soundIndex].label;
        sound_obj.transform.parent = transform;

        AudioSource new_source = sound_obj.AddComponent<AudioSource>();

        new_source.loop = true;
        new_source.volume = 0;
        new_source.clip = _soundGroup.sounds[soundIndex].clip;
        new_source.outputAudioMixerGroup = AudioMix;

        _soundGroup.sounds[soundIndex].fadeIn = false;
        _soundGroup.sounds[soundIndex].fadeTimeTracked = 0;
        _soundGroup.sounds[soundIndex].fadeTime = 0;
        _soundGroup.sounds[soundIndex].volumeScaler = 1;
        _loopingSourcePool.Add(new_source);
    }


    void Update()
    {
        ManageFading();
    }

    void ManageFading() {

        for (int i = 0; i < _soundGroup.sounds.Length; i++) {
            
            float volumeScaler = 1;
            float volume = _soundGroup.sounds[i].volume;

            if (volume == 0) {

                // stop if playing, only stop non synced loops
                if (_loopingSourcePool[i].isPlaying && !_soundGroup.sounds[i].scheduledForSyncing) {
                    _loopingSourcePool[i].Stop(); 
                }
            }
            else {
                // start if not playing
                if (!_loopingSourcePool[i].isPlaying) {
                    _loopingSourcePool[i].Play();
                }

                // modulate the volume

                float t = (Mathf.Sin(Time.time * _soundGroup.sounds[i].volume * _soundGroup.sounds[i].volumeModulationRate) + 1.0f) * 0.5f;
                volumeScaler = _soundGroup.sounds[i].volume * Mathf.Lerp(_soundGroup.sounds[i].minVolumeScale, _soundGroup.sounds[i].maxVolumeScale, t);


                if (_soundGroup.sounds[i].fadeTimeTracked < _soundGroup.sounds[i].fadeTime)
                {
                    _soundGroup.sounds[i].fadeTimeTracked += Time.deltaTime;

                    if (_soundGroup.sounds[i].fadeIn)
                    {
                        volume *= _soundGroup.sounds[i].fadeTimeTracked / (float)_soundGroup.sounds[i].fadeTime;
                    }
                    else
                    {
                        volume *= 1.0f - _soundGroup.sounds[i].fadeTimeTracked / (float)_soundGroup.sounds[i].fadeTime;
                    }
                }
                else
                {
                    if (!_soundGroup.sounds[i].fadeIn)
                    {
                        volume = 0;
                    }

                    _soundGroup.sounds[i].fadeTimeTracked = _soundGroup.sounds[i].fadeTime;
                }

                
            }
            _loopingSourcePool[i].volume = volume * volumeScaler;

        }
    }

    /// <summary>
    /// Enables a new LoopingProfile, also disables the previously loaded profile if present
    /// </summary>
    /// <param name="profileName">The name of the profile we wish to load</param>
    public void SwitchProfile(string profileName)
    {
        if (!_profilesDictionary.ContainsKey(profileName))
        {
            Debug.LogError("The LoopingProfiles dictionary does not contain a value with the provided profileName");
        }

        print("Switching profile to " + profileName);
        if(_activeProfile != null)
        {
            // disable current profile
            foreach(string loopName in _activeProfile.LoopsToEnable)
            {
                DisableLoop(loopName);
            }            
        }

        // enable new profile...
        _activeProfile = _profilesDictionary[profileName];

        foreach(string layer in _activeProfile.LoopsToEnable)
        {
            print("Enabling layer... " + layer);
            EnableLoop(layer);
        }
    }

    public void EnableLoop(string loopLabel, float loopFadeIn = -1f, float volumeScaler = 1) {

        FadeLoop(loopLabel, loopFadeIn, true, volumeScaler);
    }

    public void DisableLoop(string loopLabel, float loopFadeIn = -1f) {
        FadeLoop(loopLabel, loopFadeIn, false, -1);
    }

    public void DisableEntireCategory(LoopCategory category, float loopFadeIn = -1f) {

        for (int i = 0; i < _soundGroup.sounds.Length; i++)
        {
            if (_soundGroup.sounds[i].loopCategory == category)
            {
                FadeLoop(i, loopFadeIn, false, -1);
            }
        }
    }

    private void FadeLoop(string loopLabel, float loopFadeTime, bool fadeIn, float volumeScaler)
    {
        for (int i = 0; i < _soundGroup.sounds.Length; i++)
        {
            // found correct sound
            if (_soundGroup.sounds[i].label == loopLabel)
            {
                FadeLoop(i, loopFadeTime, fadeIn, volumeScaler);
            }
        }
    }


    private void FadeLoop(int index, float loopFadeTime, bool fadeIn, float volumeScaler)
    {

        if (volumeScaler != -1)
        {
            _soundGroup.sounds[index].volumeScaler = volumeScaler;
        }

        // we are already fading in this direction
        if (_soundGroup.sounds[index].fadeIn == fadeIn)
        {
            return;
        }

        // use default fade time
        if (loopFadeTime < 0)
        {
            loopFadeTime = DefaultFadeInTime;
        }

        _soundGroup.sounds[index].fadeTime = loopFadeTime;
        _soundGroup.sounds[index].fadeIn = fadeIn;
        _soundGroup.sounds[index].fadeTimeTracked = 0.0f;

    }

}
 