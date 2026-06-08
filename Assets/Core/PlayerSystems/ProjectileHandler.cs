using Mirror;
using Mirror.Examples.Common.Controllers.Player;
using UnityEngine;

[RequireComponent(typeof(NetworkRigidbodyReliable))]
public class ProjectileHandler : NetworkBehaviour
{
    [SerializeField]
    private float Speed;
    [SerializeField]
    private float DeathTime;
    uint owner;
    int team;
    bool Initialized;
    private NetworkRigidbodyReliable rb;

    void Start()
    {
        rb = GetComponent<NetworkRigidbodyReliable>();
    }
    [Server]
    public void Init(uint _owner,int _team)
    {
        owner = _owner;
        team = _team;
        Initialized = true;
    }
    void FixedUpdate()
    {
        if (Initialized && isServer)
        {
            rb.ServerTeleport(transform.position + transform.forward * Speed * Time.fixedDeltaTime, transform.rotation);
            DeathTime -= Time.fixedDeltaTime;
            if (DeathTime <= 0)
            {
                if (isServer)
                {
                    NetworkServer.Destroy(gameObject);
                }
            }    
        }
    }
    void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out PlayerControllerRBReliable controllerBase))
        {
            if (controllerBase.netId == owner)
            {
                return;
            }
        }
        if (other.TryGetComponent(out PlayerHealthHandler playerHealthHandler) && Initialized)
        {
            if (playerHealthHandler.Team != -1 && team != playerHealthHandler.Team)
            {
                playerHealthHandler.DamagePlayer();
                Initialized = false;
            }
        }
        if (isServer)
        {
            NetworkServer.Destroy(gameObject);
        }
    }
}
