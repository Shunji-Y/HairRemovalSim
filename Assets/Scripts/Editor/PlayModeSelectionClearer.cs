using UnityEditor;
using UnityEngine;

namespace HairRemovalSim.Editor
{
    /// <summary>
    /// Clears Inspector selection when entering Play mode to prevent
    /// MissingReferenceException from destroyed objects.
    /// </summary>
    [InitializeOnLoad]
    public static class PlayModeSelectionClearer
    {
        static PlayModeSelectionClearer()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }
        
        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            // Clear selection when entering Play mode
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                Selection.activeObject = null;
            }
        }
    }
}
