using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class NavigationMenu : MonoBehaviour
{
    [Header("UIパーツ")]
    [SerializeField] private Button openButton;       // ハンバーガーボタン（三本線）
    [SerializeField] private Button closeBackground;  // メニュー外を押したら閉じるための透明ボタン（任意）
    [SerializeField] private GameObject menuPanel;    // 4つのボタンが入っているパネル

    [Header("各ボタン")]
    [SerializeField] private Button btnHome;
    [SerializeField] private Button btnObserve;
    [SerializeField] private Button btnPost;
    [SerializeField] private Button btnAlbum;

    [Header("遷移先シーン名")]
    [SerializeField] private string sceneHome = "HomeScene";
    [SerializeField] private string sceneObserve = "ObserveScene"; // 観測
    [SerializeField] private string scenePost = "PostScene";       // 投稿
    [SerializeField] private string sceneAlbum = "AlbumScene";     // アルバム

    [Header("アニメーション設定")]
    [SerializeField] private float animDuration = 0.2f; // アニメーションにかかる時間

    private bool isOpen = false;
    private Coroutine currentAnim;

    void Start()
    {
        // 初期状態：パネルは見えないようにサイズを0にしておく
        if (menuPanel != null)
        {
            menuPanel.transform.localScale = Vector3.zero;
            menuPanel.SetActive(false);
        }

        // ボタン登録
        if (openButton) openButton.onClick.AddListener(ToggleMenu);
        if (closeBackground) closeBackground.onClick.AddListener(CloseMenu);

        // シーン遷移ボタン登録
        if (btnHome) btnHome.onClick.AddListener(() => LoadScene(sceneHome));
        if (btnObserve) btnObserve.onClick.AddListener(() => LoadScene(sceneObserve));
        if (btnPost) btnPost.onClick.AddListener(() => LoadScene(scenePost));
        if (btnAlbum) btnAlbum.onClick.AddListener(() => LoadScene(sceneAlbum));
    }

    // メニューの開閉切り替え
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

        // アニメーション (0 -> 1)
        if (currentAnim != null) StopCoroutine(currentAnim);
        currentAnim = StartCoroutine(ScaleAnimation(Vector3.zero, Vector3.one));
    }

    private void CloseMenu()
    {
        if (!isOpen) return;
        isOpen = false;

        if (closeBackground) closeBackground.gameObject.SetActive(false);

        // アニメーション (1 -> 0)
        if (currentAnim != null) StopCoroutine(currentAnim);
        currentAnim = StartCoroutine(ScaleAnimation(Vector3.one, Vector3.zero, () =>
        {
            // 縮みきったら非表示にする
            menuPanel.SetActive(false);
        }));
    }

    private void LoadScene(string sceneName)
    {
        // 今いるシーンと同じならロードしない
        if (SceneManager.GetActiveScene().name == sceneName)
        {
            CloseMenu();
            return;
        }

        SceneManager.LoadScene(sceneName);
    }

    // 小→大、大→小のアニメーションコルーチン
    private IEnumerator ScaleAnimation(Vector3 startScale, Vector3 endScale, System.Action onComplete = null)
    {
        float time = 0;

        while (time < animDuration)
        {
            time += Time.deltaTime;
            float t = time / animDuration;
            // EaseOutBackっぽい動き（少し行き過ぎて戻る）を入れると気持ちいい
            // 面倒ならシンプルに Vector3.Lerp(startScale, endScale, t) でOK
            float ease = 1f - Mathf.Pow(1f - t, 3f);

            menuPanel.transform.localScale = Vector3.Lerp(startScale, endScale, ease);
            yield return null;
        }

        menuPanel.transform.localScale = endScale;
        onComplete?.Invoke();
    }
}