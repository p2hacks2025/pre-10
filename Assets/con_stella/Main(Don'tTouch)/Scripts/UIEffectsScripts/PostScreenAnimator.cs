using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class PostScreenAnimator : MonoBehaviour
{
    [Header("アニメーション対象")]
    [SerializeField] private CanvasGroup titleGroup; // 「投稿方法を選んでください」の文字
    [SerializeField] private List<CanvasGroup> buttonGroups; // 3つのボタン（撮る、描く、読み込む）

    [Header("登場アニメーション設定")]
    [SerializeField] private float slideDistance = 50f; // 下から出現する距離
    [SerializeField] private float fadeDuration = 0.8f; // 出現にかかる時間

    [Header("待機中（ふわふわ）設定")]
    [SerializeField] private float floatAmplitude = 10f; // 上下に揺れる幅
    [SerializeField] private float floatSpeed = 2.0f;    // 上下に揺れる速さ
    [SerializeField] private float rotateAngle = 3.0f;   // 回転する角度（左右に振れる幅）
    [SerializeField] private float rotateSpeed = 1.5f;   // 回転する速さ

    // 初期位置を覚えておくためのリスト
    private List<Vector2> initialPositions = new List<Vector2>();
    // アニメーション完了フラグ
    private bool isIdleAnimationActive = false;

    void Start()
    {
        // 1. 初期化処理
        SetupInitialState();

        // 2. 登場アニメーション開始
        StartCoroutine(EntranceSequence());
    }

    void Update()
    {
        // 3. 待機アニメーション（登場が終わったら実行）
        if (isIdleAnimationActive)
        {
            PlayIdleAnimation();
        }
    }

    // --- 初期セットアップ ---
    private void SetupInitialState()
    {
        // タイトルの初期位置を記録＆透明にして下にずらす
        if (titleGroup != null)
        {
            RectTransform rect = titleGroup.GetComponent<RectTransform>();
            Vector2 startPos = rect.anchoredPosition;

            // 下にずらしてセット
            rect.anchoredPosition = startPos - new Vector2(0, slideDistance);
            titleGroup.alpha = 0f;
        }

        // ボタンの初期位置を記録＆透明にして下にずらす
        foreach (var group in buttonGroups)
        {
            if (group != null)
            {
                RectTransform rect = group.GetComponent<RectTransform>();
                initialPositions.Add(rect.anchoredPosition); // 元の場所を記憶

                // 下にずらしてセット
                rect.anchoredPosition = rect.anchoredPosition - new Vector2(0, slideDistance);
                group.alpha = 0f;
            }
        }
    }

    // --- 登場アニメーション（コルーチン） ---
    private IEnumerator EntranceSequence()
    {
        float timer = 0f;

        // タイトルの元の位置
        Vector2 titleTargetPos = Vector2.zero;
        Vector2 titleStartPos = Vector2.zero;
        RectTransform titleRect = null;

        if (titleGroup != null)
        {
            titleRect = titleGroup.GetComponent<RectTransform>();
            titleTargetPos = titleRect.anchoredPosition + new Vector2(0, slideDistance); // ずらした分戻す
            titleStartPos = titleRect.anchoredPosition;
        }

        // ボタンの元の位置は initialPositions に入っている

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float t = timer / fadeDuration;
            // EaseOutCubic (最初速くて最後ゆっくり)
            float ease = 1f - Mathf.Pow(1f - t, 3f);

            // A. タイトルのアニメーション
            if (titleGroup != null)
            {
                titleGroup.alpha = t; // フェードイン
                titleRect.anchoredPosition = Vector2.Lerp(titleStartPos, titleTargetPos, ease);
            }

            // B. ボタンのアニメーション
            for (int i = 0; i < buttonGroups.Count; i++)
            {
                if (buttonGroups[i] != null)
                {
                    RectTransform btnRect = buttonGroups[i].GetComponent<RectTransform>();
                    // 少しずつタイミングをずらす演出（i * 0.1f とか）も可能ですが、今回は一斉に
                    buttonGroups[i].alpha = t;

                    // 下から元の位置へ
                    Vector2 start = initialPositions[i] - new Vector2(0, slideDistance);
                    Vector2 end = initialPositions[i];
                    btnRect.anchoredPosition = Vector2.Lerp(start, end, ease);
                }
            }

            yield return null;
        }

        // 念のため最終位置にピタッと合わせる
        if (titleGroup != null) titleGroup.alpha = 1f;
        for (int i = 0; i < buttonGroups.Count; i++)
        {
            if (buttonGroups[i] != null)
            {
                buttonGroups[i].alpha = 1f;
                buttonGroups[i].GetComponent<RectTransform>().anchoredPosition = initialPositions[i];
            }
        }

        // 待機アニメーションへ移行
        isIdleAnimationActive = true;
    }

    // --- 待機アニメーション（ふわふわ・ゆらゆら） ---
    private void PlayIdleAnimation()
    {
        float time = Time.time;

        for (int i = 0; i < buttonGroups.Count; i++)
        {
            if (buttonGroups[i] == null) continue;

            RectTransform rect = buttonGroups[i].GetComponent<RectTransform>();
            Vector2 basePos = initialPositions[i];

            // 1. ふわふわ (Y軸移動)
            // i * 0.5f を足すことで、3つのボタンがバラバラのタイミングで動くようにする
            float yOffset = Mathf.Sin((time * floatSpeed) + (i * 1.5f)) * floatAmplitude;
            rect.anchoredPosition = basePos + new Vector2(0, yOffset);

            // 2. ゆらゆら (回転)
            // Cosを使うことで移動とタイミングを少しずらす
            float zRotation = Mathf.Cos((time * rotateSpeed) + (i * 1.5f)) * rotateAngle;
            rect.localRotation = Quaternion.Euler(0, 0, zRotation);
        }
    }
}