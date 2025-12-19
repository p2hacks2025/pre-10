using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class PhotoController : MonoBehaviour
{
    [Header("UI設定")]
    [SerializeField] private RawImage previewImage;
    [SerializeField] private TextMeshProUGUI statusText;

    [SerializeField] private GameObject frameObject;

    [Header("連携設定")]
    [SerializeField] private EdgeDetector edgeDetector;
    [SerializeField] private ConstellationGenerator constellationGenerator;
    [SerializeField] private CanvasGroup constellationCanvasGroup;
    [SerializeField] private PhotoUIManager uiManager;

    [Header("アニメーション設定")]
    [SerializeField] private float fadeDuration = 1.5f;

    private const int MaxImageSize = 1024;
    private Texture2D currentTexture;
    private Color defaultPhotoColor;

    void Start()
    {
        if (previewImage != null)
        {
            previewImage.rectTransform.sizeDelta = new Vector2(MaxImageSize, MaxImageSize);
            defaultPhotoColor = new Color(1f, 1f, 1f, 0f);
            previewImage.color = defaultPhotoColor;
        }

        if (constellationCanvasGroup != null)
        {
            constellationCanvasGroup.alpha = 0f;
        }
        if (frameObject != null) frameObject.SetActive(true);
    }

    public void OnClickCamera()
    {
        if (NativeCamera.IsCameraBusy()) return;
        UpdateStatus("カメラを起動します...");
        NativeCamera.TakePicture((path) =>
        {
            if (path != null) LoadAndShowImage(path);
            else UpdateStatus("キャンセルされました");
        }, maxSize: MaxImageSize);
    }

    public void OnClickGallery()
    {
        if (NativeGallery.IsMediaPickerBusy()) return;
        UpdateStatus("アルバムを開きます...");
        NativeGallery.GetImageFromGallery((path) =>
        {
            if (path != null) LoadAndShowImage(path);
            else UpdateStatus("キャンセルされました");
        });
    }

    private void LoadAndShowImage(string path)
    {
        if (string.IsNullOrEmpty(path)) return;
        if (currentTexture != null) Destroy(currentTexture);

        currentTexture = NativeGallery.LoadImageAtPath(path, MaxImageSize, false);
        if (currentTexture == null)
        {
            UpdateStatus("画像の読み込みに失敗しました");
            return;
        }

        ResetViewBeforeAnimation();

        // プレビュー表示
        previewImage.texture = currentTexture;
        previewImage.SetNativeSize();
        float aspect = (float)currentTexture.width / currentTexture.height;
        // 横幅を800pxとして計算（この値を基準にスケール計算します）
        float displayWidth = 800f;
        previewImage.rectTransform.sizeDelta = new Vector2(displayWidth, displayWidth / aspect);
        
        // 写真を読み込んだ直後は、まず不透明(Alpha=1)にして表示する
        previewImage.color = Color.white;

        UpdateStatus("解析を開始します...");

        if (constellationGenerator != null && edgeDetector != null && constellationCanvasGroup != null)
        {
            Debug.Log($"【検問2】輪郭抽出開始");

            // 1. 画像ではなく「輪郭データ」をもらう
            List<List<Vector2>> contours = edgeDetector.DetectContours(currentTexture);

            Debug.Log($"【検問3】輪郭データ取得完了。輪郭数: {contours.Count}");

            // 2. 星座生成（座標合わせのためのスケール倍率を渡す）
            // EdgeDetector内では512pxにリサイズして処理しているため、表示サイズとの比率を計算
            float processSize = 512f;
            float scaleFactor = displayWidth / processSize;

            constellationGenerator.GenerateFromContours(contours, scaleFactor);

            // アニメーション開始
            StartCoroutine(PlayGenerationSequence());
        }
        else
        {
            Debug.LogError("必要なコンポーネントがセットされていません！Inspectorを確認してください。");
        }
    }

    private void ResetViewBeforeAnimation()
    {
        previewImage.color = new Color(1f, 1f, 1f, 0f);
        previewImage.texture = null; // テクスチャも外す
        if (constellationCanvasGroup != null) constellationCanvasGroup.alpha = 0f;
        // 枠線を表示状態に戻す
        if (frameObject != null) frameObject.SetActive(true);
    }

    private IEnumerator PlayGenerationSequence()
    {
        UpdateStatus("星座を生成中...");
        yield return null;

        // フェードイン
        UpdateStatus("星座が浮かび上がります");
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            constellationCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
            yield return null;
        }
        constellationCanvasGroup.alpha = 1f;

        yield return new WaitForSeconds(0.5f);

        // フェードアウト
        UpdateStatus("背景が消えていきます");
        elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            // 写真(previewImage)を透明にしていく
            float newAlpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            previewImage.color = new Color(1f, 1f, 1f, newAlpha);
            yield return null;
        }
        previewImage.color = new Color(defaultPhotoColor.r, defaultPhotoColor.g, defaultPhotoColor.b, 0f);

        uiManager.ShowSettingPanel();

        UpdateStatus("生成完了！");
    }

    // 全部リセットして最初に戻る機能
    public void ResetSystem()
    {
        // 画像を破棄
        if (currentTexture != null) Destroy(currentTexture);

        // 表示をリセット
        ResetViewBeforeAnimation();

        // 星座を消去
        if (constellationGenerator != null) constellationGenerator.ClearConstellation();

        // メッセージ更新
        UpdateStatus("写真を撮るか、アルバムから選んでください");
    }

    private void UpdateStatus(string msg)
    {
        if (statusText != null) statusText.text = msg;
        Debug.Log(msg);
    }
}