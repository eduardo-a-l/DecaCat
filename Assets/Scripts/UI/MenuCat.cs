using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MenuCat : MonoBehaviour,
    IPointerDownHandler, IPointerUpHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private enum CatState
    {
        Idle,
        Walking,
        Fleeing,
        Held,
        Dropped
    }

    private const string SpriteResource = "MenuCat";
    private const float CatWidth = 170f;
    private const float EdgeMargin = 12f;
    private const float WalkSpeed = 130f;
    private const float FleeSpeed = 950f;
    private const float FleeTime = 1.1f;
    private const float DropTime = 0.35f;

    private RectTransform area;
    private RectTransform rect;
    private CatState state = CatState.Idle;
    private Vector2 position;
    private Vector2 target;
    private Vector2 fleeDirection;
    private Vector2 grabOffset;
    private float timer;
    private float wobble;
    private bool placed;
    private bool dragged;

    public static MenuCat Create(Transform parent)
    {
        Sprite sprite = Resources.Load<Sprite>(SpriteResource);

        if (sprite == null)
            return null;

        float height = CatWidth * sprite.rect.height / sprite.rect.width;

        RectTransform catRect = UIFactory.CreateRect(
            parent, "MenuCat", Vector2.zero, new Vector2(CatWidth, height)
        );

        Image image = catRect.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = true;

        MenuCat cat = catRect.gameObject.AddComponent<MenuCat>();
        cat.area = (RectTransform)parent;
        cat.rect = catRect;

        return cat;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        dragged = false;

        if (state != CatState.Held)
        {
            state = CatState.Idle;
            timer = 5f;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        dragged = true;
        state = CatState.Held;
        grabOffset = position - ToLocal(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (state != CatState.Held)
            return;

        position = ToLocal(eventData) + grabOffset;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        state = CatState.Dropped;
        timer = DropTime;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (dragged)
            return;

        Flee(ToLocal(eventData));
    }

    private void Update()
    {
        Vector2 limit = GetLimit();

        if (!placed)
        {
            if (limit == Vector2.zero)
                return;

            position = RandomPoint(limit);
            placed = true;
            StartIdle(0.5f);
        }

        float delta = Time.unscaledDeltaTime;

        switch (state)
        {
            case CatState.Idle:
                UpdateIdle(delta, limit);
                break;

            case CatState.Walking:
                UpdateWalking(delta);
                break;

            case CatState.Fleeing:
                UpdateFleeing(delta, limit);
                break;

            case CatState.Dropped:
                timer -= delta;

                if (timer <= 0f)
                    StartIdle(1.5f);

                break;
        }

        position = Clamp(position, limit);

        rect.anchoredPosition = position;

        Animate();
    }

    private void UpdateIdle(float delta, Vector2 limit)
    {
        timer -= delta;

        if (timer > 0f)
            return;

        target = RandomPoint(limit);
        state = CatState.Walking;
    }

    private void UpdateWalking(float delta)
    {
        Vector2 offset = target - position;
        float distance = offset.magnitude;
        float step = WalkSpeed * delta;

        wobble += delta * 12f;

        if (distance <= step)
        {
            position = target;
            StartIdle(Random.Range(1f, 3.5f));
            return;
        }

        position += offset / distance * step;
    }

    private void UpdateFleeing(float delta, Vector2 limit)
    {
        timer -= delta;
        wobble += delta * 30f;

        float speed = FleeSpeed * Mathf.Clamp01(timer / FleeTime + 0.25f);

        position += fleeDirection * speed * delta;

        if (position.x < -limit.x)
            fleeDirection.x = Mathf.Abs(fleeDirection.x);
        else if (position.x > limit.x)
            fleeDirection.x = -Mathf.Abs(fleeDirection.x);

        if (position.y < -limit.y)
            fleeDirection.y = Mathf.Abs(fleeDirection.y);
        else if (position.y > limit.y)
            fleeDirection.y = -Mathf.Abs(fleeDirection.y);

        if (timer <= 0f)
            StartIdle(1.2f);
    }

    private void Animate()
    {
        float tilt = 0f;
        float scale = 1f;

        if (state == CatState.Walking || state == CatState.Fleeing)
            tilt = Mathf.Sin(wobble) * 7f;

        if (state == CatState.Held)
            scale = 1.15f;

        if (state == CatState.Dropped)
            scale = 1f + 0.2f * Mathf.Sin(timer / DropTime * Mathf.PI);

        rect.localRotation = Quaternion.Euler(0f, 0f, tilt);
        rect.localScale = new Vector3(scale, scale, 1f);
    }

    private void Flee(Vector2 from)
    {
        Vector2 direction = position - from;

        if (direction.sqrMagnitude < 1f)
            direction = Random.insideUnitCircle;

        fleeDirection = direction.normalized;
        state = CatState.Fleeing;
        timer = FleeTime;
    }

    private void StartIdle(float seconds)
    {
        state = CatState.Idle;
        timer = seconds;
    }

    private Vector2 GetLimit()
    {
        Vector2 half = (area.rect.size - rect.sizeDelta) * 0.5f -
                       Vector2.one * EdgeMargin;

        return Vector2.Max(half, Vector2.zero);
    }

    private static Vector2 Clamp(Vector2 value, Vector2 limit)
    {
        return new Vector2(
            Mathf.Clamp(value.x, -limit.x, limit.x),
            Mathf.Clamp(value.y, -limit.y, limit.y)
        );
    }

    private static Vector2 RandomPoint(Vector2 limit)
    {
        return new Vector2(
            Random.Range(-limit.x, limit.x),
            Random.Range(-limit.y, limit.y)
        );
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
