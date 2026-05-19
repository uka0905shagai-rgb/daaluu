using UnityEngine;
using UnityEngine.InputSystem;
#if USE_MIRROR
using Mirror;
#endif

/// <summary>
/// Main entry point for the game - initializes and starts the game
/// </summary>
public class GameController : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private GameFlowController gameFlowController;
    [SerializeField] private GameBoardUI gameBoardUI;

    private void Awake()
    {
        // Find or create necessary components
        if (gameManager == null)
            gameManager = FindObjectOfType<GameManager>();
        if (gameManager == null)
        {
            GameObject gmObj = new GameObject("GameManager");
            gameManager = gmObj.AddComponent<GameManager>();
            gameManager.gameObject.AddComponent<DeckManager>();
        }

        if (gameFlowController == null)
            gameFlowController = FindObjectOfType<GameFlowController>();
        if (gameFlowController == null)
        {
            gameFlowController = gameManager.gameObject.AddComponent<GameFlowController>();
        }

        if (gameBoardUI == null)
            gameBoardUI = FindObjectOfType<GameBoardUI>();
    }

    private void Start()
    {
#if USE_MIRROR
        if (NetworkClient.active && !NetworkServer.active)
            return;
#endif
        // Initialize game
        gameManager.InitializeGame();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        // Debug: Press R to restart game
        if (keyboard.rKey.wasPressedThisFrame)
        {
            RestartGame();
        }

        // Debug: Press Q to quit
        if (keyboard.qKey.wasPressedThisFrame)
        {
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }
    }

    private void RestartGame()
    {
        Debug.Log("Restarting game...");
        gameManager.InitializeGame();
    }
}
