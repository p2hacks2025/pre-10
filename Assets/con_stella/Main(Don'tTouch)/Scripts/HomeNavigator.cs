using UnityEngine;

public class HomeNavigator : MonoBehaviour
{
    [Header("ズーム先のターゲット位置")]
    // Inspectorで、シーン上のオブジェクト（望遠鏡やドアなど）を指定してください
    [SerializeField] private Transform observeTarget; // 観測（望遠鏡の位置）
    [SerializeField] private Transform postTarget;    // 投稿（窓や机の位置）
    [SerializeField] private Transform albumTarget;   // アルバム（本棚の位置）

    // 投稿シーンへ遷移 (ズームあり)
    public void LoadPostScene()
    {
        if (SceneTransitionManager.instance != null && postTarget != null)
        {
            // ターゲットの位置に向かってズームしながら遷移
            SceneTransitionManager.instance.LoadSceneWithZoom("PostScene", postTarget.position);
        }
        else
        {
            // マネージャーがない場合の保険
            SceneTransitionManager.instance.LoadScene("PostScene");
        }
    }

    // アルバムシーンへ遷移 (ズームあり)
    public void LoadAlbum()
    {
        if (SceneTransitionManager.instance != null && albumTarget != null)
        {
            SceneTransitionManager.instance.LoadSceneWithZoom("AlbumScene", albumTarget.position);
        }
        else
        {
            SceneTransitionManager.instance.LoadScene("AlbumScene");
        }
    }

    // 夜空シーンへ移動 (ズームあり)
    public void LoadNightSkyScene()
    {
        if (SceneTransitionManager.instance != null && observeTarget != null)
        {
            SceneTransitionManager.instance.LoadSceneWithZoom("SkyScene", observeTarget.position);
        }
        else
        {
            SceneTransitionManager.instance.LoadScene("SkyScene");
        }
    }

    // ホームに戻るなどの「ただのフェード」用
    public void LoadHomeSimple()
    {
        if (SceneTransitionManager.instance != null)
        {
            SceneTransitionManager.instance.LoadScene("HomeScene");
        }
    }
}