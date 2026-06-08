using Mirror;
using UnityEngine;

[RequireComponent(typeof(TMPro.TMP_Text))]
public class RoundsCounter : NetworkBehaviour
{
    [SerializeField]
    private string Format = "Round {0}";
    [SerializeField]
    private GameManager manager;
    private TMPro.TMP_Text text;
    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        text = GetComponent<TMPro.TMP_Text>();
        manager.OnRoundChange += OnUpdate;
    }
    void OnUpdate(int number)
    {
        if (text == null) return;
        text.text = string.Format(Format, number);
    }
}
