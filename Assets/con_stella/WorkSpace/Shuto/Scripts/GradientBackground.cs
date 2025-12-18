using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class GradientBackground : MonoBehaviour
{
    // ★追加: モード切替用
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
    [SerializeField] private Camera targetCamera; // 指定がなければMainCameraを使います

    private SpriteRenderer _spriteRenderer;

    void Start()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _spriteRenderer.sortingOrder = sortingOrder;

        // グラデーション画像の生成
        GenerateGradientTexture();

        // モードに応じたサイズ調整
        FitBackground();
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

        // 1Unit = 1Pixel の設定でスプライト化 (幅1, 高さ2 のスプライトになる)
        _spriteRenderer.sprite = Sprite.Create(texture, new Rect(0, 0, 1, 2), new Vector2(0.5f, 0.5f), 1f);
    }

    // ★修正: モードによって処理を分岐
    public void FitBackground()
    {
        // スプライトの元サイズを取得 (GenerateGradientTextureで 1x2 になっている)
        Vector2 spriteOriginalSize = _spriteRenderer.sprite.bounds.size;
        float targetWidth = 10f;
        float targetHeight = 10f;

        switch (mode)
        {
            // パターンA: マップの広さに合わせる (SkyScene)
            case SizeMode.FillMapSize:
                if (skyCameraCon == null) skyCameraCon = FindObjectOfType<SkyCameraController>();

                if (skyCameraCon != null)
                {
                    targetWidth = skyCameraCon.GetMapSize().x * 1.2f;  // 余白1.2倍
                    targetHeight = skyCameraCon.GetMapSize().y * 1.2f;
                }
                else
                {
                    Debug.LogWarning("SkyCameraControllerが見つかりません。FillMapSizeモードが正しく動作しません。");
                }
                break;

            // パターンB: カメラの表示範囲に合わせる (他シーン)
            case SizeMode.FillCameraView:
                if (targetCamera == null) targetCamera = Camera.main;

                if (targetCamera != null)
                {
                    // Orthographicカメラの縦幅 = size * 2
                    float camHeight = targetCamera.orthographicSize * 2f;
                    // 横幅 = 縦幅 * アスペクト比
                    float camWidth = camHeight * targetCamera.aspect;

                    // 画面ピッタリより少しだけ大きくして隙間を防ぐ
                    targetWidth = camWidth * 1.05f;
                    targetHeight = camHeight * 1.05f;

                    // カメラ追従させるなら子オブジェクトにするか、Updateで追尾が必要
                    // 基本的に静止画背景ならカメラの子オブジェクトにすることをお勧めします
                }
                break;
        }

        // サイズ適用 (目標 / 元サイズ)
        float scaleX = targetWidth / spriteOriginalSize.x;
        float scaleY = targetHeight / spriteOriginalSize.y;

        transform.localScale = new Vector3(scaleX, scaleY, 1f);
    }
}