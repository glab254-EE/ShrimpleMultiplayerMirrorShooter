using Mirror;
using UnityEngine;

[RequireComponent(typeof(TMPro.TMP_Text))]
public class RoundsCounter : MonoBehaviour
{
    [SerializeField]
    private string Format = "Round {0}";
    [SerializeField]
    private GameManager manager;
    private TMPro.TMP_Text text;
    public void Start()
    {
        text = GetComponent<TMPro.TMP_Text>();
        manager.scoreService.OnWinEvent.AddListener(OnUpdate);
        manager.scoreService.OnWinEvent.AddListener(OnUpdate);
        manager.scoreService.OnScoreChange += ()=> OnUpdate(null);
    }
    void OnUpdate(string _)
    {
        if (text == null) return;
        text.text = string.Format(Format, manager.scoreService.CurrentRound);
    }
}
