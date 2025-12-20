using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

public class PostScreenAnimator : MonoBehaviour
{
    [SerializeField] private CanvasGroup titleGroup;
    [SerializeField] private List<CanvasGroup> buttonGroups;
    [SerializeField] private float slideDistance = 50f;
    [SerializeField] private float fadeDuration = 0.8f;

    void Start()
    {
        // 初期状態
        titleGroup.alpha = 0;
        foreach (var btn in buttonGroups) btn.alpha = 0;

        PlayEntrance();
    }

    void PlayEntrance()
    {
        // タイトルの登場
        titleGroup.DOFade(1f, fadeDuration);
        titleGroup.transform.DOLocalMoveY(slideDistance, fadeDuration).From(true); // 現在地から-50pxから移動

        // ボタンの連鎖登場（Sequence）
        for (int i = 0; i < buttonGroups.Count; i++)
        {
            CanvasGroup btn = buttonGroups[i];
            float delay = i * 0.2f; // 階段状に登場

            btn.DOFade(1f, fadeDuration).SetDelay(delay);
            btn.transform.DOLocalMoveY(slideDistance, fadeDuration).From(true).SetDelay(delay)
               .OnComplete(() => StartIdleAnimation(btn, i)); // 登場しきったらふわふわ開始
        }
    }

    void StartIdleAnimation(CanvasGroup btn, int index)
    {
        // 上下移動
        btn.transform.DOLocalMoveY(10f, 2f)
            .SetRelative()
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);

        // 左右回転
        btn.transform.DORotate(new Vector3(0, 0, 3f), 1.5f)
            .SetEase(Ease.InOutQuad)
            .SetLoops(-1, LoopType.Yoyo)
            .SetDelay(index * 0.3f); // タイミングをずらす
    }
}