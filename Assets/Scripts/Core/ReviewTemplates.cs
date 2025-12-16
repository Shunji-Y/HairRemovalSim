using UnityEngine;

namespace HairRemovalSim.Core
{
    /// <summary>
    /// ScriptableObject containing review templates for each star rating
    /// Stores localization keys for titles and content
    /// </summary>
    [CreateAssetMenu(fileName = "ReviewTemplates", menuName = "HairRemovalSim/Review Templates")]
    public class ReviewTemplates : ScriptableObject
    {
        [Header("Customer Avatars")]
        [Tooltip("Array of customer avatar sprites for reviews")]
        public Sprite[] avatarIcons;
        
        [Header("1 Star Reviews (Angry)")]
        [Tooltip("Localization key prefixes (e.g., 'review.1star.1' will use 'review.1star.1.title' and 'review.1star.1.content')")]
        public string[] oneStarKeyPrefixes;
        
        [Header("2 Star Reviews (Disappointed)")]
        public string[] twoStarKeyPrefixes;
        
        [Header("3 Star Reviews (Okay)")]
        public string[] threeStarKeyPrefixes;
        
        [Header("4 Star Reviews (Happy)")]
        public string[] fourStarKeyPrefixes;
        
        [Header("5 Star Reviews (Very Happy)")]
        public string[] fiveStarKeyPrefixes;
        
        /// <summary>
        /// Get random localization key prefix for given star rating
        /// Returns prefix like "review.5star.1" - append ".title" or ".content" to get full key
        /// </summary>
        public string GetRandomKeyPrefix(int stars)
        {
            string[] prefixes = stars switch
            {
                1 => oneStarKeyPrefixes,
                2 => twoStarKeyPrefixes,
                3 => threeStarKeyPrefixes,
                4 => fourStarKeyPrefixes,
                5 => fiveStarKeyPrefixes,
                _ => threeStarKeyPrefixes
            };
            
            if (prefixes == null || prefixes.Length == 0)
            {
                return $"review.{stars}star.default";
            }
            
            return prefixes[Random.Range(0, prefixes.Length)];
        }
        
        /// <summary>
        /// Get random avatar icon index
        /// </summary>
        public int GetRandomAvatarIndex()
        {
            if (avatarIcons == null || avatarIcons.Length == 0) return 0;
            return Random.Range(0, avatarIcons.Length);
        }
        
        /// <summary>
        /// Get avatar sprite by index
        /// </summary>
        public Sprite GetAvatarSprite(int index)
        {
            if (avatarIcons == null || avatarIcons.Length == 0) return null;
            return avatarIcons[Mathf.Clamp(index, 0, avatarIcons.Length - 1)];
        }
    }
}
