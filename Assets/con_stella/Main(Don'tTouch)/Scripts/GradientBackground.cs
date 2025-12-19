using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class GradientBackground : MonoBehaviour
{
    // モード切替用
    public enum SizeMode
    {
        FillMapSize,    // マップ全体を埋める (SkyScene用)
        FillCameraView  // カメラに映る範囲だけ埋める (タイトル画面やプレビュー用)
    }

    [Header("モード設定")]
    [SerializeField] private SizeMode mode = SizeMode.FillMapSize;

    [Header("グラデーションの色")]
    public Color topColor = new Color(0.1f, 0.1f, 0.3f);
    public Color bottomColor = new Color(0.0f, 0.0f, 0.1f);

    [Header("共通設定")]
    [SerializeField] private int sortingOrder = -1000;

    [Header("FillMapSizeモード用")]
    [SerializeField] private SkyCameraController skyCameraCon;

    [Header("FillCameraViewモード用")]
    [SerializeField] private Camera targetCamera;

    private SpriteRenderer _spriteRenderer;

    // 外部からサイズを取得するためのプロパティ
    public Vector2 CurrentSize { get; private set; }

    void Start()
    {
        // Startで呼ばれたときも念の為チェックして実行
        FitBackground();
    }

    // ★追加: 安全装置（初期化がまだなら、ここで行う）
    private void InitializeIfNeeded()
    {
        if (_spriteRenderer != null) return; // すでに初期化済みなら何もしない

        _spriteRenderer = GetComponent<SpriteRenderer>();
        if (_spriteRenderer != null)
        {
            _spriteRenderer.sortingOrder = sortingOrder;
            GenerateGradientTexture(); // 画像生成
        }
    }

    void GenerateGradientTexture()
    {
        // 1x2ピクセル (PPU=1) で作成
        Texture2D texture = new Texture2D(1, 2);
        texture.SetPixel(0, 0, bottomColor);
        texture.SetPixel(0, 1, topColor);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.Apply();

        _spriteRenderer.sprite = Sprite.Create(texture, new Rect(0, 0, 1, 2), new Vector2(0.5f, 0.5f), 1f);
    }

    // 外部から呼ばれる関数
    public void FitBackground()
    {
        // ★修正: 処理の前に必ず初期化チェックを行う！
        InitializeIfNeeded();

        // それでも失敗していたらリターン（エラー回避）
        if (_spriteRenderer == null || _spriteRenderer.sprite == null) return;

        // スプライトの元サイズを取得
        Vector2 spriteOriginalSize = _spriteRenderer.sprite.bounds.size;
        float targetWidth = 10f;
        float targetHeight = 10f;

        switch (mode)
        {
            case SizeMode.FillMapSize:
                if (skyCameraCon == null) skyCameraCon = FindFirstObjectByType<SkyCameraController>();

                if (skyCameraCon != null)
                {
                    targetWidth = skyCameraCon.GetMapSize().x * 1.2f;
                    targetHeight = skyCameraCon.GetMapSize().y * 1.2f;
                }
                break;

            case SizeMode.FillCameraView:
                if (targetCamera == null) targetCamera = Camera.main;

                if (targetCamera != null)
                {
                    float camHeight = targetCamera.orthographicSize * 2f;
                    float camWidth = camHeight * targetCamera.aspect;
                    targetWidth = camWidth * 1.05f;
                    targetHeight = camHeight * 1.05f;
                }
                break;
        }

        // サイズ適用
        float scaleX = targetWidth / spriteOriginalSize.x;
        float scaleY = targetHeight / spriteOriginalSize.y;

        transform.localScale = new Vector3(scaleX, scaleY, 1f);

        CurrentSize = new Vector2(targetWidth, targetHeight);
    }
}