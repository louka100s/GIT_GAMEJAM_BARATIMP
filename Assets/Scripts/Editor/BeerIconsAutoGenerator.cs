using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Génère automatiquement les 3 icônes de bière (PNG 32x32) dans Assets/HUD_Canvas/BeerIcons/
/// à chaque recompilation si les fichiers n'existent pas encore.
/// </summary>
[InitializeOnLoad]
public static class BeerIconsAutoGenerator
{
    private const string OUTPUT_FOLDER = "Assets/HUD_Canvas/BeerIcons";

    static BeerIconsAutoGenerator()
    {
        // Defer after Unity has fully initialized to avoid AssetDatabase conflicts
        EditorApplication.delayCall += GenerateAll;
    }

    private static void GenerateAll()
    {
        EditorApplication.delayCall -= GenerateAll;
        GenerateIfMissing("IconBlonde", new Color(1.00f, 0.84f, 0.10f)); // jaune doré
        GenerateIfMissing("IconRousse", new Color(0.80f, 0.38f, 0.08f)); // cuivré
        GenerateIfMissing("IconBrune",  new Color(0.22f, 0.10f, 0.04f)); // marron foncé
    }

    private static void GenerateIfMissing(string spriteName, Color color)
    {
        string assetPath = $"{OUTPUT_FOLDER}/{spriteName}.png";
        string fullPath  = Path.Combine(Application.dataPath, "..",  assetPath);

        if (File.Exists(fullPath)) return;

        // Crée le dossier si nécessaire
        string fullFolder = Path.Combine(Application.dataPath, "..", OUTPUT_FOLDER);
        if (!Directory.Exists(fullFolder))
            Directory.CreateDirectory(fullFolder);

        // Génère la texture 32×32 unicolore
        const int SIZE = 32;
        var tex    = new Texture2D(SIZE, SIZE) { filterMode = FilterMode.Point };
        var pixels = new Color[SIZE * SIZE];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
        tex.SetPixels(pixels);
        tex.Apply();

        File.WriteAllBytes(fullPath, tex.EncodeToPNG());

        // Refresh puis import pour que Unity reconnaisse le fichier
        AssetDatabase.Refresh();

        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            Debug.LogWarning($"[BeerIconsAutoGenerator] Importer introuvable pour {assetPath}. Re-ouvre Unity.");
            return;
        }

        importer.textureType         = TextureImporterType.Sprite;
        importer.spriteImportMode    = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled       = false;
        importer.filterMode          = FilterMode.Point;
        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();

        Debug.Log($"[BeerIconsAutoGenerator] Sprite créé : {assetPath}");
    }
}
