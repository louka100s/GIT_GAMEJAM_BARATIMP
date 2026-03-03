using System;
using UnityEngine;

/// <summary>
/// Singleton central du jeu. Gère l'argent et les étoiles,
/// expose des events pour l'UI et vérifie les conditions de game over.
/// </summary>
public class GameManager : MonoBehaviour
{
    // ── Singleton ────────────────────────────────────────────────────────
    public static GameManager Instance { get; private set; }

    // ── Configuration (modifiable en Inspector) ───────────────────────────
    [Header("Économie — Départ")]
    [Tooltip("Argent au début de la partie.")]
    [SerializeField] private int startingMoney = 10;

    [Header("Économie — Transactions")]
    [Tooltip("Argent dépensé chaque fois qu'un verre touche une tireuse.")]
    [SerializeField, Range(0, 10)] private int beerCost = 1;

    [Tooltip("Argent gagné quand un client est satisfait.")]
    [SerializeField, Range(0, 20)] private int beerSuccessReward = 3;

    [Header("Étoiles")]
    [Tooltip("Nombre maximum d'étoiles accumulables.")]
    [SerializeField, Range(1, 10)] private int maxStars = 5;

    [Header("Patience des clients")]
    [Tooltip("Patience de départ (secondes). Diminue selon la phase de jeu.")]
    [SerializeField] private float startingPatience = 20f;
    [Tooltip("Patience minimale plancher, jamais en dessous.")]
    [SerializeField] private float minPatience = 8f;

    // ── Runtime (visible en Inspector, lecture seule pendant le jeu) ──────
    [Header("Runtime — État actuel")]
    [SerializeField, ReadOnly] private int _money;
    [SerializeField, ReadOnly] private int _stars;
    [SerializeField, ReadOnly] private bool _hasEarnedAStar;
    [SerializeField, ReadOnly] private bool _isGameOver;

    // ── Accesseurs publics ────────────────────────────────────────────────
    public int  Money      => _money;
    public int  Stars      => _stars;
    public int  MaxStars   => maxStars;
    public bool IsGameOver => _isGameOver;

    // ── Events UI ────────────────────────────────────────────────────────
    public event Action<int>    OnMoneyChanged;
    public event Action<int>    OnStarsChanged;
    public event Action<string> OnGameOver; // "money" | "stars"

    // ── Unity ────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        _money = startingMoney;
        _stars = 0;
    }

    // ── API publique ─────────────────────────────────────────────────────

    /// <summary>Retourne la patience adaptée à la phase de jeu actuelle.</summary>
    public float GetCurrentPatience()
    {
        float elapsed = Time.timeSinceLevelLoad;
        // Diminue linéairement de startingPatience à minPatience sur 150 s
        float t = Mathf.Clamp01(elapsed / 150f);
        return Mathf.Lerp(startingPatience, minPatience, t);
    }

    /// <summary>Appelé quand un verre entre dans un trigger tireuse. Coûte beerCost argent.</summary>
    public void SpendForBeer()
    {
        if (_isGameOver) return;
        _money -= beerCost;
        OnMoneyChanged?.Invoke(_money);
        CheckGameOver();
    }

    /// <summary>Appelé quand un client reçoit la bonne bière. Rapporte beerSuccessReward argent + 1 étoile.</summary>
    public void ServeBeerSuccess()
    {
        if (_isGameOver) return;
        _money += beerSuccessReward;
        OnMoneyChanged?.Invoke(_money);
        AddStar();
    }

    /// <summary>Appelé quand le joueur sert la mauvaise bière ou que la bière déborde.</summary>
    public void ServeBeerFail()
    {
        if (_isGameOver) return;
        LoseStar();
    }

    /// <summary>Appelé quand un client part sans être servi (timeout).</summary>
    public void ClientTimeout()
    {
        if (_isGameOver) return;
        LoseStar();
    }

    /// <summary>Vérifie les deux conditions de game over et déclenche l'event si nécessaire.</summary>
    public void CheckGameOver()
    {
        if (_isGameOver) return;

        if (_money <= 0)        { TriggerGameOver("money"); return; }
        if (_stars <= 0 && _hasEarnedAStar) { TriggerGameOver("stars"); }
    }

    // ── Privé ─────────────────────────────────────────────────────────────

    private void AddStar()
    {
        _stars         = Mathf.Min(_stars + 1, maxStars);
        _hasEarnedAStar = true;
        OnStarsChanged?.Invoke(_stars);
    }

    private void LoseStar()
    {
        _stars = Mathf.Max(_stars - 1, 0);
        OnStarsChanged?.Invoke(_stars);
        CheckGameOver();
    }

    private void TriggerGameOver(string reason)
    {
        _isGameOver = true;
        Debug.Log($"[GameManager] Game Over — raison : {reason}");
        OnGameOver?.Invoke(reason);
    }
}
