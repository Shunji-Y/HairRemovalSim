using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace HairRemovalSim.Core
{
#if UNITY_EDITOR
    /// <summary>
    /// 部位別バイナリマスクテクスチャを自動生成するエディタツール
    /// 14個の部位それぞれに白黒のマスクテクスチャを生成
    /// </summary>
    public class BodyPartMaskGenerator : EditorWindow
    {
        private BodyPartsDatabase database;
        private int textureSize = 2048;
        private string outputPath = "Assets/Textures/PartMasks/";
        
        [MenuItem("Tools/Hair Removal Sim/Generate Body Part Masks")]
        public static void ShowWindow()
        {
            GetWindow<BodyPartMaskGenerator>("Body Part Mask Generator");
        }
        
        private void OnGUI()
        {
            GUILayout.Label("Per-Part Binary Mask Generator", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);
            
            EditorGUILayout.HelpBox(
                "各部位ごとに白黒のバイナリマスクを生成します。\n" +
                "白 = その部位、黒 = それ以外",
                MessageType.Info
            );
            
            EditorGUILayout.Space(10);
            
            // Database選択
            database = (BodyPartsDatabase)EditorGUILayout.ObjectField(
                "Body Parts Database", 
                database, 
                typeof(BodyPartsDatabase), 
                false
            );
            
            // テクスチャサイズ
            textureSize = EditorGUILayout.IntPopup(
                "Texture Size", 
                textureSize, 
                new string[] { "512", "1024", "2048", "4096" },
                new int[] { 512, 1024, 2048, 4096 }
            );
            
            // 出力パス
            EditorGUILayout.BeginHorizontal();
            outputPath = EditorGUILayout.TextField("Output Path", outputPath);
            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                string path = EditorUtility.SaveFolderPanel("Select Output Folder", "Assets", "");
                if (!string.IsNullOrEmpty(path))
                {
                    outputPath = "Assets" + path.Substring(Application.dataPath.Length) + "/";
                }
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(20);
            
            // 生成ボタン
            GUI.enabled = database != null;
            if (GUILayout.Button("Generate 14 Binary Masks", GUILayout.Height(40)))
            {
                GenerateAllMasks();
            }
            GUI.enabled = true;
            
            EditorGUILayout.Space(10);
            
            // 情報表示
            if (database != null)
            {
                EditorGUILayout.LabelField("Parts in Database:", EditorStyles.boldLabel);
                foreach (var part in database.allParts)
                {
                    EditorGUILayout.LabelField($"  • {part.partName} (Mat {part.materialIndex})", EditorStyles.miniLabel);
                }
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Body Parts Databaseを選択してください。",
                    MessageType.Warning
                );
            }
        }
        
        private void GenerateAllMasks()
        {
            if (database == null)
            {
                EditorUtility.DisplayDialog("Error", "Body Parts Databaseが選択されていません", "OK");
                return;
            }
            
            // 出力フォルダ作成
            CreateOutputFolder();
            
            int generatedCount = 0;
            
            // 各部位ごとにマスクを生成
            foreach (var part in database.allParts)
            {
                GenerateBinaryMaskForPart(part);
                generatedCount++;
            }
            
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog(
                "Complete", 
                $"{generatedCount}個のバイナリマスクを生成しました！\n{outputPath}", 
                "OK"
            );
        }
        
        private void CreateOutputFolder()
        {
            if (!AssetDatabase.IsValidFolder(outputPath.TrimEnd('/')))
            {
                string[] folders = outputPath.TrimEnd('/').Split('/');
                string currentPath = folders[0];
                for (int i = 1; i < folders.Length; i++)
                {
                    string newPath = currentPath + "/" + folders[i];
                    if (!AssetDatabase.IsValidFolder(newPath))
                    {
                        AssetDatabase.CreateFolder(currentPath, folders[i]);
                    }
                    currentPath = newPath;
                }
            }
        }
        
        private void GenerateBinaryMaskForPart(BodyPartDefinition part)
        {
            // テクスチャ作成（白黒）
            Texture2D mask = new Texture2D(textureSize, textureSize, TextureFormat.RGB24, false);
            
            // 黒で初期化
            Color[] pixels = new Color[textureSize * textureSize];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.black;
            }
            
            // この部位のUV領域を白で塗る
            foreach (var region in part.uvRegions)
            {
                PaintRegion(pixels, region.rect, Color.white);
            }
            
            mask.SetPixels(pixels);
            mask.Apply();
            
            // PNG保存
            string fileName = $"Mask_{part.partName}.png";
            string fullPath = outputPath + fileName;
            
            byte[] pngData = mask.EncodeToPNG();
            System.IO.File.WriteAllBytes(fullPath, pngData);
            
            Debug.Log($"Generated: {fullPath}");
            
            // インポート設定
            AssetDatabase.ImportAsset(fullPath);
            TextureImporter importer = AssetImporter.GetAtPath(fullPath) as TextureImporter;
            if (importer != null)
            {
                importer.sRGBTexture = false; // リニア
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.filterMode = FilterMode.Point;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.SaveAndReimport();
            }
            
            DestroyImmediate(mask);
        }
        
        private void PaintRegion(Color[] pixels, Rect uvRect, Color color)
        {
            // UV座標 (0,0=左下, 1,1=右上) をピクセル座標に変換
            int startX = Mathf.FloorToInt(uvRect.x * textureSize);
            int startY = Mathf.FloorToInt(uvRect.y * textureSize);
            int endX = Mathf.CeilToInt((uvRect.x + uvRect.width) * textureSize);
            int endY = Mathf.CeilToInt((uvRect.y + uvRect.height) * textureSize);
            
            // クランプ
            startX = Mathf.Clamp(startX, 0, textureSize - 1);
            startY = Mathf.Clamp(startY, 0, textureSize - 1);
            endX = Mathf.Clamp(endX, 0, textureSize);
            endY = Mathf.Clamp(endY, 0, textureSize);
            
            // 領域を白で塗る
            for (int y = startY; y < endY; y++)
            {
                for (int x = startX; x < endX; x++)
                {
                    int index = y * textureSize + x;
                    pixels[index] = color;
                }
            }
        }
    }
#endif
}
