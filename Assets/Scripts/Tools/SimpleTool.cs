using UnityEngine;

namespace HairRemovalSim.Tools
{
    public class SimpleTool : RightHandTool
    {
        public override void OnUseDown()
        {
            Debug.Log($"{toolName}: Use Down");
        }

        public override void OnUseUp()
        {
            Debug.Log($"{toolName}: Use Up");
        }

        public override void OnUseDrag(Vector3 delta)
        {
            // Debug.Log($"{toolName}: Drag {delta}");
        }
    }
}
