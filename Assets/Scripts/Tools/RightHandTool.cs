using UnityEngine;

namespace HairRemovalSim.Tools
{
    /// <summary>
    /// Base class for right hand tools (main tools).
    /// Used with left click. Includes decal tracking for surface-following tools.
    /// </summary>
    public abstract class RightHandTool : ToolBase
    {
        public enum DecalShape
        {
            Rectangle,
            Circle
        }
        
        [Header("Right Hand Settings")]
        public float painMultiplier = 1.0f;
        
        [Header("Decal Settings")]
        [Tooltip("Shape of the decal (Rectangle or Circle)")]
        public DecalShape decalShape = DecalShape.Rectangle;
        [Tooltip("Width of decal (for Rectangle) or Diameter (for Circle)")]
        public float decalWidth = 0.08f;
        [Tooltip("Height of decal (for Rectangle only, ignored for Circle)")]
        public float decalHeight = 0.12f;
        
        [Header("Decal Tracking")]
        [Tooltip("If true, tool will move to follow the decal position on the mesh surface")]
        public bool followDecalPosition = false;
        [Tooltip("Distance from the mesh surface along the normal")]
        public float decalTrackingDistance = 0.1f;
        [Tooltip("Additional local position offset when tracking decal")]
        public Vector3 decalTrackingPositionOffset = Vector3.zero;
        [Tooltip("Additional rotation offset when tracking decal (Euler angles)")]
        public Vector3 decalTrackingRotationOffset = Vector3.zero;
        [Tooltip("Smooth speed for position/rotation tracking (0 = instant)")]
        public float decalTrackingSmoothSpeed = 10f;
        
        public override HandType GetHandType() => HandType.RightHand;
        
        /// <summary>
        /// Get decal size as Vector2 (width, height). For circle, height = width.
        /// </summary>
        public Vector2 GetDecalSize()
        {
            if (decalShape == DecalShape.Circle)
            {
                return new Vector2(decalWidth, decalWidth);
            }
            return new Vector2(decalWidth, decalHeight);
        }
        
        /// <summary>
        /// Called when tool is being unequipped. Override to clean up decals etc.
        /// </summary>
        public virtual void Unequip()
        {
            // Override in subclasses to clean up
        }
        
        protected override void Awake()
        {
            base.Awake();
            // Right hand tools are usually unbreakable (permanent equipment)
            isUnbreakable = true;
        }
    }
}
