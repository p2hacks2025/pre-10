using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [Header("移動の設定")]
    public float panSpeed = 0.5f;       
    public Vector2 minPosition = new Vector2(-10, -10);
    public Vector2 maxPosition = new Vector2(10, 10);

    [Header("望遠鏡ズームの設定")]
    public float zoomStep = 3.0f;       // 1回でググッと寄る量（大きめにしました）
    public float minZoom = 1.5f;        // 最大ズーム（かなり寄れます）
    public float maxZoom = 10.0f;       // 一番引いた状態
    public float smoothSpeed = 5.0f;    // ズームの「吸い込まれる速さ」（数字が大きいほど速い）

    private Vector2 lastMousePosition;
    private Camera cam;
    private float targetZoom; // 「最終的になりたい大きさ」を記憶する変数

    void Start()
    {
        cam = GetComponent<Camera>();
        targetZoom = cam.orthographicSize; // 最初は今の大きさを目標にする
    }

    void Update()
    {
        if (Mouse.current == null || Keyboard.current == null) return;

        // ==================================================
        // 1. 移動機能 (ドラッグ)
        // ==================================================
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            lastMousePosition = Mouse.current.position.ReadValue();
        }

        if (Mouse.current.leftButton.isPressed)
        {
            Vector2 currentMousePosition = Mouse.current.position.ReadValue();
            Vector2 delta = currentMousePosition - lastMousePosition;
            
            Vector3 move = new Vector3(-delta.x * panSpeed * Time.deltaTime, -delta.y * panSpeed * Time.deltaTime, 0);
            transform.position += move;

            float x = Mathf.Clamp(transform.position.x, minPosition.x, maxPosition.x);
            float y = Mathf.Clamp(transform.position.y, minPosition.y, maxPosition.y);
            transform.position = new Vector3(x, y, transform.position.z);

            lastMousePosition = currentMousePosition;
        }

        // ==================================================
        // 2. 望遠鏡ズーム機能 (Enterキー)
        // ==================================================
        // Enterが押されたら「目標の値（targetZoom）」だけを変える
        if (Keyboard.current.enterKey.wasPressedThisFrame)
        {
            targetZoom -= zoomStep;
            // 行き過ぎ防止
            targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
        }

        // Backspaceで戻る（目標値を増やす）
        if (Keyboard.current.backspaceKey.wasPressedThisFrame)
        {
            targetZoom += zoomStep;
            targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
        }

        // ★ここがポイント！
        // 現在のサイズを、目標サイズに向かって「滑らかに」変化させる (Lerp)
        // これで「シュッ」と吸い込まれる動きになります
        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetZoom, Time.deltaTime * smoothSpeed);
    }
}