using Mirror;
using UnityEngine;

public class UIGivingManager : MonoBehaviour
{
    [SerializeField]
    private GameManager manager;
    [SerializeField]
    private GameObject UserInterfacePrefab;
    private GameObject clientUI;
    private void Start()
    {
        clientUI = Instantiate(UserInterfacePrefab, null);
        foreach (Transform child in clientUI.transform)
        {
            if (child.TryGetComponent(out IInitializable initializable1))
            {
                initializable1.Initialize();
            }
            if (child.TryGetComponent(out IInitializable<GameManager> initializable2))
            {
                initializable2.Initialize(manager);
            }
        }
    }
    private void OnDestroy()
    {
        if (clientUI != null)
        {
            Destroy(clientUI);
        }
    }
}
