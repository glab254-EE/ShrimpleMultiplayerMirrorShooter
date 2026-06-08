using System.Collections;
using System.Collections.Generic;
using Mirror;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

public class GameManager : NetworkManager
{
    [field:SerializeField]
    public List<TeamSO> Teams { get; protected set; }
    [SerializeField]
    private List<Material> MaterialsPerTeam;
    public ScoreService scoreService;
    [SerializeField] private float CheckDelay = 1;
    public event System.Action<int> OnRoundChange;
    public int CurrentRound { get; private set; } = 1;
    private List<(TeamSO, GameObject,NetworkConnectionToClient)> SpawnedTeamPlayers = new();
    private List<NetworkConnectionToClient> Connections = new();
    private Dictionary<NetworkConnectionToClient,TeamSO> TeamToConnections = new();
    private Coroutine currentMainCoroutine;
    private Coroutine roundStartCoroutine;
    private bool Starting = false;
    private bool Started = false;
    public override void OnServerConnect(NetworkConnectionToClient conn)
    {
        Connections.Add(conn);
        UpdateTeamCount();
        base.OnServerConnect(conn);
        PlayerHealthHandler[] found = FindObjectsByType<PlayerHealthHandler>();
        if (found != null && found.Length > 0)
        {
            foreach (PlayerHealthHandler player in found)
            {
                if (player.gameObject != null && player.gameObject.activeInHierarchy && player.Team > -1 && Teams.Count > player.Team)
                {
                    SpawnedTeamPlayers.Add((Teams[player.Team], player.gameObject, player.connectionToClient));
                    //bool foundItem = false;
                    //foreach (var item in Connections)
                    //{
                    //    if (item.Item2 != conn || item.Item2 != player.connectionToClient) continue;
                    //    foundItem = true;
                    //}
                    //if (!foundItem)
                    //{
                        TeamToConnections.Add(player.connectionToClient,Teams[player.Team]);
                    
                }
            }
        }
        if (!Started && !Starting)
        {
            roundStartCoroutine??=StartCoroutine(OnRoundStart());
        }
        OnServerAddPlayer(conn);
    }
    public override void Start()
    {
        scoreService = ScoreService.Instance;
        scoreService.SetUp(Teams);
        TeamToConnections = new();
        base.Start();
        UpdateTeamCount();
    }
    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        base.OnServerDisconnect(conn);
        if (Connections.Contains(conn))
        {
            Connections.Remove(conn);
        }
        if (TeamToConnections.ContainsKey(conn))
        {
            TeamToConnections.Remove(conn);
        }
        UpdateTeamCount();
    }
    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        TeamSO team = PickAvailableTeam(conn);

        if (team == null) return;
        bool found = TeamToConnections.ContainsKey(conn);
        if (!found)
        {
            TeamToConnections.Add(conn,team);
        }
        else
        {
            team = TeamToConnections[conn];
        }
        if (Started || !Starting) return;
        Debug.Log("Spawning! " + conn.connectionId + " " + team.TeamName);
        UpdateTeamCount();
        Transform startPos =RespawnService.Instance.CustomGetStartPosition(team.TeamIndex);
        GameObject player = startPos != null
            ? Instantiate(playerPrefab, startPos.position, startPos.rotation)
            : Instantiate(playerPrefab);

        // instantiating a "Player" prefab gives it the name "Player(clone)"
        // => appending the connectionId is WAY more useful for debugging!
        player.name = $"{playerPrefab.name} [connId={conn.connectionId}]";
        NetworkServer.DestroyPlayerForConnection(conn);
        int foundItem = -1;
        for (int i = SpawnedTeamPlayers.Count - 1; i >= 0; i--)
        {
            if (SpawnedTeamPlayers[i].Item2 == null || !SpawnedTeamPlayers[i].Item2.activeInHierarchy)
            {
                SpawnedTeamPlayers.RemoveAt(i);
            }
            if (SpawnedTeamPlayers[i].Item3 == conn)
            {
                if (SpawnedTeamPlayers[i].Item2 != null)
                {
                    Destroy(SpawnedTeamPlayers[i].Item2);
                    NetworkServer.DestroyPlayerForConnection(conn);
                }
                foundItem = i;
                var cloned = SpawnedTeamPlayers[i];
                cloned.Item2 = player;
                SpawnedTeamPlayers[i] = cloned;
            }
        }
        if (foundItem == -1)
        {
            SpawnedTeamPlayers.Add((team, player, conn));
        }
        UpdateTeamCount();
        NetworkServer.AddPlayerForConnection(conn, player);
        if (player.TryGetComponent(out PlayerHealthHandler handler))
        {
            handler.Init(team.TeamIndex,team.TeamColor);
        }
    }
    private TeamSO PickAvailableTeam(NetworkConnectionToClient conn)
    {
        UpdateTeamCount();
        if (conn == null) return null;
        TeamSO output = null;
        int lowestCount = int.MaxValue;
        foreach(var team in Teams)
        {
            if (team.TeamCount < lowestCount)
            {
                output = team;
                lowestCount = team.TeamCount;
            }
        }
        return output;
    }
    public void UpdateTeamCount()
    {
        scoreService.RefreshScore();
        List<NetworkConnectionToClient> filtered = new();
        foreach (var team in Teams)
        {
            team.TeamCount = 0;
        }
        foreach (var item in TeamToConnections)
        {
            if (filtered.Contains(item.Key) || item.Key == null || item.Value == null || !Teams.Contains(item.Value)){ continue; }
            filtered.Add(item.Key);
            item.Value.TeamCount++;
        }
    }
    private IEnumerator OnRoundStart()
    {
        RespawnService.Instance.ResetAvailability();
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach(GameObject player in players)
        {
            Destroy(player);
        }
        for (int i = SpawnedTeamPlayers.Count - 1; i >= 0; i--)
        {
            if (SpawnedTeamPlayers[i].Item2 != null)
            {
                Destroy(SpawnedTeamPlayers[i].Item2);
                NetworkServer.RemovePlayerForConnection(SpawnedTeamPlayers[i].Item3, RemovePlayerOptions.Destroy);
                NetworkServer.DestroyPlayerForConnection(SpawnedTeamPlayers[i].Item3);
            }
            SpawnedTeamPlayers.RemoveAt(i);
        }
        do
        {
            UpdateTeamCount();
            TeamSO FirstTeamChecked = null;
            foreach (var pair in Teams)
            {
                if (pair.TeamCount >= 1)
                {
                    if (FirstTeamChecked == null)
                    {
                        FirstTeamChecked = pair;
                    } else if (FirstTeamChecked != null && FirstTeamChecked != pair)
                    {
                        StartRound();
                        currentMainCoroutine ??= StartCoroutine(PrimaryCoroutine());    
                        yield break;
                    }
                }
            }
            yield return new WaitForSeconds(CheckDelay);
        } while (true);
    }
    private void StartRound()
    {
        Starting = true;
        for (int i = Connections.Count - 1;i >= 0; i--)
        {
            OnServerAddPlayer(Connections[i]);
        }
        Started = true;
    }
    private IEnumerator PrimaryCoroutine()
    {
        while (gameObject.activeInHierarchy)
        {
            yield return new WaitForSeconds(CheckDelay);
            if (!Started) continue;
            Starting = false;
            TeamSO winningTeam = null;
            bool IsGoing = false;
            foreach (var spawned in SpawnedTeamPlayers)
            {
                if (spawned.Item2 != null)
                {
                    if (spawned.Item1.TeamCount >= 1)
                    {
                        if (winningTeam != null)
                        {
                            winningTeam = null;
                            IsGoing = true;
                        }
                        else if (winningTeam == null && !IsGoing)
                        {
                            winningTeam = spawned.Item1;
                        }
                    }
                }
            }
            if (winningTeam != null)
            {
                Started = false;
                if (roundStartCoroutine != null)
                {
                    StopCoroutine(roundStartCoroutine);
                }
                yield return StartCoroutine(scoreService.AddPoints(winningTeam));
                CurrentRound++;
                OnRoundWinReplication();
                roundStartCoroutine = StartCoroutine(OnRoundStart());
                yield return roundStartCoroutine;
            }
        }
    }
    private void OnRoundWinReplication()
    {
        OnRoundChange?.Invoke(CurrentRound);
    }
}
