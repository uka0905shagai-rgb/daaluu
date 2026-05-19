using System.Collections.Generic;
using UnityEngine;
using System.Collections;

/// <summary>
/// Controls the main game flow and turn management
/// </summary>
public class GameFlowController : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private TrickManager trickManager;
    [SerializeField] private GameBoardUI gameBoardUI;
    [SerializeField] private float botActionDelay = 1f;
    [SerializeField] private float playRevealDelay = 1f;

    private Dictionary<int, BotAI> botAIs = new Dictionary<int, BotAI>();
    private Player currentPlayer;
    private bool waitingForPlayerInput = false;
    private readonly List<Card> selectedCardsToPlay = new List<Card>();
    private List<Card> currentValidMoves = new List<Card>();
    private List<List<Card>> currentValidPairMoves = new List<List<Card>>();
    private Coroutine rankDeclarationRoutine;
    private Coroutine gameLoopRoutine;
    private DaaluuPieceName? pendingJanlii;
    [SerializeField] private float autoPlayDelay = 0.5f;
    private int selectionVersion = 0;

    private void Awake()
    {
        if (gameManager == null)
            gameManager = GetComponent<GameManager>();
        if (gameManager == null)
            gameManager = FindObjectOfType<GameManager>();
            
        if (trickManager == null)
            trickManager = gameManager.GetComponent<TrickManager>();
        if (trickManager == null)
            trickManager = gameManager.gameObject.AddComponent<TrickManager>();
        if (gameBoardUI == null)
            gameBoardUI = FindObjectOfType<GameBoardUI>();
    }

    private void Start()
    {
        if (gameManager == null)
            gameManager = FindObjectOfType<GameManager>();
        if (gameBoardUI == null)
            gameBoardUI = FindObjectOfType<GameBoardUI>();
            
        if (gameManager != null)
        {
            gameManager.OnGameStateChanged += HandleGameStateChange;
            HandleGameStateChange(gameManager.GetGameState());
        }
    }

    private void OnDestroy()
    {
        if (gameManager != null)
            gameManager.OnGameStateChanged -= HandleGameStateChange;
    }

    private void HandleGameStateChange(GameState newState)
    {
        switch (newState)
        {
            case GameState.Setup:
                break;
            case GameState.RankDeclaration:
                if (rankDeclarationRoutine == null)
                    rankDeclarationRoutine = StartCoroutine(DoRankDeclaration());
                break;
            case GameState.Playing:
                if (gameLoopRoutine == null)
                    gameLoopRoutine = StartCoroutine(DoGameLoop());
                break;
            case GameState.GameEnd:
                break;
        }
    }

    private IEnumerator DoRankDeclaration()
    {
        Debug.Log("=== RANK DECLARATION PHASE ===");
        List<Player> players = gameManager.GetAllPlayers();
        Player starter = gameManager.GetCurrentPlayer();
        DaaluuPieceName declaredJanlii;

        if (starter.IsHuman)
        {
            pendingJanlii = null;

            if (gameBoardUI != null)
            {
                gameBoardUI.SetPlayerHandVisible(false);
                gameBoardUI.ShowJanliiSelection(starter, OnHumanJanliiSelected);
                yield return new WaitUntil(() => pendingJanlii.HasValue);
                gameBoardUI.HideJanliiSelection();
                gameBoardUI.SetPlayerHandVisible(true);
            }
            else
            {
                pendingJanlii = ChooseBestJanlii(starter);
            }

            declaredJanlii = pendingJanlii.Value;
        }
        else
        {
            declaredJanlii = GetRandomJanlii();
        }

        starter.DeclareJanlii(declaredJanlii);
        trickManager.SetJanliiPiece(declaredJanlii);
        Debug.Log($"{starter.playerName} names Janlii: {GameRules.GetPieceDisplayName(declaredJanlii)}");
        yield return new WaitForSeconds(0.5f);

        Debug.Log("Janlii declaration complete");
        rankDeclarationRoutine = null;
        gameManager.SetGameState(GameState.Playing);
    }

    private DaaluuPieceName GetRandomJanlii()
    {
        int index = Random.Range(0, GameRules.JanliiCandidates.Length);
        return GameRules.JanliiCandidates[index];
    }

    private IEnumerator DoGameLoop()
    {
        Debug.Log("=== GAME START ===");
        gameManager.DebugPlayerStates();

        if (gameManager.GetGameState() != GameState.Playing)
        {
            gameLoopRoutine = null;
            yield break;
        }

        int roundNumber = 0;
        while (gameManager.GetGameState() == GameState.Playing)
        {
            roundNumber++;
            Debug.Log($"\n--- ROUND {roundNumber} ---");
            yield return StartCoroutine(PlayRound());

            gameManager.CheckWinCondition();
        }

        Debug.Log("Game loop ended");
        gameLoopRoutine = null;
    }

    private void OnHumanJanliiSelected(DaaluuPieceName pieceName)
    {
        pendingJanlii = pieceName;
    }

    private DaaluuPieceName ChooseBestJanlii(Player player)
    {
        Dictionary<DaaluuPieceName, int> pieceCount = new Dictionary<DaaluuPieceName, int>();

        foreach (Card card in player.GetHandCopy())
        {
            if (!card.IsJanliiCandidate())
                continue;

            if (!pieceCount.ContainsKey(card.pieceName))
                pieceCount[card.pieceName] = 0;

            pieceCount[card.pieceName]++;
        }

        DaaluuPieceName bestPiece = DaaluuPieceName.Ys;
        int maxCount = 0;
        int highestValue = 0;

        foreach (var kvp in pieceCount)
        {
            int value = GameRules.GetPieceInfo(kvp.Key).Value;
            if (kvp.Value > maxCount || (kvp.Value == maxCount && value > highestValue))
            {
                maxCount = kvp.Value;
                highestValue = value;
                bestPiece = kvp.Key;
            }
        }

        return bestPiece;
    }

    private IEnumerator PlayRound()
    {
        List<Player> players = gameManager.GetAllPlayers();
        Player roundStarter = gameManager.GetCurrentPlayer();
        if (gameBoardUI != null)
            gameBoardUI.ClearWinnerStack();
        trickManager.StartNewTrick(roundStarter);
        RefreshBoardUI();

        bool roundActive = true;
        int playerIndex = gameManager.GetCurrentPlayerIndex();

        while (roundActive && gameManager.GetGameState() == GameState.Playing)
        {
            if (gameManager.AreAllHandsEmpty())
                yield break;

            currentPlayer = players[playerIndex];

            bool isRoundStart = trickManager.GetTrickPlayCount() == 0;
            int leadPlaySize = trickManager.GetLeadPlaySize();

            if (isRoundStart && leadPlaySize == 0)
            {
                bool anyEligibleStarter = HasAnyEligibleStarter(players);
                if (anyEligibleStarter && !HasEligibleStart(currentPlayer))
                {
                    Debug.Log($"{currentPlayer.playerName} cannot start (no eligible piece)");
                    playerIndex = (playerIndex + 1) % GameRules.TOTAL_PLAYERS;
                    continue;
                }
            }

            // Check if player can play
            List<Card> validMoves = GameRulesValidator.GetValidMoves(
                currentPlayer,
                trickManager,
                isRoundStart
            );

            currentValidPairMoves = GameRulesValidator.GetValidPairMoves(
                currentPlayer,
                trickManager,
                isRoundStart
            );

            if (isRoundStart && leadPlaySize == 0 && validMoves.Count == 0)
                validMoves = currentPlayer.GetHandCopy();

            if (leadPlaySize == 2 && currentValidPairMoves.Count == 0)
                currentValidPairMoves = GameRulesValidator.GetValidPairMoves(currentPlayer, trickManager, true);

            bool hasPlayableMoves = leadPlaySize == 2
                ? currentValidPairMoves.Count > 0
                : (validMoves.Count > 0 || currentValidPairMoves.Count > 0);

            if (!hasPlayableMoves)
            {
                Debug.Log($"{currentPlayer.playerName} cannot play (no valid moves)");
                playerIndex = (playerIndex + 1) % GameRules.TOTAL_PLAYERS;
                continue;
            }

            // Player plays a card
            if (currentPlayer.IsHuman)
            {
                currentValidMoves = validMoves;
                UpdateSelectionHighlights();
                TryAutoPlayPreselectedCards();
                yield return StartCoroutine(WaitForPlayerCardSelection(currentPlayer, validMoves));

                if (selectedCardsToPlay.Count > 0)
                {
                    if (selectedCardsToPlay.Count == 2)
                    {
                        currentPlayer.PlayCard(selectedCardsToPlay[0]);
                        currentPlayer.PlayCard(selectedCardsToPlay[1]);
                        trickManager.PlayCards(selectedCardsToPlay, currentPlayer);
                        Debug.Log($"{currentPlayer.playerName} plays a pair: {selectedCardsToPlay[0]} / {selectedCardsToPlay[1]}");
                    }
                    else
                    {
                        currentPlayer.PlayCard(selectedCardsToPlay[0]);
                        trickManager.PlayCard(selectedCardsToPlay[0], currentPlayer);
                        Debug.Log($"{currentPlayer.playerName} plays {selectedCardsToPlay[0]}");
                    }

                    currentValidMoves.Clear();
                    currentValidPairMoves.Clear();
                    selectedCardsToPlay.Clear();
                    selectionVersion++;
                    UpdateSelectionHighlights();
                    RefreshBoardUI();
                    yield return new WaitForSeconds(playRevealDelay);
                }
            }
            else
            {
                // Bot plays
                BotAI botAI = GetOrCreateBotAI(currentPlayer);
                if (leadPlaySize == 2 || (isRoundStart && currentValidPairMoves.Count > 0))
                {
                    List<Card> pairToPlay = botAI.DecidePairToPlay(currentValidPairMoves);
                    if (pairToPlay != null && pairToPlay.Count == 2)
                    {
                        currentPlayer.PlayCard(pairToPlay[0]);
                        currentPlayer.PlayCard(pairToPlay[1]);
                        trickManager.PlayCards(pairToPlay, currentPlayer);
                        Debug.Log($"{currentPlayer.playerName} plays a pair: {pairToPlay[0]} / {pairToPlay[1]}");
                    }
                    else
                    {
                        Card cardToPlay = botAI.DecideCardToPlay(validMoves);
                        currentPlayer.PlayCard(cardToPlay);
                        trickManager.PlayCard(cardToPlay, currentPlayer);
                        Debug.Log($"{currentPlayer.playerName} plays {cardToPlay}");
                    }
                }
                else
                {
                    Card cardToPlay = botAI.DecideCardToPlay(validMoves);
                    currentPlayer.PlayCard(cardToPlay);
                    trickManager.PlayCard(cardToPlay, currentPlayer);
                    Debug.Log($"{currentPlayer.playerName} plays {cardToPlay}");
                }

                RefreshBoardUI();
                yield return new WaitForSeconds(playRevealDelay);
            }

            // Check if trick is complete (all players have played)
            playerIndex = (playerIndex + 1) % GameRules.TOTAL_PLAYERS;
            if (trickManager.GetTrickPlayCount() >= GameRules.TOTAL_PLAYERS)
            {
                roundActive = false;
            }
        }

        // Resolve trick
        Player trickWinner = trickManager.CalculateTrickWinner();
        List<Card> collectedCards = trickManager.ResolveTrick();
        trickWinner.CollectCards(collectedCards);
        int gerAward = trickManager.GetLeadPlaySize() == 2 ? 2 : 1;
        trickWinner.AddTrickWin(gerAward);

        Debug.Log($"{trickWinner.playerName} wins the trick and collects {collectedCards.Count} cards");

        if (gameBoardUI != null)
            gameBoardUI.ShowWinnerStack(trickManager.GetTrickCards(), trickWinner);

        // Next round starter
        gameManager.currentPlayerIndex = GetPlayerIndex(trickWinner);

        yield return new WaitForSeconds(playRevealDelay);
    }

    private void RefreshBoardUI()
    {
        if (gameBoardUI == null)
            gameBoardUI = FindObjectOfType<GameBoardUI>();

        if (gameBoardUI != null)
            gameBoardUI.RefreshDisplay();
    }

    private IEnumerator WaitForPlayerCardSelection(Player player, List<Card> validMoves)
    {
        waitingForPlayerInput = true;
        Debug.Log($"Waiting for {player.playerName} to select a card");
        yield return new WaitUntil(() => !waitingForPlayerInput);
    }

    public void SelectCardForPlayer(Card card)
    {
        if (card != null)
        {
            if (gameManager == null)
                gameManager = FindObjectOfType<GameManager>();

            Player humanPlayer = gameManager != null ? gameManager.GetHumanPlayer() : null;
            if (humanPlayer == null || !humanPlayer.IsHuman)
                return;

            if (selectedCardsToPlay.Contains(card))
            {
                selectedCardsToPlay.Remove(card);
                selectionVersion++;
                UpdateSelectionHighlights();
                return;
            }

            bool isPlayersTurn = currentPlayer != null && currentPlayer == humanPlayer && waitingForPlayerInput;
            if (isPlayersTurn)
            {
                bool isValidSingle = currentValidMoves.Count == 0 || currentValidMoves.Contains(card);
                bool isValidPairMember = IsCardInValidPair(card);

                if (!isValidSingle && !isValidPairMember)
                {
                    Debug.Log($"{card} is not a valid move right now.");
                    return;
                }
            }

            HandleCardSelection(card, isPlayersTurn);
            selectionVersion++;
            UpdateSelectionHighlights();
        }
    }

    public void ConfirmSelectedPlay()
    {
        if (!waitingForPlayerInput || currentPlayer == null || !currentPlayer.IsHuman)
            return;

        int leadPlaySize = trickManager != null ? trickManager.GetLeadPlaySize() : 0;

        if (leadPlaySize == 2)
        {
            if (selectedCardsToPlay.Count != 2 || !IsPairValid(selectedCardsToPlay[0], selectedCardsToPlay[1]))
                return;

            waitingForPlayerInput = false;
            return;
        }

        if (leadPlaySize == 1)
        {
            if (selectedCardsToPlay.Count == 1 || (selectedCardsToPlay.Count == 2 && IsPairValid(selectedCardsToPlay[0], selectedCardsToPlay[1])))
            {
                waitingForPlayerInput = false;
            }
            return;
        }

        if (selectedCardsToPlay.Count == 1)
        {
            waitingForPlayerInput = false;
            return;
        }

        if (selectedCardsToPlay.Count == 2 && IsPairValid(selectedCardsToPlay[0], selectedCardsToPlay[1]))
        {
            waitingForPlayerInput = false;
        }
    }

    private void TryAutoPlayPreselectedCards()
    {
        if (selectedCardsToPlay.Count == 0)
            return;

        // If a pair is still possible, do not auto-confirm after the first click.
        // This keeps the human player in control when choosing between a single and a pair.
        if (selectedCardsToPlay.Count == 1 && currentValidPairMoves.Count > 0)
            return;

        if (!IsSelectionValidForTurn())
            return;

        int capturedVersion = selectionVersion;
        StartCoroutine(AutoPlayIfUnchanged(capturedVersion));
    }

    private IEnumerator AutoPlayIfUnchanged(int capturedVersion)
    {
        if (autoPlayDelay > 0f)
            yield return new WaitForSeconds(autoPlayDelay);

        if (selectionVersion != capturedVersion)
            yield break;

        if (IsSelectionValidForTurn())
            waitingForPlayerInput = false;
    }

    private bool IsSelectionValidForTurn()
    {
        if (selectedCardsToPlay.Count == 0)
            return false;

        int leadPlaySize = trickManager != null ? trickManager.GetLeadPlaySize() : 0;
        if (leadPlaySize == 2)
            return selectedCardsToPlay.Count == 2 && IsPairValid(selectedCardsToPlay[0], selectedCardsToPlay[1]);

        if (leadPlaySize == 1)
            return selectedCardsToPlay.Count == 1 && (currentValidMoves.Count == 0 || currentValidMoves.Contains(selectedCardsToPlay[0]));

        if (selectedCardsToPlay.Count == 1)
            return currentValidMoves.Count == 0 || currentValidMoves.Contains(selectedCardsToPlay[0]);

        if (selectedCardsToPlay.Count == 2)
            return IsPairValid(selectedCardsToPlay[0], selectedCardsToPlay[1]);

        return false;
    }

    private void HandleCardSelection(Card card, bool requireCurrentValidity)
    {
        int leadPlaySize = trickManager != null ? trickManager.GetLeadPlaySize() : 0;
        bool isPairRequired = leadPlaySize == 2;

        if (selectedCardsToPlay.Count == 0)
        {
            selectedCardsToPlay.Add(card);
            return;
        }

        if (selectedCardsToPlay.Count == 1)
        {
            Card first = selectedCardsToPlay[0];
            bool isValidPair = requireCurrentValidity ? IsPairValid(first, card) : IsPotentialPair(first, card);

            if (isValidPair)
            {
                selectedCardsToPlay.Add(card);
                return;
            }

            if (!isPairRequired)
            {
                selectedCardsToPlay.Clear();
                selectedCardsToPlay.Add(card);
            }

            return;
        }
    }

    private bool IsPotentialPair(Card first, Card second)
    {
        return first != null && second != null && first.pieceName == second.pieceName;
    }

    private void UpdateSelectionHighlights()
    {
        if (gameBoardUI != null)
            gameBoardUI.SetSelectedCards(selectedCardsToPlay);
    }

    private bool HasAnyEligibleStarter(List<Player> players)
    {
        foreach (Player player in players)
        {
            if (HasEligibleStart(player))
                return true;
        }

        return false;
    }

    private bool HasEligibleStart(Player player)
    {
        if (player == null)
            return false;

        DaaluuPieceName? janliiPiece = trickManager != null ? trickManager.GetJanliiPiece() : null;
        Dictionary<DaaluuPieceName, int> pieceCounts = new Dictionary<DaaluuPieceName, int>();

        foreach (Card card in player.GetHandCopy())
        {
            if (card == null)
                continue;

            if (card.eyeCount >= GameRules.MINIMUM_SINGLE_CARD_EYES)
                return true;

            if (janliiPiece.HasValue && card.pieceName == janliiPiece.Value)
                return true;

            if (!pieceCounts.ContainsKey(card.pieceName))
                pieceCounts[card.pieceName] = 0;

            pieceCounts[card.pieceName]++;
            if (pieceCounts[card.pieceName] >= 2)
                return true;
        }

        return false;
    }

    private bool HasMatchingPairInHand(Player player, Card card)
    {
        if (player == null || card == null)
            return false;

        int count = 0;
        foreach (Card handCard in player.GetHandCopy())
        {
            if (handCard.pieceName == card.pieceName)
                count++;
        }

        return count >= 2;
    }

    private bool IsCardInValidPair(Card card)
    {
        foreach (List<Card> pair in currentValidPairMoves)
        {
            if (pair.Contains(card))
                return true;
        }

        return false;
    }

    private bool IsPairValid(Card first, Card second)
    {
        if (first == null || second == null)
            return false;

        foreach (List<Card> pair in currentValidPairMoves)
        {
            if (pair.Contains(first) && pair.Contains(second))
                return true;
        }

        return false;
    }

    private BotAI GetOrCreateBotAI(Player player)
    {
        if (!botAIs.ContainsKey(player.playerID))
        {
            GameObject botObj = new GameObject($"BotAI_{player.playerID}");
            botObj.transform.SetParent(transform);
            BotAI botAI = botObj.AddComponent<BotAI>();
            botAI.Initialize(player, trickManager, BotAI.Difficulty.Medium);
            botAIs[player.playerID] = botAI;
        }

        return botAIs[player.playerID];
    }

    private int GetPlayerIndex(Player player)
    {
        List<Player> players = gameManager.GetAllPlayers();
        for (int i = 0; i < players.Count; i++)
        {
            if (players[i] == player)
                return i;
        }
        return 0;
    }
}
