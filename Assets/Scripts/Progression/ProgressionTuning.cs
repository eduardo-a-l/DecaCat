using System;
using UnityEngine;

[Serializable]
public class ProgressionTuning
{
    [Header("Every floor (slight) and every group of 5 floors (bigger)")]
    [SerializeField] private float healthPerFloor = 0.04f;
    [SerializeField] private float healthPerGroup = 0.15f;
    [SerializeField] private float speedPerFloor = 0.01f;
    [SerializeField] private float speedPerGroup = 0.03f;
    [SerializeField] private float maxSpeedMultiplier = 1.6f;

    [Header("Contact damage +1 every N groups of 5 floors")]
    [SerializeField] private int groupsPerDamageBonus = 3;

    [Header("How often enemies from older groups still show up (1 = as often as new ones)")]
    [SerializeField, Range(0.05f, 1f)] private float olderEnemyWeight = 0.6f;

    public float HealthPerFloor => healthPerFloor;
    public float HealthPerGroup => healthPerGroup;
    public float SpeedPerFloor => speedPerFloor;
    public float SpeedPerGroup => speedPerGroup;
    public float MaxSpeedMultiplier => Mathf.Max(1f, maxSpeedMultiplier);
    public int GroupsPerDamageBonus => Mathf.Max(1, groupsPerDamageBonus);
    public float OlderEnemyWeight => Mathf.Clamp(olderEnemyWeight, 0.05f, 1f);
}
