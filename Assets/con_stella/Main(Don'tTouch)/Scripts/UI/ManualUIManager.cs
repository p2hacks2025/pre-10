using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class ManualUIManager : UIBaseManager
{
    public enum InputState
    {
        Name,
        Description
    }

    [Header("描画エディター")]
    [SerializeField] private ManualConstellationEditor editor;

    [Header("ボタン")]
    [SerializeField] private Button undoButton;
    [SerializeField] private Button redoButton;

    [Header("パネル")]
    [SerializeField] private RectTransform postPanel;
    [SerializeField] private RectTransform deletePopupPanel;

    [Header("パネルセッティング")]
    [SerializeField] private float panelHiddenOffset = -1200f;
    [SerializeField] private float panelShowOffset = 0f;

    [Header("投稿パネルUI")]
    [SerializeField] private TMP_InputField sharedInputField;
    [SerializeField] private TextMeshProUGUI inputTitleText;
    [SerializeField] private TextMeshProUGUI placeholderText;

    [Header("削除ボタン設定")]
    [SerializeField] private float longPressDuration = 0.8f;
    private bool isDeletingPress = false;
    private float deletePressTimer = 0f;
    private bool longPressTriggered = false;

    private InputState currentState = InputState.Name;
    private string tempConstellationName = "";

    // 外部から「今パネルが開いているか」を確認するためのプロパティ
    public bool HasActivePanel => currentPanel != null;

    protected override void Start()
    {
        base.Start();

        // 初期化：パネルを隠す
        if (postPanel != null)
        {
            postPanel.anchoredPosition = new Vector2(0, panelHiddenOffset);
        }
        if (deletePopupPanel != null)
        {
            deletePopupPanel.anchoredPosition = new Vector2(0, panelHiddenOffset);
        }

        UpdateHistoryButtons();
    }

    void Update()
    {
        if (isDeletingPress)
        {
            deletePressTimer += Time.deltaTime;
            if (deletePressTimer >= longPressDuration && !longPressTriggered)
            {
                longPressTriggered = true;
                ShowDeletePopup();
            }
        }
        UpdateHistoryButtons();
    }

    private void UpdateHistoryButtons()
    {
        if (editor != null)
        {
            if (undoButton != null) undoButton.interactable = editor.CanUndo;
            if (redoButton != null) redoButton.interactable = editor.CanRedo;
        }
    }

    //パネル操作
    private void SwitchPanel(RectTransform targetPanel, float targetOffset, System.Action onComplete = null)
    {
        if (currentPanel != null && currentPanel != targetPanel)
        {
            // 既に開いているパネルがあれば隠す
            currentPanel.anchoredPosition = new Vector2(0, panelHiddenOffset);
        }

        currentPanel = targetPanel;

        if (currentPanel != null)
        {
            DoSlide(currentPanel, targetOffset, onComplete);
        }
    }

    public void OnUndoButtonClicked() { if (editor != null) editor.Undo(); }
    public void OnRedoButtonClicked() { if (editor != null) editor.Redo(); }

    //削除ボタンを押したとき
    public void OnDeleteButtonDown()
    {
        if (currentPanel != null) return;
        isDeletingPress = true;
        deletePressTimer = 0f;
        longPressTriggered = false;
    }

    //削除ボタンを離したとき
    public void OnDeleteButtonUp()
    {
        if (isDeletingPress && !longPressTriggered)
        {
            if (editor != null) editor.DeleteSelected();
        }
        isDeletingPress = false;
        deletePressTimer = 0f;
    }

    //全削除警告ポップ
    private void ShowDeletePopup()
    {
        if (deletePopupPanel != null)
        {
            SwitchPanel(deletePopupPanel, panelShowOffset);
            isDeletingPress = false;
        }
    }

    //全削除OKボタン
    public void OnDeletePopupOkClicked()
    {
        if (editor != null) editor.DeleteAll();
        ClosePopup(); // パネルを閉じる
    }

    //手描き完了ボタン
    public void OnEditorOkButtonClicked()
    {
        if (currentPanel != null) return;

        // 1星がない場合
        if (!editor.HasStars)
        {
            editor.ShowWarning("星が一つもありません！");
            return;
        }

        // 線がない場合
        if (!editor.HasConnections)
        {
            editor.ShowWarning("線が引かれていません！");
            return;
        }

        if (postPanel != null)
        {
            SetupInputState(InputState.Name);
            // 投稿パネルをスライドイン
            SwitchPanel(postPanel, panelShowOffset);
        }
    }

    //投稿フロー
    public void OnPostPanelOkClicked()
    {
        if (sharedInputField == null) return;
        string inputText = sharedInputField.text;

        switch (currentState)
        {
            case InputState.Name:
                if (string.IsNullOrEmpty(inputText))
                {
                    Debug.LogWarning("名前を入力してください");
                    return;
                }
                tempConstellationName = inputText;
                SetupInputState(InputState.Description);
                break;

            case InputState.Description:
                FinalizePost(inputText);
                Debug.Log("ボタンは反応してます");
                break;
        }
    }

    //InputField内のテキスト更新（ガイドテキスト）
    private void SetupInputState(InputState state)
    {
        currentState = state;
        if (sharedInputField != null)
        {
            sharedInputField.text = "";
            if (currentState == InputState.Name)
            {
                if (inputTitleText != null) inputTitleText.text = "星座の名前";
                if (placeholderText != null) placeholderText.text = "名前を入力...";
            }
            else
            {
                if (inputTitleText != null) inputTitleText.text = "星座の説明";
                if (placeholderText != null) placeholderText.text = "説明を入力(任意)...";
            }
        }
    }

    //投稿ボタン
    private void FinalizePost(string description)
    {
        ConstellationData data = editor.GetConstellationData();
        data.constellationName = tempConstellationName;
        data.description = description;

        //保存処理
        DataManager.instance.SaveConstellation(data); // クラウド・ローカル両方
        DataManager.instance.SaveMyConstellationGuid(data.guid);
        DataManager.instance.SaveNextFocusGuid(data.guid);

        OnCloseButtonClicked();
        PostSequenceManager.instance.GoToSky();
    }

    public override void OnCloseButtonClicked()
    {
        // 削除パネルが開いている場合
        if (deletePopupPanel != null && currentPanel == deletePopupPanel)
        {
            ClosePopup();
        }
        // 投稿パネルが開いている場合
        else if (currentPanel == postPanel)
        {
            ClosePopup();
        }
        // 何も開いていなければシーン遷移
        else
        {
            SceneManager.LoadScene("SkyScene");
        }
    }

    private void ClosePopup()
    {
        SwitchPanel(null, panelHiddenOffset);
    }
}