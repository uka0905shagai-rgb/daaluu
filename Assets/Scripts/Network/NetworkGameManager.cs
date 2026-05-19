using UnityEngine;

#if USE_MIRROR
using Mirror;
using System.Collections.Generic;
using System.Linq;

public struct LobbyStateMessage : NetworkMessage
{
    public string[] playerNames;
}

public struct RequestLobbyStateMessage : NetworkMessage { }

public class NetworkGameManager : NetworkBehaviour
{
    public static NetworkGameManager Instance { get; private set; }

    private readonly Dictionary<int, string> lobbyPlayers = new Dictionary<int, string>();

    public override void OnStartServer()
    {
        base.OnStartServer();
        Instance = this;
        NetworkServer.RegisterHandler<RequestLobbyStateMessage>(OnRequestLobbyState, false);
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        Instance = null;
        lobbyPlayers.Clear();
    }

    private void OnRequestLobbyState(NetworkConnectionToClient conn, RequestLobbyStateMessage msg)
    {
        SendLobbyState(conn);
    }

    public void AddOrUpdatePlayer(NetworkPlayer networkPlayer)
    {
        if (networkPlayer == null)
            return;

        int connectionId = networkPlayer.connectionToClient != null ? networkPlayer.connectionToClient.connectionId : networkPlayer.netPlayerID;
        if (connectionId < 0)
            return;

        lobbyPlayers[connectionId] = networkPlayer.netPlayerName;
        BroadcastLobbyState();
    }

    public void RemovePlayer(NetworkPlayer networkPlayer)
    {
        if (networkPlayer == null)
            return;

        int connectionId = networkPlayer.connectionToClient != null ? networkPlayer.connectionToClient.connectionId : networkPlayer.netPlayerID;
        if (connectionId < 0)
            return;

        if (lobbyPlayers.Remove(connectionId))
            BroadcastLobbyState();
    }

    private void BroadcastLobbyState()
    {
        var msg = new LobbyStateMessage
        {
            playerNames = lobbyPlayers.Values.ToArray()
        };

        foreach (NetworkConnectionToClient conn in NetworkServer.connections.Values)
        {
            conn.Send(msg);
        }
    }

    private void SendLobbyState(NetworkConnectionToClient conn)
    {
        if (conn == null)
            return;

        var msg = new LobbyStateMessage
        {
            playerNames = lobbyPlayers.Values.ToArray()
        };

        conn.Send(msg);
    }

    // Server-side API for dealing and authoritative actions
    [Server]
    public void ServerDealCards(int seed)
    {
        GameManager.Instance.InitializeGame(false);
        // TODO: make dealing deterministic using seed and synchronize hands via SyncLists or ClientRpcs
    }
}

#else

public class NetworkGameManager : UnityEngine.MonoBehaviour
{
    private void Awake()
    {
        Debug.Log("[NetworkGameManager] Mirror support not enabled. Install Mirror and add 'USE_MIRROR' to Scripting Define Symbols.");
    }
}

#endif
