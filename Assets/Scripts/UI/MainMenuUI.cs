using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.SceneManagement;

public class MainMenuUI : MonoBehaviour
{
    private GameObject canvasObject;
    private GameObject contentObject;
    private TMP_InputField nameInput;
    private TMP_InputField ipInput;
    private Button hostButton;
    private Button joinButton;
    private Button botButton;
    private TextMeshProUGUI statusText;
    private GameObject panelObject;
    private GameObject lobbyPanel;
    private TextMeshProUGUI lobbyHostIpText;
    private TextMeshProUGUI lobbyPlayerListText;
    private Button startGameButton;
    private Button leaveLobbyButton;
    private List<string> lobbyPlayers = new List<string>();
    private bool lobbyIsHost = false;
    private bool lobbyActive = false;
    [SerializeField] private string gameSceneName = "SampleScene";
    [SerializeField] private bool showInGameScene = false;
    private bool startGameAfterLoad;
    private bool ownsCanvas;

    private const string PlayerNameKey = "PlayerName";

    private void Start()
    {
        if (!showInGameScene && SceneManager.GetActiveScene().name == gameSceneName)
        {
            Destroy(gameObject);
            return;
        }

        BuildUI();
        SetStatus("Idle");
        UpdateNameRequirements();
#if USE_MIRROR
        RegisterLobbyHandlers();
#endif
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Update()
    {
#if USE_MIRROR
        if (statusText == null) return;
        bool serverActive = global::Mirror.NetworkServer.active;
        bool clientActive = global::Mirror.NetworkClient.isConnected;
        if (serverActive && clientActive)
        {
            SetStatus("Hosting (client connected)");
        }
        else if (serverActive)
        {
            SetStatus("Hosting...");
        }
        else if (clientActive)
        {
            SetStatus("Connected");
        }
#endif
    }

    private void BuildUI()
    {
        if (canvasObject != null) return;

        // Ensure EventSystem uses the correct input module for the project's active input system.
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
        }
        else
        {
            // If EventSystem exists but uses StandaloneInputModule while Input System package is present, replace it.
            if (inputSystemModuleType != null)
            {
                var hasNew = false;
                foreach (var comp in existingEs.GetComponents<Component>())
                {
                    if (comp.GetType().FullName == "UnityEngine.InputSystem.UI.InputSystemUIInputModule") { hasNew = true; break; }
                }
                if (!hasNew)
                {
                    var standalone = existingEs.GetComponent<StandaloneInputModule>();
                    if (standalone != null)
                    {
                        Object.DestroyImmediate(standalone);
                        existingEs.gameObject.AddComponent(inputSystemModuleType);
                    }
                }
            }
        }

        // If a Canvas already exists in the scene, reuse it instead of creating a new one.
        Canvas existingCanvas = FindObjectOfType<Canvas>();
        if (existingCanvas != null)
        {
            canvasObject = existingCanvas.gameObject;
            ownsCanvas = false;
        }
        else
        {
            canvasObject = new GameObject("MainMenuCanvas", typeof(RectTransform));
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            canvas.overrideSorting = true;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();
            DontDestroyOnLoad(canvasObject);
            ownsCanvas = true;
        }
        // Full-screen dim background (blocks interaction with underlying UI)
        GameObject panel = new GameObject("Panel", typeof(RectTransform));
        panelObject = panel;
        panel.transform.SetParent(canvasObject.transform, false);
        // Ensure the panel is the topmost element inside the canvas so it overlays other UI.
        panel.transform.SetAsLastSibling();
        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.6f);
        panelImage.raycastTarget = true;
        RectTransform panelRt = panel.GetComponent<RectTransform>();
        panelRt.anchorMin = Vector2.zero;
        panelRt.anchorMax = Vector2.one;
        panelRt.offsetMin = Vector2.zero;
        panelRt.offsetMax = Vector2.zero;

        // Content container centered on top of the dim background
        GameObject content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(panel.transform, false);
        Image contentImage = content.AddComponent<Image>();
        contentImage.color = new Color(0.12f, 0.12f, 0.12f, 0.95f);
        RectTransform panelRtContent = content.GetComponent<RectTransform>();
        panelRtContent.anchorMin = new Vector2(0.25f, 0.2f);
        panelRtContent.anchorMax = new Vector2(0.75f, 0.7f);
        panelRtContent.offsetMin = Vector2.zero;
        panelRtContent.offsetMax = Vector2.zero;
        contentObject = content;

        // Title
        GameObject titleObj = new GameObject("Title", typeof(RectTransform));
        titleObj.transform.SetParent(content.transform, false);
        TextMeshProUGUI title = titleObj.AddComponent<TextMeshProUGUI>();
        title.alignment = TextAlignmentOptions.Center;
        title.fontSize = 48;
        title.text = "Daaluu - Main Menu";
        RectTransform titleRt = title.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.1f, 0.75f);
        titleRt.anchorMax = new Vector2(0.9f, 0.95f);
        titleRt.offsetMin = Vector2.zero;
        titleRt.offsetMax = Vector2.zero;

        // Name label + input
        GameObject nameLabelObj = new GameObject("NameLabel", typeof(RectTransform));
        nameLabelObj.transform.SetParent(content.transform, false);
        TextMeshProUGUI nameLabel = nameLabelObj.AddComponent<TextMeshProUGUI>();
        nameLabel.text = "Player name:";
        nameLabel.fontSize = 26;
        RectTransform nlRt = nameLabel.GetComponent<RectTransform>();
        nlRt.anchorMin = new Vector2(0.08f, 0.62f);
        nlRt.anchorMax = new Vector2(0.32f, 0.7f);
        nlRt.offsetMin = Vector2.zero;
        nlRt.offsetMax = Vector2.zero;

        GameObject nameInputObj = new GameObject("NameInput", typeof(RectTransform));
        nameInputObj.transform.SetParent(content.transform, false);
        Image nameInputBg = nameInputObj.AddComponent<Image>();
        nameInputBg.color = new Color(0.18f, 0.18f, 0.18f, 0.95f);
        nameInput = nameInputObj.AddComponent<TMP_InputField>();
        TextMeshProUGUI namePlaceholder = CreatePlaceholder(nameInputObj.transform, "Enter name");
        TextMeshProUGUI nameText = CreateText(nameInputObj.transform);
        nameInput.textComponent = nameText;
        nameInput.placeholder = namePlaceholder;
        nameInput.targetGraphic = nameInputBg;
        nameInput.textViewport = nameInputObj.GetComponent<RectTransform>();
        RectTransform niRt = nameInputObj.GetComponent<RectTransform>();
        niRt.anchorMin = new Vector2(0.34f, 0.62f);
        niRt.anchorMax = new Vector2(0.88f, 0.7f);
        niRt.offsetMin = Vector2.zero;
        niRt.offsetMax = Vector2.zero;

        // IP label + input
        GameObject ipLabelObj = new GameObject("IPLabel", typeof(RectTransform));
        ipLabelObj.transform.SetParent(content.transform, false);
        TextMeshProUGUI ipLabel = ipLabelObj.AddComponent<TextMeshProUGUI>();
        ipLabel.text = "Host IP:";
        ipLabel.fontSize = 26;
        RectTransform ilRt = ipLabel.GetComponent<RectTransform>();
        ilRt.anchorMin = new Vector2(0.08f, 0.52f);
        ilRt.anchorMax = new Vector2(0.32f, 0.6f);
        ilRt.offsetMin = Vector2.zero;
        ilRt.offsetMax = Vector2.zero;

        GameObject ipInputObj = new GameObject("IPInput", typeof(RectTransform));
        ipInputObj.transform.SetParent(content.transform, false);
        Image ipInputBg = ipInputObj.AddComponent<Image>();
        ipInputBg.color = new Color(0.18f, 0.18f, 0.18f, 0.95f);
        ipInput = ipInputObj.AddComponent<TMP_InputField>();
        TextMeshProUGUI ipPlaceholder = CreatePlaceholder(ipInputObj.transform, "localhost");
        TextMeshProUGUI ipText = CreateText(ipInputObj.transform);
        ipInput.textComponent = ipText;
        ipInput.placeholder = ipPlaceholder;
        ipInput.targetGraphic = ipInputBg;
        ipInput.textViewport = ipInputObj.GetComponent<RectTransform>();
        RectTransform ipRt = ipInputObj.GetComponent<RectTransform>();
        ipRt.anchorMin = new Vector2(0.34f, 0.52f);
        ipRt.anchorMax = new Vector2(0.88f, 0.6f);
        ipRt.offsetMin = Vector2.zero;
        ipRt.offsetMax = Vector2.zero;

        // Buttons
        GameObject hostBtnObj = new GameObject("HostButton", typeof(RectTransform));
        hostBtnObj.transform.SetParent(content.transform, false);
        Image hostImg = hostBtnObj.AddComponent<Image>();
        hostImg.color = new Color(0.8f, 0.9f, 1f, 1f);
        hostButton = hostBtnObj.AddComponent<Button>();
        RectTransform hbRt = hostBtnObj.GetComponent<RectTransform>();
        hbRt.anchorMin = new Vector2(0.12f, 0.12f);
        hbRt.anchorMax = new Vector2(0.38f, 0.26f);
        hbRt.offsetMin = Vector2.zero;
        hbRt.offsetMax = Vector2.zero;
        TextMeshProUGUI hostText = CreateButtonText(hostBtnObj.transform, "Host (Start)");

        GameObject joinBtnObj = new GameObject("JoinButton", typeof(RectTransform));
        joinBtnObj.transform.SetParent(content.transform, false);
        Image joinImg = joinBtnObj.AddComponent<Image>();
        joinImg.color = new Color(0.9f, 0.9f, 0.95f, 1f);
        joinButton = joinBtnObj.AddComponent<Button>();
        RectTransform jbRt = joinBtnObj.GetComponent<RectTransform>();
        jbRt.anchorMin = new Vector2(0.42f, 0.12f);
        jbRt.anchorMax = new Vector2(0.68f, 0.26f);
        jbRt.offsetMin = Vector2.zero;
        jbRt.offsetMax = Vector2.zero;
        TextMeshProUGUI joinText = CreateButtonText(joinBtnObj.transform, "Join");

        GameObject botBtnObj = new GameObject("BotButton", typeof(RectTransform));
        botBtnObj.transform.SetParent(content.transform, false);
        Image botImg = botBtnObj.AddComponent<Image>();
        botImg.color = new Color(0.2f, 0.7f, 0.2f, 1f);
        botButton = botBtnObj.AddComponent<Button>();
        RectTransform bbRt = botBtnObj.GetComponent<RectTransform>();
        bbRt.anchorMin = new Vector2(0.25f, 0.28f);
        bbRt.anchorMax = new Vector2(0.75f, 0.4f);
        bbRt.offsetMin = Vector2.zero;
        bbRt.offsetMax = Vector2.zero;
        TextMeshProUGUI botText = CreateButtonText(botBtnObj.transform, "Play With Bots");

        // Status line
        GameObject statusObj = new GameObject("StatusText", typeof(RectTransform));
        statusObj.transform.SetParent(content.transform, false);
        statusText = statusObj.AddComponent<TextMeshProUGUI>();
        statusText.alignment = TextAlignmentOptions.Center;
        statusText.fontSize = 20;
        statusText.color = new Color(0.8f, 0.8f, 0.8f);
        RectTransform stRt = statusObj.GetComponent<RectTransform>();
        stRt.anchorMin = new Vector2(0.12f, 0.02f);
        stRt.anchorMax = new Vector2(0.88f, 0.1f);
        stRt.offsetMin = Vector2.zero;
        stRt.offsetMax = Vector2.zero;

        hostButton.onClick.AddListener(OnHostClicked);
        joinButton.onClick.AddListener(OnJoinClicked);
        botButton.onClick.AddListener(OnPlayWithBotClicked);
        nameInput.onValueChanged.AddListener(_ => UpdateNameRequirements());

        // Load saved name
        string saved = PlayerPrefs.GetString(PlayerNameKey, "");
        if (!string.IsNullOrEmpty(saved)) nameInput.text = saved;
        ipInput.text = "localhost";
    }

    private TextMeshProUGUI CreatePlaceholder(Transform parent, string text)
    {
        GameObject ph = new GameObject("Placeholder", typeof(RectTransform));
        ph.transform.SetParent(parent, false);
        TextMeshProUGUI phText = ph.AddComponent<TextMeshProUGUI>();
        phText.fontSize = 24;
        phText.text = text;
        phText.color = new Color(0.7f, 0.7f, 0.7f);
        phText.enableWordWrapping = false;
        phText.alignment = TextAlignmentOptions.Left;
        RectTransform rt = ph.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        return phText;
    }

    private TextMeshProUGUI CreateText(Transform parent)
    {
        GameObject txt = new GameObject("Text", typeof(RectTransform));
        txt.transform.SetParent(parent, false);
        TextMeshProUGUI t = txt.AddComponent<TextMeshProUGUI>();
        t.fontSize = 24;
        t.text = "";
        t.enableWordWrapping = false;
        t.alignment = TextAlignmentOptions.Left;
        RectTransform rt = t.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        return t;
    }

    private TextMeshProUGUI CreateButtonText(Transform parent, string content)
    {
        GameObject txt = new GameObject("BtnText", typeof(RectTransform));
        txt.transform.SetParent(parent, false);
        TextMeshProUGUI t = txt.AddComponent<TextMeshProUGUI>();
        t.alignment = TextAlignmentOptions.Center;
        t.fontSize = 26;
        t.text = content;
        RectTransform rt = t.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        return t;
    }

    private void OnHostClicked()
    {
        if (!IsNameValid())
        {
            SetStatus("Enter a player name to continue");
            return;
        }

        string playerName = nameInput == null ? "" : nameInput.text.Trim();
        PlayerPrefs.SetString(PlayerNameKey, playerName);
        PlayerPrefs.Save();

#if USE_MIRROR
        EnsureMirrorNetworkManager();

        if (global::Mirror.NetworkManager.singleton == null)
        {
            Debug.LogError("Mirror NetworkManager not present in scene. Add a NetworkManager gameobject.");
            SetStatus("No NetworkManager in scene");
            return;
        }

        Debug.Log("Starting Host...");
        SetStatus("Hosting room...");
        global::Mirror.NetworkManager.singleton.StartHost();
        ShowLobbyPanel(true);
        RequestLobbyStateUpdate();
#else
        Debug.Log("Mirror not enabled. To enable multiplayer, install Mirror and add 'USE_MIRROR' define.");
        SetStatus("Mirror not enabled");
#endif
    }

    private void OnJoinClicked()
    {
        if (!IsNameValid())
        {
            SetStatus("Enter a player name to continue");
            return;
        }

        string playerName = nameInput == null ? "" : nameInput.text.Trim();
        PlayerPrefs.SetString(PlayerNameKey, playerName);
        PlayerPrefs.Save();
        string ip = string.IsNullOrWhiteSpace(ipInput.text) ? "localhost" : ipInput.text.Trim();

#if USE_MIRROR
        EnsureMirrorNetworkManager();

        if (global::Mirror.NetworkManager.singleton == null)
        {
            Debug.LogError("Mirror NetworkManager not present in scene. Add a NetworkManager gameobject.");
            SetStatus("No NetworkManager in scene");
            return;
        }
        global::Mirror.NetworkManager.singleton.networkAddress = ip;
        Debug.Log($"Joining {ip}...");
        SetStatus($"Joining {ip}...");
        global::Mirror.NetworkManager.singleton.StartClient();
        ShowLobbyPanel(false);
        RequestLobbyStateUpdate();
#else
        Debug.Log("Mirror not enabled. To enable multiplayer, install Mirror and add 'USE_MIRROR' define.");
        SetStatus("Mirror not enabled");
#endif
    }

    private void OnPlayWithBotClicked()
    {
        if (!IsNameValid())
        {
            SetStatus("Enter a player name to continue");
            return;
        }
        // Ensure name is saved when starting a local bot game as well
        string playerName = nameInput == null ? "" : nameInput.text.Trim();
        PlayerPrefs.SetString(PlayerNameKey, playerName);
        PlayerPrefs.Save();
        SetStatus("Loading game...");
        DestroyMenu();
        Scene active = SceneManager.GetActiveScene();
        if (active.name == gameSceneName)
        {
            StartLocalGameInScene();
            return;
        }

        startGameAfterLoad = true;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.LoadScene(gameSceneName);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!startGameAfterLoad)
            return;

        if (scene.name != gameSceneName)
            return;

        startGameAfterLoad = false;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        StartLocalGameInScene();
    }

    private void StartLocalGameInScene()
    {
        SetStatus("Starting local game...");
        if (GameManager.Instance == null)
        {
            StartCoroutine(WaitForGameManagerAndStart());
            return;
        }

        GameManager.Instance.InitializeGame(true);
    }

    private IEnumerator WaitForGameManagerAndStart()
    {
        const float timeout = 3f;
        float elapsed = 0f;
        while (GameManager.Instance == null && elapsed < timeout)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager not found in scene. Add GameManager to start a local game.");
            SetStatus("GameManager missing");
            yield break;
        }

        GameManager.Instance.InitializeGame(true);
    }

    private void DestroyMenu()
    {
        if (panelObject != null)
            panelObject.SetActive(false);

        if (ownsCanvas && canvasObject != null)
        {
            Destroy(canvasObject);
        }
        else if (panelObject != null)
        {
            Destroy(panelObject);
        }
    }

#if USE_MIRROR
    private void RegisterLobbyHandlers()
    {
        if (lobbyHandlersRegistered)
            return;

        global::Mirror.NetworkClient.RegisterHandler<LobbyStateMessage>(OnLobbyStateMessage, false);
        lobbyHandlersRegistered = true;
    }

    private bool lobbyHandlersRegistered = false;

    private void OnLobbyStateMessage(LobbyStateMessage msg)
    {
        lobbyPlayers.Clear();
        if (msg.playerNames != null)
            lobbyPlayers.AddRange(msg.playerNames);

        UpdateLobbyUI();
    }

    private void ShowLobbyPanel(bool isHost)
    {
        lobbyIsHost = isHost;
        lobbyActive = true;
        SetMenuInteractable(false);
        EnsureLobbyPanel();

        if (lobbyPanel != null)
            lobbyPanel.SetActive(true);

        if (startGameButton != null)
            startGameButton.gameObject.SetActive(isHost);

        if (leaveLobbyButton != null)
            leaveLobbyButton.gameObject.SetActive(true);

        UpdateLobbyUI();
        SetStatus(isHost ? "Hosting room... waiting for players" : "Joining room... waiting for host");
    }

    private void HideLobbyPanel()
    {
        lobbyActive = false;
        SetMenuInteractable(true);
        if (lobbyPanel != null)
            lobbyPanel.SetActive(false);
        lobbyPlayers.Clear();
    }

    private void SetMenuInteractable(bool interactable)
    {
        if (hostButton != null)
            hostButton.interactable = interactable;
        if (joinButton != null)
            joinButton.interactable = interactable;
        if (botButton != null)
            botButton.interactable = interactable;
        if (!interactable)
            UpdateNameRequirements();
    }

    private void EnsureLobbyPanel()
    {
        if (lobbyPanel != null)
            return;

        if (contentObject == null)
            return;

        lobbyPanel = new GameObject("LobbyPanel", typeof(RectTransform));
        lobbyPanel.transform.SetParent(contentObject.transform, false);
        Image panelImage = lobbyPanel.AddComponent<Image>();
        panelImage.color = new Color(0.08f, 0.08f, 0.08f, 0.95f);
        RectTransform lobbyRect = lobbyPanel.GetComponent<RectTransform>();
        lobbyRect.anchorMin = new Vector2(0.06f, 0.02f);
        lobbyRect.anchorMax = new Vector2(0.94f, 0.18f);
        lobbyRect.offsetMin = Vector2.zero;
        lobbyRect.offsetMax = Vector2.zero;

        TextMeshProUGUI titleText = CreateTextElement("LobbyTitle", lobbyPanel.transform, "Room Lobby", 24, TextAlignmentOptions.Center);
        RectTransform titleRect = titleText.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.05f, 0.65f);
        titleRect.anchorMax = new Vector2(0.95f, 0.95f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;

        lobbyHostIpText = CreateTextElement("LobbyHostIP", lobbyPanel.transform, "", 18, TextAlignmentOptions.Center);
        RectTransform hostIpRect = lobbyHostIpText.GetComponent<RectTransform>();
        hostIpRect.anchorMin = new Vector2(0.05f, 0.55f);
        hostIpRect.anchorMax = new Vector2(0.95f, 0.65f);
        hostIpRect.offsetMin = Vector2.zero;
        hostIpRect.offsetMax = Vector2.zero;

        lobbyPlayerListText = CreateTextElement("LobbyPlayers", lobbyPanel.transform, "Waiting for players...", 18, TextAlignmentOptions.TopLeft);
        lobbyPlayerListText.enableWordWrapping = true;
        RectTransform listRect = lobbyPlayerListText.GetComponent<RectTransform>();
        listRect.anchorMin = new Vector2(0.05f, 0.15f);
        listRect.anchorMax = new Vector2(0.7f, 0.5f);
        listRect.offsetMin = Vector2.zero;
        listRect.offsetMax = Vector2.zero;

        startGameButton = CreateButton(
            "StartGameButton",
            lobbyPanel.transform,
            new Vector2(1f, 0f),
            new Vector2(1f, 0f),
            new Vector2(1f, 0f),
            new Vector2(-16f, 16f),
            new Vector2(140f, 36f),
            "Start Game",
            new Color(0.16f, 0.6f, 0.16f, 1f),
            Color.white
        );
        startGameButton.onClick.AddListener(OnStartGameClicked);

        leaveLobbyButton = CreateButton(
            "LeaveLobbyButton",
            lobbyPanel.transform,
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0f, 16f),
            new Vector2(140f, 36f),
            "Leave Room",
            new Color(0.68f, 0.18f, 0.18f, 1f),
            Color.white
        );
        leaveLobbyButton.onClick.AddListener(OnLeaveLobbyClicked);

        lobbyPanel.SetActive(false);
    }

    private TextMeshProUGUI CreateTextElement(string name, Transform parent, string text, float fontSize, TextAlignmentOptions alignment)
    {
        GameObject textObj = new GameObject(name, typeof(RectTransform));
        textObj.transform.SetParent(parent, false);
        TextMeshProUGUI textComponent = textObj.AddComponent<TextMeshProUGUI>();
        textComponent.fontSize = fontSize;
        textComponent.text = text;
        textComponent.color = Color.white;
        textComponent.alignment = alignment;
        RectTransform rect = textObj.GetComponent<RectTransform>();
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return textComponent;
    }

    private Button CreateButton(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta, string label, Color backgroundColor, Color textColor)
    {
        GameObject buttonObj = new GameObject(name, typeof(RectTransform));
        buttonObj.transform.SetParent(parent, false);
        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = backgroundColor;
        Button button = buttonObj.AddComponent<Button>();

        RectTransform rect = buttonObj.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        TextMeshProUGUI buttonText = CreateTextElement(name + "Text", buttonObj.transform, label, 20, TextAlignmentOptions.Center);
        buttonText.color = textColor;
        buttonText.enableWordWrapping = false;
        RectTransform textRect = buttonText.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        return button;
    }

    private void UpdateLobbyUI()
    {
        if (lobbyHostIpText != null)
        {
            if (lobbyIsHost)
                lobbyHostIpText.text = $"Host IP: {GetHostIpAddress()}";
            else
                lobbyHostIpText.text = $"Connected to: {GetConnectedHostAddress()}";
        }

        if (lobbyPlayerListText == null)
            return;

        if (lobbyPlayers.Count == 0)
        {
            lobbyPlayerListText.text = "Waiting for players...";
            return;
        }

        string playerList = "Players in room:\n";
        for (int i = 0; i < lobbyPlayers.Count; i++)
        {
            playerList += $"{i + 1}. {lobbyPlayers[i]}\n";
        }

        lobbyPlayerListText.text = playerList.TrimEnd();
    }

    private void OnStartGameClicked()
    {
        if (global::Mirror.NetworkManager.singleton == null || !global::Mirror.NetworkServer.active)
            return;

        SetStatus("Starting game...");
        HideLobbyPanel();
        DestroyMenu();
        global::Mirror.NetworkManager.singleton.ServerChangeScene(gameSceneName);
    }

    private void OnLeaveLobbyClicked()
    {
        if (global::Mirror.NetworkManager.singleton != null)
        {
            if (global::Mirror.NetworkServer.active)
                global::Mirror.NetworkManager.singleton.StopHost();
            else if (global::Mirror.NetworkClient.isConnected)
                global::Mirror.NetworkManager.singleton.StopClient();
        }

        HideLobbyPanel();
        SetStatus("Idle");
    }

    private void RequestLobbyStateUpdate()
    {
        StartCoroutine(RequestLobbyStateWhenConnected());
    }

    private IEnumerator RequestLobbyStateWhenConnected()
    {
        float timeout = 5f;
        float elapsed = 0f;
        while (elapsed < timeout)
        {
            if (global::Mirror.NetworkClient.isConnected)
            {
                global::Mirror.NetworkClient.Send(new RequestLobbyStateMessage());
                yield break;
            }

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private string GetHostIpAddress()
    {
        try
        {
            var entries = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName()).AddressList;
            foreach (var addr in entries)
            {
                if (addr.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && !System.Net.IPAddress.IsLoopback(addr))
                {
                    return addr.ToString();
                }
            }
        }
        catch { }

        return "localhost";
    }

    private string GetConnectedHostAddress()
    {
        if (global::Mirror.NetworkManager.singleton != null && !string.IsNullOrWhiteSpace(global::Mirror.NetworkManager.singleton.networkAddress))
            return global::Mirror.NetworkManager.singleton.networkAddress;

        return "unknown";
    }
#endif

    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }

    private bool IsNameValid()
    {
        return nameInput != null && !string.IsNullOrWhiteSpace(nameInput.text);
    }

    private void UpdateNameRequirements()
    {
        bool valid = IsNameValid();
        if (hostButton != null) hostButton.interactable = valid;
        if (joinButton != null) joinButton.interactable = valid;
        if (botButton != null) botButton.interactable = valid;
        if (!valid)
            SetStatus("Enter a player name to continue");
    }

#if USE_MIRROR
    private void EnsureMirrorNetworkManager()
    {
        if (global::Mirror.NetworkManager.singleton != null)
        {
            ConfigureMirrorNetworkManager(global::Mirror.NetworkManager.singleton);
            return;
        }

        GameObject nmObj = GameObject.Find("NetworkManager");
        global::Mirror.NetworkManager manager = null;
        if (nmObj == null)
        {
            nmObj = new GameObject("NetworkManager");
            manager = nmObj.AddComponent<global::Mirror.NetworkManager>();
        }
        else
        {
            manager = nmObj.GetComponent<global::Mirror.NetworkManager>();
            if (manager == null)
                manager = nmObj.AddComponent<global::Mirror.NetworkManager>();
        }

        EnsureMirrorTransport(manager);
        ConfigureMirrorNetworkManager(manager);
    }

    private void EnsureMirrorTransport(global::Mirror.NetworkManager manager)
    {
        if (manager.GetComponent<global::Mirror.Transport>() != null)
            return;

        foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            System.Type transportType = asm.GetType("Mirror.KcpTransport") ?? asm.GetType("Mirror.TelepathyTransport");
            if (transportType != null)
            {
                manager.gameObject.AddComponent(transportType);
                return;
            }
        }
    }

    private void ConfigureMirrorNetworkManager(global::Mirror.NetworkManager manager)
    {
        manager.offlineScene = SceneManager.GetActiveScene().name;
        manager.onlineScene = gameSceneName;
        manager.autoCreatePlayer = true;

        if (manager.playerPrefab == null)
        {
            manager.playerPrefab = CreateNetworkPlayerPrefab();
        }
    }

    private GameObject CreateNetworkPlayerPrefab()
    {
        GameObject prefab = new GameObject("NetworkPlayerPrefab");
        prefab.hideFlags = HideFlags.HideAndDontSave;
        prefab.AddComponent<global::Mirror.NetworkIdentity>();
        prefab.AddComponent<NetworkPlayer>();
        prefab.AddComponent<Player>();
        prefab.SetActive(false);
        global::Mirror.NetworkClient.RegisterPrefab(prefab);
        return prefab;
    }
#endif
}
