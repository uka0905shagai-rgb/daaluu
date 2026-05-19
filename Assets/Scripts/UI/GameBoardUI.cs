using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.IO;

/// <summary>
/// Main UI controller for the game
/// </summary>
public class GameBoardUI : MonoBehaviour
{
    private const float CardWidth = 70f;
    private const float CardHeight = 96f;
    private const float StatusWidth = 150f;
    private const float StatusHeight = 60f;
    private const string RulesText = "Цай хураах дүрэм\n\n" +
                                     "1. Зорилго\n" +
                                     "Тоглогчид цай хурааж өрсөлдөнө. 10 цай хураасан эхний тоглогч ялна.\n\n" +
                                     "2. Мод тараалт\n" +
                                     "60 модноос 10 модыг цай гэж ялгаж, тоглогч бүрт 2 цай өгнө. Үлдсэн 50 модыг хольж, тоглогч бүрт 10 мод тараана.\n\n" +
                                     "3. Эхлэлт\n" +
                                     "Тоглоом эхлэхэд хүн тоглогч эхэлнэ. Жанлийг гараа харахаас өмнө нэрлэнэ.\n\n" +
                                     "4. Жанлий\n" +
                                     "Нэрлэсэн Жанлий мод нь тухайн тоглолтод бусад бүх модыг дийлнэ.\n\n" +
                                     "5. Тоглох\n" +
                                     "Нэг мод эсвэл ижил хосоор тоглож болно. Хосын дарамттай үед хосоор л хариулна.\n\n" +
                                     "6. Цай ба өр\n" +
                                     "Тоглолтын дараа цайны тооцоо хийгдэж, өр үүсвэл Check Debts товчоор шалгана.";

    [SerializeField] private Canvas gameCanvas;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private GameFlowController gameFlowController;
    [SerializeField] private TrickManager trickManager;

    // UI Elements
    [SerializeField] private TextMeshProUGUI gameStateText;
    [SerializeField] private TextMeshProUGUI currentPlayerText;
    [SerializeField] private TextMeshProUGUI janliiStatusText;
    [SerializeField] private Transform playerHandContainer;
    [SerializeField] private Transform boardPlayAreaContainer;
    [SerializeField] private Transform playerPositionsContainer;

    // Prefabs
    [SerializeField] private GameObject cardUIPrefab;
    [SerializeField] private GameObject playerStatusPrefab;

    // Game state
    private Dictionary<int, PlayerHandUI> playerHandUIs = new Dictionary<int, PlayerHandUI>();
    private Dictionary<int, PlayerStatusUI> playerStatusUIs = new Dictionary<int, PlayerStatusUI>();
    private Transform trickDisplayRoot;
    private Button rulesButton;
    private Button playButton;
    private Button leaseButton;
        private Button exitButton;
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    private GameObject rulesPanel;
    private TextMeshProUGUI rulesBodyText;
    private Button rulesCloseButton;
    private GameObject leasePanel;
    private TextMeshProUGUI leaseBodyText;
    private Button leaseCloseButton;
    private GameObject janliiPanel;
    private TextMeshProUGUI janliiPromptText;
    private Transform janliiButtonContainer;
    private Action<DaaluuPieceName> janliiSelectionCallback;
    private bool winnerStackActive = false;
    private int winnerStackPlayerId = -1;
    private readonly Dictionary<int, List<List<Card>>> completedPlaysByPlayer = new Dictionary<int, List<List<Card>>>();

    private void Start()
    {
        EnsureEventSystem();

        if (gameManager == null)
            gameManager = FindObjectOfType<GameManager>();
        if (gameFlowController == null)
            gameFlowController = FindObjectOfType<GameFlowController>();
        if (trickManager == null)
            trickManager = FindObjectOfType<TrickManager>();

        if (gameCanvas == null)
            gameCanvas = GetComponentInParent<Canvas>();

        // Auto-find text components if not assigned
        if (gameStateText == null)
            gameStateText = transform.Find("GameStatePanel")?.GetComponent<TextMeshProUGUI>();
        if (currentPlayerText == null)
            currentPlayerText = transform.Find("CurrentPlayerText")?.GetComponent<TextMeshProUGUI>();
        if (playerHandContainer == null)
            playerHandContainer = transform.Find("PlayerHandContainer");
        if (boardPlayAreaContainer == null)
            boardPlayAreaContainer = transform.Find("BoardPlayArea");
        if (playerPositionsContainer == null)
            playerPositionsContainer = transform.Find("PlayerPositions");

        EnsureRuntimePrefabs();
        EnsureBoardPlayArea();
        EnsureJanliiStatusText();
        EnsureRulesButton();
        EnsurePlayButton();
        EnsureLeaseButton();
        EnsureExitButton();

        // Debug logging
        Debug.Log($"GameBoardUI initialized");
        Debug.Log($"GameStateText found: {gameStateText != null}");
        Debug.Log($"CurrentPlayerText found: {currentPlayerText != null}");
        Debug.Log($"PlayerHandContainer found: {playerHandContainer != null}");
        Debug.Log($"GameManager found: {gameManager != null}");

        if (gameManager != null)
        {
            gameManager.OnGameStateChanged += UpdateGameState;
            UpdateGameState(gameManager.GetGameState());
        }
        else
            Debug.LogError("GameManager not found!");
    }

    private void EnsureEventSystem()
    {
        EventSystem existingEs = FindObjectOfType<EventSystem>();
        System.Type inputSystemModuleType = null;
        foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            inputSystemModuleType = asm.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule");
            if (inputSystemModuleType != null) break;
        }

        if (existingEs == null)
        {
            GameObject esGO = new GameObject("EventSystem", typeof(EventSystem));
            if (inputSystemModuleType != null)
                esGO.AddComponent(inputSystemModuleType);
            else
                esGO.AddComponent<StandaloneInputModule>();
            return;
        }

        if (inputSystemModuleType != null)
        {
            bool hasNew = false;
            foreach (var comp in existingEs.GetComponents<Component>())
            {
                if (comp.GetType().FullName == "UnityEngine.InputSystem.UI.InputSystemUIInputModule")
                {
                    hasNew = true;
                    break;
                }
            }

            if (!hasNew)
            {
                var standalone = existingEs.GetComponent<StandaloneInputModule>();
                if (standalone != null)
                {
                    DestroyImmediate(standalone);
                    existingEs.gameObject.AddComponent(inputSystemModuleType);
                }
            }
        }
    }

    private void OnDestroy()
    {
        if (gameManager != null)
            gameManager.OnGameStateChanged -= UpdateGameState;
    }

    private void UpdateGameState(GameState newState)
    {
        Debug.Log($"UpdateGameState called: {newState}");
        Debug.Log($"GameStateText is {(gameStateText != null ? "NOT null" : "NULL")}");
        
        if (gameStateText != null)
        {
            gameStateText.text = $"Game State: {newState}";
            Debug.Log($"Set text to: Game State: {newState}");
        }

        switch (newState)
        {
            case GameState.Setup:
                InitializeUI();
                break;
            case GameState.RankDeclaration:
                EnsurePlayerDisplays();
                SetPlayerHandVisible(false);
                UpdateAllPlayerDisplays();
                break;
            case GameState.Playing:
                EnsurePlayerDisplays();
                SetPlayerHandVisible(true);
                UpdateAllPlayerDisplays();
                break;
        }
    }

    private void InitializeUI()
    {
        ClearBoardPlayArea();
        CreatePlayerDisplays();
    }

    private void CreatePlayerDisplays()
    {
        ClearPlayerDisplays();
        ClearHistoryStackObjects();
        ClearBoardPlayArea();
        completedPlaysByPlayer.Clear();
        winnerStackActive = false;
        winnerStackPlayerId = -1;

        if (gameManager == null || playerHandContainer == null || playerPositionsContainer == null)
        {
            Debug.LogError("GameBoardUI is missing required scene references.");
            return;
        }

        List<Player> players = gameManager.GetAllPlayers();
        if (players.Count == 0)
            return;

        for (int i = 0; i < players.Count; i++)
        {
            Player player = players[i];

            // Create player hand UI
            if (player.IsHuman)
            {
                GameObject handUIObj = new GameObject($"PlayerHand");
                handUIObj.transform.SetParent(playerHandContainer, false);
                RectTransform handRect = handUIObj.AddComponent<RectTransform>();
                handRect.anchorMin = new Vector2(0.5f, 0f);
                handRect.anchorMax = new Vector2(0.5f, 0f);
                handRect.pivot = new Vector2(0.5f, 0f);
                handRect.anchoredPosition = new Vector2(0f, 16f);
                handRect.sizeDelta = new Vector2(820f, CardHeight);

                PlayerHandUI handUI = handUIObj.AddComponent<PlayerHandUI>();
                handUI.Initialize(player, cardUIPrefab, gameFlowController);
                playerHandUIs[player.playerID] = handUI;
            }

            // Create player status UI (for all players)
            GameObject statusObj = Instantiate(playerStatusPrefab, playerPositionsContainer);
            statusObj.name = $"PlayerStatus_{player.playerID}";
            statusObj.SetActive(true);
            PositionStatusUI(statusObj.GetComponent<RectTransform>(), i, player.IsHuman);

            PlayerStatusUI statusUI = statusObj.GetComponent<PlayerStatusUI>();
            if (statusUI != null)
            {
                statusUI.Initialize(player);
                playerStatusUIs[player.playerID] = statusUI;
            }
        }
    }

    private void ClearPlayerDisplays()
    {
        foreach (var handUI in playerHandUIs.Values)
        {
            if (handUI != null)
                Destroy(handUI.gameObject);
        }

        foreach (var statusUI in playerStatusUIs.Values)
        {
            if (statusUI != null)
                Destroy(statusUI.gameObject);
        }

        playerHandUIs.Clear();
        playerStatusUIs.Clear();
    }

    private void UpdateAllPlayerDisplays()
    {
        if (gameManager == null)
            return;

        DaaluuPieceName? janliiPiece = trickManager != null ? trickManager.GetJanliiPiece() : null;

        foreach (var handUI in playerHandUIs.Values)
        {
            handUI.SetJanliiPiece(janliiPiece);
            handUI.RefreshHandDisplay();
        }

        foreach (var statusUI in playerStatusUIs.Values)
        {
            statusUI.RefreshDisplay();
        }

        UpdateCurrentPlayerDisplay();
        UpdateJanliiStatus();
        UpdateBoardPlayArea();
    }

    private void UpdateCurrentPlayerDisplay()
    {
        if (gameManager.GetAllPlayers().Count == 0)
            return;

        Player currentPlayer = gameManager.GetCurrentPlayer();
        if (currentPlayerText != null)
            currentPlayerText.text = $"Current Player: {currentPlayer.playerName}";
    }

    public void RefreshDisplay()
    {
        UpdateAllPlayerDisplays();
    }

    public void SetSelectedCards(List<Card> selectedCards)
    {
        if (gameManager == null)
            return;

        Player humanPlayer = gameManager.GetHumanPlayer();
        if (humanPlayer == null)
            return;

        if (playerHandUIs.TryGetValue(humanPlayer.playerID, out PlayerHandUI handUI))
            handUI.SetSelectedCards(selectedCards);
    }

    public void SetPlayerHandVisible(bool isVisible)
    {
        if (playerHandContainer != null)
            playerHandContainer.gameObject.SetActive(isVisible);
    }

    public void ShowJanliiSelection(Player player, Action<DaaluuPieceName> onSelected)
    {
        if (gameCanvas == null)
            gameCanvas = GetComponentInParent<Canvas>();

        EnsureJanliiPanel();
        janliiSelectionCallback = onSelected;
        BuildJanliiButtons();

        if (janliiPromptText != null)
            janliiPromptText.text = "Жанлий нэрлэнэ үү (гараа харахаас өмнө)";

        if (janliiPanel != null)
            janliiPanel.SetActive(true);
    }

    public void HideJanliiSelection()
    {
        if (janliiPanel != null)
            janliiPanel.SetActive(false);
    }

    public void RefreshBoardPlayArea()
    {
        UpdateBoardPlayArea();
    }

    private void EnsureRuntimePrefabs()
    {
        if (cardUIPrefab == null)
            cardUIPrefab = CreateCardUIPrefab();

        if (playerStatusPrefab == null)
            playerStatusPrefab = CreatePlayerStatusPrefab();
    }

    private void EnsurePlayerDisplays()
    {
        if (playerHandUIs.Count == 0 || playerStatusUIs.Count == 0)
            InitializeUI();
    }

    private void EnsureBoardPlayArea()
    {
        if (boardPlayAreaContainer == null || trickDisplayRoot != null)
            return;

        GameObject rootObj = new GameObject("CurrentTrickDisplay");
        rootObj.transform.SetParent(boardPlayAreaContainer, false);

        RectTransform rootRect = rootObj.AddComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        Image backgroundImage = rootObj.AddComponent<Image>();
        backgroundImage.color = new Color(0f, 0f, 0f, 0f);

        HorizontalLayoutGroup layoutGroup = rootObj.AddComponent<HorizontalLayoutGroup>();
        layoutGroup.childAlignment = TextAnchor.MiddleCenter;
        layoutGroup.spacing = 12f;
        layoutGroup.childControlWidth = false;
        layoutGroup.childControlHeight = false;
        layoutGroup.childForceExpandWidth = false;
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.enabled = false;

        trickDisplayRoot = rootObj.transform;
    }

    private void ClearBoardPlayArea()
    {
        if (trickDisplayRoot == null)
            return;

        foreach (Transform child in trickDisplayRoot)
        {
            Destroy(child.gameObject);
        }
    }

    private void ClearHistoryStackObjects()
    {
        if (playerPositionsContainer == null)
            return;

        List<Transform> toDestroy = new List<Transform>();
        foreach (Transform child in playerPositionsContainer)
        {
            if (child.gameObject.name.StartsWith("HistoryStack_"))
                toDestroy.Add(child);
        }

        foreach (Transform child in toDestroy)
        {
            Destroy(child.gameObject);
        }
    }

    private void EnsureRulesButton()
    {
        if (rulesButton != null)
            return;

        if (gameCanvas == null)
            gameCanvas = GetComponentInParent<Canvas>();

        if (gameCanvas == null)
            return;

        rulesButton = CreateButton(
            "RulesButton",
            gameCanvas.transform,
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(-16f, -16f),
            new Vector2(110f, 34f),
            "Rules",
            new Color(0.1f, 0.1f, 0.1f, 0.78f),
            Color.white
        );

        rulesButton.onClick.AddListener(ShowRulesPanel);
    }

    private void EnsureLeaseButton()
    {
        if (leaseButton != null)
            return;

        if (gameCanvas == null)
            gameCanvas = GetComponentInParent<Canvas>();

        if (gameCanvas == null)
            return;

        leaseButton = CreateButton(
            "LeaseButton",
            gameCanvas.transform,
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(-16f, -56f),
            new Vector2(110f, 34f),
            "Check Debts",
            new Color(0.1f, 0.1f, 0.1f, 0.78f),
            Color.white
        );

        leaseButton.onClick.AddListener(ShowLeasePanel);
    }

    private void EnsureExitButton()
    {
        if (exitButton != null)
            return;

        if (gameCanvas == null)
            gameCanvas = GetComponentInParent<Canvas>();

        if (gameCanvas == null)
            return;

        exitButton = CreateButton(
            "ExitButton",
            gameCanvas.transform,
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(-16f, -16f),
            new Vector2(110f, 34f),
            "Exit",
            new Color(0.5f, 0.1f, 0.1f, 1f),
            Color.white
        );

        exitButton.onClick.AddListener(OnExitClicked);
    }

    private void OnExitClicked()
    {
        if (string.IsNullOrWhiteSpace(mainMenuSceneName))
        {
            Debug.LogError("Main menu scene name is empty. Set it on GameBoardUI.");
            return;
        }

        int buildIndex = FindSceneBuildIndex(mainMenuSceneName);
        if (buildIndex >= 0)
        {
            SceneManager.LoadScene(buildIndex);
            return;
        }

        if (SceneManager.sceneCountInBuildSettings > 0)
        {
            Debug.LogWarning($"Scene '{mainMenuSceneName}' not in Build Settings. Loading build index 0.");
            SceneManager.LoadScene(0);
            return;
        }

        Debug.LogError($"Scene '{mainMenuSceneName}' not found in Build Settings and no scenes are available.");
    }

    private int FindSceneBuildIndex(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            string name = Path.GetFileNameWithoutExtension(path);
            if (name == sceneName)
                return i;
        }

        return -1;
    }

    private void EnsurePlayButton()
    {
        if (playButton != null)
            return;

        if (gameCanvas == null)
            gameCanvas = GetComponentInParent<Canvas>();

        if (gameCanvas == null)
            return;

        playButton = CreateButton(
            "PlayButton",
            gameCanvas.transform,
            new Vector2(1f, 0f),
            new Vector2(1f, 0f),
            new Vector2(1f, 0f),
            new Vector2(-16f, 120f),
            new Vector2(110f, 34f),
            "Play",
            new Color(0.06f, 0.63f, 0.19f, 1f),
            Color.white
        );

        playButton.onClick.AddListener(OnPlayClicked);
    }

    private void OnPlayClicked()
    {
        if (gameFlowController != null)
            gameFlowController.ConfirmSelectedPlay();
    }

    private void EnsureRulesPanel()
    {
        if (rulesPanel != null)
            return;

        if (gameCanvas == null)
            gameCanvas = GetComponentInParent<Canvas>();

        if (gameCanvas == null)
            return;

        rulesPanel = new GameObject("RulesPanel");
        rulesPanel.transform.SetParent(gameCanvas.transform, false);
        RectTransform panelRect = rulesPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(820f, 540f);

        Image panelBg = rulesPanel.AddComponent<Image>();
        panelBg.color = new Color(0f, 0f, 0f, 0.85f);

        TextMeshProUGUI title = CreateText(
            "RulesTitle",
            rulesPanel.transform,
            new Vector2(0f, 240f),
            new Vector2(640f, 36f),
            24f,
            TextAlignmentOptions.Center
        );
        title.color = Color.white;
        title.text = "Дүрэм";

        rulesCloseButton = CreateButton(
            "RulesCloseButton",
            rulesPanel.transform,
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(-12f, -12f),
            new Vector2(32f, 28f),
            "X",
            new Color(0.2f, 0.2f, 0.2f, 0.9f),
            Color.white
        );
        rulesCloseButton.onClick.AddListener(HideRulesPanel);

        GameObject scrollObj = new GameObject("RulesScroll");
        scrollObj.transform.SetParent(rulesPanel.transform, false);
        RectTransform scrollRect = scrollObj.AddComponent<RectTransform>();
        scrollRect.anchorMin = new Vector2(0.5f, 0.5f);
        scrollRect.anchorMax = new Vector2(0.5f, 0.5f);
        scrollRect.pivot = new Vector2(0.5f, 0.5f);
        scrollRect.sizeDelta = new Vector2(760f, 440f);
        scrollRect.anchoredPosition = new Vector2(0f, -10f);

        ScrollRect scroll = scrollObj.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;

        GameObject viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollObj.transform, false);
        RectTransform viewportRect = viewport.AddComponent<RectTransform>();
        viewportRect.anchorMin = new Vector2(0f, 0f);
        viewportRect.anchorMax = new Vector2(1f, 1f);
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;
        Image viewportImage = viewport.AddComponent<Image>();
        viewportImage.color = new Color(0f, 0f, 0f, 0f);
        viewportImage.raycastTarget = false;
        viewport.AddComponent<RectMask2D>();

        GameObject content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        RectTransform contentRect = content.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        rulesBodyText = content.AddComponent<TextMeshProUGUI>();
        rulesBodyText.text = RulesText;
        rulesBodyText.fontSize = 18f;
        rulesBodyText.color = Color.white;
        rulesBodyText.alignment = TextAlignmentOptions.TopLeft;
        rulesBodyText.enableWordWrapping = true;
        rulesBodyText.raycastTarget = false;

        scroll.viewport = viewportRect;
        scroll.content = contentRect;

        rulesPanel.SetActive(false);
    }

    private void ShowRulesPanel()
    {
        EnsureRulesPanel();

        if (rulesPanel != null)
            rulesPanel.SetActive(true);
    }

    private void HideRulesPanel()
    {
        if (rulesPanel != null)
            rulesPanel.SetActive(false);
    }

    private void EnsureLeasePanel()
    {
        if (leasePanel != null)
            return;

        if (gameCanvas == null)
            gameCanvas = GetComponentInParent<Canvas>();

        if (gameCanvas == null)
            return;

        leasePanel = new GameObject("LeasePanel");
        leasePanel.transform.SetParent(gameCanvas.transform, false);
        RectTransform panelRect = leasePanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(520f, 360f);

        Image panelBg = leasePanel.AddComponent<Image>();
        panelBg.color = new Color(0f, 0f, 0f, 0.85f);

        TextMeshProUGUI title = CreateText(
            "LeaseTitle",
            leasePanel.transform,
            new Vector2(0f, 150f),
            new Vector2(360f, 32f),
            22f,
            TextAlignmentOptions.Center
        );
        title.color = Color.white;
        title.text = "Debts";

        leaseCloseButton = CreateButton(
            "LeaseCloseButton",
            leasePanel.transform,
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(-12f, -12f),
            new Vector2(32f, 28f),
            "X",
            new Color(0.2f, 0.2f, 0.2f, 0.9f),
            Color.white
        );
        leaseCloseButton.onClick.AddListener(HideLeasePanel);

        leaseBodyText = CreateText(
            "LeaseBody",
            leasePanel.transform,
            new Vector2(0f, -10f),
            new Vector2(460f, 260f),
            16f,
            TextAlignmentOptions.TopLeft
        );
        leaseBodyText.color = Color.white;
        leaseBodyText.enableWordWrapping = true;
        leaseBodyText.raycastTarget = false;

        leasePanel.SetActive(false);
    }

    private void ShowLeasePanel()
    {
        EnsureLeasePanel();

        if (leaseBodyText != null && gameManager != null)
        {
            var map = gameManager.GetDebtMap();
            if (map == null || map.Count == 0)
            {
                leaseBodyText.text = "No outstanding debts.";
            }
            else
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                foreach (var debtorEntry in map)
                {
                    Player debtor = gameManager.GetPlayerByID(debtorEntry.Key);
                    string debtorName = debtor != null ? debtor.playerName : $"P{debtorEntry.Key}";
                    int totalDebt = 0;

                    sb.AppendLine($"{debtorName}:");
                    foreach (var creditorEntry in debtorEntry.Value)
                    {
                        if (creditorEntry.Value <= 0) continue;
                        Player creditor = gameManager.GetPlayerByID(creditorEntry.Key);
                        string creditorName = creditor != null ? creditor.playerName : $"P{creditorEntry.Key}";
                        totalDebt += creditorEntry.Value;
                        sb.AppendLine($"  owes {creditorName}: {creditorEntry.Value}");
                    }

                    sb.AppendLine($"  total: {totalDebt}");
                }
                string outText = sb.ToString();
                leaseBodyText.text = string.IsNullOrEmpty(outText) ? "No outstanding debts." : outText.TrimEnd();
            }
        }

        if (leasePanel != null)
            leasePanel.SetActive(true);
    }

    private void HideLeasePanel()
    {
        if (leasePanel != null)
            leasePanel.SetActive(false);
    }

    private void EnsureJanliiPanel()
    {
        if (janliiPanel != null)
            return;

        if (gameCanvas == null)
            gameCanvas = GetComponentInParent<Canvas>();

        if (gameCanvas == null)
            return;

        janliiPanel = new GameObject("JanliiPanel");
        janliiPanel.transform.SetParent(gameCanvas.transform, false);
        RectTransform panelRect = janliiPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(540f, 260f);

        Image panelBg = janliiPanel.AddComponent<Image>();
        panelBg.color = new Color(0f, 0f, 0f, 0.86f);

        janliiPromptText = CreateText(
            "JanliiPrompt",
            janliiPanel.transform,
            new Vector2(0f, 96f),
            new Vector2(480f, 36f),
            20f,
            TextAlignmentOptions.Center
        );
        janliiPromptText.color = Color.white;

        GameObject buttonContainer = new GameObject("JanliiButtons");
        buttonContainer.transform.SetParent(janliiPanel.transform, false);
        RectTransform containerRect = buttonContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 0.5f);
        containerRect.anchorMax = new Vector2(0.5f, 0.5f);
        containerRect.pivot = new Vector2(0.5f, 0.5f);
        containerRect.anchoredPosition = new Vector2(0f, -12f);
        containerRect.sizeDelta = new Vector2(480f, 140f);

        GridLayoutGroup grid = buttonContainer.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(110f, 34f);
        grid.spacing = new Vector2(8f, 8f);
        grid.childAlignment = TextAnchor.MiddleCenter;

        janliiButtonContainer = buttonContainer.transform;
        janliiPanel.SetActive(false);
    }

    private void BuildJanliiButtons()
    {
        if (janliiButtonContainer == null)
            return;

        foreach (Transform child in janliiButtonContainer)
        {
            Destroy(child.gameObject);
        }

        foreach (DaaluuPieceName piece in GameRules.JanliiCandidates)
        {
            DaaluuPieceName capturedPiece = piece;
            Button button = CreateButton(
                $"Janlii_{piece}",
                janliiButtonContainer,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(110f, 34f),
                GameRules.GetPieceDisplayName(piece),
                new Color(0.2f, 0.2f, 0.2f, 0.9f),
                Color.white
            );
            button.onClick.AddListener(() => janliiSelectionCallback?.Invoke(capturedPiece));
        }
    }

    private void EnsureJanliiStatusText()
    {
        if (janliiStatusText != null)
            return;

        if (gameCanvas == null)
            gameCanvas = GetComponentInParent<Canvas>();

        if (gameCanvas == null)
            return;

        GameObject textObj = new GameObject("JanliiStatusText");
        textObj.transform.SetParent(gameCanvas.transform, false);
        RectTransform rectTransform = textObj.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(0f, 1f);
        rectTransform.pivot = new Vector2(0f, 1f);
        rectTransform.anchoredPosition = new Vector2(16f, -56f);
        rectTransform.sizeDelta = new Vector2(360f, 28f);

        janliiStatusText = textObj.AddComponent<TextMeshProUGUI>();
        janliiStatusText.alignment = TextAlignmentOptions.Left;
        janliiStatusText.color = Color.white;
        janliiStatusText.fontSize = 18f;
        janliiStatusText.raycastTarget = false;
    }

    private void UpdateJanliiStatus()
    {
        if (janliiStatusText == null)
            return;

        if (trickManager == null)
            trickManager = FindObjectOfType<TrickManager>();

        DaaluuPieceName? janlii = trickManager != null ? trickManager.GetJanliiPiece() : null;
        if (janlii.HasValue)
            janliiStatusText.text = $"Janlii: {GameRules.GetPieceDisplayName(janlii.Value)}";
        else
            janliiStatusText.text = "Janlii: -";
    }

    private void UpdateBoardPlayArea()
    {
        if (trickManager == null)
            trickManager = FindObjectOfType<TrickManager>();

        EnsureBoardPlayArea();

        if (trickDisplayRoot == null || trickManager == null)
            return;

        foreach (Transform child in trickDisplayRoot)
        {
            Destroy(child.gameObject);
        }

        List<Player> players = gameManager != null ? gameManager.GetAllPlayers() : new List<Player>();

        DrawCompletedStacks(players);

        List<TrickCard> trickCards = trickManager.GetTrickCards();
        if (trickCards.Count == 0)
        {
            if (completedPlaysByPlayer.Count == 0)
                CreateEmptyTrickText();
            return;
        }

        Dictionary<int, List<List<TrickCard>>> playsByPlayer = GroupPlaysByPlayer(trickCards);

        if (winnerStackActive)
        {
            int winnerIndex = GetPlayerIndex(players, winnerStackPlayerId);
            Vector2 anchor = GetPlayerAnchor(winnerIndex);
            DrawStackAtAnchor(anchor, GetPlayerStackOffset(winnerIndex), GetSortedPlays(trickManager.GetPlaysByOrder()), "WinnerStack", 0.85f);
            return;
        }

        DrawCurrentTrickStacks(playsByPlayer, players);
    }

    private void DrawCurrentTrickStacks(Dictionary<int, List<List<TrickCard>>> playsByPlayer, List<Player> players)
    {
        foreach (Player player in players)
        {
            if (!playsByPlayer.ContainsKey(player.playerID))
                continue;

            int playerIndex = GetPlayerIndex(players, player.playerID);
            DrawStackAtAnchor(
                GetPlayerAnchor(playerIndex),
                GetPlayerStackOffset(playerIndex),
                playsByPlayer[player.playerID],
                $"PlayStack_{player.playerID}",
                0.85f
            );
        }
    }

    private void CreateEmptyTrickText()
    {
        GameObject textObj = new GameObject("WaitingForPlayText");
        textObj.transform.SetParent(trickDisplayRoot, false);

        RectTransform rectTransform = textObj.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(360f, 42f);

        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = "Waiting for first play";
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.fontSize = 22f;
        text.enableAutoSizing = true;
        text.fontSizeMin = 12f;
        text.fontSizeMax = 22f;
        text.raycastTarget = false;
    }

    private void CreatePlayedPieceView(TrickCard trickCard, Transform parent, Vector2 offset)
    {
        GameObject tileObj = new GameObject($"Played_{trickCard.playedBy.playerName}");
        tileObj.transform.SetParent(parent, false);

        RectTransform tileRect = tileObj.AddComponent<RectTransform>();
        tileRect.sizeDelta = new Vector2(CardWidth, CardHeight);
        tileRect.anchoredPosition = offset;

        Image image = tileObj.AddComponent<Image>();
        // Use same color scheme as card prefab but slightly translucent
        image.color = trickCard.card.color == DaaluuColor.Red
            ? new Color(0.93f, 0.45f, 0.38f, 0.95f)
            : new Color(0.98f, 0.96f, 0.90f, 0.98f);

        string colorLabel = trickCard.card.color == DaaluuColor.Red ? "R" : "W";
        string janliiMark = GameRulesValidator.IsJanlii(trickCard.card, trickManager.GetJanliiPiece()) ? "\nJANLII" : "";

        var playedText = CreateText(
            "PlayedPieceText",
            tileObj.transform,
            Vector2.zero,
            tileRect.sizeDelta,
            20f,
            TextAlignmentOptions.Center
        );
        if (playedText != null)
        {
            playedText.color = new Color(0.08f, 0.06f, 0.04f, 1f);
            playedText.text = $"{trickCard.card.displayName}\n{trickCard.card.value} {colorLabel}{janliiMark}";
        }

        var byText = CreateText(
            "PlayedByText",
            tileObj.transform,
            new Vector2(0f, -CardHeight / 2f + 12f),
            new Vector2(CardWidth - 8f, 18f),
            12f,
            TextAlignmentOptions.Center
        );
        if (byText != null)
        {
            byText.color = new Color(0.15f, 0.15f, 0.15f, 0.85f);
            byText.text = trickCard.playedBy.playerName;
        }
    }

    private Dictionary<int, List<TrickCard>> GroupCardsByPlayer(List<TrickCard> trickCards)
    {
        Dictionary<int, List<TrickCard>> grouped = new Dictionary<int, List<TrickCard>>();
        foreach (TrickCard trickCard in trickCards)
        {
            int playerId = winnerStackActive ? winnerStackPlayerId : trickCard.playedBy.playerID;
            if (!grouped.ContainsKey(playerId))
                grouped[playerId] = new List<TrickCard>();

            grouped[playerId].Add(trickCard);
        }

        return grouped;
    }

    private Dictionary<int, List<List<TrickCard>>> GroupPlaysByPlayer(List<TrickCard> trickCards)
    {
        Dictionary<int, List<List<TrickCard>>> grouped = new Dictionary<int, List<List<TrickCard>>>();
        Dictionary<int, List<TrickCard>> playsByOrder = trickManager != null ? trickManager.GetPlaysByOrder() : new Dictionary<int, List<TrickCard>>();

        foreach (var kvp in playsByOrder)
        {
            if (kvp.Value.Count == 0)
                continue;

            int playerId = kvp.Value[0].playedBy.playerID;
            if (!grouped.ContainsKey(playerId))
                grouped[playerId] = new List<List<TrickCard>>();

            grouped[playerId].Add(kvp.Value);
        }

        return grouped;
    }

    private List<List<TrickCard>> GetSortedPlays(Dictionary<int, List<TrickCard>> plays)
    {
        List<List<TrickCard>> playList = new List<List<TrickCard>>(plays.Values);
        playList.Sort((a, b) => trickManager != null ? trickManager.ComparePlays(a, b) : 0);
        return playList;
    }

    private void DrawStackAtAnchor(Vector2 anchor, Vector2 offset, List<List<TrickCard>> plays, string name, float scale)
    {
        GameObject stackRoot = new GameObject(name);
        stackRoot.transform.SetParent(trickDisplayRoot, false);
        RectTransform stackRect = stackRoot.AddComponent<RectTransform>();
        stackRect.anchorMin = anchor;
        stackRect.anchorMax = anchor;
        stackRect.pivot = new Vector2(0.5f, 0.5f);
        stackRect.anchoredPosition = offset;
        stackRect.sizeDelta = new Vector2(140f, 200f);
        stackRect.localScale = new Vector3(scale, scale, 1f);

        for (int i = 0; i < plays.Count; i++)
        {
            List<TrickCard> playCards = plays[i];
            Vector2 stackOffset = new Vector2(0f, -i * 18f);

            if (playCards.Count == 2)
            {
                CreatePlayedPieceView(playCards[0], stackRoot.transform, stackOffset + new Vector2(-28f, 0f));
                CreatePlayedPieceView(playCards[1], stackRoot.transform, stackOffset + new Vector2(28f, 0f));
            }
            else if (playCards.Count == 1)
            {
                CreatePlayedPieceView(playCards[0], stackRoot.transform, stackOffset);
            }
            else
            {
                for (int j = 0; j < playCards.Count; j++)
                {
                    CreatePlayedPieceView(playCards[j], stackRoot.transform, stackOffset + new Vector2(j * 8f, -j * 6f));
                }
            }
        }
    }

    private int GetPlayerIndex(List<Player> players, int playerId)
    {
        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].playerID == playerId)
                return i;
        }

        return 0;
    }

    private Vector2 GetPlayerAnchor(int index)
    {
        Vector2[] anchors =
        {
            new Vector2(0.5f, 0.15f),
            new Vector2(0.08f, 0.55f),
            new Vector2(0.28f, 0.88f),
            new Vector2(0.72f, 0.88f),
            new Vector2(0.92f, 0.55f)
        };

        return anchors[index % anchors.Length];
    }

    private Vector2 GetPlayerStackOffset(int index)
    {
        Vector2[] offsets =
        {
            new Vector2(90f, 0f),
            new Vector2(90f, 0f),
            new Vector2(90f, 0f),
            new Vector2(90f, 0f),
            new Vector2(90f, 0f)
        };

        return offsets[index % offsets.Length];
    }

    private Vector2 GetPlayerHistoryOffset(int index)
    {
        Vector2[] offsets =
        {
            new Vector2(150f, 0f),
            new Vector2(0f, 0f),
            new Vector2(-200f, -20f),
            new Vector2(200f, -20f),
            new Vector2(0f, 0f)
        };

        return offsets[index % offsets.Length];
    }

    public void ShowWinnerStack(List<TrickCard> trickCards, Player winner)
    {
        if (winner == null)
            return;

        AddCompletedPlays(trickCards, winner.playerID);
        winnerStackActive = true;
        winnerStackPlayerId = winner.playerID;
        UpdateBoardPlayArea();
    }

    public void ClearWinnerStack()
    {
        winnerStackActive = false;
        winnerStackPlayerId = -1;
    }

    private void AddCompletedPlays(List<TrickCard> trickCards, int winnerPlayerId)
    {
        if (trickCards == null || trickCards.Count == 0)
            return;

        if (!completedPlaysByPlayer.ContainsKey(winnerPlayerId))
            completedPlaysByPlayer[winnerPlayerId] = new List<List<Card>>();

        List<Card> cards = new List<Card>();
        foreach (TrickCard trickCard in trickCards)
        {
            if (trickCard != null && trickCard.playedBy != null && trickCard.playedBy.playerID == winnerPlayerId)
                cards.Add(trickCard.card);
        }

        if (cards.Count > 0)
            completedPlaysByPlayer[winnerPlayerId].Add(cards);
    }

    private void DrawCompletedStacks(List<Player> players)
    {
        if (completedPlaysByPlayer.Count == 0)
            return;

        foreach (var kvp in completedPlaysByPlayer)
        {
            Player player = players.Find(p => p.playerID == kvp.Key);
            if (player == null)
                continue;

            if (playerStatusUIs == null || !playerStatusUIs.TryGetValue(player.playerID, out var statusUI))
                continue;

            RectTransform statusRect = statusUI.GetComponent<RectTransform>();
            if (statusRect == null)
                continue;

            Vector2 historyPosition = statusRect.anchoredPosition + GetHistoryStackOffset(statusRect);
            if (player.playerID == 0)
                historyPosition.y = 160f;
            DrawCompletedStackAtPosition(statusRect.parent, statusRect.anchorMin, statusRect.anchorMax, historyPosition, kvp.Value, $"HistoryStack_{kvp.Key}", player);
        }
    }

    private Vector2 GetHistoryStackOffset(RectTransform statusRect)
    {
        float horizontalMargin = statusRect.sizeDelta.x * 1.2f + 80f;
        float verticalMargin = statusRect.sizeDelta.y * 1.4f + 40f;
        Vector2 anchor = statusRect.anchorMin;

        if (anchor.y > 0.75f)
            return new Vector2(0f, -verticalMargin);
        if (anchor.y < 0.25f)
            return new Vector2(horizontalMargin, 0f);
        if (anchor.x < 0.25f)
            return new Vector2(horizontalMargin, 0f);
        if (anchor.x > 0.75f)
            return new Vector2(-horizontalMargin, 0f);

        return new Vector2(0f, -verticalMargin);
    }

    private void DrawCompletedStackAtPosition(Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, List<List<Card>> plays, string name, Player player)
    {
        GameObject stackRoot = new GameObject(name);
        stackRoot.transform.SetParent(parent, false);
        RectTransform stackRect = stackRoot.AddComponent<RectTransform>();
        stackRect.anchorMin = anchorMin;
        stackRect.anchorMax = anchorMax;
        stackRect.pivot = new Vector2(0.5f, 0.5f);
        stackRect.anchoredPosition = position;
        stackRect.sizeDelta = new Vector2(120f, 180f);
        stackRect.localScale = new Vector3(0.4f, 0.4f, 1f);

        float historySpacing = 90f;

        for (int i = 0; i < plays.Count; i++)
        {
            List<Card> playCards = plays[i];
            Vector2 stackOffset = new Vector2(i * historySpacing, 0f);
            Player owner = player;

            if (playCards.Count == 2)
            {
                CreatePlayedPieceView(new TrickCard(playCards[0], owner, 0), stackRoot.transform, stackOffset + new Vector2(-20f, 0f));
                CreatePlayedPieceView(new TrickCard(playCards[1], owner, 0), stackRoot.transform, stackOffset + new Vector2(20f, 0f));
            }
            else if (playCards.Count == 1)
            {
                CreatePlayedPieceView(new TrickCard(playCards[0], owner, 0), stackRoot.transform, stackOffset);
            }
            else
            {
                for (int j = 0; j < playCards.Count; j++)
                {
                    CreatePlayedPieceView(new TrickCard(playCards[j], owner, 0), stackRoot.transform, stackOffset + new Vector2(j * 6f, -j * 4f));
                }
            }
        }
    }

    private GameObject CreateCardUIPrefab()
    {
        GameObject cardObj = new GameObject("RuntimeCardUIPrefab");
        cardObj.SetActive(false);
        cardObj.transform.SetParent(transform, false);

        RectTransform rectTransform = cardObj.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(CardWidth, CardHeight);

        Image image = cardObj.AddComponent<Image>();
        image.color = new Color(0.96f, 0.94f, 0.86f, 1f);

        Button button = cardObj.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(1f, 0.98f, 0.76f, 1f);
        colors.pressedColor = new Color(0.86f, 0.82f, 0.66f, 1f);
        button.colors = colors;

        cardObj.AddComponent<CardUI>();
        var cardText = CreateText("CardText", cardObj.transform, Vector2.zero, rectTransform.sizeDelta, 20, TextAlignmentOptions.Center);
        // Card text should be dark on light card backgrounds
        if (cardText != null)
            cardText.color = new Color(0.08f, 0.06f, 0.04f, 1f);

        return cardObj;
    }

    private GameObject CreatePlayerStatusPrefab()
    {
        GameObject statusObj = new GameObject("RuntimePlayerStatusPrefab");
        statusObj.SetActive(false);
        statusObj.transform.SetParent(transform, false);

        RectTransform rectTransform = statusObj.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(StatusWidth, StatusHeight);

        Image image = statusObj.AddComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.6f);

        CreateText("PlayerName", statusObj.transform, new Vector2(0f, 18f), new Vector2(StatusWidth - 16f, 24f), 18, TextAlignmentOptions.Center);
        CreateText("HandSize", statusObj.transform, new Vector2(-36f, -12f), new Vector2(72f, 18f), 14, TextAlignmentOptions.Center);
        CreateText("TeaCards", statusObj.transform, new Vector2(36f, -12f), new Vector2(72f, 18f), 14, TextAlignmentOptions.Center);

        statusObj.AddComponent<PlayerStatusUI>();
        return statusObj;
    }

    private TextMeshProUGUI CreateText(string name, Transform parent, Vector2 anchoredPosition, Vector2 size, float fontSize, TextAlignmentOptions alignment)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent, false);

        RectTransform rectTransform = textObj.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;

        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.alignment = alignment;
        text.color = Color.white;
        text.fontSize = fontSize;
        text.enableAutoSizing = true;
        text.fontSizeMin = 10f;
        text.fontSizeMax = fontSize;
        text.raycastTarget = false;

        return text;
    }

    private Button CreateButton(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 size, string label, Color background, Color textColor)
    {
        GameObject buttonObj = new GameObject(name);
        buttonObj.transform.SetParent(parent, false);

        RectTransform rectTransform = buttonObj.AddComponent<RectTransform>();
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.pivot = pivot;
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;

        Image image = buttonObj.AddComponent<Image>();
        image.color = background;

        Button button = buttonObj.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(
            Mathf.Clamp01(background.r + 0.1f),
            Mathf.Clamp01(background.g + 0.1f),
            Mathf.Clamp01(background.b + 0.1f),
            background.a
        );
        colors.pressedColor = new Color(
            Mathf.Clamp01(background.r - 0.05f),
            Mathf.Clamp01(background.g - 0.05f),
            Mathf.Clamp01(background.b - 0.05f),
            background.a
        );
        button.colors = colors;

        TextMeshProUGUI text = CreateText("Label", buttonObj.transform, Vector2.zero, size, 16f, TextAlignmentOptions.Center);
        text.text = label;
        text.color = textColor;

        return button;
    }

    private void PositionStatusUI(RectTransform rectTransform, int index, bool isHuman)
    {
        if (rectTransform == null)
            return;

        if (isHuman)
        {
            rectTransform.anchorMin = new Vector2(0f, 0f);
            rectTransform.anchorMax = new Vector2(0f, 0f);
            rectTransform.pivot = new Vector2(0f, 0f);
            rectTransform.anchoredPosition = new Vector2(16f, 120f);
            rectTransform.sizeDelta = new Vector2(200f, StatusHeight);
            return;
        }

        if (index == 0)
        {
            rectTransform.anchorMin = new Vector2(0f, 0f);
            rectTransform.anchorMax = new Vector2(0f, 0f);
            rectTransform.pivot = new Vector2(0f, 0f);
            rectTransform.anchoredPosition = new Vector2(5f, 120f);
            rectTransform.sizeDelta = new Vector2(StatusWidth, StatusHeight);
            return;
        }

        Vector2[] anchors =
        {
            new Vector2(0.5f, 0.12f),
            new Vector2(0.08f, 0.55f),
            new Vector2(0.28f, 0.88f),
            new Vector2(0.72f, 0.88f),
            new Vector2(0.92f, 0.55f)
        };

        Vector2 anchor = anchors[index % anchors.Length];
        rectTransform.anchorMin = anchor;
        rectTransform.anchorMax = anchor;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        float yOffset = 0f;
        if (index == 2 || index == 3)
            yOffset = 30f;
        rectTransform.anchoredPosition = new Vector2(anchor.x < 0.5f ? -12f : (anchor.x > 0.5f ? 12f : 0f), yOffset);
        rectTransform.sizeDelta = new Vector2(StatusWidth, StatusHeight);
    }
}
