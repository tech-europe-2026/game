using UnityEditor;

/// <summary>All generated art is pixel art: no filtering, compression, mips or power-of-two scaling.</summary>
public class PixelImport : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        var ti = (TextureImporter)assetImporter;
        ti.textureType = TextureImporterType.Default;
        ti.npotScale = TextureImporterNPOTScale.None;
        ti.mipmapEnabled = false;
        ti.filterMode = UnityEngine.FilterMode.Point;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.wrapMode = UnityEngine.TextureWrapMode.Clamp;
        ti.alphaIsTransparency = true;
        ti.maxTextureSize = 4096;
    }
}
