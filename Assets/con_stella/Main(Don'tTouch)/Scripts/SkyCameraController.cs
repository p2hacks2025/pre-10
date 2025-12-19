using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections; // コルーチン用

public class SkyCameraController : MonoBehaviour
{
    [Header("背景マップのサイズ設定")]
    [SerializeField] private float mapWidth = 150f;
    [SerializeField] private float mapHeight = 150f;

    [Header("移動設定")]
    [SerializeField] private Vector2 mapSize = new Vector2(100f, 100f);

    [Header("ズーム設定")]
    [SerializeField] private float zoomSpeed = 0.01f;      // マウスホイール用感度
    [SerializeField] private float touchZoomSpeed = 0.01f; // ★追加: スマホピンチ用感度
    [SerializeField] private float minZoom = 2f;           // 最大ズーム（寄り）

    [Header("アニメーション設定")]
    [SerializeField] private float smoothTime = 0.3f; // ★追加: 移動にかける時間

    [Header("UI連携")]
    [SerializeField] private SkyUIManager uiManager;

    private bool isInputLocked = false;

    private Vector3 dragStartPos;
    private Vector2 clickStartScreenPos;
    private bool isDragging = false;
    private Camera cam;

    // アニメーション用変数
    private Vector3 currentVelocityPos; // SmoothDamp用
    private float currentVelocityZoom;  // SmoothDamp用
    private Coroutine currentMoveCoroutine;

    void Start()
    {
        cam = GetComponent<Camera>();
        ClampCameraPosition();
    }

    void Update()
    {
        if (isInputLocked) return;

        // アニメーション中は操作を受け付けない、または操作したらアニメーションを止めるなどの制御が可能
        // ここでは「操作したらアニメーション停止」は実装せず、並列で動かないようにだけ注意します

        // マウス・タッチ共通のドラッグ移動
        if (Pointer.current != null)
        {
            HandlePan();
        }

        // ズーム処理（マウスホイール & ピンチ操作）
        HandleZoom();
        HandleTouchZoom(); // ★追加
    }

    private void HandlePan()
    {
        // ピンチ操作中（2本指）はパン移動（1本指）をさせないようにガード
        if (Touchscreen.current != null && Touchscreen.current.touches.Count >= 2)
        {
            isDragging = false;
            return;
        }

        // 1. ドラッグ開始
        if (Pointer.current.press.wasPressedThisFrame)
        {
            // アニメーション中に触ったら止める（直感的な操作のため）
            if (currentMoveCoroutine != null) StopCoroutine(currentMoveCoroutine);

            Vector2 screenPos = Pointer.current.position.ReadValue();
            clickStartScreenPos = screenPos;
            dragStartPos = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0));
            isDragging = false;
        }

        // 2. ドラッグ中
        if (Pointer.current.press.isPressed)
        {
            Vector2 screenPos = Pointer.current.position.ReadValue();

            if (Vector2.Distance(screenPos, clickStartScreenPos) > 10f)
            {
                isDragging = true;
            }

            if (isDragging)
            {
                Vector3 currentPos = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0));
                Vector3 difference = dragStartPos - currentPos;

                transform.position += difference;
                ClampCameraPosition();
            }
        }

        // 3. リリース（クリック判定）
        if (Pointer.current.press.wasReleasedThisFrame)
        {
            if (!isDragging)
            {
                CheckClickObject(Pointer.current.position.ReadValue());
            }
            isDragging = false;
        }
    }

    private void CheckClickObject(Vector2 screenPos)
    {
        Vector3 worldPos = cam.ScreenToWorldPoint(screenPos);
        RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);

        if (hit.collider != null)
        {
            ConstellationClickTrigger trigger = hit.collider.GetComponent<ConstellationClickTrigger>();
            if (trigger == null) trigger = hit.collider.GetComponentInParent<ConstellationClickTrigger>();

            if (trigger != null)
            {
                // ★修正: 滑らかにズーム
                FocusOnTarget(hit.transform.position);

                isInputLocked = true;
                ConstellationData data = trigger.GetData();
                uiManager.ShowDetail(data);
            }
        }
    }

    // ★追加: スマホのピンチズーム処理
    private void HandleTouchZoom()
    {
        // タッチパネルがない、または指が2本ない場合は無視
        if (Touchscreen.current == null || Touchscreen.current.touches.Count < 2) return;

        // 2本の指の情報を取得
        var touch0 = Touchscreen.current.touches[0];
        var touch1 = Touchscreen.current.touches[1];

        // いずれかの指が動いていないなら処理しない
        if (touch0.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Stationary &&
            touch1.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Stationary)
        {
            return;
        }

        // 現在の2点間の距離
        Vector2 touch0Pos = touch0.position.ReadValue();
        Vector2 touch1Pos = touch1.position.ReadValue();
        float currentDist = Vector2.Distance(touch0Pos, touch1Pos);

        // 直前のフレームの2点間の距離（Deltaを使って逆算）
        Vector2 touch0PrevPos = touch0Pos - touch0.delta.ReadValue();
        Vector2 touch1PrevPos = touch1Pos - touch1.delta.ReadValue();
        float prevDist = Vector2.Distance(touch0PrevPos, touch1PrevPos);

        // 差分（プラスなら拡大操作、マイナスなら縮小操作）
        float zoomMagnitude = prevDist - currentDist;

        // アニメーション停止
        if (currentMoveCoroutine != null && Mathf.Abs(zoomMagnitude) > 0.1f) StopCoroutine(currentMoveCoroutine);

        // ズーム適用
        if (Mathf.Abs(zoomMagnitude) > 0.01f)
        {
            cam.orthographicSize += zoomMagnitude * touchZoomSpeed;
            ClampCameraPosition();
        }
    }

    private void HandleZoom()
    {
        if (Mouse.current != null)
        {
            float scroll = Mouse.current.scroll.ReadValue().y;
            if (scroll != 0.0f)
            {
                // アニメーション停止
                if (currentMoveCoroutine != null) StopCoroutine(currentMoveCoroutine);

                // マウスホイールの値は大きいので調整
                cam.orthographicSize -= scroll * zoomSpeed;
                ClampCameraPosition();
            }
        }
    }

    // クランプ処理（変更なし）
    private void ClampCameraPosition()
    {
        float maxCamSizeV = mapHeight / 2f;
        float maxCamSizeH = (mapWidth / 2f) / cam.aspect;
        float maxZoomAllowed = Mathf.Min(maxCamSizeV, maxCamSizeH);

        cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoomAllowed);

        float vertExtent = cam.orthographicSize;
        float horzExtent = vertExtent * cam.aspect;

        float minX = -mapWidth / 2f + horzExtent;
        float maxX = mapWidth / 2f - horzExtent;
        float minY = -mapHeight / 2f + vertExtent;
        float maxY = mapHeight / 2f - vertExtent;

        Vector3 pos = transform.position;
        pos.x = (minX > maxX) ? 0f : Mathf.Clamp(pos.x, minX, maxX);
        pos.y = (minY > maxY) ? 0f : Mathf.Clamp(pos.y, minY, maxY);

        transform.position = pos;
    }

    // 滑らかにターゲットへ移動する機能
    public void FocusOnTarget(Vector3 targetPos)
    {
        // ターゲット位置の制限計算
        float clampedX = Mathf.Clamp(targetPos.x, -mapSize.x, mapSize.x);
        float clampedY = Mathf.Clamp(targetPos.y, -mapSize.y, mapSize.y);
        Vector3 finalPos = new Vector3(clampedX, clampedY, -10f);

        // 目標のズーム値
        float targetZoom = minZoom + 2f;

        // 既に動いているコルーチンがあれば止める
        if (currentMoveCoroutine != null) StopCoroutine(currentMoveCoroutine);

        // コルーチン開始
        currentMoveCoroutine = StartCoroutine(SmoothMoveRoutine(finalPos, targetZoom));
    }

    // ★追加: アニメーション用コルーチン
    private IEnumerator SmoothMoveRoutine(Vector3 targetPos, float targetZoom)
    {
        // ほぼ目標値になるまでループ
        while (Vector3.Distance(transform.position, targetPos) > 0.01f || Mathf.Abs(cam.orthographicSize - targetZoom) > 0.01f)
        {
            // 位置の補間 (SmoothDamp)
            transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref currentVelocityPos, smoothTime);

            // ズームの補間 (SmoothDamp)
            cam.orthographicSize = Mathf.SmoothDamp(cam.orthographicSize, targetZoom, ref currentVelocityZoom, smoothTime);

            // 補間中も画面外にはみ出さないようガード
            ClampCameraPosition();

            yield return null;
        }

        // 最後はきっちり値を合わせる
        transform.position = targetPos;
        cam.orthographicSize = targetZoom;
        ClampCameraPosition();

        currentMoveCoroutine = null;
    }

    public void ResetView()
    {
        // リセット時も滑らかに戻したい場合はここもコルーチンにできますが、
        // 閉じるボタンでUIが動くので、ここは即座に戻すか、お好みで。
        // 今回は即座に戻すままにします。
        cam.orthographicSize = 10f;
        ClampCameraPosition();
        isInputLocked = false;

        // アニメーション中なら止める
        if (currentMoveCoroutine != null) StopCoroutine(currentMoveCoroutine);
    }

    public Vector3 GetMapSize()
    {
        return new Vector3(mapWidth, mapHeight, 1);
    }
}