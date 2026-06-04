using UnityEngine;

[CreateAssetMenu(fileName = "TeamSO", menuName = "Scriptable Objects/TeamSO")]
public class TeamSO : ScriptableObject
{
    [field: SerializeField]
    public int TeamIndex { get; private set; }
    [field:SerializeField]
    public Material playerMaterial { get; set; }

    public int TeamCount;
    [field: SerializeField]
    public string TeamName { get; private set; }
}
