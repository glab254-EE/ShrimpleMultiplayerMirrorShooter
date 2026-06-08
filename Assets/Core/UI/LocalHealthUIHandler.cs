using System.Linq;
using Mirror;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
[DisallowMultipleComponent]
public class LocalHealthUIHandler : NetworkBehaviour
{
    [SerializeField]
    private string Format = "{0} / {1}";
    private PlayerHealthHandler player;
    private TMP_Text text;
    public override void OnStartLocalPlayer()
    {
        player = PlayerHealthHandler.LocalPlayer;
        if (player == null)
        {
            var foundPlayers = FindObjectsByType<PlayerHealthHandler>();
            var found = foundPlayers.Where(a => a.isOwned).FirstOrDefault();
            player = found;
        }
        Debug.Log(player.name);
        text = GetComponent<TMP_Text>();
        player.OnDamaged += OnUpdate;
        OnUpdate(player.MaxHealth);
    }
    private void OnUpdate(int newHealth)
    {
        text.text = string.Format(Format, newHealth, player.MaxHealth);
        Debug.Log(text.text);
    }
}
