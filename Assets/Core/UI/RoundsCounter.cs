using UnityEngine;

[RequireComponent(typeof(TMPro.TMP_Text))]
public class RoundsCounter : MonoBehaviour
{
    [SerializeField]
    private string Format = "Round {0}";
    [SerializeField]
    private GameManager manager;
    private TMPro.TMP_Text text;
    void Start()
    {
        text = GetComponent<TMPro.TMP_Text>();
        manager.OnItemUpdateEvent.AddListener(OnUpdate);
    }
    void OnUpdate()
    {
        text.text = string.Format(Format,manager.CurrentRound);
    }
}
