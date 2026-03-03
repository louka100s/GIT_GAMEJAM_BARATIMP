using UnityEngine;

/// <summary>
/// Génère un sprite carré de couleur unie à l'exécution.
/// Utilisé comme placeholder visuel pour les clients et les bulles de commande.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
[ExecuteAlways]
public class PlaceholderColorSprite : MonoBehaviour
{
    [SerializeField] private Color color     = Color.white;
    [SerializeField, Range(2, 128)] private int pixelSize = 32;

    private void Awake() => Apply();

#if UNITY_EDITOR
    private void OnValidate() => Apply();
#endif

    /// <summary>Crée une texture carrée unie et l'assigne au SpriteRenderer.</summary>
    private void Apply()
    {
        var tex    = new Texture2D(pixelSize, pixelSize) { filterMode = FilterMode.Point };
        var pixels = new Color[pixelSize * pixelSize];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
        tex.SetPixels(pixels);
        tex.Apply();

        GetComponent<SpriteRenderer>().sprite = Sprite.Create(
            tex,
            new Rect(0, 0, pixelSize, pixelSize),
            new Vector2(0.5f, 0.5f),
            pixelSize
        );
    }
}
