using UnityEngine;
using UnityEngine.SceneManagement; // シーン移動に必要

public class SceneNavigator : MonoBehaviour
{

    //外部からのアクセスを可能にする
    public static SceneNavigator Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(this.gameObject);
    }

    // 写真シーンへ移動
    public void LoadPhotoScene()
    {
        SceneManager.LoadScene("PhotoScene");
    }

    // 手書きシーンへ移動
    public void LoadManualScene()
    {
        SceneManager.LoadScene("ManualScene");
    }

    public void LoadNightSkyScene()
    {
        SceneManager.LoadScene("NightSkyScene");
    }

    // タイトルへ戻る（各シーンの「戻る」ボタン用）
    public void LoadTitleScene()
    {
        SceneManager.LoadScene("TitleScene");
    }
}