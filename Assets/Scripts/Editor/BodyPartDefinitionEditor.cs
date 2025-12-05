using UnityEngine;
using UnityEditor;

namespace HairRemovalSim.Core
{
#if UNITY_EDITOR
    /// <summary>
    /// BodyPartDefinitionのカスタムエディタ
    /// UV領域を視覚的に確認できるプレビューを追加
    /// </summary>
    [CustomEditor(typeof(BodyPartDefinition))]
    public class BodyPartDefinitionEditor : Editor
    {
        private const float PREVIEW_SIZE = 256f;
        
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            
            BodyPartDefinition def = (BodyPartDefinition)target;
            
            // UV領域のビジュアルプレビュー
            if (def.uvRegions != null && def.uvRegions.Count > 0)
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField("UV Regions Preview", EditorStyles.boldLabel);
                
                // 正方形のプレビュー領域を確保
                Rect previewRect = GUILayoutUtility.GetRect(PREVIEW_SIZE, PREVIEW_SIZE);
                
                // 背景画像の表示（オプション）
                Texture2D backgroundTexture = def.previewTexture;
                if (backgroundTexture != null)
                {
                    GUI.DrawTexture(previewRect, backgroundTexture, ScaleMode.StretchToFill);
                }
                else
                {
                    // 背景（UV空間全体）
                    EditorGUI.DrawRect(previewRect, new Color(0.2f, 0.2f, 0.2f));
                }
                
                // グリッド線
                Handles.BeginGUI();
                Handles.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                for (int i = 0; i <= 10; i++)
                {
                    float t = i / 10f;
                    // 横線
                    Vector3 start = new Vector3(previewRect.x, previewRect.y + previewRect.height * t, 0);
                    Vector3 end = new Vector3(previewRect.x + previewRect.width, previewRect.y + previewRect.height * t, 0);
                    Handles.DrawLine(start, end);
                    
                    // 縦線
                    start = new Vector3(previewRect.x + previewRect.width * t, previewRect.y, 0);
                    end = new Vector3(previewRect.x + previewRect.width * t, previewRect.y + previewRect.height, 0);
                    Handles.DrawLine(start, end);
                }
                
                // UV領域を描画
                foreach (var region in def.uvRegions)
                {
                    Rect uvRect = region.rect;
                    
                    // UV座標をスクリーン座標に変換（UVは下が0、上が1なので反転）
                    Rect screenRect = new Rect(
                        previewRect.x + uvRect.x * previewRect.width,
                        previewRect.y + (1 - uvRect.y - uvRect.height) * previewRect.height,
                        uvRect.width * previewRect.width,
                        uvRect.height * previewRect.height
                    );
                    
                    // 領域の塗りつぶし（半透明）
                    EditorGUI.DrawRect(screenRect, new Color(0.3f, 0.7f, 1f, 0.3f));
                    
                    // 枠線
                    Handles.color = new Color(0.3f, 0.7f, 1f, 1f);
                    Handles.DrawSolidRectangleWithOutline(screenRect, Color.clear, Handles.color);
                    
                    // 説明テキスト
                    if (!string.IsNullOrEmpty(region.description))
                    {
                        GUIStyle style = new GUIStyle(EditorStyles.label);
                        style.normal.textColor = Color.white;
                        style.fontSize = 10;
                        style.alignment = TextAnchor.MiddleCenter;
                        style.fontStyle = FontStyle.Bold;
                        
                        // 背景を追加（読みやすくするため）
                        Rect textBgRect = screenRect;
                        textBgRect.height = 20;
                        textBgRect.y += (screenRect.height - 20) * 0.5f;
                        EditorGUI.DrawRect(textBgRect, new Color(0, 0, 0, 0.5f));
                        
                        GUI.Label(screenRect, region.description, style);
                    }
                }
                
                Handles.EndGUI();
                
                // 情報表示
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField($"Total Regions: {def.uvRegions.Count}");
                EditorGUILayout.LabelField($"Total Area: {def.GetTotalArea():P1}");
                EditorGUILayout.LabelField($"Mask Value: {def.maskValue:F2}");
            }
            
            // クイック追加ボタン
            EditorGUILayout.Space(10);
            if (GUILayout.Button("Add New UV Region"))
            {
                Undo.RecordObject(def, "Add UV Region");
                def.uvRegions.Add(new UVRegion());
                EditorUtility.SetDirty(def);
            }
            
            // 背景画像のヘルプ
            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox(
                "Preview Textureにテクスチャを設定すると、プレビューの背景に表示されます。\n" +
                "UV レイアウトや実際のテクスチャを設定すると位置確認が簡単になります。",
                MessageType.Info);
        }
    }
#endif
}
