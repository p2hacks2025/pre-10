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

    [Header("投稿演出")]
    [SerializeField] private CanvasGroup postMessageCanvasGroup; // 「投稿しました！」のCanvasGroup
    [SerializeField] private float fadeDuration = 1.5f;          // フェードにかける時間

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

        // 1. 星がない場合
        if (!editor.HasStars)
        {
            editor.ShowWarning("星が一つもありません！\n画面をタップして星を作ってください");
            return;
        }

        // 2. 線がない場合 (★追加)
        if (!editor.HasConnections)
        {
            editor.ShowWarning("線が引かれていません！\n星をつないで星座にしてください");
            return;
        }

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

        //SaveToLocal(data);
        SaveToFireBase(data);
        StartCoroutine(PostSequence(data));
    }
/// <summary>
///  ローカル保存
/// </summary>
/// <param name="newData"></param>
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

    private void SaveToFireBase(ConstellationData newData)
    {
        if (FirebaseManager.instance != null)
        {
            // 保存が終わったらログを出す
            FirebaseManager.instance.SaveConstellation(newData, (success) => {
                if (success) Debug.Log("【Manual】クラウド保存完了！");
            });
        }
        else
        {
            Debug.LogError("FirebaseManagerがいません！");
        }
    }

    private IEnumerator PostSequence(ConstellationData data)
    {
        Debug.Log($"投稿完了: {data.constellationName}");
        PlayerPrefs.SetString("NextFocusGUID", data.guid);
        // 「自分の星座リスト」にこのGUIDを追加保存する
        SaveMyConstellationGuid(data.guid);
        PlayerPrefs.Save();
        OnCloseButtonClicked();  //パネルを閉じる
        // フェードイン演出
        if (postMessageCanvasGroup != null)
        {
            // まず表示状態にして、完全に透明にする
            postMessageCanvasGroup.gameObject.SetActive(true);
            postMessageCanvasGroup.alpha = 0f;

            float timer = 0f;

            // 指定した時間をかけて alpha を 0 から 1 にする
            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                postMessageCanvasGroup.alpha = Mathf.Lerp(0f, 1f, timer / fadeDuration);
                yield return null; // 1フレーム待つ
            }

            // 念のため最後に確実に1にする
            postMessageCanvasGroup.alpha = 1f;

            // 文字が見えきってから少しだけ余韻を持たせる（0.5秒待機）
            yield return new WaitForSeconds(0.5f);
        }
        else
        {
            // 設定し忘れたとき用（今まで通りの待機）
            yield return new WaitForSeconds(1.0f);
        }

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