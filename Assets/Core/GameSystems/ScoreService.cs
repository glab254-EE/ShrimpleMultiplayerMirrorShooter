using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.Events;

public class ScoreService : NetworkBehaviour
{
    [SerializeField] private float ShutdownAfterWinDelay = 2;
    [SerializeField] private float ScoreAdditionPerWin = 1;
    [SerializeField] private float ScoreRequiredToWin = 12;
    [SerializeField] private string RoundWinTextFormat = "Team {0} won the round!";
    [SerializeField] private string WinTextFormat = "Team {0} won the game!!";
    public List<TeamSO> Teams { get; protected set; } = new();
    public static ScoreService Instance;
    public event Action OnScoreChange;
    public readonly SyncDictionary<int, float> Score = new();
    public UnityEvent<string> OnRoundWinEvent;
    public UnityEvent<string> OnWinEvent;
    public int CurrentRound { get; private set; } = 1;
    [SyncVar(hook = nameof(OnRoundEndChange))]
    private int CurrentRoundServer = 1;
    private bool Initialized = false;
    public override void OnStartServer()
    {
        base.OnStartServer();
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    public override void OnStartClient()
    {
        base.OnStartClient();
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    public void SetUp(List<TeamSO> teams)
    {
        if (!isServer && isClient)
        {
            SetUpServiceCmd(teams);

        } 
        else
        {
            SetUpService(teams);
        } 
    }
    [Command]
    public void SetUpServiceCmd(List<TeamSO> teams)
    {
        SetUpService(teams);
    }
    [Server]
    public void SetUpService(List<TeamSO> teams)
    {
        Initialized = true;
        Teams = new(teams);
    }
    [Server]
    public IEnumerator AddPoints(TeamSO winningTeam)
    {
        if (!isServer) yield break;
        Dictionary<int, float> cloned = new(Score);
        foreach (var scoreData in cloned)
        {
            if (Score.ContainsKey(scoreData.Key) && scoreData.Key == winningTeam.TeamIndex)
            {
                Score[scoreData.Key] += ScoreAdditionPerWin;
                if (Score[scoreData.Key] >= ScoreRequiredToWin)
                {
                    ReplicateWinEvents(true,winningTeam);
                    yield return new WaitForSecondsRealtime(ShutdownAfterWinDelay);
                    NetworkServer.DisconnectAll();
                    yield break;
                }
                else
                {
                    ReplicateWinEvents(false, winningTeam);
                    CurrentRoundServer++;
                    yield return new WaitForSecondsRealtime(ShutdownAfterWinDelay);
                }
            }
        }
    }
    [ClientRpc]
    public void ReplicateWinEvents(bool IsGameWon, TeamSO winningTeam)
    {
        if (IsGameWon)
        {
            OnWinEvent?.Invoke(string.Format(WinTextFormat, winningTeam.name));
        } else
        {
            OnRoundWinEvent?.Invoke(string.Format(RoundWinTextFormat, winningTeam.TeamName));
        }
        OnScoreChange?.Invoke();
    }
    [Server]
    public void RefreshScore()
    {
        if (!isServer || !Initialized) return;
        foreach (var team in Teams)
        {
            team.TeamCount = 0;
            if (!Score.ContainsKey(team.TeamIndex))
            {
                Score.Add(team.TeamIndex, 0);
            }
        }
    }
    private void OnRoundEndChange(int _, int newv)
    {
        CurrentRound = newv;
    }
}
