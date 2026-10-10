using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraFit : MonoBehaviour
{
    private const float MinViewWidth = 17.78f;

    private Camera fitCamera;
    private float baseSize;
    private int lastWidth;
    private int lastHeight;

    private void Awake()
    {
        fitCamera = GetComponent<Camera>();
        baseSize = fitCamera.orthographicSize;
    }

    private void LateUpdate()
    {
        if (Screen.width == lastWidth && Screen.height == lastHeight)
            return;

        lastWidth = Screen.width;
        lastHeight = Screen.height;

        if (lastHeight <= 0)
            return;

        float aspect = (float)lastWidth / lastHeight;

        fitCamera.orthographicSize =
            Mathf.Max(baseSize, MinViewWidth / (2f * aspect));
    }
}
