using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class SkyUIManager : UIBaseManager
{
    [Header("UIパーツ")]
    [SerializeField] private RectTransform detailPanel; // 星座詳細パネル
    [SerializeField] private RectTransform postStarPanel; //流れ星投稿用パネル
    [SerializeField] private TextMeshProUGUI nameText;  // 星座名を表示するテキスト
    [SerializeField] private TextMeshProUGUI descriptionText; //星座の説明を表示するテキスト

    // 外部スクリプト連携
    [SerializeField] private SkyCameraController cameraController;

    protected override void Start()
    {
        base.Start();

        // 最初は隠しておく（念の為）
        detailPanel.anchoredPosition = new Vector2(0, -panelHeight);
        postStarPanel.anchoredPosition = new Vector2(0, -panelHeight);
    }

    // 星座がクリックされたら呼ばれる関数
    public void ShowDetail(string constellationName)
    {
        // 1. テキスト更新
        nameText.text = constellationName;
        //descriptionText.text = desc;

        currentPanel = detailPanel;
        // 2. パネルを「出す」アニメーション
        if (currentAnimation != null) StopCoroutine(currentAnimation);
        currentAnimation = StartCoroutine(SlidePanel(0)); // Y=0 (画面内) へ
    }

    // 流れ星ボタンが押されたら呼ばれる関数
    public void ShowPostStarPanel()
    {
        //TODO : 入力パネルを有効化する（別メンバーの作成した関数を使用。今は表示だけ）
        currentPanel = postStarPanel;
        if (currentAnimation != null) StopCoroutine(currentAnimation);
        currentAnimation = StartCoroutine(SlidePanel(0));
    }

    // 閉じるボタンが押されたら呼ばれる関数
    public override void OnCloseButtonClicked()
    {
        base.OnCloseButtonClicked();

        cameraController.ResetView();
    }
}