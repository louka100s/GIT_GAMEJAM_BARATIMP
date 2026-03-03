using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HUDManager : MonoBehaviour
{
    [Header("Stars")]
    public Image[] starImages = new Image[5];
    public Sprite starFull;
    public Sprite starEmpty;

    [Header("Money")]
    public Text moneyText;
    private Color moneyDefaultColor = Color.white;

    [Header("Game Over")]
    public GameObject gameOverPanel;
    public Text gameOverReasonText;
    public Text gameOverScoreText;
    public Button restartButton;

    // Référence de la coroutine pour pouvoir l'interrompre proprement
    private Coroutine _flashCoroutine;

    void Start()
    {
        gameOverPanel.SetActive(false);
        GameManager.Instance.OnMoneyChanged += UpdateMoney;
        GameManager.Instance.OnStarsChanged += UpdateStars;
        GameManager.Instance.OnGameOver     += ShowGameOver;

        // Propriétés en PascalCase car _money/_stars sont privés dans GameManager
        UpdateMoney(GameManager.Instance.Money);
        UpdateStars(GameManager.Instance.Stars);
    }

    void OnDestroy()
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.OnMoneyChanged -= UpdateMoney;
        GameManager.Instance.OnStarsChanged -= UpdateStars;
        GameManager.Instance.OnGameOver     -= ShowGameOver;
    }

    private void UpdateMoney(int value)
    {
        moneyText.text = "$" + value;

        if (_flashCoroutine != null)
            StopCoroutine(_flashCoroutine);
        _flashCoroutine = StartCoroutine(FlashMoney(value));
    }

    private IEnumerator FlashMoney(int value)
    {
        moneyText.color = value >= 0 ? Color.green : Color.red;
        yield return new WaitForSeconds(0.3f);
        moneyText.color = moneyDefaultColor;
        _flashCoroutine = null;
    }

    private void UpdateStars(int value)
    {
        for (int i = 0; i < starImages.Length; i++)
        {
            bool filled = i < value;

            // Sprite : starFull remplie, starEmpty vide (ou starFull seul pour les deux)
            if (starFull != null)
                starImages[i].sprite = (filled || starEmpty == null) ? starFull : starEmpty;

            // La couleur s'applique toujours pour tinter le sprite : doré / gris
            starImages[i].color = filled
                ? new Color(1f, 0.84f, 0f, 1f)         // doré
                : new Color(0.3f, 0.3f, 0.3f, 1f);     // gris
        }
    }

    private void ShowGameOver(string reason)
    {
        gameOverPanel.SetActive(true);
        gameOverReasonText.text = reason == "money" ? "FAILLITE" : "FERMÉ — 0 ÉTOILES";
        gameOverScoreText.text  = "Score final : "
            + GameManager.Instance.Stars + " étoiles | $"
            + GameManager.Instance.Money;
        Time.timeScale = 0f;
    }

    public void OnRestartClicked()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
