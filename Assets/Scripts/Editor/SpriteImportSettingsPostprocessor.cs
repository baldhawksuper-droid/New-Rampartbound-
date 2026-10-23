#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Rampartbound.Editor
{
    /// <summary>
    /// AssetPostprocessor otomatis untuk standardisasi import settings sprite 2D Rampartbound: Merge Defense.
    /// Menjamin seluruh sprite di Assets/Art/Sprites mematuhi:
    /// - Pixels Per Unit (PPU): 100
    /// - Filter Mode: Bilinear
    /// - Texture Compression: Uncompressed (atau Normal)
    /// - Max Texture Size: 2048
    /// - Pivot: Center (0.5, 0.5)
    /// </summary>
    public class SpriteImportSettingsPostprocessor : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/Art/Sprites/")) return;

            TextureImporter importer = (TextureImporter)assetImporter;
            if (importer == null) return;

            bool modified = false;

            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                modified = true;
            }

            if (importer.spritePixelsPerUnit != 100f)
            {
                importer.spritePixelsPerUnit = 100f;
                modified = true;
            }

            if (importer.filterMode != FilterMode.Bilinear)
            {
                importer.filterMode = FilterMode.Bilinear;
                modified = true;
            }

            if (importer.maxTextureSize < 2048)
            {
                importer.maxTextureSize = 2048;
                modified = true;
            }

            if (importer.spritePivot != new Vector2(0.5f, 0.5f))
            {
                importer.spritePivot = new Vector2(0.5f, 0.5f);
                modified = true;
            }

            if (!importer.alphaIsTransparency)
            {
                importer.alphaIsTransparency = true;
                modified = true;
            }

            if (importer.textureCompression != TextureImporterCompression.Uncompressed)
            {
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                modified = true;
            }

            if (modified)
            {
                Debug.Log($"[SpritePostprocessor] Standar sprite diterapkan ke: {assetPath}");
            }
        }

        [MenuItem("Rampartbound/Validate and Fix Sprite Import Settings", false, 20)]
        public static void ValidateAllSprites()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Art/Sprites" });
            int fixedCount = 0;

            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;

                bool needsReimport = false;

                if (importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    needsReimport = true;
                }

                if (importer.spritePixelsPerUnit != 100f)
                {
                    importer.spritePixelsPerUnit = 100f;
                    needsReimport = true;
                }

                if (importer.filterMode != FilterMode.Bilinear)
                {
                    importer.filterMode = FilterMode.Bilinear;
                    needsReimport = true;
                }

                if (importer.maxTextureSize < 2048)
                {
                    importer.maxTextureSize = 2048;
                    needsReimport = true;
                }

                if (importer.spritePivot != new Vector2(0.5f, 0.5f))
                {
                    importer.spritePivot = new Vector2(0.5f, 0.5f);
                    needsReimport = true;
                }

                if (!importer.alphaIsTransparency)
                {
                    importer.alphaIsTransparency = true;
                    needsReimport = true;
                }

                if (importer.textureCompression != TextureImporterCompression.Uncompressed)
                {
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    needsReimport = true;
                }

                if (needsReimport)
                {
                    fixedCount++;
                    importer.SaveAndReimport();
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[SpritePostprocessor] Validasi Selesai! Sebanyak {fixedCount} sprite disesuaikan ke standar resmi PPU 100, Filter Bilinear, Pivot Center.");
        }
    }
}
#endif
