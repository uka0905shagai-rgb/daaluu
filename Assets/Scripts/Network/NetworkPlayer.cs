using UnityEngine;

#if USE_MIRROR
using Mirror;

public class NetworkPlayer : NetworkBehaviour
{
    [SyncVar] public int netPlayerID = -1;
    [SyncVar] public string netPlayerName = "Player";

    public Player localPlayer;

    public override void OnStartClient()
    {
        base.OnStartClient();
        // Try to link to an existing Player component on the same GameObject
        localPlayer = GetComponent<Player>();
        if (localPlayer != null)
        {
            localPlayer.playerID = netPlayerID;
            localPlayer.playerName = netPlayerName;
        }
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        if (connectionToClient != null)
        {
            netPlayerID = connectionToClient.connectionId;
        }

        if (string.IsNullOrWhiteSpace(netPlayerName))
        {
            netPlayerName = $"Player_{netPlayerID}";
        }

        Player serverPlayer = GetComponent<Player>();
        if (serverPlayer != null)
        {
            serverPlayer.Initialize(netPlayerID, true, netPlayerName);
        }

        NetworkGameManager.Instance?.AddOrUpdatePlayer(this);
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        NetworkGameManager.Instance?.RemovePlayer(this);
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        // When this is the local player, send our preferred name to the server
        string saved = UnityEngine.PlayerPrefs.GetString("PlayerName", "");
        if (!string.IsNullOrWhiteSpace(saved))
        {
            CmdSetName(saved);
        }
    }

    [Command]
    public void CmdSetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return;
        netPlayerName = name.Trim();

        // If a Player component exists server-side, update it as well
        Player serverPlayer = GetComponent<Player>();
        if (serverPlayer != null)
        {
            serverPlayer.playerName = netPlayerName;
            serverPlayer.playerID = netPlayerID >= 0 ? netPlayerID : serverPlayer.playerID;
        }

        NetworkGameManager.Instance?.AddOrUpdatePlayer(this);
    }

    [Command]
    public void CmdRequestPlayCard(int cardInstanceId)
    {
        if (!isServer) return;
        // Server-side validation should be done here. For now, forward the request to GameManager by id.
        Player serverPlayer = GameManager.Instance.GetPlayerByID(netPlayerID);
        if (serverPlayer == null) return;

        bool accepted = GameManager.Instance.ServerRequestPlayCard(netPlayerID, cardInstanceId);
        if (accepted)
        {
            // Optionally notify clients via Rpc or SyncLists (to implement)
        }
    }
}

#else

public class NetworkPlayer : UnityEngine.MonoBehaviour
{
    private void Awake()
    {
        Debug.Log("[NetworkPlayer] Mirror support not enabled. Install Mirror and add 'USE_MIRROR' to Scripting Define Symbols.");
    }
}

#endif
