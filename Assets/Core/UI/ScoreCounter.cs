using System;
using System.Collections.Generic;
using Mirror;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(TMPro.TMP_Text))]
public class ScoreCounter : MonoBehaviour, IInitializable<GameManager>
{
    private GameManager manager;
    private TMPro.TMP_Text text;
    public void Initialize(GameManager paremetre)
    {
        manager = paremetre;
        text = GetComponent<TMPro.TMP_Text>();
        manager.scoreService.Score.OnChange += UpdateText;
    }
    private void UpdateText(SyncIDictionary<string, float>.Operation operation, string sO, float arg3)
    {
        if (text == null) return; 
        string ToShow = "";
        List<TeamSO> teams = manager.Teams;
        Debug.Log(teams.ToCommaSeparatedString());
        var score = manager.scoreService.Score;

        float bestTeamScore = -1;

        foreach (var item in score)
        {
            Debug.Log(item.Key);
            TeamSO foundTeam = teams.Find(a => a.TeamName == item.Key);

            if (foundTeam == null) continue;
            if (bestTeamScore < item.Value)
            {
                ToShow = $"{foundTeam.TeamName} - {item.Value}\n{ToShow}";
                bestTeamScore = item.Value;
            }
            else
            {
                ToShow = $"{ToShow}\n{foundTeam.TeamName} - {item.Value}";
            }
        }
        Debug.Log(ToShow);
        text.text = ToShow;
    }
}
