using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class SpriteImportTable
{
    // name -> (width, height, bodyCenterX, bodyCenterYFromTop, bodyDiameter) in source pixels
    public static readonly Dictionary<string, float[]> Body = new Dictionary<string, float[]>
    {
        { "bounce", new float[] { 184, 139, 91, 52, 80 } },
        { "crouch", new float[] { 116, 108, 58, 54, 92 } },
        { "freeze", new float[] { 167, 151, 75, 70, 108 } },
        { "grow", new float[] { 193, 183, 82, 87, 132 } },
        { "heal", new float[] { 169, 148, 80, 69, 92 } },
        { "idle", new float[] { 120, 154, 58, 100, 86 } },
        { "invisibility", new float[] { 175, 140, 58, 60, 104 } },
        { "spin", new float[] { 178, 178, 96, 91, 80 } },
        { "stun", new float[] { 178, 157, 91, 71, 88 } },
        { "teleport", new float[] { 196, 159, 96, 77, 104 } },
    };
}

public class SpriteImport : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (!assetPath.Contains("Resources/Sprites")) return;
        var ti = (TextureImporter)assetImporter;
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.mipmapEnabled = false;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.alphaIsTransparency = true;
        var s = new TextureImporterSettings();
        ti.ReadTextureSettings(s);
        s.filterMode = FilterMode.Point;
        string name = System.IO.Path.GetFileNameWithoutExtension(assetPath);
        if (SpriteImportTable.Body.TryGetValue(name, out var b))
        {
            s.spriteAlignment = (int)SpriteAlignment.Custom;
            s.spritePivot = new Vector2(b[2] / b[0], 1f - b[3] / b[1]);
            s.spritePixelsPerUnit = b[4];
        }
        ti.SetTextureSettings(s);
    }
}
