using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerInputReader))]
public class PlayerMovement : MonoBehaviour
{
    private const float KnockbackSeconds = 0.18f;

    private Vector2 knockbackVelocity;
    private float knockbackTimer;

    [SerializeField] private float moveSpeed = 5f;

    private Rigidbody2D rb;
    private PlayerInputReader inputReader;
    private Vector2 movement;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        inputReader = GetComponent<PlayerInputReader>();
    }

    private void Update()
    {
        movement = inputReader != null ? inputReader.Move : Vector2.zero;
    }

    public void Knockback(Vector2 source, float distance)
    {
        Vector2 direction = (Vector2)transform.position - source;

        if (direction.sqrMagnitude < 0.0001f)
            direction = Vector2.right;

        knockbackVelocity = direction.normalized * (distance / KnockbackSeconds);
        knockbackTimer = KnockbackSeconds;
    }

    private void FixedUpdate()
    {
        if (knockbackTimer > 0f)
        {
            knockbackTimer -= Time.fixedDeltaTime;
            rb.linearVelocity = knockbackVelocity;
            return;
        }

        PlayerPower power = PlayerPower.Instance;

        if (power != null && power.IsDashing)
            return;

        float multiplier = power != null ? power.SpeedMultiplier : 1f;

        rb.linearVelocity = movement * moveSpeed * multiplier;
    }
}