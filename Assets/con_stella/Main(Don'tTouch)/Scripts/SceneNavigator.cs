using UnityEngine;
using UnityEngine.SceneManagement;

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

    // 夜空シーンへ移動
    public void LoadNightSkyScene()
    {
        SceneManager.LoadScene("SkyScene");
    }

    // タイトルへ戻る
    public void LoadTitleScene()
    {
        SceneManager.LoadScene("TitleScene");
    }
}