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

    private Vector3 dragStartPos; // ドラッグ開始位置（ワールド座標）
    private Vector2 clickStartScreenPos; // クリック判定用の開始位置（スクリーン座標）
    private bool isDragging = false; // ドラッグ中かどうかの判定
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
            clickStartScreenPos = screenPos;
            dragStartPos = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0));
            isDragging = false;
        }

        // 2. ドラッグ中（押している間）
        if (Pointer.current.press.isPressed)
        {
            Vector2 screenPos = Pointer.current.position.ReadValue();

            // クリック開始位置から一定以上動いたら「ドラッグ」とみなす
            if (Vector2.Distance(screenPos, clickStartScreenPos) > 10f) // 10ピクセル以上動いたら
            {
                isDragging = true;
            }

            if (isDragging)
            {
                Vector3 currentPos = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0));
                Vector3 difference = dragStartPos - currentPos;

                transform.position += difference;

                // 移動制限
                float clampedX = Mathf.Clamp(transform.position.x, -mapSize.x, mapSize.x);
                float clampedY = Mathf.Clamp(transform.position.y, -mapSize.y, mapSize.y);
                transform.position = new Vector3(clampedX, clampedY, -10f);
            }
        }

        // 3. 指を離した瞬間（ドラッグしていなければクリックとみなす）
        if (Pointer.current.press.wasReleasedThisFrame)
        {
            if (!isDragging)
            {
                // ここでクリック処理を実行！
                CheckClickObject(Pointer.current.position.ReadValue());
            }
            isDragging = false;
        }
    }

    // クリックした場所に何があるか調べる
    private void CheckClickObject(Vector2 screenPos)
    {
        Vector3 worldPos = cam.ScreenToWorldPoint(screenPos);

        // 2DのRaycastを飛ばす
        RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);

        if (hit.collider != null)
        {
            // 当たったオブジェクトが「星座」か確認する
            // ConstellationClickTriggerがついているかチェック
            ConstellationClickTrigger trigger = hit.collider.GetComponent<ConstellationClickTrigger>();

            if (trigger != null)
            {
                Debug.Log("星座をクリックしました: " + hit.collider.gameObject.name);
                // その星座の中心へズームイン！
                FocusOnTarget(hit.transform.position);
            }
        }
    }

    private void HandleZoom()
    {
        if (Mouse.current != null)
        {
            float scroll = Mouse.current.scroll.ReadValue().y;
            if (scroll != 0.0f)
            {
                cam.orthographicSize -= scroll * zoomSpeed;
                cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);
            }
        }
    }

    // 特定の星座へズームインする機能
    public void FocusOnTarget(Vector3 targetPos)
    {
        // ターゲット位置も移動制限の範囲内に収める
        float clampedX = Mathf.Clamp(targetPos.x, -mapSize.x, mapSize.x);
        float clampedY = Mathf.Clamp(targetPos.y, -mapSize.y, mapSize.y);

        transform.position = new Vector3(clampedX, clampedY, -10f);
        cam.orthographicSize = minZoom + 2f; // いい感じのズーム率にする
    }
}