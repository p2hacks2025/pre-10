using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

public class PhotoUIManager : UIBaseManager
{
    [Header("UIパーツ（入力用）")]
    [SerializeField] private RectTransform conSettingPanel; // 星座設定パネル
    [SerializeField] private TMP_InputField inputField;     // 使い回す入力欄
    [SerializeField] private TextMeshProUGUI titleText;     // 「名前を入力」「説明を入力」のタイトル
    [SerializeField] private TextMeshProUGUI placeholderText; // プレースホルダー（入力例）
    [SerializeField] private TextMeshProUGUI buttonText;      // 「次へ」「保存」などのボタン文字

    [Header("投稿演出")]
    [SerializeField] private CanvasGroup postMessageCanvasGroup; // フェード用
    [SerializeField] private float fadeDuration = 1.5f;

    [Header("参照")]
    [SerializeField] private ConstellationGenerator generator; // 保存処理を持つスクリプト
    [SerializeField] private PhotoController photoController;

    // 内部データ保持用
    private string tempName = "";
    private string tempDescription = "";

    private enum InputStep
    {
        InputName,        //星座名入力
        InputDescription  //説明入力
    }

    private InputStep currentStep;

    protected override void Start()
    {
        base.Start();

        // 最初は隠しておく（念の為）
        if (conSettingPanel != null) conSettingPanel.anchoredPosition = new Vector2(0, -panelHeight);
        if (postMessageCanvasGroup != null)
        {
            postMessageCanvasGroup.alpha = 0f;
            postMessageCanvasGroup.gameObject.SetActive(false);
        }
    }

    // 入力パネルを表示開始する
    public void ShowSettingPanel()
    {
        // 1. ベースマネージャーに渡す
        currentPanel = conSettingPanel;

        // 2. 状態を「名前入力」にリセット
        currentStep = InputStep.InputName;
        tempName = "";
        tempDescription = "";

        // 3. UIの見た目を更新
        UpdateUIDisplay();

        // 4. パネルを出すアニメーション開始
        if (currentAnimation != null) StopCoroutine(currentAnimation);
        currentAnimation = StartCoroutine(SlidePanel(0));
    }

    // キャンセル（やり直し）ボタン
    public void OnCancelButtonClicked()
    {
        // 1. パネルを閉じる
        OnCloseButtonClicked();

        // 2. システム全体をリセット（画像消去など）
        if (photoController != null)
        {
            photoController.ResetSystem();
        }
    }

    //OKボタンを押したときの処理
    public void OnOKButtonClicked()
    {
        string currentInput = inputField.text;

        switch (currentStep)
        {
            case InputStep.InputName:
                // --- 名前入力の処理 ---
                if (string.IsNullOrEmpty(currentInput))
                {
                    Debug.LogWarning("名前が空です");
                    return;
                }

                tempName = currentInput; // 名前を一時保存

                // 次のステップ（説明入力）へ進む
                currentStep = InputStep.InputDescription;
                UpdateUIDisplay();
                break;

            case InputStep.InputDescription:
                // --- 説明入力の処理 ---
                tempDescription = currentInput; // 説明を一時保存

                // 最終保存処理を実行
                FinalizeSave();
                break;
        }
    }

    // 現在のステップに合わせてテキストなどを書き換える
    private void UpdateUIDisplay()
    {
        // 入力欄をクリア
        inputField.text = "";

        if (currentStep == InputStep.InputName)
        {
            if (titleText != null) titleText.text = "星座の名前を決めてください";
            if (placeholderText != null) placeholderText.text = "例：オリオン座";
            if (buttonText != null) buttonText.text = "次へ";
        }
        else if (currentStep == InputStep.InputDescription)
        {
            if (titleText != null) titleText.text = "星座の説明を入力してください";
            if (placeholderText != null) placeholderText.text = "例：冬の代表的な星座です";
            if (buttonText != null) buttonText.text = "保存";
        }

        // 入力欄にフォーカス（スマホキーボード用）
        inputField.ActivateInputField();
    }

    // Generatorにデータを渡して保存し、パネルを閉じる
    private void FinalizeSave()
    {
        if (generator != null)
        {
            // データを登録して、結果(data)を受け取る
            ConstellationData data = generator.RegisterConstellationData(tempName, tempDescription);

            // ★修正: データが正しく返ってきたら、投稿演出(PostSequence)を開始する
            if (data != null)
            {
                StartCoroutine(PostSequence(data));
            }
            else
            {
                // データ生成失敗時は閉じるだけにする（エラーハンドリング）
                OnCloseButtonClicked();
            }
        }
        else
        {
            Debug.LogError("ConstellationGeneratorがセットされていません！");
            OnCloseButtonClicked();
        }
    }

    private IEnumerator PostSequence(ConstellationData data)
    {
        OnCloseButtonClicked();  //パネルを

        yield return PostSequenceManager.instance.PlayPostSequence(
        data,
        postMessageCanvasGroup,
        fadeDuration
    );
    }

    // ★追加: GUIDをカンマ区切りで保存するヘルパー関数
    // (ManualUIManager と PhotoUIManager の両方の末尾に追加してください)
    private void SaveMyConstellationGuid(string guid)
    {
        string key = "MyConstellationGuids";
        string currentSaved = PlayerPrefs.GetString(key, "");

        // まだリストになければ追加
        if (!currentSaved.Contains(guid))
        {
            if (string.IsNullOrEmpty(currentSaved))
            {
                currentSaved = guid;
            }
            else
            {
                currentSaved += "," + guid;
            }
            PlayerPrefs.SetString(key, currentSaved);
            PlayerPrefs.Save();
        }
    }
}