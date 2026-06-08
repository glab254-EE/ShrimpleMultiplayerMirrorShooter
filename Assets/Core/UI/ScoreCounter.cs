using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

[RequireComponent(typeof(TMPro.TMP_Text))]
public class ScoreCounter : MonoBehaviour
{
    [SerializeField]
    private GameManager manager;
    private TMPro.TMP_Text text;
    public void Start()
    {
        text = GetComponent<TMPro.TMP_Text>();
        manager.scoreService.OnScoreChange += OnUpdate;
        UpdateText();
    }
    private void OnUpdate()
    {
        UpdateText();
    }

    void UpdateText()
    {
        if (text == null) return; 
        string ToShow = "";
        var score = manager.scoreService.Score;
        List<TeamSO> teams = manager.Teams;

        float bestTeamScore = -1;

        foreach (var item in score)
        {
            if (teams.Count > item.Key && item.Key >= 0)
            {
                if (bestTeamScore < item.Value)
                {
                    ToShow = $"{teams[item.Key].TeamName} - {item.Value}\n{ToShow}";
                    bestTeamScore = item.Value;
                }
                else
                {
                    ToShow = $"{ToShow}\n{teams[item.Key].TeamName} - {item.Value}";
                }
            }
        }
        text.text = ToShow;
    }
}
