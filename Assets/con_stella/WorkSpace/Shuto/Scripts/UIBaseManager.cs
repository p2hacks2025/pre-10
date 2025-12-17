using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class UIBaseManager : MonoBehaviour
{
    [Header("共通UIパーツ")]
    [SerializeField] protected Button closeButton;        // 閉じるボタン

    [Header("アニメーション設定")]
    [SerializeField] protected float slideDuration = 0.3f; // 出てくるまでの時間（秒）
    [SerializeField] protected float panelHeight = 300f;   // パネルの高さ

    protected Coroutine currentAnimation;
    protected RectTransform currentPanel;

    protected virtual void Start()
    {
        // 閉じるボタンに機能を登録
        closeButton.onClick.AddListener(OnCloseButtonClicked);
    }

    // 閉じるボタンが押されたら呼ばれる関数
    public virtual void OnCloseButtonClicked()
    {
        // 1. パネルを「隠す」アニメーション
        if (currentAnimation != null) StopCoroutine(currentAnimation);
        currentAnimation = StartCoroutine(SlidePanel(-panelHeight)); // Y=-300 (画面外) へ
    }

    // パネルを開く共通メソッド
    public virtual void Open()
    {
        if (currentPanel == null) return;

        if (currentAnimation != null) StopCoroutine(currentAnimation);
        currentAnimation = StartCoroutine(SlidePanel(0)); // Y=0 (画面内) へ
    }

    // パネルを閉じる共通メソッド
    public virtual void Close()
    {
        OnCloseButtonClicked();
    }

    // 「にゅっ」と動かす処理
    protected  virtual IEnumerator SlidePanel(float targetY)
    {
        Vector2 startPos = currentPanel.anchoredPosition;
        Vector2 targetPos = new Vector2(0, targetY);
        float time = 0;

        while (time < slideDuration)
        {
            // SmoothStepを使うと「入り」と「抜き」が滑らかになります
            float t = time / slideDuration;
            t = t * t * (3f - 2f * t);

            currentPanel.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
            time += Time.deltaTime;
            yield return null;
        }

        currentPanel.anchoredPosition = targetPos;
    }
}