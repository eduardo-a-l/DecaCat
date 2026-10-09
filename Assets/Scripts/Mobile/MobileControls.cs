using UnityEngine;

public class MobileControls : MonoBehaviour
{
    private const float StickDiameter = 320f;
    private const float StickMargin = 260f;
    private const float ButtonDiameter = 140f;
    private const float FlickFraction = 0.6f;
    private const float MoveDeadzone = 0.12f;
    private const float AttackBufferSeconds = 0.15f;

    private static readonly Vector2 ItemSlotShift = new Vector2(0f, 470f);

    private PlayerInputReader reader;
    private RectTransform safeRoot;
    private VirtualStick moveStick;
    private VirtualStick aimStick;
    private Rect lastSafeArea;
    private float attackBufferedUntil = -1f;
    private bool interactQueued;
    private bool dropQueued;

    public Vector2 MoveValue
    {
        get
        {
            Vector2 value = moveStick.Value;
            float magnitude = value.magnitude;

            if (magnitude < MoveDeadzone)
                return Vector2.zero;

            float scaled = Mathf.InverseLerp(MoveDeadzone, 1f, magnitude);

            return value / magnitude * scaled;
        }
    }

    public Vector2 AimDirection => aimStick.Direction;

    public bool AttackBuffered => Time.unscaledTime <= attackBufferedUntil;

    public static MobileControls Create(PlayerInputReader owner)
    {
        GameObject root = UIFactory.CreateOverlayCanvas("MobileControls", 10);

        MobileControls controls = root.AddComponent<MobileControls>();
        controls.Build(owner);

        return controls;
    }

    public void ClearAttackBuffer()
    {
        attackBufferedUntil = -1f;
    }

    public bool ConsumeInteract()
    {
        bool value = interactQueued;
        interactQueued = false;

        return value;
    }

    public bool ConsumeDrop()
    {
        bool value = dropQueued;
        dropQueued = false;

        return value;
    }

    private void Build(PlayerInputReader owner)
    {
        reader = owner;

        GameObject safeObject = new GameObject("SafeArea", typeof(RectTransform));
        safeObject.transform.SetParent(transform, false);

        safeRoot = (RectTransform)safeObject.transform;
        safeRoot.anchorMin = Vector2.zero;
        safeRoot.anchorMax = Vector2.one;
        safeRoot.offsetMin = Vector2.zero;
        safeRoot.offsetMax = Vector2.zero;

        moveStick = VirtualStick.Create(
            safeRoot, "MoveStick", new Vector2(0f, 0f),
            new Vector2(StickMargin, StickMargin),
            StickDiameter, false, 0f
        );

        aimStick = VirtualStick.Create(
            safeRoot, "AttackStick", new Vector2(1f, 0f),
            new Vector2(-StickMargin, StickMargin),
            StickDiameter, true, FlickFraction
        );

        aimStick.Flicked += OnFlick;

        TouchButton.Create(
            safeRoot, "InteractButton", "E", new Vector2(1f, 0f),
            new Vector2(-590f, 130f), ButtonDiameter,
            () => interactQueued = true
        );

        TouchButton.Create(
            safeRoot, "DropButton", "Q", new Vector2(1f, 0f),
            new Vector2(-776f, 130f), ButtonDiameter,
            () => dropQueued = true
        );
    }

    private void Start()
    {
        ItemDisplay display = FindFirstObjectByType<ItemDisplay>();

        if (display != null)
            display.ShiftBy(ItemSlotShift);
    }

    private void Update()
    {
        bool visible = reader != null && reader.InputEnabled;

        if (safeRoot.gameObject.activeSelf != visible)
            safeRoot.gameObject.SetActive(visible);

        if (!visible)
        {
            interactQueued = false;
            dropQueued = false;
            attackBufferedUntil = -1f;
        }

        ApplySafeArea();
    }

    private void OnFlick(Vector2 direction)
    {
        attackBufferedUntil = Time.unscaledTime + AttackBufferSeconds;
    }

    private void ApplySafeArea()
    {
        Rect safe = Screen.safeArea;

        if (safe == lastSafeArea || Screen.width <= 0 || Screen.height <= 0)
            return;

        lastSafeArea = safe;

        safeRoot.anchorMin = new Vector2(
            safe.xMin / Screen.width, safe.yMin / Screen.height
        );

        safeRoot.anchorMax = new Vector2(
            safe.xMax / Screen.width, safe.yMax / Screen.height
        );

        safeRoot.offsetMin = Vector2.zero;
        safeRoot.offsetMax = Vector2.zero;
    }
}
