using System;
using Mirror;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

public class PlayerHealthHandler : NetworkBehaviour
{
    public UnityEvent OnHealthChangedEvent;
    public int Team { get; private set; } = -1;
    public int CurrentHealth { get; private set; } = 0;
    [field:SerializeField]
    public int MaxHealth {get;private set;}
    public event Action<GameObject> OnDeath;
    [SerializeField]
    private GameObject UIPrefab;
    [SyncVar(hook = nameof(SyncHealth))]
    private int _syncHealth = 0;
    private Material Material;
    TMP_Text textField;
    GameObject UIobject;
    void Start()
    {
        CurrentHealth = MaxHealth;
        _syncHealth = CurrentHealth;
        if (isClient && isOwned)
        {
            OnHealthChangedInvoke();
            GameObject foundUI = GameObject.FindWithTag("MainCanvas");
            if (foundUI != null && UIobject == null && UIPrefab != null)
            {
                UIobject = Instantiate(UIPrefab, foundUI.transform);
                UIobject.TryGetComponent(out textField);
            }
        }
    }
    private void OnDestroy()
    {
        Destroy(UIobject);
    }
    void SyncHealth(int _, int newV)
    {
        CurrentHealth = newV;
        OnHealthChangedInvoke();
    }
    [Server]
    public void Init(int team, Material material)
    {
        if (Team == -1 && isServer) 
        { 
            Team = team;
            Material = material;
            if (gameObject.TryGetComponent(out MeshRenderer renderer))
            {
                renderer.material = material;
            }
        }
    }
    [Server]
    public void ChangeHealthValue(int newV)
    {
        _syncHealth = newV;
        CurrentHealth = _syncHealth;
        if (newV <= 0)
        {
            OnDeath?.Invoke(gameObject);
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
        else
        {
            DamageCommand();
            OnHealthChangedInvoke();
        }
        if (Material != null && gameObject.TryGetComponent(out MeshRenderer renderer) && renderer.material != Material)
        {
            renderer.material = Material;
        }
    }
    [Client]
    public void OnHealthChangedInvoke()
    {
        if (textField != null && connectionToClient == this.connectionToClient && connectionToServer == this.connectionToServer && netId == this.netId)
        {
            textField.text = $"{CurrentHealth} / {MaxHealth}";
        }
    }
}
