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

    [Header("Editor Reference")]
    [SerializeField] private ManualConstellationEditor editor;

    [Header("Buttons")]
    [SerializeField] private Button undoButton;
    [SerializeField] private Button redoButton;

    [Header("Panels")]
    [SerializeField] private RectTransform postPanel;
    [SerializeField] private RectTransform deletePopupPanel; // GameObjectからRectTransformに変更

    [Header("Panel Settings")]
    [SerializeField] private float panelHiddenOffset = -1200f;
    [SerializeField] private float panelShowOffset = 0f;

    [Header("Post Panel UI")]
    [SerializeField] private TMP_InputField sharedInputField;
    [SerializeField] private TextMeshProUGUI inputTitleText;
    [SerializeField] private TextMeshProUGUI placeholderText;

    [Header("Delete Button Settings")]
    [SerializeField] private float longPressDuration = 0.8f;

    private bool isDeletingPress = false;
    private float deletePressTimer = 0f;
    private bool longPressTriggered = false;

    private InputState currentState = InputState.Name;
    private string tempConstellationName = "";

    // ★追加: 外部から「今パネルが開いているか」を確認するためのプロパティ
    public bool HasActivePanel => currentPanel != null;

    protected override void Start()
    {
        base.Start();

        // 初期化：パネルを隠す
        if (postPanel != null)
        {
            postPanel.anchoredPosition = new Vector2(0, panelHiddenOffset);
            postPanel.gameObject.SetActive(false);
        }
        if (deletePopupPanel != null)
        {
            deletePopupPanel.anchoredPosition = new Vector2(0, panelHiddenOffset);
            deletePopupPanel.gameObject.SetActive(false);
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

    // ================================================================================
    // パネル操作
    // ================================================================================

    private void SwitchPanel(RectTransform targetPanel, float targetOffset, System.Action onComplete = null)
    {
        if (currentPanel != null && currentPanel != targetPanel)
        {
            // 既に開いているパネルがあれば隠す
            // ここでは即座に隠していますが、アニメーションさせてもOK
            currentPanel.anchoredPosition = new Vector2(0, panelHiddenOffset);
            currentPanel.gameObject.SetActive(false);
        }

        currentPanel = targetPanel;

        if (currentPanel != null)
        {
            currentPanel.gameObject.SetActive(true);
            if (currentAnimation != null) StopCoroutine(currentAnimation);
            currentAnimation = StartCoroutine(SlidePanelWithCallback(targetOffset, onComplete));
        }
    }

    private IEnumerator SlidePanelWithCallback(float targetY, System.Action onComplete)
    {
        yield return StartCoroutine(SlidePanel(targetY));
        onComplete?.Invoke();
    }

    // ================================================================================
    // UI Event Handlers
    // ================================================================================

    public void OnUndoButtonClicked() { if (editor != null) editor.Undo(); }
    public void OnRedoButtonClicked() { if (editor != null) editor.Redo(); }

    public void OnDeleteButtonDown()
    {
        if (currentPanel != null) return;
        isDeletingPress = true;
        deletePressTimer = 0f;
        longPressTriggered = false;
    }

    public void OnDeleteButtonUp()
    {
        if (isDeletingPress && !longPressTriggered)
        {
            if (editor != null) editor.DeleteSelected();
        }
        isDeletingPress = false;
        deletePressTimer = 0f;
    }

    private void ShowDeletePopup()
    {
        if (deletePopupPanel != null)
        {
            // ★修正: SetActiveだけでなく、SwitchPanelを使ってスライドインさせる
            SwitchPanel(deletePopupPanel, panelShowOffset);
            isDeletingPress = false;
        }
    }

    public void OnDeletePopupOkClicked()
    {
        if (editor != null) editor.DeleteAll();
        ClosePopup(); // パネルを閉じる
    }

    public void OnEditorOkButtonClicked()
    {
        if (currentPanel != null) return;

        if (postPanel != null)
        {
            SetupInputState(InputState.Name);
            // 投稿パネルをスライドイン
            SwitchPanel(postPanel, panelShowOffset);
        }
    }

    // ================================================================================
    // 投稿フロー
    // ================================================================================

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
                break;
        }
    }

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

    private void FinalizePost(string description)
    {
        ConstellationData data = editor.GetConstellationData();
        data.constellationName = tempConstellationName;
        data.description = description;

        SaveToLocal(data);
        StartCoroutine(PostSequence(data));
    }

    private void SaveToLocal(ConstellationData newData)
    {
        ConstellationListWrapper wrapper = new ConstellationListWrapper();
        if (PlayerPrefs.HasKey("LocalSaveList"))
        {
            string json = PlayerPrefs.GetString("LocalSaveList");
            wrapper = JsonUtility.FromJson<ConstellationListWrapper>(json);
        }
        if (wrapper.list == null) wrapper.list = new List<ConstellationData>();

        wrapper.list.Add(newData);
        string newJson = JsonUtility.ToJson(wrapper);
        PlayerPrefs.SetString("LocalSaveList", newJson);
        PlayerPrefs.Save();
    }

    private IEnumerator PostSequence(ConstellationData data)
    {
        Debug.Log($"投稿完了: {data.constellationName}");
        yield return new WaitForSeconds(1.0f);
        SceneManager.LoadScene("SkyScene");
    }

    // ================================================================================
    // 共通処理
    // ================================================================================

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
        // ★修正: スライドアウトさせてから非表示にする
        SwitchPanel(null, panelHiddenOffset, () => {
            // アニメーション完了後の処理が必要ならここに書く
        });
    }
}