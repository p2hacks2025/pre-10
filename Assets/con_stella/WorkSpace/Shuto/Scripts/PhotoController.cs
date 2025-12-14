using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PhotoController : MonoBehaviour
{
    [Header("UI設定")]
    [SerializeField] private RawImage previewImage; // 撮影した画像を表示する場所
    [SerializeField] private Text statusText;       // (あれば)状況を表示するテキスト

    // 画像処理（リサイズ）の最大サイズ。大きすぎるとメモリ不足で落ちるため制限する。
    private const int MaxImageSize = 1024;

    // 保持用のテクスチャ（これを画像認識に渡す）
    private Texture2D currentTexture;

    void Start()
    {
        // プレビューの縦横比を保つ設定
        if (previewImage != null)
            previewImage.rectTransform.sizeDelta = new Vector2(MaxImageSize, MaxImageSize);
    }

    /// <summary>
    /// カメラボタンに割り当てる関数
    /// </summary>
    public void OnClickCamera()
    {
        if (NativeCamera.IsCameraBusy()) return; // 連打防止

        UpdateStatus("カメラを起動します...");

        // カメラ起動
        NativeCamera.TakePicture((path) =>
        {
            if (path != null)
            {
                // 撮影成功！画像をロードして表示
                LoadAndShowImage(path);
            }
            else
            {
                UpdateStatus("キャンセルされました");
            }
        }, maxSize: MaxImageSize); // ここでリサイズ指定
    }

    /// <summary>
    /// ギャラリーボタンに割り当てる関数
    /// </summary>
    public void OnClickGallery()
    {
        if (NativeGallery.IsMediaPickerBusy()) return;

        UpdateStatus("アルバムを開きます...");

        // ギャラリー起動
        NativeGallery.GetImageFromGallery((path) =>
        {
            if (path != null)
            {
                // 選択成功！画像をロードして表示
                LoadAndShowImage(path);
            }
            else
            {
                UpdateStatus("キャンセルされました");
            }
        }); // ギャラリー側はLoad時にリサイズする
    }

    /// <summary>
    /// 画像を読み込んで表示する共通処理
    /// </summary>
    /// <param name="path"></param>
    private void LoadAndShowImage(string path)
    {
        if (string.IsNullOrEmpty(path)) return;

        // メモリ節約のため、前の画像があれば破棄する
        if (currentTexture != null) Destroy(currentTexture);

        // 画像をテクスチャとして読み込む（ここでもMaxサイズを指定して回転ズレを修正）
        currentTexture = NativeGallery.LoadImageAtPath(path, MaxImageSize, false);

        if (currentTexture == null)
        {
            UpdateStatus("画像の読み込みに失敗しました");
            return;
        }

        // プレビューにセット
        previewImage.texture = currentTexture;

        // アスペクト比修正（重要）
        previewImage.SetNativeSize();
        float aspect = (float)currentTexture.width / currentTexture.height;
        // 幅に合わせて高さを調整（簡易的なフィッティング）
        previewImage.rectTransform.sizeDelta = new Vector2(800, 800 / aspect);

        UpdateStatus("画像取得完了！");

        // ★★★ ここで「画像認識プログラム」を呼ぶ！ ★★★
        // StartImageRecognition(currentTexture);
    }

    /// <summary>
    /// デバッグ用テキスト表示
    /// </summary>
    /// <param name="msg"></param>
    private void UpdateStatus(string msg)
    {
        if (statusText != null) statusText.text = msg;
        Debug.Log(msg);
    }
}