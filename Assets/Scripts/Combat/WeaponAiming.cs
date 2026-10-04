using UnityEngine;

public class WeaponAiming : MonoBehaviour
{
    [SerializeField] private Transform weaponPivot;
    [SerializeField] private Transform weaponVisual;

    private PlayerInputReader inputReader;
    private Vector3 originalScale;

    public float AimAngle { get; private set; }
    public bool IsSwinging { get; private set; }

    private void Awake()
    {
        inputReader = GetComponent<PlayerInputReader>();

        if (weaponVisual != null)
            originalScale = weaponVisual.localScale;
    }

    private void Update()
    {
        if (inputReader == null || weaponPivot == null)
            return;

        Vector3 aimPosition = inputReader.AimWorldPosition;

        Vector2 direction =
            (Vector2)(aimPosition - weaponPivot.position);

        if (direction.sqrMagnitude < 0.001f)
            return;

        AimAngle = Mathf.Atan2(
            direction.y, direction.x
        ) * Mathf.Rad2Deg;

        if (weaponVisual != null)
        {
            Vector3 scale = originalScale;
            scale.y *= direction.x < 0 ? -1 : 1;
            weaponVisual.localScale = scale;
        }

        if (!IsSwinging)
        {
            weaponPivot.rotation =
                Quaternion.Euler(0f, 0f, AimAngle);
        }
    }

    public void BeginSwing()
    {
        IsSwinging = true;
    }

    public void EndSwing()
    {
        IsSwinging = false;
    }
}