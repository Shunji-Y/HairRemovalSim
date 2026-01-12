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
        [SerializeField] private AudioSource ambientSource;
        [SerializeField] private int sfxPoolSize = 10;
        
        [Header("Volume Settings")]
        [Range(0f, 1f)] public float masterVolume = 1f;
        [Range(0f, 1f)] public float sfxVolume = 1f;
        [Range(0f, 1f)] public float musicVolume = 1f;
        [Range(0f, 1f)] public float ambientVolume = 0.5f;
        
        private List<AudioSource> sfxPool = new List<AudioSource>();
        private Dictionary<string, SoundLibrary.SoundEntry> soundLookup = new Dictionary<string, SoundLibrary.SoundEntry>();
        private Coroutine ambientFadeCoroutine;
        
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
        
        #region Ambient Sound
        
        /// <summary>
        /// Play ambient sound by ID
        /// </summary>
        public void PlayAmbient(string ambientId, bool loop = true)
        {
            if (ambientSource == null)
            {
                GameObject ambientObj = new GameObject("Ambient_Source");
                ambientObj.transform.SetParent(transform);
                ambientSource = ambientObj.AddComponent<AudioSource>();
            }
            
            if (soundLookup.TryGetValue(ambientId, out var entry))
            {
                ambientSource.clip = entry.clip;
                ambientSource.loop = loop;
                ambientSource.volume = masterVolume * ambientVolume * entry.volume;
                ambientSource.Play();
                Debug.Log($"[SoundManager] Playing ambient: {ambientId}");
            }
            else
            {
                Debug.LogWarning($"[SoundManager] Ambient not found: {ambientId}");
            }
        }
        
        /// <summary>
        /// Play ambient sound from AudioClip directly
        /// </summary>
        public void PlayAmbient(AudioClip clip, bool loop = true)
        {
            if (clip == null) return;
            
            if (ambientSource == null)
            {
                GameObject ambientObj = new GameObject("Ambient_Source");
                ambientObj.transform.SetParent(transform);
                ambientSource = ambientObj.AddComponent<AudioSource>();
            }
            
            ambientSource.clip = clip;
            ambientSource.loop = loop;
            ambientSource.volume = masterVolume * ambientVolume;
            ambientSource.Play();
        }
        
        /// <summary>
        /// Stop ambient sound
        /// </summary>
        public void StopAmbient()
        {
            if (ambientSource != null)
            {
                ambientSource.Stop();
            }
        }
        
        /// <summary>
        /// Fade ambient volume to target over duration
        /// </summary>
        public void FadeAmbientVolume(float targetVolume, float duration)
        {
            if (ambientSource == null) return;
            
            if (ambientFadeCoroutine != null)
            {
                StopCoroutine(ambientFadeCoroutine);
            }
            
            ambientFadeCoroutine = StartCoroutine(FadeAmbientCoroutine(targetVolume, duration));
        }
        
        private System.Collections.IEnumerator FadeAmbientCoroutine(float targetVolume, float duration)
        {
            float startVolume = ambientSource.volume;
            float targetActualVolume = masterVolume * targetVolume;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                ambientSource.volume = Mathf.Lerp(startVolume, targetActualVolume, elapsed / duration);
                yield return null;
            }
            
            ambientSource.volume = targetActualVolume;
            ambientVolume = targetVolume;
            ambientFadeCoroutine = null;
        }
        
        /// <summary>
        /// Set ambient volume immediately
        /// </summary>
        public void SetAmbientVolume(float volume)
        {
            ambientVolume = Mathf.Clamp01(volume);
            if (ambientSource != null)
            {
                ambientSource.volume = masterVolume * ambientVolume;
            }
        }
        
        #endregion
        
        // Dictionary to track looping SFX by ID (stores both sources for crossfade)
        private Dictionary<string, CrossfadeLoopData> loopingSFX = new Dictionary<string, CrossfadeLoopData>();
        
        private class CrossfadeLoopData
        {
            public AudioSource sourceA;
            public AudioSource sourceB;
            public Coroutine loopCoroutine;
            public float targetVolume;
            public bool isPlaying;
            public GameObject loopObject; // Parent object to destroy on stop
        }
        
        [Header("Loop Sound Settings")]
        [SerializeField] private float crossfadeDuration = 0.3f; // Overlap duration at loop point
        
        /// <summary>
        /// Play a looping sound effect with crossfade at loop points
        /// </summary>
        public AudioSource PlayLoopSFX(string sfxId)
        {
            // If already playing this loop, return existing source
            if (loopingSFX.TryGetValue(sfxId, out var existingData) && existingData.isPlaying)
            {
                return existingData.sourceA;
            }
            
            if (soundLookup.TryGetValue(sfxId, out var entry))
            {
                // Create two dedicated audio sources for crossfade loop
                GameObject loopObj = new GameObject($"LoopSFX_{sfxId}");
                loopObj.transform.SetParent(transform);
                AudioSource sourceA = loopObj.AddComponent<AudioSource>();
                AudioSource sourceB = loopObj.AddComponent<AudioSource>();
                
                float targetVolume = masterVolume * sfxVolume * entry.volume;
                
                // Setup source A
                sourceA.clip = entry.clip;
                sourceA.volume = 0f;
                sourceA.pitch = 1f;
                sourceA.playOnAwake = false;
                sourceA.loop = false; // We handle looping manually
                
                // Setup source B
                sourceB.clip = entry.clip;
                sourceB.volume = 0f;
                sourceB.pitch = 1f;
                sourceB.playOnAwake = false;
                sourceB.loop = false;
                
                var loopData = new CrossfadeLoopData
                {
                    sourceA = sourceA,
                    sourceB = sourceB,
                    targetVolume = targetVolume,
                    isPlaying = true,
                    loopObject = loopObj
                };
                
                loopingSFX[sfxId] = loopData;
                
                // Start crossfade loop coroutine
                loopData.loopCoroutine = StartCoroutine(CrossfadeLoopCoroutine(sfxId, loopData, entry.clip));
                
                return sourceA;
            }
            else
            {
                Debug.LogWarning($"[SoundManager] Loop SFX not found: {sfxId}");
            }
            return null;
        }
        
        private System.Collections.IEnumerator CrossfadeLoopCoroutine(string sfxId, CrossfadeLoopData data, AudioClip clip)
        {
            float clipLength = clip.length;
            float crossfadeStart = clipLength - crossfadeDuration;
            bool useSourceA = true;
            
            // Start immediately at full volume
            data.sourceA.volume = data.targetVolume;
            data.sourceA.Play();
            
            while (data.isPlaying)
            {
                AudioSource currentSource = useSourceA ? data.sourceA : data.sourceB;
                AudioSource nextSource = useSourceA ? data.sourceB : data.sourceA;
                
                // Wait until crossfade point
                float timeToWait = crossfadeStart - currentSource.time;
                if (timeToWait > 0)
                {
                    yield return new WaitForSeconds(timeToWait);
                }
                
                if (!data.isPlaying) break;
                
                // Start next source and crossfade
                nextSource.time = 0f;
                nextSource.volume = 0f;
                nextSource.Play();
                
                // Crossfade over duration
                float elapsed = 0f;
                while (elapsed < crossfadeDuration && data.isPlaying)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / crossfadeDuration;
                    currentSource.volume = Mathf.Lerp(data.targetVolume, 0f, t);
                    nextSource.volume = Mathf.Lerp(0f, data.targetVolume, t);
                    yield return null;
                }
                
                currentSource.Stop();
                useSourceA = !useSourceA;
            }
        }
        
        /// <summary>
        /// Stop a looping sound effect immediately
        /// </summary>
        public void StopLoopSFX(string sfxId)
        {
            if (loopingSFX.TryGetValue(sfxId, out var data) && data.isPlaying)
            {
                data.isPlaying = false;
                
                if (data.loopCoroutine != null)
                {
                    StopCoroutine(data.loopCoroutine);
                }
                
                // Stop and cleanup
                if (data.loopObject != null)
                {
                    Destroy(data.loopObject);
                }
                loopingSFX.Remove(sfxId);
            }
        }
        
        private System.Collections.IEnumerator FadeAudioSource(AudioSource source, float targetVolume, float duration)
        {
            float startVolume = source.volume;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                source.volume = Mathf.Lerp(startVolume, targetVolume, elapsed / duration);
                yield return null;
            }
            
            source.volume = targetVolume;
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
            
            if (ambientSource != null)
            {
                ambientSource.volume = masterVolume * ambientVolume;
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
