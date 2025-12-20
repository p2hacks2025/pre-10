using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class UIGlowingBlink : MonoBehaviour
{
    [Header("点滅の設定")]
    [SerializeField] private float blinkSpeed = 2.0f;
    [SerializeField] private float minIntensity = 0.5f;
    [SerializeField] private float maxIntensity = 3.0f;

    [Header("色設定")]
    [ColorUsage(true, true)] // HDRカラー対応
    [SerializeField] private Color baseColor = Color.white;

    private Image targetImage;

    void Start()
    {
        targetImage = GetComponent<Image>();  //Imageコンポーネント取得

        if (targetImage == null)
        {
            // なければ追加
            targetImage = gameObject.AddComponent<Image>();
        }

        //点滅ループ
        DOTween.To(() => minIntensity, x => UpdateColor(x), maxIntensity, blinkSpeed)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    void UpdateColor(float intensity)
    {
        if (targetImage != null)
        {
            targetImage.color = baseColor * intensity;
        }
    }
}