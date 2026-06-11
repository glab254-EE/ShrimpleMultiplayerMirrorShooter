using System;
using Mirror;
using UnityEngine;
using UnityEngine.Events;

public class PlayerHealthHandler : NetworkBehaviour
{
    public static PlayerHealthHandler LocalPlayer;
    public UnityEvent OnHealthChangedEvent;
    [field:SerializeField]
    public int Team { get; private set; } = -1;
    public int CurrentHealth { get; private set; } = 0;
    [field:SerializeField]
    public int MaxHealth {get;private set;}
    public event Action<GameObject> OnDeath;
    public event Action<int> OnDamaged;
    [SerializeField]
    private GameObject UIPrefab;
    [SyncVar(hook = nameof(SyncHealth))]
    private int _syncHealth = 0;
    private Color localPlayerColor;
    [SyncVar(hook = nameof(SyncColorChange))]
    private Color playerColor;
    private MeshRenderer Renderer;
    GameObject UIobject;
    void Awake()
    {
        TryGetComponent(out Renderer);
        CurrentHealth = MaxHealth;
        _syncHealth = CurrentHealth;
    }
    void SyncColorChange(Color _, Color newColor)
    {
        localPlayerColor = newColor;
        Renderer.material.color = localPlayerColor;
    }
    void SyncHealth(int _, int newV)
    {
        CurrentHealth = newV;
    }
    [TargetRpc]
    void ReplicateToPlayerNewValue(NetworkConnection target, int value)
    {
        OnDamaged?.Invoke(value);
        if (value <= 0)
        {
            OnDeath?.Invoke(gameObject);
        }
    }
    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        TryGetComponent(out Renderer);
        CurrentHealth = MaxHealth;
        _syncHealth = CurrentHealth;
        if (isOwned) LocalPlayer = this;
        GameObject foundUI = GameObject.FindWithTag("MainCanvas");
        if (foundUI != null && UIobject == null && UIPrefab != null)
        {
            UIobject = Instantiate(UIPrefab, foundUI.transform);
        }
        LocalChangeColor();
    }
    public override void OnStopLocalPlayer()
    {
        base.OnStopLocalPlayer();
        if (UIobject != null)
        {
            Destroy(UIobject);
        }
        OnDeath = null;
        OnDamaged = null;
    }
    private void LocalChangeColor()
    {
        if (localPlayerColor != null && Renderer != null && Renderer.material.color != localPlayerColor)
        {
            Renderer.material.color = localPlayerColor;
        }
    }
    [TargetRpc]
    private void ReplicateChangeColorLocal(NetworkConnectionToClient target)
    {
        LocalChangeColor();
    }
    [Server]
    public void Init(int team,Color color)
    {
        if (Team == -1 && isServer) 
        { 
            Team = team;
        }
        if (Renderer != null)
        {
            Renderer.material.color = color;
        }
        playerColor = color;
        ReplicateToPlayerNewValue(connectionToClient, CurrentHealth);
        ReplicateChangeColorLocal(connectionToClient);
    }
    [Server]
    public void ChangeHealthValue(int newV)
    {
        _syncHealth = newV;
        CurrentHealth = _syncHealth;
        ReplicateToPlayerNewValue(connectionToClient, CurrentHealth);
        if (newV <= 0)
        {
            NetworkServer.Destroy(gameObject);
        }
    }
    [Command]
    public void DamageCommand()
    {
        ChangeHealthValue(CurrentHealth-1);
    }
    public void DamagePlayer()
    {
        if (isServer)
        {
            ChangeHealthValue(CurrentHealth-1);
        }
        if (playerColor != null && gameObject.TryGetComponent(out MeshRenderer renderer) && renderer.material.color != playerColor)
        {
            renderer.material.color = playerColor;
        }
    }
}
