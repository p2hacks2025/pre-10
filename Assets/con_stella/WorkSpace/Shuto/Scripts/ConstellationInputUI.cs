using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Junya; // CommentManagerのため
using Cysharp.Threading.Tasks; // UniTaskのため

public class ConstellationInputUI : UIBaseManager
{
    [Header("UIパーツ")]
    [SerializeField] private RectTransform inputPanel;
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Button sendButton;
    [SerializeField] private Button cancelButton;

    private ConstellationData targetData;

    protected override void Start()
    {
        // 親クラスのStartでは隠す処理が入っている
        // 今回は「隠れている位置」と「出ている位置」の設定に注意
        base.Start();

        if (inputPanel != null)
        {
            currentPanel = inputPanel; // 親クラスに「これを動かすよ」と教える
            inputPanel.anchoredPosition = new Vector2(0, -panelHeight);
        }

        if (sendButton != null) sendButton.onClick.AddListener(OnSendClicked);
        if (cancelButton != null) cancelButton.onClick.AddListener(OnCancelClicked);
    }

    // 外部から呼ばれる：入力モード開始
    public void ShowInputForm(ConstellationData data)
    {
        targetData = data;

        // ★追加：動かすパネルをセット
        currentPanel = inputPanel;

        if (inputField != null)
        {
            inputField.text = "";
            inputField.ActivateInputField();
        }

        // 画面下から「にゅっ」と出す（親クラスのOpenを使う）
        Open();
    }

    public async void OnSendClicked()
    {
        if (inputField == null || string.IsNullOrEmpty(inputField.text)) return;

        string text = inputField.text;

        // 1. ConstellationDataに新しいコメントを追加する
        // ※JunyaさんのCommentクラスのコンストラクタに合わせてください
        Comment newComment = new Comment(text);

        // 2. 星座データにアタッチ（紐付け）
        if (targetData != null)
        {
            targetData.Attach(ref newComment);
        }

        Debug.Log($"コメント投稿: {text}");

        // --- 終了処理 ---
        inputField.text = "";
        Close(); // パネルを下げる
    }

    private void OnCancelClicked()
    {
        Close();
    }
}