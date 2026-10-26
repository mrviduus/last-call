using UnityEditor;
using UnityEngine;

// Pixel art under Resources/Art: crisp, uncompressed, and readable so palettes can be swapped at runtime.
public class PixelImport : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/Resources/Art/")) return;
        var t = (TextureImporter)assetImporter;
        t.textureType = TextureImporterType.Default;
        t.isReadable = true;
        t.filterMode = FilterMode.Point;
        t.textureCompression = TextureImporterCompression.Uncompressed;
        t.mipmapEnabled = false;
        t.npotScale = TextureImporterNPOTScale.None;
        t.wrapMode = TextureWrapMode.Clamp;
        t.alphaIsTransparency = true;
    }
}
