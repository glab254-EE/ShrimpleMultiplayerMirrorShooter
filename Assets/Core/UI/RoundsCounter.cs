using Mirror;
using UnityEngine;

[RequireComponent(typeof(TMPro.TMP_Text))]
public class RoundsCounter : MonoBehaviour, IInitializable<GameManager>
{
    [SerializeField]
    private string Format = "Round {0}";
    private GameManager manager;
    private TMPro.TMP_Text text;
    public void Initialize(GameManager paremetre)
    {
        manager = paremetre;
        text = GetComponent<TMPro.TMP_Text>();
        manager.scoreService.OnWinEvent.AddListener(OnUpdate);
        manager.scoreService.OnRoundWinEvent.AddListener(OnUpdate);
        OnUpdate(null);
    }
    void OnUpdate(string _)
    {
        if (text == null) return;
        text.text = string.Format(Format, manager.scoreService.CurrentRound);
    }
}
