using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PlayerInputReader : MonoBehaviour
{
    private static readonly List<RaycastResult> RaycastResults =
        new List<RaycastResult>();

    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private KeyCode dropKey = KeyCode.Q;
    [SerializeField] private KeyCode restartKey = KeyCode.R;
    [SerializeField] private KeyCode pauseKey = KeyCode.Escape;
    [SerializeField] private KeyCode mapKey = KeyCode.M;

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

    public bool AttackPressed =>
        InputEnabled && Input.GetMouseButtonDown(0) && !IsPointerOverButton();

    public string InteractKeyLabel => interactKey.ToString();

    public bool InteractPressed => InputEnabled && Input.GetKeyDown(interactKey);
    public bool DropPressed => InputEnabled && Input.GetKeyDown(dropKey);
    public bool RestartPressed => Input.GetKeyDown(restartKey);
    public bool PausePressed => Input.GetKeyDown(pauseKey);
    public bool MapPressed => Input.GetKeyDown(mapKey);

    private static bool IsPointerOverButton()
    {
        EventSystem eventSystem = EventSystem.current;

        if (eventSystem == null)
            return false;

        PointerEventData pointerData = new PointerEventData(eventSystem);
        pointerData.position = Input.mousePosition;

        RaycastResults.Clear();
        eventSystem.RaycastAll(pointerData, RaycastResults);

        foreach (RaycastResult result in RaycastResults)
        {
            if (result.gameObject.GetComponentInParent<Selectable>() != null)
                return true;
        }

        return false;
    }
}
