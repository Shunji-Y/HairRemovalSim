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
        [Header("Sound Library")]
        [SerializeField] private SoundLibrary soundLibrary;
        
        [Header("Audio Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private int sfxPoolSize = 10;
        
        [Header("Volume Settings")]
        [Range(0f, 1f)] public float masterVolume = 1f;
        [Range(0f, 1f)] public float sfxVolume = 1f;
        [Range(0f, 1f)] public float musicVolume = 1f;
        
        private List<AudioSource> sfxPool = new List<AudioSource>();
        private Dictionary<string, SoundLibrary.SoundEntry> soundLookup = new Dictionary<string, SoundLibrary.SoundEntry>();
        
        protected override void Awake()
        {
            base.Awake();
            InitializeSFXPool();
            BuildSoundLookup();
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
        
        private void BuildSoundLookup()
        {
            soundLookup.Clear();
            
            if (soundLibrary == null)
            {
                Debug.LogWarning("[SoundManager] No SoundLibrary assigned!");
                return;
            }
            
            foreach (var sound in soundLibrary.sounds)
            {
                if (!string.IsNullOrEmpty(sound.id) && sound.clip != null)
                {
                    soundLookup[sound.id] = sound;
                }
            }
            
            Debug.Log($"[SoundManager] Loaded {soundLookup.Count} sounds from library");
        }
        
        /// <summary>
        /// Play a sound effect by ID with pitch variation from SoundLibrary settings
        /// </summary>
        public void PlaySFX(string sfxId)
        {
            if (soundLookup.TryGetValue(sfxId, out var entry))
            {
                float pitch = entry.minPitch == entry.maxPitch ? entry.minPitch : Random.Range(entry.minPitch, entry.maxPitch);
                PlaySFXInternal(entry.clip, entry.volume, pitch);
            }
            else
            {
                Debug.LogWarning($"[SoundManager] SFX not found: {sfxId}");
            }
        }
        
        /// <summary>
        /// Play a sound effect by ID with volume multiplier and pitch variation
        /// </summary>
        public void PlaySFX(string sfxId, float volumeMultiplier)
        {
            if (soundLookup.TryGetValue(sfxId, out var entry))
            {
                float pitch = entry.minPitch == entry.maxPitch ? entry.minPitch : Random.Range(entry.minPitch, entry.maxPitch);
                PlaySFXInternal(entry.clip, entry.volume * volumeMultiplier, pitch);
            }
            else
            {
                Debug.LogWarning($"[SoundManager] SFX not found: {sfxId}");
            }
        }
        
        /// <summary>
        /// Play a sound effect clip directly
        /// </summary>
        public void PlaySFX(AudioClip clip, float volumeMultiplier = 1f)
        {
            if (clip == null) return;
            PlaySFXInternal(clip, volumeMultiplier, 1f);
        }
        

        
        /// <summary>
        /// Play a sound effect at a specific position in 3D space
        /// </summary>
        public void PlaySFXAtPosition(string sfxId, Vector3 position, float volumeMultiplier = 1f)
        {
            if (soundLookup.TryGetValue(sfxId, out var entry))
            {
                AudioSource.PlayClipAtPoint(entry.clip, position, masterVolume * sfxVolume * entry.volume * volumeMultiplier);
            }
        }
        
        private void PlaySFXInternal(AudioClip clip, float volume, float pitch)
        {
            AudioSource source = GetAvailableSFXSource();
            if (source != null)
            {
                source.clip = clip;
                source.volume = masterVolume * sfxVolume * volume;
                source.pitch = pitch;
                source.Play();
            }
        }
        
        /// <summary>
        /// Play background music
        /// </summary>
        public void PlayMusic(AudioClip clip, bool loop = true)
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
}
