using UnityEngine;

namespace HairRemovalSim.Core
{
    /// <summary>
    /// A static instance is similar to a singleton, but instead of destroying any new
    /// instances, it overrides the current instance. This is handy for resetting the state
    /// and saves you doing it manually.
    /// </summary>
    public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        public static T Instance { get; private set; }

        protected virtual void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            
            Instance = this as T;
            
            // Optional: If you want all singletons to persist, uncomment this.
            // However, it's often better to handle DontDestroyOnLoad in a specific initialization script or the specific manager.
            // DontDestroyOnLoad(gameObject); 
        }

        protected virtual void OnApplicationQuit()
        {
            Instance = null;
            Destroy(gameObject);
        }
    }
}
