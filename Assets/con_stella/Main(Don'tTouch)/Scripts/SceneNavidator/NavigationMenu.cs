using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using DG.Tweening;

public class NavigationMenu : MonoBehaviour
{
    [Header("UIパーツ")]
    [SerializeField] private Button openButton;
    [SerializeField] private Button closeBackground;
    [SerializeField] private GameObject menuPanel;

    [Header("各ボタン")]
    [SerializeField] private Button btnHome;
    [SerializeField] private Button btnObserve;
    [SerializeField] private Button btnPost;
    [SerializeField] private Button btnAlbum;

    [Header("遷移先シーン名")]
    [SerializeField] private string sceneHome = "HomeScene";
    [SerializeField] private string sceneObserve = "ObserveScene";
    [SerializeField] private string scenePost = "PostScene";
    [SerializeField] private string sceneAlbum = "AlbumScene";

    [Header("アニメーション設定")]
    [SerializeField] private float animDuration = 0.2f;

    private bool isOpen = false;
    private Coroutine currentAnim;

    void Start()
    {
        if (menuPanel != null)
        {
            menuPanel.transform.localScale = Vector3.zero;
            menuPanel.SetActive(false);
        }

        if (openButton) openButton.onClick.AddListener(ToggleMenu);
        if (closeBackground) closeBackground.onClick.AddListener(CloseMenu);

        // ボタンにリスナー登録
        if (btnHome) btnHome.onClick.AddListener(() => LoadScene(sceneHome));
        if (btnObserve) btnObserve.onClick.AddListener(() => LoadScene(sceneObserve));
        if (btnPost) btnPost.onClick.AddListener(() => LoadScene(scenePost));
        if (btnAlbum) btnAlbum.onClick.AddListener(() => LoadScene(sceneAlbum));
    }

    public void ToggleMenu()
    {
        if (isOpen) CloseMenu();
        else OpenMenu();
    }

    private void OpenMenu()
    {
        if (isOpen) return;
        isOpen = true;

        menuPanel.SetActive(true);
        if (closeBackground) closeBackground.gameObject.SetActive(true);

        menuPanel.transform.localScale = Vector3.zero;
        menuPanel.transform.DOScale(Vector3.one, animDuration).SetEase(Ease.OutBack);
    }

    private void CloseMenu()
    {
        if (!isOpen) return;
        isOpen = false;

        if (closeBackground) closeBackground.gameObject.SetActive(false);

        if (currentAnim != null) StopCoroutine(currentAnim);
        currentAnim = StartCoroutine(ScaleAnimation(Vector3.one, Vector3.zero, () =>
        {
            menuPanel.SetActive(false);
        }));
    }

    // ★ここを修正しました
    private void LoadScene(string sceneName)
    {
        // 今いるシーンと同じならメニューを閉じるだけ
        if (SceneManager.GetActiveScene().name == sceneName)
        {
            CloseMenu();
            return;
        }

        // ★修正: 自分でロードせず、演出担当(SceneTransitionManager)にお願いする
        if (SceneTransitionManager.instance != null)
        {
            SceneTransitionManager.instance.LoadScene(sceneName);
        }
        else
        {
            // マネージャーがいない場合の保険（通常のロード）
            SceneManager.LoadScene(sceneName);
        }
    }

    private IEnumerator ScaleAnimation(Vector3 startScale, Vector3 endScale, System.Action onComplete = null)
    {
        float time = 0;
        while (time < animDuration)
        {
            time += Time.deltaTime;
            float t = time / animDuration;
            float ease = 1f - Mathf.Pow(1f - t, 3f);

            menuPanel.transform.localScale = Vector3.Lerp(startScale, endScale, ease);
            yield return null;
        }
        menuPanel.transform.localScale = endScale;
        onComplete?.Invoke();
    }
}