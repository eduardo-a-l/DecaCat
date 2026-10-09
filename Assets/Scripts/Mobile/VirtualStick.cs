using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class VirtualStick : MonoBehaviour,
    IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    private const float DirectionDeadzone = 0.12f;
    private const float KnobRatio = 0.42f;
    private const float TravelRatio = 0.3f;
    private const float TouchPaddingRatio = 0.3f;

    private static readonly Color BaseColor = new Color(1f, 1f, 1f, 0.14f);
    private static readonly Color KnobColor = new Color(1f, 1f, 1f, 0.4f);

    private RectTransform area;
    private RectTransform knob;
    private float radius;
    private bool relativeToTouch;
    private float flickDistance;
    private bool flickArmed = true;
    private bool active;
    private int activePointer;
    private Vector2 origin;

    public event Action<Vector2> Flicked;

    public Vector2 Value { get; private set; }
    public Vector2 Direction { get; private set; } = Vector2.right;
    public bool Held => active;

    public static VirtualStick Create(
        Transform parent, string objectName, Vector2 anchor, Vector2 position,
        float diameter, bool relativeToTouch, float flickFraction)
    {
        GameObject baseObject = new GameObject(
            objectName, typeof(RectTransform), typeof(Image)
        );

        baseObject.transform.SetParent(parent, false);

        RectTransform baseRect = (RectTransform)baseObject.transform;
        baseRect.anchorMin = anchor;
        baseRect.anchorMax = anchor;
        baseRect.pivot = new Vector2(0.5f, 0.5f);
        baseRect.anchoredPosition = position;
        baseRect.sizeDelta = Vector2.one * diameter;

        Image baseImage = baseObject.GetComponent<Image>();
        baseImage.sprite = UIFactory.CircleSprite;
        baseImage.color = BaseColor;

        float padding = diameter * TouchPaddingRatio;
        baseImage.raycastPadding = new Vector4(-padding, -padding, -padding, -padding);

        GameObject knobObject = new GameObject(
            "Knob", typeof(RectTransform), typeof(Image)
        );

        knobObject.transform.SetParent(baseObject.transform, false);

        RectTransform knobRect = (RectTransform)knobObject.transform;
        knobRect.anchorMin = new Vector2(0.5f, 0.5f);
        knobRect.anchorMax = new Vector2(0.5f, 0.5f);
        knobRect.pivot = new Vector2(0.5f, 0.5f);
        knobRect.anchoredPosition = Vector2.zero;
        knobRect.sizeDelta = Vector2.one * diameter * KnobRatio;

        Image knobImage = knobObject.GetComponent<Image>();
        knobImage.sprite = UIFactory.CircleSprite;
        knobImage.color = KnobColor;
        knobImage.raycastTarget = false;

        VirtualStick stick = baseObject.AddComponent<VirtualStick>();
        stick.area = baseRect;
        stick.knob = knobRect;
        stick.radius = diameter * TravelRatio;
        stick.relativeToTouch = relativeToTouch;
        stick.flickDistance = flickFraction * stick.radius;

        return stick;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (active)
            return;

        active = true;
        activePointer = eventData.pointerId;
        flickArmed = true;

        Vector2 local = ToLocal(eventData);

        origin = relativeToTouch ? local : Vector2.zero;

        Evaluate(local);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!active || eventData.pointerId != activePointer)
            return;

        Evaluate(ToLocal(eventData));
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!active || eventData.pointerId != activePointer)
            return;

        Evaluate(ToLocal(eventData));
        Release();
    }

    private void OnDisable()
    {
        Release();
    }

    private void Evaluate(Vector2 local)
    {
        Vector2 delta = local - origin;
        Vector2 clamped = Vector2.ClampMagnitude(delta, radius);

        Value = clamped / radius;
        knob.anchoredPosition = clamped;

        float distance = delta.magnitude;

        if (distance >= radius * DirectionDeadzone)
            Direction = delta / distance;

        if (flickDistance <= 0f)
            return;

        if (flickArmed && distance >= flickDistance)
        {
            flickArmed = false;

            if (Flicked != null)
                Flicked(delta / distance);
        }
        else if (!flickArmed && distance < flickDistance * 0.5f)
        {
            flickArmed = true;
        }
    }

    private void Release()
    {
        active = false;
        flickArmed = true;
        Value = Vector2.zero;

        if (knob != null)
            knob.anchoredPosition = Vector2.zero;
    }

    private Vector2 ToLocal(PointerEventData eventData)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            area, eventData.position, eventData.pressEventCamera,
            out Vector2 local
        );

        return local;
    }
}
