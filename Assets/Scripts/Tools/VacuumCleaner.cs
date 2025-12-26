using UnityEngine;
using HairRemovalSim.Core;

namespace HairRemovalSim.Tools
{
    /// <summary>
    /// Vacuum cleaner tool for cleaning hair debris.
    /// Can only be equipped before/after business hours.
    /// </summary>
    public class VacuumCleaner : ToolBase
    {
        [Header("Vacuum Settings")]
        [SerializeField] private ParticleSystem suctionEffect;

        private bool isVacuuming = false;
        
        protected override void Awake()
        {
            base.Awake();

        }
        
        public override HandType GetHandType()
        {
            return HandType.RightHand;
        }
        
        public override void OnUseDown()
        {
            // Start vacuuming sound when mouse down
            StartVacuuming();
        }
        
        public override void OnUseUp()
        {
            // Stop vacuuming sound when mouse up
            StopVacuuming();
        }
        
        public override void OnUseDrag(Vector3 delta)
        {
            // Vacuum cleaner doesn't need drag handling
        }
        
        /// <summary>
        /// Play vacuum effect when cleaning
        /// </summary>
        public void PlayCleanEffect(Vector3 position)
        {
            if (suctionEffect != null)
            {
                suctionEffect.transform.position = position;
                suctionEffect.Play();
            }
        }
        
        private void StartVacuuming()
        {
            if (isVacuuming) return;
            isVacuuming = true;



        }

        private void StopVacuuming()
        {
            if (!isVacuuming) return;
            isVacuuming = false;
            
       
            
            if (suctionEffect != null)
            {
                suctionEffect.Stop();
            }
        }
        
        private void OnDisable()
        {
            StopVacuuming();
        }
    }
}
