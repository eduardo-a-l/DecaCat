using UnityEngine;

public static class SpecialWeapons
{
    private const float WandSpread = 35f;
    private const float WandHomingTurn = 3f;

    public static readonly Color PoisonTint = new Color(0.6f, 1f, 0.55f, 1f);
    public static readonly Color SuctionTint = new Color(1f, 0.75f, 0.9f, 1f);

    public static bool TryUse(
        ItemData item, PlayerItem playerItem, PlayerPower power,
        Vector2 origin, Vector2 direction, float angle)
    {
        switch (item.Behaviour)
        {
            case WeaponBehaviour.Potion:
                ThrowPotion(item, playerItem, power, origin, direction, angle);
                return true;

            case WeaponBehaviour.Fan:
                UseFan(item, playerItem, power, origin, direction, angle);
                return true;

            case WeaponBehaviour.Magnet:
                UseMagnet(item, playerItem, power, origin, direction, angle);
                return true;

            case WeaponBehaviour.MagicWand:
                FireWand(item, playerItem, power, origin, direction, angle);
                return true;

            default:
                return false;
        }
    }

    public static bool AfterSwing(
        ItemData item, Vector2 origin, Vector2 direction)
    {
        if (item.Behaviour == WeaponBehaviour.Lollipop)
        {
            float distance = Mathf.Max(0.7f, item.AttackRange + 0.45f);

            StickyPatch.Spawn(
                origin + direction * distance, item.AreaRadius,
                item.EffectSeconds, item.SlowFactor
            );

            return false;
        }

        if (item.Behaviour == WeaponBehaviour.Spatula)
            return ReflectProjectiles(item, origin, direction);

        return false;
    }

    public static void ApplySuction(ItemData item, Enemy enemy)
    {
        if (item.HitDotSeconds <= 0f || item.HitDotDamage <= 0)
            return;

        enemy.ApplyDot("suction", item.HitDotSeconds, item.HitDotDamage, SuctionTint);

        StuckDart.Attach(enemy, item.ProjectileColor, item.HitDotSeconds);
    }

    private static int ScaleDamage(PlayerPower power, int baseDamage)
    {
        return power != null ? power.ModifyDamage(baseDamage) : baseDamage;
    }

    private static AttackContext CreateContext(
        Vector2 origin, Vector2 direction, float angle, int baseDamage, int damage)
    {
        return new AttackContext
        {
            Origin = origin,
            Direction = direction,
            Angle = angle,
            BaseDamage = baseDamage,
            Damage = damage
        };
    }

    private static void ThrowPotion(
        ItemData item, PlayerItem playerItem, PlayerPower power,
        Vector2 origin, Vector2 direction, float angle)
    {
        int damage = ScaleDamage(power, item.Damage);
        AttackContext context = CreateContext(origin, direction, angle, item.Damage, damage);

        ThrownPotion.Throw(
            origin + direction * 0.6f, direction, item, damage,
            enemy =>
            {
                if (power != null)
                    power.OnHit(context, enemy);
            }
        );

        if (power != null)
            power.OnAttackEnded(context, 0);

        playerItem.UseShot();
    }

    private static void UseFan(
        ItemData item, PlayerItem playerItem, PlayerPower power,
        Vector2 origin, Vector2 direction, float angle)
    {
        float halfAngle = item.ConeAngle * 0.5f;
        int affected = 0;

        foreach (Enemy enemy in PowerFx.GetRoomEnemies())
        {
            Vector2 toEnemy = (Vector2)enemy.transform.position - origin;

            if (toEnemy.magnitude > item.ConeRange ||
                Vector2.Angle(direction, toEnemy) > halfAngle)
                continue;

            enemy.Push(origin, item.ForceDistance);
            affected++;
        }

        foreach (EnemyProjectile projectile in
                 Object.FindObjectsByType<EnemyProjectile>(FindObjectsSortMode.None))
        {
            Vector2 toProjectile = (Vector2)projectile.transform.position - origin;

            if (toProjectile.magnitude > item.ConeRange ||
                Vector2.Angle(direction, toProjectile) > halfAngle)
                continue;

            projectile.Redirect(toProjectile.normalized, 1.25f, false);
            affected++;
        }

        for (int i = 1; i <= 3; i++)
        {
            float distance = item.ConeRange * i / 3f;

            ZoneEffect.Spawn(
                origin + direction * distance, 0.35f + 0.25f * i,
                new Color(0.85f, 0.95f, 1f, 0.55f), 0.35f, true, 0.3f, 6
            );
        }

        AttackContext context = CreateContext(origin, direction, angle, 0, 0);

        if (power != null)
            power.OnAttackEnded(context, affected);

        if (affected > 0)
            playerItem.UseItem();
    }

    private static void UseMagnet(
        ItemData item, PlayerItem playerItem, PlayerPower power,
        Vector2 origin, Vector2 direction, float angle)
    {
        int affected = 0;

        foreach (Enemy enemy in PowerFx.GetRoomEnemies())
        {
            Vector2 position = enemy.transform.position;
            float distance = Vector2.Distance(position, origin);

            if (distance > item.AreaRadius || distance <= 1.2f)
                continue;

            float pull = Mathf.Min(distance - 1f, item.ForceDistance);
            Vector2 source = position + (position - origin);

            enemy.Push(source, pull);
            affected++;
        }

        ZoneEffect.Spawn(
            origin, item.AreaRadius, new Color(0.8f, 0.3f, 1f, 0.5f),
            0.4f, true, 0.4f, 6
        );

        ZoneEffect.Spawn(
            origin, item.AreaRadius * 0.5f, new Color(0.8f, 0.3f, 1f, 0.4f),
            0.3f, true, 0.3f, 6
        );

        AttackContext context = CreateContext(origin, direction, angle, 0, 0);

        if (power != null)
            power.OnAttackEnded(context, affected);

        if (affected > 0)
            playerItem.UseItem();
    }

    private static void FireWand(
        ItemData item, PlayerItem playerItem, PlayerPower power,
        Vector2 origin, Vector2 direction, float angle)
    {
        int damage = ScaleDamage(power, item.Damage);
        AttackContext context = CreateContext(origin, direction, angle, item.Damage, damage);
        int count = item.ProjectileCount;

        for (int i = 0; i < count; i++)
        {
            float t = count == 1 ? 0.5f : (float)i / (count - 1);
            float spread = Mathf.Lerp(-WandSpread, WandSpread, t);

            ProjectileSettings shot = new ProjectileSettings
            {
                Direction = PowerFx.Rotate(direction, spread),
                Speed = item.ProjectileSpeed,
                Damage = damage,
                Radius = Mathf.Max(item.ProjectileSize.x, item.ProjectileSize.y) * 0.5f,
                Size = item.ProjectileSize,
                Lifetime = item.ProjectileLifetime,
                Color = item.ProjectileColor,
                Sprite = item.ProjectileSprite,
                Homing = true,
                HomingTurn = WandHomingTurn,
                OnEnemyHit = enemy =>
                {
                    if (power != null)
                        power.OnHit(context, enemy);
                }
            };

            PowerProjectile.Spawn(origin + direction * 0.6f, shot);
        }

        if (power != null)
            power.OnAttackEnded(context, 0);

        playerItem.UseShot();
    }

    private static bool ReflectProjectiles(
        ItemData item, Vector2 origin, Vector2 direction)
    {
        float halfAngle = item.ConeAngle * 0.5f;
        bool reflected = false;

        foreach (EnemyProjectile projectile in
                 Object.FindObjectsByType<EnemyProjectile>(FindObjectsSortMode.None))
        {
            Vector2 toProjectile = (Vector2)projectile.transform.position - origin;

            if (toProjectile.magnitude > item.ConeRange ||
                Vector2.Angle(direction, toProjectile) > halfAngle)
                continue;

            projectile.Redirect(direction, 1.3f, true);
            reflected = true;
        }

        if (reflected)
        {
            ZoneEffect.Spawn(
                origin + direction * 0.9f, 0.8f,
                new Color(1f, 1f, 0.6f, 0.7f), 0.3f, true, 0.3f, 6
            );
        }

        return reflected;
    }
}
