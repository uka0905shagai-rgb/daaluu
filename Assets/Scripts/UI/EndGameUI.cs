using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EndGameUI : MonoBehaviour
{
    public static EndGameUI Instance { get; private set; }

    private GameObject canvasObject;
    private GameObject overlayPanel;
    private GameObject contentPanel;
    private TextMeshProUGUI iconText;
    private TextMeshProUGUI winnerText;
    private TextMeshProUGUI statsText;
    private TextMeshProUGUI debtsText;
    private Button restartButton;
    private Button newGameButton;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        BuildUI();

        if (GameManager.Instance != null)
            GameManager.Instance.OnGameStateChanged += OnGameStateChanged;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnGameStateChanged -= OnGameStateChanged;
    }

    private void OnGameStateChanged(GameState newState)
    {
        if (newState != GameState.GameEnd)
            Hide();
    }

    private void BuildUI()
    {
        if (canvasObject != null)
            return;

        canvasObject = new GameObject("EndGameCanvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();
        DontDestroyOnLoad(canvasObject);

        overlayPanel = new GameObject("Overlay");
        overlayPanel.transform.SetParent(canvasObject.transform, false);
        Image bgImage = overlayPanel.AddComponent<Image>();
        bgImage.color = new Color(0f, 0f, 0f, 0.6f);
        RectTransform overlayRt = overlayPanel.GetComponent<RectTransform>();
        overlayRt.anchorMin = Vector2.zero;
        overlayRt.anchorMax = Vector2.one;
        overlayRt.offsetMin = Vector2.zero;
        overlayRt.offsetMax = Vector2.zero;

        // Content panel (center)
        contentPanel = new GameObject("Content");
        contentPanel.transform.SetParent(overlayPanel.transform, false);
        Image contentImage = contentPanel.AddComponent<Image>();
        contentImage.color = new Color(0.12f, 0.12f, 0.12f, 0.95f);
        RectTransform contentRt = contentPanel.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0.2f, 0.2f);
        contentRt.anchorMax = new Vector2(0.8f, 0.8f);
        contentRt.offsetMin = Vector2.zero;
        contentRt.offsetMax = Vector2.zero;

        // Trophy / Icon
        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(contentPanel.transform, false);
        iconText = iconObj.AddComponent<TextMeshProUGUI>();
        iconText.alignment = TextAlignmentOptions.Center;
        iconText.fontSize = 72;
        iconText.text = "🏆";
        RectTransform iconRt = iconText.GetComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0.4f, 0.75f);
        iconRt.anchorMax = new Vector2(0.6f, 0.95f);
        iconRt.offsetMin = Vector2.zero;
        iconRt.offsetMax = Vector2.zero;

        GameObject textObj = new GameObject("WinnerText");
        textObj.transform.SetParent(contentPanel.transform, false);
        winnerText = textObj.AddComponent<TextMeshProUGUI>();
        winnerText.alignment = TextAlignmentOptions.Center;
        winnerText.fontSize = 48;
        RectTransform textRt = winnerText.GetComponent<RectTransform>();
        textRt.anchorMin = new Vector2(0.1f, 0.55f);
        textRt.anchorMax = new Vector2(0.9f, 0.75f);
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        GameObject statsObj = new GameObject("WinnerStats");
        statsObj.transform.SetParent(contentPanel.transform, false);
        statsText = statsObj.AddComponent<TextMeshProUGUI>();
        statsText.alignment = TextAlignmentOptions.Center;
        statsText.fontSize = 22;
        RectTransform statsRt = statsText.GetComponent<RectTransform>();
        statsRt.anchorMin = new Vector2(0.1f, 0.38f);
        statsRt.anchorMax = new Vector2(0.9f, 0.55f);
        statsRt.offsetMin = Vector2.zero;
        statsRt.offsetMax = Vector2.zero;

        GameObject debtsObj = new GameObject("DebtsText");
        debtsObj.transform.SetParent(contentPanel.transform, false);
        debtsText = debtsObj.AddComponent<TextMeshProUGUI>();
        debtsText.alignment = TextAlignmentOptions.Center;
        debtsText.fontSize = 20;
        RectTransform debtsRt = debtsText.GetComponent<RectTransform>();
        debtsRt.anchorMin = new Vector2(0.1f, 0.2f);
        debtsRt.anchorMax = new Vector2(0.9f, 0.38f);
        debtsRt.offsetMin = Vector2.zero;
        debtsRt.offsetMax = Vector2.zero;

        // Buttons container
        GameObject btnContainer = new GameObject("Buttons");
        btnContainer.transform.SetParent(contentPanel.transform, false);
        RectTransform btnContRt = btnContainer.AddComponent<RectTransform>();
        btnContRt.anchorMin = new Vector2(0.1f, 0.03f);
        btnContRt.anchorMax = new Vector2(0.9f, 0.18f);
        btnContRt.offsetMin = Vector2.zero;
        btnContRt.offsetMax = Vector2.zero;

        // Restart button (left)
        GameObject buttonObj = new GameObject("RestartButton");
        buttonObj.transform.SetParent(btnContainer.transform, false);
        Image btnImage = buttonObj.AddComponent<Image>();
        btnImage.color = new Color(0.9f, 0.9f, 0.95f, 1f);
        restartButton = buttonObj.AddComponent<Button>();
        RectTransform btnRt = buttonObj.GetComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(0f, 0f);
        btnRt.anchorMax = new Vector2(0.48f, 1f);
        btnRt.offsetMin = Vector2.zero;
        btnRt.offsetMax = Vector2.zero;

        GameObject btnTextObj = new GameObject("BtnText");
        btnTextObj.transform.SetParent(buttonObj.transform, false);
        TextMeshProUGUI btnText = btnTextObj.AddComponent<TextMeshProUGUI>();
        btnText.alignment = TextAlignmentOptions.Center;
        btnText.fontSize = 24;
        btnText.text = "Restart";
        RectTransform btnTextRt = btnText.GetComponent<RectTransform>();
        btnTextRt.anchorMin = Vector2.zero;
        btnTextRt.anchorMax = Vector2.one;
        btnTextRt.offsetMin = Vector2.zero;
        btnTextRt.offsetMax = Vector2.zero;

        // New Game (clear debts) button (right)
        GameObject ngButtonObj = new GameObject("NewGameButton");
        ngButtonObj.transform.SetParent(btnContainer.transform, false);
        Image ngImage = ngButtonObj.AddComponent<Image>();
        ngImage.color = new Color(0.8f, 0.9f, 1f, 1f);
        newGameButton = ngButtonObj.AddComponent<Button>();
        RectTransform ngRt = ngButtonObj.GetComponent<RectTransform>();
        ngRt.anchorMin = new Vector2(0.52f, 0f);
        ngRt.anchorMax = new Vector2(1f, 1f);
        ngRt.offsetMin = Vector2.zero;
        ngRt.offsetMax = Vector2.zero;

        GameObject ngTextObj = new GameObject("NGText");
        ngTextObj.transform.SetParent(ngButtonObj.transform, false);
        TextMeshProUGUI ngText = ngTextObj.AddComponent<TextMeshProUGUI>();
        ngText.alignment = TextAlignmentOptions.Center;
        ngText.fontSize = 20;
        ngText.text = "New Game\n(clear debts)";
        RectTransform ngTextRt = ngText.GetComponent<RectTransform>();
        ngTextRt.anchorMin = Vector2.zero;
        ngTextRt.anchorMax = Vector2.one;
        ngTextRt.offsetMin = Vector2.zero;
        ngTextRt.offsetMax = Vector2.zero;

        restartButton.onClick.AddListener(OnRestartClicked);
        newGameButton.onClick.AddListener(OnNewGameClicked);

        canvasObject.SetActive(true);
        overlayPanel.SetActive(false);
    }

    public void Show(Player winner)
    {
        if (winner == null)
            return;

        if (canvasObject == null)
            BuildUI();

        winnerText.text = $"{winner.playerName} wins!";
        statsText.text = $"Tea: {winner.GetTeaCardsCollected()}/{GameRules.TOTAL_TEA_CARDS}\nTricks: {winner.GetTricksWon()}";

        // Build debts info: show who owes the winner and totals
        string debtsSummary = "";
        int totalOwed = 0;
        if (GameManager.Instance != null)
        {
            var map = GameManager.Instance.GetDebtMap();
            foreach (var debtorEntry in map)
            {
                int debtorId = debtorEntry.Key;
                foreach (var creditorEntry in debtorEntry.Value)
                {
                    int creditorId = creditorEntry.Key;
                    int amount = creditorEntry.Value;
                    if (amount <= 0) continue;
                    if (creditorId == winner.playerID)
                    {
                        Player debtor = GameManager.Instance.GetPlayerByID(debtorId);
                        string debtorName = debtor != null ? debtor.playerName : $"P{debtorId}";
                        debtsSummary += $"{debtorName} owes {amount}\n";
                        totalOwed += amount;
                    }
                }
            }
        }

        if (string.IsNullOrEmpty(debtsSummary))
            debtsText.text = "No outstanding debts to winner.";
        else
            debtsText.text = $"Debts owed to winner ({totalOwed}):\n" + debtsSummary.TrimEnd();

        overlayPanel.SetActive(true);
    }

    public void Hide()
    {
        if (overlayPanel != null)
            overlayPanel.SetActive(false);
    }

    private void OnRestartClicked()
    {
        Hide();
        if (GameManager.Instance != null)
        {
            GameManager.Instance.InitializeGame();
        }
    }

    private void OnNewGameClicked()
    {
        Hide();
        if (GameManager.Instance != null)
        {
            // Start a fresh game and clear debts
            GameManager.Instance.InitializeGame(true);
        }
    }
}
