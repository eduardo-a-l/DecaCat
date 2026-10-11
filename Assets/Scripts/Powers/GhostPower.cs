using UnityEngine;

public class GhostPower : PowerBehaviour
{
    private const float TeleportDistance = 4.5f;
    private const float WallMargin = 0.7f;
    private const float MinTeleport = 0.5f;
    private const float AttackLockSeconds = 1.5f;
    private const float DecoySeconds = 3f;

    private bool inObstacle;
    private float attackLockUntil;

    public override float SpecialCooldown => 6f;
    public override bool CanAttack => Time.time >= attackLockUntil;

    public override void OnGained()
    {
        PhysicsLayers.Exclude(Owner.gameObject, PhysicsLayers.ObstacleMask);
    }

    public override void OnLost()
    {
        PhysicsLayers.Restore(Owner.gameObject, PhysicsLayers.ObstacleMask);
    }

    public override void Tick(float step)
    {
        Room room = PowerFx.CurrentRoom;
        bool now = room != null && room.IsObstacleAt(Owner.Position);

        if (inObstacle && !now)
            attackLockUntil = Time.time + AttackLockSeconds;

        inObstacle = now;
    }

    public override void OnAttackBlocked()
    {
        ZoneEffect.Spawn(Owner.Position, 0.5f, new Color(0.7f, 0.7f, 0.8f, 0.6f), 0.25f, true, 0.4f, 6);
    }

    public override bool UseSpecial(Vector2 direction)
    {
        Vector2 origin = Owner.Position;
        float distance = TeleportDistance;

        int mask = PhysicsLayers.BorderMask;

        if (mask != 0)
        {
            RaycastHit2D hit = Physics2D.Raycast(origin, direction, TeleportDistance, mask);

            if (hit.collider != null)
                distance = Mathf.Max(0f, hit.distance - WallMargin);
        }

        if (distance < MinTeleport)
            return false;

        GhostDecoy.Spawn(origin, Definition.PlayerSprite, DecoySeconds);

        Owner.Teleport(origin + direction * distance);

        ZoneEffect.Spawn(origin + direction * distance, 0.9f, new Color(0.85f, 0.85f, 1f, 0.6f), 0.4f, true, 0.2f, 6);

        return true;
    }

    public override string GetStatusText()
    {
        float lockLeft = attackLockUntil - Time.time;

        return lockLeft > 0f ? "Attack locked " + lockLeft.ToString("0.0") + "s" : string.Empty;
    }
}
