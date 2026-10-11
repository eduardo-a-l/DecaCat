using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerInputReader))]
public class PlayerMovement : MonoBehaviour
{
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

    private void FixedUpdate()
    {
        PlayerPower power = PlayerPower.Instance;

        if (power != null && power.IsDashing)
            return;

        float multiplier = power != null ? power.SpeedMultiplier : 1f;

        rb.linearVelocity = movement * moveSpeed * multiplier;
    }
}