using UnityEngine;

public class PlayerInputReader : MonoBehaviour
{
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private KeyCode dropKey = KeyCode.Q;
    [SerializeField] private KeyCode restartKey = KeyCode.R;

    private Camera mainCamera;

    public bool InputEnabled { get; set; } = true;

    public Vector2 Move
    {
        get
        {
            if (!InputEnabled)
                return Vector2.zero;

            Vector2 move = new Vector2(
                Input.GetAxisRaw("Horizontal"),
                Input.GetAxisRaw("Vertical")
            );

            return move.normalized;
        }
    }

    public Vector3 AimWorldPosition
    {
        get
        {
            if (mainCamera == null)
                mainCamera = Camera.main;

            if (mainCamera == null)
                return transform.position;

            return mainCamera.ScreenToWorldPoint(
                new Vector3(
                    Input.mousePosition.x,
                    Input.mousePosition.y,
                    -mainCamera.transform.position.z
                )
            );
        }
    }

    public bool AttackPressed => InputEnabled && Input.GetMouseButtonDown(0);
    public bool InteractPressed => InputEnabled && Input.GetKeyDown(interactKey);
    public bool DropPressed => InputEnabled && Input.GetKeyDown(dropKey);
    public bool RestartPressed => Input.GetKeyDown(restartKey);
}
