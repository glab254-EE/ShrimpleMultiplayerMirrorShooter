using Mirror;
using Mirror.Examples.Common.Controllers.Player;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerShootOrigin : NetworkBehaviour
{
    [SerializeField]
    private InputActionReference ShootKeybind;
    [SerializeField]
    private Vector3 SpawnDirectionRelative;
    [SerializeField]
    private Transform Point;
    [SerializeField]
    private GameObject prefab;
    [SerializeField]
    private PlayerHealthHandler healthHandler;
    private void Start()
    {
        ShootKeybind.action.performed += OnShootPress;
        ShootKeybind.action.Enable();
    }
    private void OnDestroy()
    {
        ShootKeybind.action.performed -= OnShootPress;
        ShootKeybind.action.Disable();
    }
    [Server]
    void SpawnBullet()
    {
        if (prefab == null || healthHandler == null)
        {
            Debug.LogWarning("No controller or no prefab.");
            return;
        }
        if (healthHandler.CurrentHealth <= 0)
        {
            return;
        }
        GameObject bulletGO = Instantiate(prefab, Point.position,Quaternion.identity);
        bulletGO.transform.forward = (Point.forward * SpawnDirectionRelative.z + Point.up * SpawnDirectionRelative.y+ Point.right * SpawnDirectionRelative.x).normalized;
        NetworkServer.Spawn(bulletGO);
        if (bulletGO.TryGetComponent(out ProjectileHandler handler))
        {
            handler.Init(healthHandler.netId,healthHandler.Team);
        }
    }
    [Command]
    void SpawnBulletCommand()
    {
        SpawnBullet();
    }
    void OnShootPress(InputAction.CallbackContext _)
    {
        if (isOwned)
        {
            if (isServer)
            {
                SpawnBullet();
            }
            else
            {
                SpawnBulletCommand();
            }
        }
    }
}
