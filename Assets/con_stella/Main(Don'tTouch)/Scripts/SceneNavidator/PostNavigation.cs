using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PostMethodNavigator : MonoBehaviour
{
    [Header("ボタン")]
    [SerializeField] private Button btnCamera; // 撮る
    [SerializeField] private Button btnDraw;   // 描く
    [SerializeField] private Button btnImport; // 読み込む

    [Header("遷移先シーン名")]
    [SerializeField] private string sceneCamera = "CameraScene"; // 撮影シーンの名前
    [SerializeField] private string sceneDraw = "DrawScene";   // お絵描きシーンの名前
    [SerializeField] private string sceneImport = "ImportScene"; // アルバム読み込みシーンの名前

    void Start()
    {
        // ボタンが押されたら、それぞれのシーンへ移動する設定
        if (btnCamera) btnCamera.onClick.AddListener(() => GoToScene(sceneCamera));
        if (btnDraw) btnDraw.onClick.AddListener(() => GoToScene(sceneDraw));
        if (btnImport) btnImport.onClick.AddListener(() => GoToScene(sceneImport));
    }

    private void GoToScene(string sceneName)
    {
        // フェード演出マネージャーにお願いして移動
        if (SceneTransitionManager.instance != null)
        {
            SceneTransitionManager.instance.LoadScene(sceneName);
        }
        else
        {
            // マネージャーがいない場合（テスト用）
            SceneManager.LoadScene(sceneName);
        }
    }
}