using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager instance;

    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeDuration = 1.0f;
    [SerializeField] private float zoomDuration = 2.0f;

    void Awake()
    {
        if (instance == null) { instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }
    }

    void Start()
    {
        FadeIn();
    }

    public void LoadScene(string sceneName)
    {
        // フェードアウト(0->1)して、終わったらシーンロード
        fadeImage.gameObject.SetActive(true);
        fadeImage.DOFade(1f, fadeDuration).OnComplete(() => {
            SceneManager.LoadScene(sceneName);
            // ロード後にフェードイン
            SceneManager.sceneLoaded += OnSceneLoaded;
        });
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        FadeIn();
    }

    public void FadeIn()
    {
        fadeImage.gameObject.SetActive(true);
        fadeImage.DOFade(0f, fadeDuration).OnComplete(() => fadeImage.gameObject.SetActive(false));
    }

    // ★ズーム付き遷移もDOTweenならシンプル
    public void LoadSceneWithZoom(string sceneName, Vector3 targetPos)
    {
        Camera cam = Camera.main;
        fadeImage.gameObject.SetActive(true);

        // シーケンスを作成して同時実行
        Sequence seq = DOTween.Sequence();
        seq.Join(fadeImage.DOFade(1f, zoomDuration));
        seq.Join(cam.transform.DOMove(new Vector3(targetPos.x, targetPos.y, cam.transform.position.z), zoomDuration));
        seq.Join(cam.DOOrthoSize(cam.orthographicSize * 0.1f, zoomDuration));

        seq.OnComplete(() => {
            SceneManager.LoadScene(sceneName);
            SceneManager.sceneLoaded += OnSceneLoaded;
        });
    }
}