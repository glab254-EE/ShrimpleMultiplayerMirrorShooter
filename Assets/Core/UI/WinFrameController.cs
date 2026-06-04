using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class WinFrameController : MonoBehaviour
{
    [SerializeField]
    private GameObject Frame;
    [SerializeField]
    private TMP_Text Text;
    [SerializeField]
    private GameManager manager;
    [SerializeField]
    private float DisplayDuration = 1;
    void Start()
    {
        if (manager != null)
        {
            if (Frame != null && Text != null)
            {
                manager.OnRoundWinEvent.AddListener(OnWin);
                manager.OnWinEvent.AddListener(OnWin);
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
