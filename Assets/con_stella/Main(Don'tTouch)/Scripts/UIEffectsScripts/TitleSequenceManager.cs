using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;

public class TitleSequenceManager : MonoBehaviour
{
    [Header("UIパーツ")]
    [SerializeField] private CanvasGroup titleLogoGroup; // タイトルロゴ
    [SerializeField] private CanvasGroup fullScreenFadeGroup; // シーン全体のフェード用

    [Header("星の演出設定")]
    [SerializeField] private GameObject starObject;
    [SerializeField] private TrailRenderer starTrail;
    [SerializeField] private Transform startPoint;
    [SerializeField] private Transform endPoint;
    [SerializeField] private float starMoveDuration = 1.5f;
    [SerializeField] private float totalRotation = -720f; // 飛んでいる間に何度回るか（マイナスは時計回り）
    void Start()
    {
        // 初期状態のセット
        titleLogoGroup.alpha = 0f;
        starObject.SetActive(false);
        starTrail.emitting = false; // 最初は軌跡を出さない

        // 演出開始
        PlayTitleSequence();
    }

    private void PlayTitleSequence()
    {
        // シーケンスの作成
        Sequence seq = DOTween.Sequence();

        // 1. ロゴをフェードイン
        seq.Append(titleLogoGroup.DOFade(1f, 1.5f));
        seq.AppendInterval(0.5f); // 少し待機

        // 2. 星の登場と移動
        seq.AppendCallback(() => {
            starObject.SetActive(true);
            starObject.transform.position = startPoint.position;
            starTrail.emitting = true; // 軌跡開始
        });

        // 真横に投げて落ちるような動き（放物線）
        seq.Join(starObject.transform.DOMoveX(endPoint.position.x, starMoveDuration).SetEase(Ease.Linear));
        seq.Join(starObject.transform.DOMoveY(endPoint.position.y, starMoveDuration).SetEase(Ease.InQuad));
        seq.Join(starObject.transform.DORotate(new Vector3(0, 0, totalRotation), starMoveDuration, RotateMode.FastBeyond360)
            .SetEase(Ease.Linear));  //回転処理
        // 3. 停止して余韻
        seq.AppendInterval(1.5f);

        // 4. シーン全体のフェードアウト
        seq.Append(titleLogoGroup.DOFade(0f, 1.0f));
        seq.Join(starObject.GetComponent<SpriteRenderer>().DOFade(0f, 1.0f)); // 星本体も消す

        // 5. HomeSceneへ移動
        seq.OnComplete(() => {
            if (SceneTransitionManager.instance != null)
            {
                SceneTransitionManager.instance.LoadScene("HomeScene");
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("HomeScene");
            }
        });
    }
}