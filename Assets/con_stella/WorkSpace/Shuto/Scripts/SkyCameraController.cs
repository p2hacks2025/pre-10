using UnityEngine;
using UnityEngine.InputSystem;

public class SkyCameraController : MonoBehaviour
{
    [Header("背景マップのサイズ設定")]
    // BackgroundCanvasのPanelのWidth/Heightと同じ値を入れてください
    [SerializeField] private float mapWidth = 150f;
    [SerializeField] private float mapHeight = 150f;

    [Header("移動設定")]
    [SerializeField] private Vector2 mapSize = new Vector2(100f, 100f); // 移動上限

    [Header("ズーム設定")]
    [SerializeField] private float zoomSpeed = 0.001f; // 感度調整（スクロール値が大きいので小さめに）
    [SerializeField] private float minZoom = 2f;  // 最大ズーム（寄り）

    [Header("UI連携")]
    [SerializeField] private SkyUIManager uiManager;

    private bool isInputLocked = false; //操作ロックフラグ

    private Vector3 dragStartPos; // ドラッグ開始位置（ワールド座標）
    private Vector2 clickStartScreenPos; // クリック判定用の開始位置（スクリーン座標）
    private bool isDragging = false; // ドラッグ中かどうかの判定
    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();

        // 開始時に位置とズームを補正して、はみ出さないようにする
        ClampCameraPosition();
    }

    void Update()
    {
        // ポインター（マウスやタッチ）がなければ何もしない
        if (Pointer.current == null) return;
        if (isInputLocked) return;

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
                ClampCameraPosition();
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

    //ConstellationClickTrigger から名前などのデータを正しく取得するには、
    //Trigger側に public ConstellationData GetData() のようなメソッドを作り、
    //そこから constellationName を取ってくるのがベストです。
    //今回は簡易的に gameObject.name を渡しています。
    // クリックした場所に何があるか調べる
    private void CheckClickObject(Vector2 screenPos)
    {
        Vector3 worldPos = cam.ScreenToWorldPoint(screenPos);

        // 2DのRaycastを飛ばす
        RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);

        if (hit.collider != null)
        {
            // まずはクリック用トリガーを探す
            ConstellationClickTrigger trigger = hit.collider.GetComponent<ConstellationClickTrigger>();

            // なければ親などをたどって探す（念の為）
            if (trigger == null) trigger = hit.collider.GetComponentInParent<ConstellationClickTrigger>();

            if (trigger != null)
            {
                // ズーム実行
                FocusOnTarget(hit.transform.position);

                // ★UIを表示して、操作をロックする
                isInputLocked = true;

                // トリガーからデータを取得して名前を表示（データ取得メソッドが必要）
                // 仮でオブジェクト名を渡します
                uiManager.ShowDetail(hit.collider.gameObject.name);
            }
        }
    }

    // カメラが背景からはみ出さないように位置とズームを制限する関数
    private void ClampCameraPosition()
    {
        // 1. まずズーム（OrthographicSize）の上限を計算
        // 縦方向の限界: マップの高さ半分
        float maxCamSizeV = mapHeight / 2f;
        // 横方向の限界: マップの幅半分 / アスペクト比
        float maxCamSizeH = (mapWidth / 2f) / cam.aspect;

        // 縦と横、どちらか厳しい方を「最大ズームアウト量」とする
        float maxZoomAllowed = Mathf.Min(maxCamSizeV, maxCamSizeH);

        // ズームを制限範囲内に収める
        cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoomAllowed);

        // 2. 現在のカメラの表示範囲（縦横の半分サイズ）を計算
        float vertExtent = cam.orthographicSize;
        float horzExtent = vertExtent * cam.aspect;

        // 3. 移動可能な限界座標を計算
        // マップの端っこ - カメラの表示範囲 = カメラの中心が行ける限界
        float minX = -mapWidth / 2f + horzExtent;
        float maxX = mapWidth / 2f - horzExtent;
        float minY = -mapHeight / 2f + vertExtent;
        float maxY = mapHeight / 2f - vertExtent;

        // 4. 位置をClamp（制限）する
        Vector3 pos = transform.position;

        // もしズームアウトしすぎて計算がおかしくなった場合は中心(0)に戻す
        pos.x = (minX > maxX) ? 0f : Mathf.Clamp(pos.x, minX, maxX);
        pos.y = (minY > maxY) ? 0f : Mathf.Clamp(pos.y, minY, maxY);

        transform.position = pos;
    }

    private void HandleZoom()
    {
        if (Mouse.current != null)
        {
            float scroll = Mouse.current.scroll.ReadValue().y;
            if (scroll != 0.0f)
            {
                cam.orthographicSize -= scroll * zoomSpeed;

                ClampCameraPosition();
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

    // エディタ上でマップの大きさを緑の線で表示
    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(mapWidth, mapHeight, 0));
    }

    // 元に戻す機能（閉じるボタンから呼ばれる）
    public void ResetView()
    {
        // ズームを引く（最小ズーム値に戻すなど、お好みで）
        // ここでは「少し引いた状態」に戻します
        cam.orthographicSize = 10f; // 適当な引きの値

        // 画面外に出ていないか補正
        ClampCameraPosition();

        // ロック解除
        isInputLocked = false;
    }
}