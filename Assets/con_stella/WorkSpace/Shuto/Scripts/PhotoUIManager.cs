using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class PhotoUIManager : UIBaseManager
{
    [Header("UIパーツ（入力用）")]
    [SerializeField] private RectTransform conSettingPanel; // 星座設定パネル
    [SerializeField] private TMP_InputField inputField;     // 使い回す入力欄
    [SerializeField] private TextMeshProUGUI titleText;     // 「名前を入力」「説明を入力」のタイトル
    [SerializeField] private TextMeshProUGUI placeholderText; // プレースホルダー（入力例）
    [SerializeField] private TextMeshProUGUI buttonText;      // 「次へ」「保存」などのボタン文字

    [Header("参照")]
    [SerializeField] private ConstellationGenerator generator; // 保存処理を持つスクリプト

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
        conSettingPanel.anchoredPosition = new Vector2(0, -panelHeight);
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
            // Generator側のメソッドを呼ぶ
            generator.RegisterConstellationData(tempName, tempDescription);
        }
        else
        {
            Debug.LogError("ConstellationGeneratorがセットされていません！");
        }

        // パネルを閉じる（親クラスのメソッド）
        OnCloseButtonClicked();
    }
}