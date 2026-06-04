using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;
using static GameManager;

public class RespawnService : MonoBehaviour
{
    [Serializable]
    public class PlayerRespawnPointPerTeam
    {
        public TeamSO team;
        public Transform spawnPoint;
        public bool Available = true;
    }
    [SerializeField] private List<PlayerRespawnPointPerTeam> respawnPoints;
    public static RespawnService Instance;
    private void Start()
    {
        Instance = this;
    }
    public void ResetAvailability()
    {
        foreach (var item in respawnPoints)
        {
            if (!item.Available)
            {
                item.Available = true;
            }
        }
    }
    public Transform CustomGetStartPosition(int pteam)
    {
        // team ones
        if (respawnPoints != null)
        {
            List<PlayerRespawnPointPerTeam> filteredList = respawnPoints;
            filteredList.RemoveAll(c => c.team.TeamIndex != pteam);
            filteredList.RemoveAll(m => !m.Available);
            if (filteredList != null)
            {
                PlayerRespawnPointPerTeam first = filteredList.FirstOrDefault();
                if (respawnPoints.Contains(first))
                {
                    int indax = respawnPoints.IndexOf(first);
                    if (respawnPoints[indax].Available)
                    {
                        respawnPoints[indax].Available = false;
                        return respawnPoints[indax].spawnPoint;
                    }
                }
            }
        }
        // defaults
        return null;
    }
}
