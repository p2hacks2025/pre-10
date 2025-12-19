using UnityEngine;
using UnityEngine.UI; // ★UIを操作するために必要

public class UIGlowingBlink : MonoBehaviour
{
    [Header("点滅の設定")]
    [SerializeField] private float blinkSpeed = 2.0f;
    [SerializeField] private float minIntensity = 0.5f;
    [SerializeField] private float maxIntensity = 3.0f; // Bloomさせるなら1.0以上にする

    [Header("色設定")]
    [ColorUsage(true, true)] // HDRカラー対応
    [SerializeField] private Color baseColor = Color.white;

    private Image targetImage; // ★SpriteRendererからImageに変更

    void Start()
    {
        // アタッチされているImageコンポーネントを取得
        targetImage = GetComponent<Image>();

        if (targetImage == null)
        {
            // なければ追加（親切設計）
            targetImage = gameObject.AddComponent<Image>();
        }
    }

    void Update()
    {
        if (targetImage == null) return;

        // サイン波で強弱を作る (0.0 〜 1.0)
        float sinWave = Mathf.Sin(Time.time * blinkSpeed);
        float factor = (sinWave + 1.0f) / 2.0f;

        // 強度を計算
        float currentIntensity = Mathf.Lerp(minIntensity, maxIntensity, factor);

        // HDRカラーを作成
        Color finalColor = baseColor * currentIntensity;

        // 元の画像の透明度(Alpha)は維持する
        finalColor.a = targetImage.color.a;

        // 適用
        targetImage.color = finalColor;
    }
}