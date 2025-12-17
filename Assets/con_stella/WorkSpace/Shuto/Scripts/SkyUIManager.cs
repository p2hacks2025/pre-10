using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Junya;

public class SkyUIManager : UIBaseManager
{
    // =================================================
    // 1. 設定項目
    // =================================================
    [Header("各パネルの隠れる位置 (高さ設定)")]
    [SerializeField] private float detailPanelOffset = 1200f;
    [SerializeField] private float postStarPanelOffset = 600f;
    [SerializeField] private float replyPanelOffset = 1800f;   // 十分大きな値を設定

    // =================================================
    // 2. UIパーツの参照
    // =================================================
    [Header("【1】DetailPanel (星座詳細)")]
    [SerializeField] private RectTransform detailPanel;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Transform listContentRoot;       // ここが ScrollView/Viewport/Content であること
    [SerializeField] private CommentListElement commentItemPrefab;

    [Header("【2】PostStarPanel (流れ星投稿)")]
    [SerializeField] private RectTransform postStarPanel;
    [SerializeField] private TMP_InputField postContentInput;

    [Header("【3】ReplyPanel (返信)")]
    [SerializeField] private RectTransform replyPanel;
    [SerializeField] private TMP_InputField replyContentInput;

    [Header("その他")]
    [SerializeField] private SkyCameraController cameraController;

    // 内部変数
    private ConstellationData currentData;
    private float currentPanelOffset;

    protected override void Start()
    {
        // 初期位置へ飛ばす
        if (detailPanel != null) detailPanel.anchoredPosition = new Vector2(0, -detailPanelOffset);
        if (postStarPanel != null) postStarPanel.anchoredPosition = new Vector2(0, -postStarPanelOffset);
        if (replyPanel != null) replyPanel.anchoredPosition = new Vector2(0, -replyPanelOffset);
    }

    // =================================================
    // パネル切り替えロジック
    // =================================================
    private void SwitchPanel(RectTransform targetPanel, float targetOffset)
    {
        // 開いているパネルがターゲットと違う場合、古いパネルを隠す
        if (currentPanel != null && currentPanel != targetPanel)
        {
            float oldOffset = 0f;
            if (currentPanel == detailPanel) oldOffset = detailPanelOffset;
            else if (currentPanel == postStarPanel) oldOffset = postStarPanelOffset;
            else if (currentPanel == replyPanel) oldOffset = replyPanelOffset;

            // 即座に隠す
            currentPanel.anchoredPosition = new Vector2(0, -oldOffset);
        }

        // 新しいパネルをセット
        currentPanel = targetPanel;
        currentPanelOffset = targetOffset;

        // アニメーション開始
        if (currentAnimation != null) StopCoroutine(currentAnimation);
        currentAnimation = StartCoroutine(SlidePanel(0)); // 画面内(Y=0)へ
    }

    // =================================================
    // 詳細画面 (DetailPanel)
    // =================================================
    public void ShowDetail(ConstellationData data)
    {
        currentData = data;

        if (nameText) nameText.text = data.constellationName;
        if (descriptionText) descriptionText.text = data.description;

        // ★重要: ScrollViewの中身を一度空にして、データから再生成する
        // これにより、さっき追加したコメントも正しくScrollViewの中に表示されます
        if (listContentRoot)
        {
            foreach (Transform child in listContentRoot) Destroy(child.gameObject);

            if (data.root != null && data.root.children != null)
            {
                foreach (var childComment in data.root.children)
                {
                    // ここで Instantiate することで、ScrollViewの中に綺麗に並びます
                    var item = Instantiate(commentItemPrefab, listContentRoot);
                    item.Setup(childComment.GetContent(), Color.cyan);
                }
            }
        }

        SwitchPanel(detailPanel, detailPanelOffset);
    }

    // 返信ボタン (DetailPanel -> ReplyPanel)
    public void OnReplyButtonClicked()
    {
        if (replyContentInput) replyContentInput.text = "";
        SwitchPanel(replyPanel, replyPanelOffset);
    }

    // =================================================
    // 返信機能 (ReplyPanel)
    // =================================================
    public void OnSendReplyClicked()
    {
        if (replyContentInput == null || string.IsNullOrEmpty(replyContentInput.text)) return;
        if (currentData == null) return;

        string text = replyContentInput.text;

        // 1. コメントデータを作成 (Junya.Commentクラス)
        Comment newComment = new Comment(text);

        // 2. ConstellationData にデータを追加 (Attach修正済み)
        currentData.Attach(ref newComment);

        Debug.Log("返信データ追加完了: " + text);

        // 3. 入力欄クリア
        replyContentInput.text = "";

        // 4. ★詳細画面を再表示する
        // これにより SwitchPanel が呼ばれ、
        // ReplyPanel は即座に replyPanelOffset の位置(画面外)へ飛び、
        // DetailPanel がアニメーションして出てきます。
        // さらに ShowDetail の中でリストが再生成されるので、自分のコメントが表示されます。
        ShowDetail(currentData);
    }

    // =================================================
    // 流れ星機能
    // =================================================
    public void ShowPostStarPanel()
    {
        if (postContentInput) postContentInput.text = "";
        SwitchPanel(postStarPanel, postStarPanelOffset);
    }

    public void OnPostStarClicked()
    {
        if (postContentInput == null) return;
        string msg = postContentInput.text;
        if (string.IsNullOrEmpty(msg)) return;

        ShootingStarData data = new ShootingStarData(msg, "自分");
        if (ShootingStarManager.instance != null)
        {
            ShootingStarManager.instance.SpawnStar(data);
        }

        OnCloseButtonClicked();
    }

    // =================================================
    // 閉じる処理
    // =================================================
    public override void OnCloseButtonClicked()
    {
        // 返信画面で「閉じる」を押したときは、詳細に戻るのが自然
        if (currentPanel == replyPanel && currentData != null)
        {
            ShowDetail(currentData);
            return;
        }

        if (currentPanel == null) return;
        if (currentAnimation != null) StopCoroutine(currentAnimation);

        // 現在のパネル設定値を使って隠す
        currentAnimation = StartCoroutine(SlidePanel(-currentPanelOffset));

        if (cameraController != null) cameraController.ResetView();
    }
}