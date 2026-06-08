using System.Collections;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class WinFrameController : MonoBehaviour, IInitializable<GameManager>
{
    [SerializeField]
    private GameObject Frame;
    [SerializeField]
    private TMP_Text Text;
    [SerializeField]
    private float DisplayDuration = 1;
    private GameManager manager;
    public void Initialize(GameManager _manager)
    {
        if (manager == null)
        {
            manager = _manager;
            if (Frame != null && Text != null)
            {
                manager.scoreService.OnRoundWinEvent.AddListener(OnWin);
                manager.scoreService.OnWinEvent.AddListener(OnWin);
            }
        }
    }
    void OnWin(string text)
    {
        StopAllCoroutines();
        StartCoroutine(DisplayEnumerator(text));
    }
    IEnumerator DisplayEnumerator(string text)
    {
        Frame.SetActive(true);
        Text.text = text;
        yield return new WaitForSeconds(DisplayDuration);
        Frame.SetActive(false);
    }
}
