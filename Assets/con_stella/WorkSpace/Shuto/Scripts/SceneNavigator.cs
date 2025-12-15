using UnityEngine;
using UnityEngine.SceneManagement; // シーン移動に必要

public class SceneNavigator : MonoBehaviour
{
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

    // タイトルへ戻る（各シーンの「戻る」ボタン用）
    public void LoadTitleScene()
    {
        SceneManager.LoadScene("TitleScene");
    }
}