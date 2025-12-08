using UnityEngine;
using System.Collections.Generic;

namespace HairRemovalSim.Core
{
    /// <summary>
    /// Manages sound effects and background music playback.
    /// Uses object pooling for efficient SFX playback.
    /// </summary>
    public class SoundManager : Singleton<SoundManager>
    {
        [Header("Audio Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private int sfxPoolSize = 10;
        
        [Header("Volume Settings")]
        [Range(0f, 1f)] public float masterVolume = 1f;
        [Range(0f, 1f)] public float sfxVolume = 1f;
        [Range(0f, 1f)] public float musicVolume = 1f;
        
        [Header("Sound Effects Library")]
        [SerializeField] private SoundEffect[] soundEffects;
        
        private List<AudioSource> sfxPool = new List<AudioSource>();
        private Dictionary<string, AudioClip> sfxDictionary = new Dictionary<string, AudioClip>();
        
        protected override void Awake()
        {
            base.Awake();
            InitializeSFXPool();
            BuildSFXDictionary();
        }
        
        private void InitializeSFXPool()
        {
            for (int i = 0; i < sfxPoolSize; i++)
            {
                GameObject sfxObject = new GameObject($"SFX_Source_{i}");
                sfxObject.transform.SetParent(transform);
                AudioSource source = sfxObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                sfxPool.Add(source);
            }
        }
        
        private void BuildSFXDictionary()
        {
            sfxDictionary.Clear();
            if (soundEffects == null) return;
            
            foreach (var sfx in soundEffects)
            {
                if (sfx.clip != null && !string.IsNullOrEmpty(sfx.name))
                {
                    sfxDictionary[sfx.name] = sfx.clip;
                }
            }
        }
        
        /// <summary>
        /// Play a sound effect by name
        /// </summary>
        public void PlaySFX(string sfxName, float volumeMultiplier = 1f)
        {
            if (sfxDictionary.TryGetValue(sfxName, out AudioClip clip))
            {
                PlaySFX(clip, volumeMultiplier);
            }
            else
            {
                Debug.LogWarning($"[SoundManager] SFX not found: {sfxName}");
            }
        }
        
        /// <summary>
        /// Play a sound effect clip directly
        /// </summary>
        public void PlaySFX(AudioClip clip, float volumeMultiplier = 1f)
        {
            if (clip == null) return;
            
            AudioSource source = GetAvailableSFXSource();
            if (source != null)
            {
                source.clip = clip;
                source.volume = masterVolume * sfxVolume * volumeMultiplier;
                source.pitch = 1f;
                source.Play();
            }
        }
        
        /// <summary>
        /// Play a sound effect with random pitch variation
        /// </summary>
        public void PlaySFXWithPitchVariation(string sfxName, float minPitch = 0.9f, float maxPitch = 1.1f, float volumeMultiplier = 1f)
        {
            if (sfxDictionary.TryGetValue(sfxName, out AudioClip clip))
            {
                AudioSource source = GetAvailableSFXSource();
                if (source != null)
                {
                    source.clip = clip;
                    source.volume = masterVolume * sfxVolume * volumeMultiplier;
                    source.pitch = Random.Range(minPitch, maxPitch);
                    source.Play();
                }
            }
        }
        
        /// <summary>
        /// Play a sound effect at a specific position in 3D space
        /// </summary>
        public void PlaySFXAtPosition(string sfxName, Vector3 position, float volumeMultiplier = 1f)
        {
            if (sfxDictionary.TryGetValue(sfxName, out AudioClip clip))
            {
                AudioSource.PlayClipAtPoint(clip, position, masterVolume * sfxVolume * volumeMultiplier);
            }
        }
        
        /// <summary>
        /// Play background music
        /// </summary>
        public void PlayMusic(AudioClip clip, bool loop = true, float fadeTime = 1f)
        {
            if (musicSource == null)
            {
                GameObject musicObj = new GameObject("Music_Source");
                musicObj.transform.SetParent(transform);
                musicSource = musicObj.AddComponent<AudioSource>();
            }
            
            musicSource.clip = clip;
            musicSource.loop = loop;
            musicSource.volume = masterVolume * musicVolume;
            musicSource.Play();
        }
        
        /// <summary>
        /// Stop background music
        /// </summary>
        public void StopMusic()
        {
            if (musicSource != null)
            {
                musicSource.Stop();
            }
        }
        
        /// <summary>
        /// Update all volumes (call after changing volume settings)
        /// </summary>
        public void UpdateVolumes()
        {
            if (musicSource != null)
            {
                musicSource.volume = masterVolume * musicVolume;
            }
        }
        
        private AudioSource GetAvailableSFXSource()
        {
            foreach (var source in sfxPool)
            {
                if (!source.isPlaying)
                {
                    return source;
                }
            }
            
            // All sources busy, use the first one (oldest)
            return sfxPool.Count > 0 ? sfxPool[0] : null;
        }
    }
    
    [System.Serializable]
    public class SoundEffect
    {
        public string name;
        public AudioClip clip;
    }
}
