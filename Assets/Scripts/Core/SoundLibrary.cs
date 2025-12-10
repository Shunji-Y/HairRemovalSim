using UnityEngine;

namespace HairRemovalSim.Core
{
    /// <summary>
    /// ScriptableObject that holds sound effect definitions.
    /// Create via Assets > Create > HairRemovalSim > Sound Library
    /// </summary>
    [CreateAssetMenu(fileName = "SoundLibrary", menuName = "HairRemovalSim/Sound Library")]
    public class SoundLibrary : ScriptableObject
    {
        [System.Serializable]
        public class SoundEntry
        {
            public string id;
            public AudioClip clip;
            [Range(0f, 1f)] public float volume = 1f;
            [Range(0.5f, 2f)] public float minPitch = 1f;
            [Range(0.5f, 2f)] public float maxPitch = 1f;
        }
        
        public SoundEntry[] sounds;
        
        public SoundEntry GetSound(string id)
        {
            if (sounds == null) return null;
            
            foreach (var sound in sounds)
            {
                if (sound.id == id) return sound;
            }
            return null;
        }
    }
}
