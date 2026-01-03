using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using HairRemovalSim.Environment;

namespace HairRemovalSim.Core
{
    /// <summary>
    /// Manages all chairs in the scene and provides chair finding functionality
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class ChairManager : MonoBehaviour
    {
        public static ChairManager Instance { get; private set; }
        
        public List<Chair> allChairs = new List<Chair>();
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        /// <summary>
        /// Register a chair (called by Chair.OnEnable)
        /// </summary>
        public void RegisterChair(Chair chair)
        {
            if (chair != null && !allChairs.Contains(chair))
            {
                allChairs.Add(chair);
                Debug.Log($"[ChairManager] Registered chair {chair.name} ({chair.category})");
            }
        }
        
        /// <summary>
        /// Unregister a chair (called by Chair.OnDisable)
        /// </summary>
        public void UnregisterChair(Chair chair)
        {
            if (chair != null)
            {
                allChairs.Remove(chair);
            }
        }
        
        /// <summary>
        /// Find an empty chair, prioritizing the specified category
        /// </summary>
        /// <param name="preferredCategory">Category to search first</param>
        /// <returns>Empty chair or null if none available</returns>
        public Chair FindEmptyChair(ChairCategory preferredCategory)
        {
            // First try to find in preferred category
            var preferredChair = allChairs
                .Where(c => c.category == preferredCategory && !c.IsOccupied)
                .FirstOrDefault();
            
            if (preferredChair != null)
            {
                return preferredChair;
            }
            
            // If no preferred category chair available, try any empty chair
            return allChairs
                .Where(c => !c.IsOccupied)
                .FirstOrDefault();
        }
        
        /// <summary>
        /// Find the closest empty chair to a position, prioritizing the specified category
        /// Optimized version without LINQ for better performance with many chairs
        /// </summary>
        public Chair FindClosestEmptyChair(Vector3 fromPosition, ChairCategory preferredCategory)
        {
            Chair closestPreferred = null;
            Chair closestAny = null;
            float closestPreferredDist = float.MaxValue;
            float closestAnyDist = float.MaxValue;
            
            for (int i = 0; i < allChairs.Count; i++)
            {
                var chair = allChairs[i];
                if (chair == null || chair.IsOccupied) continue;
                
                float dist = (fromPosition - chair.SeatPosition.position).sqrMagnitude; // sqrMagnitude faster than Distance
                
                if (chair.category == preferredCategory)
                {
                    if (dist < closestPreferredDist)
                    {
                        closestPreferredDist = dist;
                        closestPreferred = chair;
                    }
                }
                
                if (dist < closestAnyDist)
                {
                    closestAnyDist = dist;
                    closestAny = chair;
                }
            }
            
            return closestPreferred ?? closestAny;
        }
        
        /// <summary>
        /// Get count of empty chairs in a category
        /// </summary>
        public int GetEmptyChairCount(ChairCategory category)
        {
            return allChairs.Count(c => c.category == category && !c.IsOccupied);
        }
        
        /// <summary>
        /// Get chair's index in the list (for debugging)
        /// </summary>
        public int GetChairIndex(Chair chair)
        {
            return allChairs.IndexOf(chair);
        }
        
        /// <summary>
        /// Get total chair count
        /// </summary>
        public int TotalChairCount => allChairs.Count;
    }
}
