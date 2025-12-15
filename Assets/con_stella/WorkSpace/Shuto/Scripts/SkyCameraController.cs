using UnityEngine;
using UnityEngine.InputSystem; // Input Systemを使うために必要

public class SkyCameraController : MonoBehaviour
{
    [Header("移動設定")]
    [SerializeField] private Vector2 mapSize = new Vector2(100f, 100f); // 移動上限

    [Header("ズーム設定")]
    [SerializeField] private float zoomSpeed = 0.001f; // 感度調整（スクロール値が大きいので小さめに）
    [SerializeField] private float minZoom = 2f;  // 最大ズーム（寄り）
    [SerializeField] private float maxZoom = 20f; // 最小ズーム（引き）

    private Vector3 dragOrigin;
    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
    }

    void Update()
    {
        // ポインター（マウスやタッチ）がなければ何もしない
        if (Pointer.current == null) return;

        HandlePan();
        HandleZoom();
    }

    private void HandlePan()
    {
        // 1. ドラッグ開始（押した瞬間）
        if (Pointer.current.press.wasPressedThisFrame)
        {
            Vector2 screenPos = Pointer.current.position.ReadValue();
            dragOrigin = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0));
        }

        // 2. ドラッグ中（押している間）
        if (Pointer.current.press.isPressed)
        {
            Vector2 screenPos = Pointer.current.position.ReadValue();
            Vector3 currentPos = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0));

            // 差分を計算してカメラを逆方向に移動
            Vector3 difference = dragOrigin - currentPos;

            transform.position += difference;

            // 移動制限（Clamp）
            float clampedX = Mathf.Clamp(transform.position.x, -mapSize.x, mapSize.x);
            float clampedY = Mathf.Clamp(transform.position.y, -mapSize.y, mapSize.y);

            transform.position = new Vector3(clampedX, clampedY, -10f); // Zは固定
        }
    }

    private void HandleZoom()
    {
        float scroll = 0f;

        // ★修正箇所：Pointerではなく Mouse が存在する場合のみスクロール値を取る
        if (Mouse.current != null)
        {
            // Vector2.y がスクロール量
            scroll = Mouse.current.scroll.ReadValue().y;
        }

        if (scroll != 0.0f)
        {
            cam.orthographicSize -= scroll * zoomSpeed;
            // ズーム制限
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);
        }
    }

    // 特定の星座へズームインする機能
    public void FocusOnTarget(Vector3 targetPos)
    {
        transform.position = new Vector3(targetPos.x, targetPos.y, -10f);
        cam.orthographicSize = minZoom + 2f;
    }
}