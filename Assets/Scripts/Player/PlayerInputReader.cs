using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerInputReader : MonoBehaviour
{
    private const float TouchAimDistance = 6f;

    private static readonly List<RaycastResult> RaycastResults =
        new List<RaycastResult>();

    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private KeyCode dropKey = KeyCode.Q;
    [SerializeField] private KeyCode restartKey = KeyCode.R;
    [SerializeField] private KeyCode pauseKey = KeyCode.Escape;
    [SerializeField] private KeyCode mapKey = KeyCode.M;
    [SerializeField] private bool forceTouchControls;

    private Camera mainCamera;
    private MobileControls mobile;
    private Key interactInput;
    private Key dropInput;
    private Key restartInput;
    private Key pauseInput;
    private Key mapInput;

    public bool InputEnabled { get; set; } = true;

    private void Awake()
    {
        interactInput = ToKey(interactKey);
        dropInput = ToKey(dropKey);
        restartInput = ToKey(restartKey);
        pauseInput = ToKey(pauseKey);
        mapInput = ToKey(mapKey);

        if (Application.isMobilePlatform || forceTouchControls)
            mobile = MobileControls.Create(this);
    }

    public Vector2 Move
    {
        get
        {
            if (!InputEnabled)
                return Vector2.zero;

            Vector2 move = Vector2.zero;
            Keyboard keyboard = Keyboard.current;

            if (keyboard != null)
            {
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
                    move.x -= 1f;

                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
                    move.x += 1f;

                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
                    move.y -= 1f;

                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
                    move.y += 1f;
            }

            move = move.normalized;

            Gamepad gamepad = Gamepad.current;

            if (gamepad != null)
                move = Vector2.ClampMagnitude(move + gamepad.leftStick.ReadValue(), 1f);

            if (mobile != null)
                move = Vector2.ClampMagnitude(move + mobile.MoveValue, 1f);

            return move;
        }
    }

    public Vector3 AimWorldPosition
    {
        get
        {
            if (mobile != null)
            {
                return transform.position +
                       (Vector3)(mobile.AimDirection * TouchAimDistance);
            }

            if (mainCamera == null)
                mainCamera = Camera.main;

            Mouse mouse = Mouse.current;

            if (mainCamera == null || mouse == null)
                return transform.position;

            Vector2 screen = mouse.position.ReadValue();

            return mainCamera.ScreenToWorldPoint(
                new Vector3(screen.x, screen.y, -mainCamera.transform.position.z)
            );
        }
    }

    public bool AttackPressed
    {
        get
        {
            if (!InputEnabled)
                return false;

            if (mobile != null)
                return mobile.AttackBuffered;

            Mouse mouse = Mouse.current;

            return mouse != null &&
                   mouse.leftButton.wasPressedThisFrame &&
                   !IsPointerOverButton(mouse.position.ReadValue());
        }
    }

    public void AcknowledgeAttack()
    {
        if (mobile != null)
            mobile.ClearAttackBuffer();
    }

    public string InteractKeyLabel => interactKey.ToString();

    public bool InteractPressed =>
        InputEnabled &&
        (WasPressed(interactInput) ||
         (mobile != null && mobile.ConsumeInteract()));

    public bool DropPressed =>
        InputEnabled &&
        (WasPressed(dropInput) ||
         (mobile != null && mobile.ConsumeDrop()));

    public bool RestartPressed => WasPressed(restartInput);
    public bool PausePressed => WasPressed(pauseInput);
    public bool MapPressed => WasPressed(mapInput);

    private static Key ToKey(KeyCode keyCode)
    {
        if (Enum.TryParse(keyCode.ToString(), out Key key))
            return key;

        return Key.None;
    }

    private static bool WasPressed(Key key)
    {
        Keyboard keyboard = Keyboard.current;

        return keyboard != null &&
               key != Key.None &&
               keyboard[key].wasPressedThisFrame;
    }

    private static bool IsPointerOverButton(Vector2 screenPosition)
    {
        EventSystem eventSystem = EventSystem.current;

        if (eventSystem == null)
            return false;

        PointerEventData pointerData = new PointerEventData(eventSystem);
        pointerData.position = screenPosition;

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
