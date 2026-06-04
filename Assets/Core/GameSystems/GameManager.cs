using System.Collections;
using System.Collections.Generic;
using Mirror;
using Mirror.BouncyCastle.Tls;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

public class GameManager : NetworkManager
{
    [field:SerializeField]
    public List<TeamSO> Teams { get; protected set; }
    [SerializeField]
    private List<Material> MaterialsPerTeam;
    public Dictionary<int, float> Score { get; protected set; } = new();
    public UnityEvent<string> OnRoundWinEvent;
    public UnityEvent<string> OnWinEvent;
    public UnityEvent OnItemUpdateEvent;
    [SerializeField] private float CheckDelay=1;
    [SerializeField] private float ShutdownAfterWinDelay = 2;
    [SerializeField] private float ScoreAdditionPerWin = 1;
    [SerializeField] private float ScoreRequiredToWin = 12;
    [SerializeField] private string RoundWinTextFormat = "Team {0} won the round!";
    [SerializeField] private string WinTextFormat = "Team {0} won the game!!";
    public int CurrentRound { get; private set; } = 1;
    private List<(TeamSO, GameObject,NetworkConnectionToClient)> SpawnedTeamPlayers = new();
    private List<(TeamSO, NetworkConnectionToClient)> Connections = new();
    private Coroutine currentMainCoroutine;
    private Coroutine roundStartCoroutine;
    private bool Starting = false;
    private bool Started = false;
    public override void OnServerConnect(NetworkConnectionToClient conn)
    {
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
                    bool foundItem = false;
                    foreach (var item in Connections)
                    {
                        if (item.Item2 != conn || item.Item2 != player.connectionToClient) continue;
                        foundItem = true;
                    }
                    if (!foundItem)
                    {
                        Connections.Add((Teams[player.Team], player.connectionToClient));
                    }
                }
            }
        }
        if (!Started && !Starting)
        {
            roundStartCoroutine??=StartCoroutine(OnRoundStart());
        }
        OnServerAddPlayer(conn);
        OnItemUpdateEvent?.Invoke();
    }
    public override void Start()
    {
        Connections = new();
        base.Start();
        UpdateTeamCount();
    }
    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        base.OnServerDisconnect(conn);
        UpdateTeamCount();
    }
    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        TeamSO team = PickAvailableTeam();

        int foundExsistingConnection = Connections.FindIndex(a => a.Item2 == conn);
        if (foundExsistingConnection == -1)
        {
            Connections.Add((team, conn));
        }
        if (Started || !Starting) return;
        Debug.Log("Spawning! " + conn.connectionId);
        UpdateTeamCount();
        Transform startPos =RespawnService.Instance.CustomGetStartPosition(team.TeamIndex);
        GameObject player = startPos != null
            ? Instantiate(playerPrefab, startPos.position, startPos.rotation)
            : Instantiate(playerPrefab);

        // instantiating a "Player" prefab gives it the name "Player(clone)"
        // => appending the connectionId is WAY more useful for debugging!
        player.name = $"{playerPrefab.name} [connId={conn.connectionId}]";
        NetworkServer.DestroyPlayerForConnection(conn);
        NetworkServer.AddPlayerForConnection(conn, player);
        int foundItem = -1;
        for (int i = SpawnedTeamPlayers.Count - 1; i > 0; i--)
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
        if (team.playerMaterial == null && team.TeamIndex < MaterialsPerTeam.Count)
        {
            team.playerMaterial = MaterialsPerTeam[team.TeamIndex];
        }
        if (player.TryGetComponent(out MeshRenderer renderer))
        {
            renderer.material = team.playerMaterial;
        }
        if (player.TryGetComponent(out PlayerHealthHandler handler))
        {
            handler.Init(team.TeamIndex, team.playerMaterial);
        }
    }
    private TeamSO PickAvailableTeam()
    {
        UpdateTeamCount();
        TeamSO output = null;
        int lowestCount = 1000;
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
        List<NetworkConnectionToClient> filtered = new();
        foreach (var team in Teams)
        {
            team.TeamCount = 0;
            if (!Score.ContainsKey(team.TeamIndex))
            {
                Score.Add(team.TeamIndex, 0);
            }
            foreach (var item in Connections)
            {
                if (filtered.Contains(item.Item2) || item.Item1 == null || item.Item2 == null || !Teams.Contains(item.Item1) || item.Item1 != team) continue;
                filtered.Add(item.Item2);
                team.TeamCount++;
            }
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
        for (int i = SpawnedTeamPlayers.Count - 1; i > 0; i--)
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
        for (int i = Connections.Count - 1;i> 0; i--)
        {
            OnServerAddPlayer(Connections[i].Item2);
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
                Dictionary<int, float> cloned = new(Score);
                foreach (var scoreData in cloned)
                {
                    if (Score.ContainsKey(scoreData.Key) &&scoreData.Key == winningTeam.TeamIndex)
                    {
                        Score[scoreData.Key] += ScoreAdditionPerWin;
                        OnItemUpdateEvent?.Invoke();
                        if (Score[scoreData.Key] >= ScoreRequiredToWin)
                        {
                            OnWinEvent?.Invoke(string.Format(WinTextFormat, winningTeam.name));
                            yield return new WaitForSecondsRealtime(ShutdownAfterWinDelay);
                            NetworkServer.DisconnectAll();
                            yield break;
                        }
                        else
                        {
                            OnRoundWinEvent?.Invoke(string.Format(RoundWinTextFormat, winningTeam.TeamName));
                            yield return new WaitForSecondsRealtime(ShutdownAfterWinDelay);
                        }
                    }
                }
                if (roundStartCoroutine != null)
                {
                    StopCoroutine(roundStartCoroutine);
                }
                CurrentRound++;
                OnItemUpdateEvent?.Invoke();
                roundStartCoroutine = StartCoroutine(OnRoundStart());
                yield return roundStartCoroutine;
            }
        }
    }
}
