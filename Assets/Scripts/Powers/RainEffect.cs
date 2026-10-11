using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RainEffect : MonoBehaviour
{
    private const float SlowFactor = 0.7f;
    private const float SlowInterval = 0.25f;
    private const float StunSeconds = 2f;

    private static RainEffect current;

    private readonly List<RectTransform> drops = new List<RectTransform>();

    private RectTransform area;
    private Image flash;
    private float remaining;
    private float nextSlow;
    private float nextStrike;
    private float flashAlpha;
    private bool thunderstorm;

    public static bool IsActive => current != null;
    public static float Remaining => current != null ? current.remaining : 0f;

    public static void Begin(float seconds, bool thunderstorm)
    {
        if (current != null)
            return;

        GameObject root = UIFactory.CreateOverlayCanvas("Rain", 6);

        RainEffect effect = root.AddComponent<RainEffect>();
        effect.remaining = seconds;
        effect.thunderstorm = thunderstorm;
        effect.nextStrike = Time.time + 1.5f;
        effect.Build(root.transform);

        current = effect;
    }

    private void OnDestroy()
    {
        if (current == this)
            current = null;
    }

    private void Build(Transform root)
    {
        area = (RectTransform)root;

        Image tint = UIFactory.CreatePanel(root, "Tint", new Color(0.35f, 0.5f, 0.75f, 0.12f)).GetComponent<Image>();
        tint.raycastTarget = false;

        flash = UIFactory.CreatePanel(root, "Flash", new Color(1f, 1f, 0.85f, 0f)).GetComponent<Image>();
        flash.raycastTarget = false;

        for (int i = 0; i < 90; i++)
        {
            Image drop = UIFactory.CreateImage(
                root, "Drop", Vector2.zero, new Vector2(3f, Random.Range(28f, 52f)),
                new Color(0.75f, 0.88f, 1f, Random.Range(0.25f, 0.55f))
            );

            RectTransform rect = drop.rectTransform;
            rect.localRotation = Quaternion.Euler(0f, 0f, 12f);
            rect.anchoredPosition = new Vector2(
                Random.Range(-1200f, 1200f), Random.Range(-700f, 700f)
            );

            drops.Add(rect);
        }
    }

    private void Update()
    {
        float step = Time.deltaTime;

        remaining -= step;

        if (remaining <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        MoveDrops(step);

        if (Time.time >= nextSlow)
        {
            nextSlow = Time.time + SlowInterval;

            foreach (Enemy enemy in PowerFx.GetRoomEnemies())
                enemy.ApplySlow(SlowFactor, 0.5f);
        }

        if (thunderstorm && Time.time >= nextStrike)
        {
            nextStrike = Time.time + Random.Range(2.2f, 3.6f);
            Strike();
        }

        flashAlpha = Mathf.MoveTowards(flashAlpha, 0f, step * 2.5f);

        Color color = flash.color;
        color.a = flashAlpha;
        flash.color = color;
    }

    private void MoveDrops(float step)
    {
        Vector2 half = area.rect.size * 0.5f + new Vector2(80f, 80f);

        foreach (RectTransform drop in drops)
        {
            Vector2 position = drop.anchoredPosition;

            position += new Vector2(-190f, -1500f) * step;

            if (position.y < -half.y)
            {
                position.y = half.y;
                position.x = Random.Range(-half.x, half.x);
            }

            drop.anchoredPosition = position;
        }
    }

    private void Strike()
    {
        List<Enemy> enemies = PowerFx.GetRoomEnemies();

        flashAlpha = 0.45f;

        if (enemies.Count == 0)
            return;

        Enemy target = enemies[Random.Range(0, enemies.Count)];

        target.Stun(StunSeconds);

        ZoneEffect.Spawn(target.transform.position, 0.9f, new Color(1f, 1f, 0.5f, 0.9f), 0.35f, true, 0.2f, 6);
    }
}
