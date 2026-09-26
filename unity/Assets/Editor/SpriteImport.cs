using UnityEditor;
using UnityEngine;

public class SpriteImport : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (!assetPath.Contains("Resources/Sprites")) return;
        var ti = (TextureImporter)assetImporter;
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.mipmapEnabled = true;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.alphaIsTransparency = true;
        var s = new TextureImporterSettings();
        ti.ReadTextureSettings(s);
        s.filterMode = FilterMode.Bilinear;
        s.spriteAlignment = (int)SpriteAlignment.Center;
        s.spritePixelsPerUnit = 256;
        ti.SetTextureSettings(s);
    }
}
