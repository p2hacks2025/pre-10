using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;
using System.Collections;

public class UIBaseManager : MonoBehaviour
{
    [Header("共通UIパーツ")]
    [SerializeField] protected Button closeButton;        // 閉じるボタン

    [Header("アニメーション設定")]
    [SerializeField] protected float slideDuration = 0.3f; // 出てくるまでの時間（秒）
    [SerializeField] protected float panelHeight = 300f;   // パネルの高さ

    protected RectTransform currentPanel;  //処理対象のパネル
    private Tween currentTween; // 実行中のアニメーションを管理

    protected virtual void Start()
    {
        // 閉じるボタンに機能を登録
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(OnCloseButtonClicked);
        }
    }

    // 閉じるボタンが押されたら呼ばれる関数
    public virtual void OnCloseButtonClicked() => Close();

    // パネルを開く共通メソッド
    public virtual void Open() => DoSlide(currentPanel, 0);

    // パネルを閉じる共通メソッド
    public virtual void Close() => DoSlide(currentPanel, -panelHeight);

    // 子クラスが「好きなパネルを好きな位置へ」動かすための共通メソッド
    protected void DoSlide(RectTransform panel, float targetY, System.Action onComplete = null)
    {
        if (panel == null) return;

        currentTween?.Kill(); // 実行中のアニメを停止
        currentTween = panel.DOAnchorPos(new Vector2(0, targetY), slideDuration)
            .SetEase(targetY == 0 ? Ease.OutQuart : Ease.InQuart) // 開く,閉じるで挙動を変える
            .OnComplete(() => onComplete?.Invoke());
    }

    // InputFieldを選択状態にするコルーチン
    protected IEnumerator AutoSelectInputField(TMP_InputField targetField)
    {
        // パネルが表示される(SetActiveなど)のを1フレーム待つ
        yield return null;

        if (targetField != null)
        {
            // 強制的にアクティブ化して選択
            targetField.ActivateInputField();
            targetField.Select();
        }
    }
}