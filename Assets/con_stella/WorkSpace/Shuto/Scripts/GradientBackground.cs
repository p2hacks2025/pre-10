using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class GradientBackground : MonoBehaviour
{
    [Header("グラデーションの色")]
    public Color topColor = new Color(0.1f, 0.1f, 0.3f);    // 上の色
    public Color bottomColor = new Color(0.0f, 0.0f, 0.1f); // 下の色

    [Header("設定")]
    [SerializeField] private int sortingOrder = -1000; // 最背面に配置

    // インスペクターで設定し忘れても、自動で探すように変更
    [SerializeField] private SkyCameraController cameraCon;

    private SpriteRenderer _spriteRenderer;

    void Start()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();

        // CameraControllerがセットされていなければ自動で探す
        if (cameraCon == null)
        {
            cameraCon = FindObjectOfType<SkyCameraController>();
        }

        // 背景描画順の設定
        _spriteRenderer.sortingOrder = sortingOrder;

        // グラデーションテクスチャを生成
        GenerateGradientTexture();

        // マップサイズに合わせて引き伸ばす
        FitToMapSize();
    }

    void GenerateGradientTexture()
    {
        // 1x2ピクセルのテクスチャを作成
        Texture2D texture = new Texture2D(1, 2);

        // 色をセット
        texture.SetPixel(0, 0, bottomColor);
        texture.SetPixel(0, 1, topColor);

        // 補間設定 (Bilinearで滑らかに)
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.Apply();

        // ★修正点: 第4引数に「1」を指定 (1ピクセル = 1Unit として生成)
        // これで計算が直感的になります
        _spriteRenderer.sprite = Sprite.Create(texture, new Rect(0, 0, 1, 2), new Vector2(0.5f, 0.5f), 1f);
    }

    void FitToMapSize()
    {
        if (cameraCon == null)
        {
            Debug.LogError("SkyCameraControllerが見つかりません。背景サイズを決定できません。");
            return;
        }

        // 1. マップの目標サイズを取得
        float targetWidth = cameraCon.GetMapSize().x;
        float targetHeight = cameraCon.GetMapSize().y;

        // 2. 現在のスプライトのサイズを取得 (GenerateGradientTextureでPPU=1にしたので 1x2 になっているはず)
        Vector2 spriteSize = _spriteRenderer.sprite.bounds.size;

        // 3. 倍率を計算 (目標サイズ / 元のサイズ)
        // 1.2倍などの余白はお好みで残しています
        float scaleX = (targetWidth / spriteSize.x) * 1.2f;
        float scaleY = (targetHeight / spriteSize.y) * 1.2f;

        // 4. 適用
        transform.localScale = new Vector3(scaleX, scaleY, 1f);
    }
}