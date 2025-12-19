using UnityEngine;
using UnityEngine.SceneManagement;

public class HomeNavigator : MonoBehaviour
{
    //投稿シーンへ遷移
    public void LoadPostScene()
    {
        SceneManager.LoadScene("PostScene");
    }

    //アルバムシーンへ遷移
    public void LoadAlbum()
    {
        SceneManager.LoadScene("AlbumScene");
    }

    // 夜空シーンへ移動
    public void LoadNightSkyScene()
    {
        SceneManager.LoadScene("SkyScene");
    }
}