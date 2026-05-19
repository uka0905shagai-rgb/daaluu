using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GameState
{
    Setup,
    RankDeclaration,
    Playing,
    RoundEnd,
    GameEnd
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private DeckManager deckManager;
    
    private List<Player> players = new List<Player>();
    private Player humanPlayer;
    public int currentPlayerIndex = 0; // Public for GameFlowController
    private int nextStartingPlayerIndex = 0;
    private GameState gameState = GameState.Setup;

    // Game flow
    private int roundNumber = 0;
    private List<Card> currentTrick = new List<Card>();
    private Player trickStarter;
    private Player trickWinner;
    private Player gameWinner;
    private readonly Dictionary<int, Dictionary<int, int>> teaDebt = new Dictionary<int, Dictionary<int, int>>();
    private bool suppressDebtPayment = false;
    [SerializeField] private float autoRestartDelay = 2f;
    private Coroutine autoRestartRoutine;
    private bool isInitializingGame = false;
    private bool settlementApplied = false;

    // Event system
    public delegate void GameStateChangeHandler(GameState newState);
    public event GameStateChangeHandler OnGameStateChanged;

    public delegate void PlayerActionHandler(Player player);
    public event PlayerActionHandler OnPlayerTurnStarted;
    public event PlayerActionHandler OnPlayerMadeAction;

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
        if (deckManager == null)
        {
            deckManager = GetComponent<DeckManager>();
            if (deckManager == null)
            {
                deckManager = gameObject.AddComponent<DeckManager>();
            }
        }
    }

    public void InitializeGame()
    {
        InitializeGame(true);
    }

    /// <summary>
    /// Initialize a new game. If <paramref name="clearDebts"/> is false, existing debts are preserved.
    /// </summary>
    public void InitializeGame(bool clearDebts)
    {
        if (isInitializingGame)
            return;

        isInitializingGame = true;

        try
        {
            gameWinner = null;
            settlementApplied = false;
            if (clearDebts)
                ClearAllDebts();

            // Ensure DeckManager is initialized
            if (deckManager == null)
            {
                deckManager = GetComponent<DeckManager>();
                if (deckManager == null)
                {
                    deckManager = gameObject.AddComponent<DeckManager>();
                }
            }

            SetGameState(GameState.Setup);
            PreparePlayersForNewGame();

            if (players.Count == 0)
            {
                Debug.LogError("Game initialization aborted: no players available.");
                return;
            }

            if (players.Count > 0)
                nextStartingPlayerIndex = Mathf.Clamp(nextStartingPlayerIndex, 0, players.Count - 1);
            currentPlayerIndex = nextStartingPlayerIndex;
            deckManager.InitializeDeck();
            DealInitialCards();
            SetGameState(GameState.RankDeclaration);
        }
        finally
        {
            isInitializingGame = false;
        }
    }

    private void PreparePlayersForNewGame()
    {
        if (players.Count == 0)
        {
            CreatePlayers();
            return;
        }

        foreach (Player player in players)
        {
            if (player != null)
                player.ResetForNewGame(true);
        }
    }

    private void CreatePlayers()
    {
        players.Clear();

#if USE_MIRROR
        if (global::Mirror.NetworkServer.active)
        {
            NetworkPlayer[] networkPlayers = FindObjectsOfType<NetworkPlayer>();
            if (networkPlayers.Length > 0)
            {
                System.Array.Sort(networkPlayers, (a, b) => a.netPlayerID.CompareTo(b.netPlayerID));
                foreach (NetworkPlayer networkPlayer in networkPlayers)
                {
                    Player playerComponent = networkPlayer.GetComponent<Player>();
                    if (playerComponent == null)
                        continue;

                    playerComponent.Initialize(networkPlayer.netPlayerID, true, networkPlayer.netPlayerName);
                    playerComponent.transform.SetParent(transform);
                    players.Add(playerComponent);
                }

                if (players.Count > 0)
                    humanPlayer = players[0];
            }
        }
#endif

        if (players.Count == 0)
        {
#if USE_MIRROR
            if (global::Mirror.NetworkServer.active)
            {
                Debug.LogWarning("Network server active but no NetworkPlayer objects found; not creating bot players for online game.");
                return;
            }
#endif
            string savedName = PlayerPrefs.GetString("PlayerName", "");

            // Create human player
            GameObject humanPlayerObj = new GameObject("HumanPlayer");
            humanPlayerObj.transform.SetParent(transform);
            Player humanPlayerComponent = humanPlayerObj.AddComponent<Player>();
            humanPlayerComponent.Initialize(0, true, savedName);
            players.Add(humanPlayerComponent);
            humanPlayer = humanPlayerComponent;

            // Create bot players
            for (int i = 1; i < GameRules.TOTAL_PLAYERS; i++)
            {
                GameObject botObj = new GameObject($"Bot_{i}");
                botObj.transform.SetParent(transform);
                Player botComponent = botObj.AddComponent<Player>();
                botComponent.Initialize(i, false, $"Bot_{i}");
                players.Add(botComponent);
            }
        }

        Debug.Log($"Created {players.Count} players");
    }

    public void ClearAllDebts()
    {
        teaDebt.Clear();
        Debug.Log("[GameManager] All debts cleared.");
    }

    private void DealInitialCards()
    {
        // Tea pieces are score pieces, not playable hand pieces.
        bool grantStartingTea = true;
        foreach (Player player in players)
        {
            if (player.GetTeaCardsCollected() > 0)
            {
                grantStartingTea = false;
                break;
            }
        }

        if (grantStartingTea)
        {
            for (int i = 0; i < GameRules.TOTAL_PLAYERS; i++)
            {
                players[i].AddTeaCards(GameRules.TEA_CARDS_PER_PLAYER);
            }
        }

        // Deal remaining cards
        List<Card> regularCards = new List<Card>();
        for (int i = 0; i < GameRules.STARTING_HAND_SIZE * GameRules.TOTAL_PLAYERS; i++)
        {
            Card card = deckManager.DrawCard();
            if (card != null)
                regularCards.Add(card);
        }

        for (int i = 0; i < regularCards.Count; i++)
        {
            players[i % GameRules.TOTAL_PLAYERS].AddCardToHand(regularCards[i]);
        }

        Debug.Log("Initial cards dealt");
    }

    public void SetGameState(GameState newState)
    {
        if (gameState == newState) return;

        gameState = newState;
        Debug.Log($"Game State: {gameState}");
        OnGameStateChanged?.Invoke(gameState);
    }

    public GameState GetGameState()
    {
        return gameState;
    }

    public List<Player> GetAllPlayers()
    {
        return new List<Player>(players);
    }

    public Player GetPlayerByID(int id)
    {
        return players.Find(p => p.playerID == id);
    }

    public Player GetCurrentPlayer()
    {
        return players[currentPlayerIndex];
    }

    public Player GetHumanPlayer()
    {
        return humanPlayer;
    }

    public int GetCurrentPlayerIndex()
    {
        return currentPlayerIndex;
    }

    public void NextPlayer()
    {
        currentPlayerIndex = (currentPlayerIndex + 1) % GameRules.TOTAL_PLAYERS;
    }

    public void EndGame(Player winner)
    {
        gameWinner = winner;
        ApplyTeaSettlement();
        SetGameState(GameState.GameEnd);

        if (players.Count > 0)
            nextStartingPlayerIndex = (nextStartingPlayerIndex + 1) % players.Count;

        // Show end-game UI overlay if available
        if (EndGameUI.Instance != null)
            EndGameUI.Instance.Show(winner);
        Debug.Log($"Game Over! {winner.playerName} won by collecting all tea cards!");

        if (autoRestartRoutine != null)
            StopCoroutine(autoRestartRoutine);

        autoRestartRoutine = StartCoroutine(AutoRestartGame());
    }

    private IEnumerator AutoRestartGame()
    {
        if (autoRestartDelay > 0f)
            yield return new WaitForSeconds(autoRestartDelay);

        // Auto-restart should preserve debts (tsai) so pass clearDebts=false
        InitializeGame(false);
    }

    public void CheckWinCondition()
    {
        foreach (Player player in players)
        {
            if (player.HasAllTeaCards())
            {
                EndGame(player);
                return;
            }
        }

        if (AreAllHandsEmpty())
        {
            EndGame(GetLeadingPlayer());
        }
    }

    // Server-side API placeholder: handle a play request from a networked player by id and card instance id.
    // Returns true if the server accepted the play.
    public bool ServerRequestPlayCard(int playerId, int cardInstanceId)
    {
        Debug.Log($"[GameManager] ServerRequestPlayCard received from P{playerId} cardInstance:{cardInstanceId}");
        // Placeholder: integrate with server-side validation and trick resolution later.
        return false;
    }

    public Player GetGameWinner()
    {
        return gameWinner;
    }

    public bool AreAllHandsEmpty()
    {
        foreach (Player player in players)
        {
            if (player.GetHandSize() > 0)
                return false;
        }

        return players.Count > 0;
    }

    private Player GetLeadingPlayer()
    {
        Player leader = players[0];

        foreach (Player player in players)
        {
            if (player.GetTeaCardsCollected() > leader.GetTeaCardsCollected())
            {
                leader = player;
            }
            else if (player.GetTeaCardsCollected() == leader.GetTeaCardsCollected() &&
                     player.GetCollectedCardCount() > leader.GetCollectedCardCount())
            {
                leader = player;
            }
        }

        return leader;
    }

    public void DebugPlayerStates()
    {
        foreach (Player player in players)
        {
            Debug.Log(player.ToString());
        }
    }

    public string GetDebtSummary()
    {
        if (teaDebt.Count == 0)
            return "No debts.";

        string summary = "Lease List:\n";
        foreach (var debtorEntry in teaDebt)
        {
            Player debtor = GetPlayerByID(debtorEntry.Key);
            if (debtorEntry.Value.Count == 0)
                continue;

            foreach (var creditorEntry in debtorEntry.Value)
            {
                if (creditorEntry.Value <= 0)
                    continue;

                Player creditor = GetPlayerByID(creditorEntry.Key);
                string debtorName = debtor != null ? debtor.playerName : $"P{debtorEntry.Key}";
                string creditorName = creditor != null ? creditor.playerName : $"P{creditorEntry.Key}";
                summary += $"{debtorName} owes {creditorName}: {creditorEntry.Value}\n";
            }
        }

        return summary.TrimEnd();
    }

    // Returns a deep copy of the current debt map: debtorId -> (creditorId -> amount)
    public Dictionary<int, Dictionary<int, int>> GetDebtMap()
    {
        var copy = new Dictionary<int, Dictionary<int, int>>();
        foreach (var debtorEntry in teaDebt)
        {
            var inner = new Dictionary<int, int>();
            foreach (var creditorEntry in debtorEntry.Value)
            {
                inner[creditorEntry.Key] = creditorEntry.Value;
            }
            copy[debtorEntry.Key] = inner;
        }
        return copy;
    }

    public void HandleTeaGain(Player player, int gained)
    {
        if (player == null || suppressDebtPayment)
            return;

        // If player owes tea to others, attempt to settle debts first.
        if (teaDebt.ContainsKey(player.playerID))
        {
            Dictionary<int, int> debts = teaDebt[player.playerID];
            if (debts.Count > 0)
            {
                // Pay highest debts first.
                List<KeyValuePair<int, int>> orderedDebts = new List<KeyValuePair<int, int>>(debts);
                orderedDebts.Sort((a, b) => b.Value.CompareTo(a.Value));
                foreach (var entry in orderedDebts)
                {
                    int creditorId = entry.Key;
                    int owed = entry.Value;
                    if (owed <= 0)
                        continue;

                    int available = player.GetTeaCardsCollected();
                    if (available <= 0)
                        break;

                    int payment = Mathf.Min(available, owed);
                    Player creditor = GetPlayerByID(creditorId);
                    if (creditor != null)
                    {
                        suppressDebtPayment = true;
                        creditor.AddTeaCards(payment);
                        suppressDebtPayment = false;
                    }

                    player.RemoveTeaCards(payment);
                    owed -= payment;
                    if (owed <= 0)
                        debts.Remove(creditorId);
                    else
                        debts[creditorId] = owed;
                }
            }
        }

        // After resolving debts (if any), check for win condition: reaching total tea cards.
        if (player.GetTeaCardsCollected() >= GameRules.TOTAL_TEA_CARDS)
        {
            if (gameWinner == null)
                EndGame(player);
            return;
        }

        // Also verify other win conditions (e.g., all hands empty)
        CheckWinCondition();
    }

    public void ApplyTeaSettlement()
    {
        Debug.Log("[GameManager] ApplyTeaSettlement called.");
        if (players.Count == 0)
            return;
        if (settlementApplied)
        {
            Debug.Log("[GameManager] ApplyTeaSettlement skipped: already applied for this game.");
            return;
        }
        try
        {
            settlementApplied = true;
            Dictionary<int, int> remainingReceives = new Dictionary<int, int>();
            List<Player> payers = new List<Player>();
            List<Player> receivers = new List<Player>();

            foreach (Player player in players)
            {
                int delta = player.GetTricksWon() - 2;
                if (delta < 0)
                    payers.Add(player);
                else if (delta > 0)
                    receivers.Add(player);

                if (delta > 0)
                    remainingReceives[player.playerID] = delta;
            }

            if (payers.Count == 0 || receivers.Count == 0)
                return;

            foreach (Player payer in payers)
            {
                int owed = 2 - payer.GetTricksWon();
                if (owed <= 0)
                    continue;

                bool startedWithTsai = payer.GetTeaCardsCollected() > 0;
                bool allowDebtWhenOut = !startedWithTsai || payer.GetTricksWon() == 0;

                foreach (Player receiver in receivers)
                {
                    if (owed <= 0)
                        break;

                    if (!remainingReceives.TryGetValue(receiver.playerID, out int remaining))
                        continue;

                    while (owed > 0 && remaining > 0)
                    {
                        if (payer.GetTeaCardsCollected() > 0)
                        {
                            payer.RemoveTeaCards(1);
                            receiver.AddTeaCards(1);
                        }
                        else if (allowDebtWhenOut)
                        {
                            AddTeaDebt(payer, receiver, 1);
                        }
                        else
                        {
                            // Payer started with tsai; if they run out, do not create debt.
                            owed = 0;
                            break;
                        }

                        owed -= 1;
                        remaining -= 1;
                    }

                    if (remaining <= 0)
                        remainingReceives.Remove(receiver.playerID);
                    else
                        remainingReceives[receiver.playerID] = remaining;
                }
            }

            Debug.Log("[GameManager] ApplyTeaSettlement completed. Current debts: " + teaDebt.Count);
        }
        finally
        {
        }
    }

    private void AddTeaDebt(Player debtor, Player creditor, int amount)
    {
        if (debtor == null || creditor == null)
            return;

        if (debtor.playerID == creditor.playerID)
            return; // do not allow self-debt

        if (amount <= 0)
            return;

        // Ensure both players are known in current game
        if (GetPlayerByID(debtor.playerID) == null || GetPlayerByID(creditor.playerID) == null)
            return;

        if (!teaDebt.ContainsKey(debtor.playerID))
            teaDebt[debtor.playerID] = new Dictionary<int, int>();

        Dictionary<int, int> debts = teaDebt[debtor.playerID];
        if (!debts.ContainsKey(creditor.playerID))
            debts[creditor.playerID] = 0;

        debts[creditor.playerID] += amount;
        Debug.Log($"[GameManager] AddTeaDebt: {debtor.playerName} now owes {creditor.playerName} {debts[creditor.playerID]}");
    }
}
