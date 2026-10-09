using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TouchButton : MonoBehaviour,
    IPointerDownHandler, IPointerUpHandler
{
    private static readonly Color NormalColor = new Color(1f, 1f, 1f, 0.18f);
    private static readonly Color PressedColor = new Color(1f, 1f, 1f, 0.5f);

    private Image image;
    private Action onPress;

    public static TouchButton Create(
        Transform parent, string objectName, string label, Vector2 anchor,
        Vector2 position, float diameter, Action onPress)
    {
        GameObject buttonObject = new GameObject(
            objectName, typeof(RectTransform), typeof(Image)
        );

        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = (RectTransform)buttonObject.transform;
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = Vector2.one * diameter;

        Image image = buttonObject.GetComponent<Image>();
        image.sprite = UIFactory.CircleSprite;
        image.color = NormalColor;

        float padding = diameter * 0.12f;
        image.raycastPadding = new Vector4(-padding, -padding, -padding, -padding);

        UIFactory.CreateText(
            rect, "Label", label, diameter * 0.45f, Color.white,
            Vector2.zero, Vector2.one * diameter
        );

        TouchButton button = buttonObject.AddComponent<TouchButton>();
        button.image = image;
        button.onPress = onPress;

        return button;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        image.color = PressedColor;

        if (onPress != null)
            onPress();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        image.color = NormalColor;
    }

    private void OnDisable()
    {
        if (image != null)
            image.color = NormalColor;
    }
}
