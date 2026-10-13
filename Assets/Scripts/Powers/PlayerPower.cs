using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerPower : MonoBehaviour
{
    public const int MaxCharges = 10;

    private readonly List<PowerAltar> nearbyAltars = new List<PowerAltar>();

    private PlayerHealth health;
    private PlayerInputReader input;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D body;
    private Collider2D[] playerColliders;
    private PowerBehaviour behaviour;
    private Sprite originalSprite;
    private Coroutine dashRoutine;
    private Vector2 lastPosition;
    private Vector2 lastMoveDirection = Vector2.right;
    private float specialReadyAt;
    private float damageCarry;
    private bool restored;

    public static PlayerPower Instance { get; private set; }

    public bool IsDashing { get; private set; }
    public bool IsMoving { get; private set; }
    public float MovedThisFrame { get; private set; }
    public float SpecialCooldownTotal { get; private set; }
    public int Charges { get; private set; }

    public bool HasPower => behaviour != null;
    public PowerDefinition Current => behaviour != null ? behaviour.Definition : null;
    public PlayerHealth Health => health;
    public Rigidbody2D Body => body;
    public SpriteRenderer Renderer => spriteRenderer;
    public Collider2D[] PlayerColliders => playerColliders;
    public Vector2 Position => body != null ? body.position : (Vector2)transform.position;

    public float SpecialCooldownRemaining =>
        Mathf.Max(0f, specialReadyAt - Time.time);

    public bool SpecialBlocked =>
        behaviour != null && behaviour.IsSpecialBlocked;

    public bool SpecialReady =>
        behaviour != null && SpecialCooldownRemaining <= 0f && !SpecialBlocked;

    public float SpeedMultiplier =>
        behaviour != null ? behaviour.SpeedMultiplier : 1f;

    public float CooldownMultiplier =>
        behaviour != null ? behaviour.CooldownMultiplier : 1f;

    public bool CanAttack => behaviour == null || behaviour.CanAttack;

    public string StatusText =>
        behaviour != null ? behaviour.GetStatusText() : string.Empty;

    public Vector2 AimDirection
    {
        get
        {
            if (input == null)
                return lastMoveDirection;

            Vector2 aim = (Vector2)input.AimWorldPosition - Position;

            return aim.sqrMagnitude < 0.001f ? lastMoveDirection : aim.normalized;
        }
    }

    public Vector2 MoveOrAimDirection
    {
        get
        {
            if (input != null && input.Move.sqrMagnitude > 0.01f)
                return input.Move.normalized;

            return AimDirection;
        }
    }

    private void Awake()
    {
        Instance = this;

        health = GetComponent<PlayerHealth>();
        input = GetComponent<PlayerInputReader>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        body = GetComponent<Rigidbody2D>();
        playerColliders = GetComponentsInChildren<Collider2D>();

        if (spriteRenderer != null)
            originalSprite = spriteRenderer.sprite;

        if (health != null)
            health.Damaged += OnPlayerDamaged;
    }

    private void Start()
    {
        lastPosition = Position;

        RestoreFromSave();

        PowerHud.Create(this);
    }

    private void OnDestroy()
    {
        if (health != null)
            health.Damaged -= OnPlayerDamaged;

        Enemy.GlobalSpeedMultiplier = 1f;

        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        UpdateMovementTracking();

        if (behaviour != null)
        {
            behaviour.Tick(Time.deltaTime);

            if (input != null && input.SpecialPressed)
                TryUseSpecial();
        }
    }

    public void SaveTo(SaveData data)
    {
        if (!restored)
            return;

        data.loadoutPower = behaviour != null
            ? behaviour.Definition.Type.ToString()
            : string.Empty;

        data.loadoutPowerCharges = Charges;
    }

    public void Acquire(PowerType type, int charges = MaxCharges)
    {
        PowerCatalog catalog = PowerCatalog.Instance;
        PowerDefinition definition = catalog != null ? catalog.Get(type) : null;

        if (definition == null)
        {
            Debug.LogWarning("Power " + type + " is missing from the Powers catalog");
            return;
        }

        if (behaviour != null)
            Lose();

        behaviour = CreateBehaviour(type);
        behaviour.Attach(this, definition);

        Charges = Mathf.Clamp(charges, 1, MaxCharges);
        specialReadyAt = 0f;
        SpecialCooldownTotal = 0f;
        damageCarry = 0f;

        if (spriteRenderer != null && definition.PlayerSprite != null)
            spriteRenderer.sprite = definition.PlayerSprite;

        behaviour.OnGained();

        Discoveries.Add(Discoveries.PowerKind, type.ToString(), definition.DisplayName);

        ZoneEffect.Spawn(Position, 1.4f, definition.Accent, 0.5f, true, 0.2f, 6);
    }

    public void Lose()
    {
        if (behaviour == null)
            return;

        behaviour.OnLost();
        behaviour = null;
        Charges = 0;

        if (dashRoutine != null)
        {
            StopCoroutine(dashRoutine);
            dashRoutine = null;
            IsDashing = false;
        }

        if (spriteRenderer != null && originalSprite != null)
            spriteRenderer.sprite = originalSprite;

        Enemy.GlobalSpeedMultiplier = 1f;
    }

    public int ModifyDamage(int baseDamage)
    {
        if (baseDamage <= 0)
            return baseDamage;

        float multiplier = PermanentUpgrades.DamageMultiplier;

        if (behaviour != null)
            multiplier *= behaviour.DamageMultiplier;

        if (behaviour == null && PermanentUpgrades.DamageStacks == 0)
            return baseDamage;

        float total = baseDamage * multiplier + damageCarry;
        int result = Mathf.FloorToInt(total);

        damageCarry = total - result;

        return Mathf.Max(1, result);
    }

    public void OnAttackBlocked()
    {
        if (behaviour != null)
            behaviour.OnAttackBlocked();
    }

    public void OnHit(AttackContext attack, IDamageable target)
    {
        if (behaviour != null)
            behaviour.OnHit(attack, target);
    }

    public void OnAttackEnded(AttackContext attack, int hits)
    {
        if (behaviour != null)
            behaviour.OnAttackEnded(attack, hits);
    }

    public bool TryAbsorbDamage(DamageElement element)
    {
        return behaviour != null && behaviour.TryAbsorbDamage(element);
    }

    public void OnEnemyKilled(Enemy enemy, bool wasBurning, Vector3 position)
    {
        if (behaviour != null)
            behaviour.OnEnemyKilled(enemy, wasBurning, position);
    }

    public int ModifyDroppedDurability(int durability)
    {
        return behaviour != null
            ? behaviour.ModifyDroppedDurability(durability)
            : durability;
    }

    public void Teleport(Vector2 position)
    {
        if (body != null)
        {
            body.position = position;
            body.linearVelocity = Vector2.zero;
        }

        transform.position = new Vector3(position.x, position.y, transform.position.z);
    }

    public void StartDash(
        Vector2 direction, float distance, float duration,
        bool throughObstacles, Action<Vector2> onStep)
    {
        if (dashRoutine != null)
            StopCoroutine(dashRoutine);

        dashRoutine = StartCoroutine(
            DashRoutine(direction.normalized, distance, duration, throughObstacles, onStep)
        );
    }

    public void EnterAltar(PowerAltar altar)
    {
        if (altar != null && !nearbyAltars.Contains(altar))
            nearbyAltars.Add(altar);
    }

    public void ExitAltar(PowerAltar altar)
    {
        nearbyAltars.Remove(altar);

        if (altar != null)
            altar.SetPromptVisible(false, string.Empty);

        PowerHud.Hide(altar);
    }

    public PowerAltar GetClosestAltar()
    {
        PowerAltar closest = null;
        float best = float.MaxValue;

        foreach (PowerAltar altar in nearbyAltars)
        {
            if (altar == null || !altar.isActiveAndEnabled || altar.Taken)
                continue;

            float distance = ((Vector2)altar.transform.position - Position).sqrMagnitude;

            if (distance < best)
            {
                best = distance;
                closest = altar;
            }
        }

        return closest;
    }

    public void UpdateAltarPrompts(string keyLabel)
    {
        PowerAltar closest = GetClosestAltar();

        foreach (PowerAltar altar in nearbyAltars)
        {
            if (altar != null)
                altar.SetPromptVisible(altar == closest, keyLabel);
        }

        PowerHud.ShowAltar(closest, behaviour != null);
    }

    public void TakeAltar(PowerAltar altar)
    {
        if (altar == null || altar.Taken)
            return;

        if (altar.IsUpgrade)
        {
            PermanentUpgradeType upgrade = altar.Upgrade;

            nearbyAltars.Remove(altar);
            altar.Take();

            PowerHud.Hide(altar);

            PermanentUpgrades.Apply(upgrade);

            ZoneEffect.Spawn(
                Position, 1.4f, PermanentUpgrades.GetAccent(upgrade),
                0.5f, true, 0.2f, 6
            );

            return;
        }

        PowerType type = altar.PowerType;

        nearbyAltars.Remove(altar);
        altar.Take();

        PowerHud.Hide(altar);

        Acquire(type);
    }

    private void TryUseSpecial()
    {
        if (IsDashing || !SpecialReady)
            return;

        if (!behaviour.UseSpecial(MoveOrAimDirection))
            return;

        Charges--;

        SpecialCooldownTotal = behaviour.SpecialCooldown;
        specialReadyAt = Time.time + SpecialCooldownTotal;

        if (Charges <= 0)
            Lose();
    }

    private void RestoreFromSave()
    {
        restored = true;

        if (!SaveSession.IsActive || behaviour != null)
            return;

        SaveData data = SaveSession.Data;

        if (string.IsNullOrEmpty(data.loadoutPower))
            return;

        if (Enum.TryParse(data.loadoutPower, out PowerType type) && data.loadoutPowerCharges > 0)
            Acquire(type, data.loadoutPowerCharges);
    }

    private void UpdateMovementTracking()
    {
        Vector2 position = Position;

        MovedThisFrame = Vector2.Distance(position, lastPosition);
        lastPosition = position;

        Vector2 move = input != null ? input.Move : Vector2.zero;

        IsMoving = move.sqrMagnitude > 0.01f;

        if (IsMoving)
            lastMoveDirection = move.normalized;
    }

    private void OnPlayerDamaged()
    {
        if (behaviour != null)
            behaviour.OnDamaged();
    }

    private IEnumerator DashRoutine(
        Vector2 direction, float distance, float duration,
        bool throughObstacles, Action<Vector2> onStep)
    {
        IsDashing = true;

        int mask = throughObstacles ? PhysicsLayers.ObstacleMask : 0;

        PhysicsLayers.Exclude(gameObject, mask);

        float speed = distance / Mathf.Max(0.05f, duration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            body.linearVelocity = direction * speed;

            if (onStep != null)
                onStep(body.position);

            elapsed += Time.fixedDeltaTime;

            yield return new WaitForFixedUpdate();
        }

        body.linearVelocity = Vector2.zero;

        if (behaviour == null || !(behaviour is GhostPower))
            PhysicsLayers.Restore(gameObject, mask);

        IsDashing = false;
        dashRoutine = null;
    }

    private static PowerBehaviour CreateBehaviour(PowerType type)
    {
        switch (type)
        {
            case PowerType.Ice: return new IcePower();
            case PowerType.Fire: return new FirePower();
            case PowerType.Cloud: return new CloudPower();
            case PowerType.Leaf: return new LeafPower();
            case PowerType.Ghost: return new GhostPower();
            case PowerType.Water: return new WaterPower();
            case PowerType.Electric: return new ElectricPower();
            case PowerType.Bunny: return new BunnyPower();
            default: return new CrystalPower();
        }
    }
}
