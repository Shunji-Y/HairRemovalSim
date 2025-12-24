using UnityEngine;
using HairRemovalSim.Customer;
using HairRemovalSim.Core;
using HairRemovalSim.Core.Effects;

namespace HairRemovalSim.Tools
{
    /// <summary>
    /// Cooling gel tool - reduces customer pain when used.
    /// Left hand tool (right click to use).
    /// </summary>
    public class CoolingGel : LeftHandTool
    {
        [Header("Cooling Gel Settings")]
        [Tooltip("Fallback pain reduction if no ItemData effects (legacy)")]
        public float fallbackPainReduction = 30f;
        
        [Header("Effect")]
        [Tooltip("Effect name to play from EffectManager")]
        public string effectName = "CoolingEffect";
        
        protected override void Awake()
        {
            base.Awake();
            
            // Set defaults for cooling gel
            toolType = ToolType.Single;
            maxDurability = 3;
            currentDurability = maxDurability;
        }
        
        public override void OnUseDown()
        {
            if (!isEquipped || IsBroken) return;
            if (interactionController == null) return;
            
            // Get customer from right hand tool's target
            if (interactionController.currentTool == null) return;
            
            // Use decalPivot position (where right hand tool is pointing)
            Vector3 effectPosition = interactionController.decalPivot != null 
                ? interactionController.decalPivot.position 
                : interactionController.cameraTransform.position;
            
            // Find customer at that position
            Collider[] hits = Physics.OverlapSphere(effectPosition, 0.5f);
            foreach (var hit in hits)
            {
                CustomerController customer = hit.GetComponentInParent<CustomerController>();
                if (customer != null)
                {
                    ApplyCooling(customer, effectPosition);
                    return;
                }
            }
        }
        
        private void ApplyCooling(CustomerController customer, Vector3 position)
        {
            float painReduction = fallbackPainReduction;
            
            // Use ItemData effects if available
            if (itemData != null && itemData.effects != null && itemData.effects.Count > 0)
            {
                var ctx = EffectContext.CreateForTreatment(customer, customer.currentPain);
                EffectHelper.ApplyEffects(itemData, ctx);
                
                if (ctx.PainReduction > 0f)
                {
                    painReduction = ctx.PainReduction;
                }
            }
            
            // Reduce pain
            customer.ReducePain(painReduction);
            
            // Play effect via EffectManager
            if (EffectManager.Instance != null)
            {
                EffectManager.Instance.PlayEffect(effectName, position);
            }
            
            Debug.Log($"[CoolingGel] Applied cooling to {customer.name}, reduced pain by {painReduction}");
            
            // Use durability
            UseDurability();
        }
        
        public override void OnUseUp() { }
        public override void OnUseDrag(Vector3 delta) { }
    }
}
