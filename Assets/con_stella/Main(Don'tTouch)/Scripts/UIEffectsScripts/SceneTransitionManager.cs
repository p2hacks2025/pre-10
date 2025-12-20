using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager instance;

    [Header("設定")]
    [SerializeField] private Canvas transitionCanvas;
    [SerializeField] private Image fadeImage;

    [Header("アニメーション速度設定")]
    [Tooltip("暗転にかかる時間")]
    [SerializeField] private float fadeDuration = 1.0f;

    [Tooltip("ズームにかかる時間（ここを調整！）")]
    [SerializeField] private float zoomDuration = 2.0f; // ★ここを追加

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        StartCoroutine(FadeIn());
    }

    // 通常のフェード遷移
    public void LoadScene(string sceneName)
    {
        StartCoroutine(TransitionRoutine(sceneName));
    }

    // ズーム付き遷移
    public void LoadSceneWithZoom(string sceneName, Vector3 targetWorldPos)
    {
        StartCoroutine(ZoomTransitionRoutine(sceneName, targetWorldPos));
    }

    private IEnumerator ZoomTransitionRoutine(string sceneName, Vector3 targetPos)
    {
        // カメラチェック
        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogError("【エラー】MainCameraが見つかりません！タグを確認してください。");
            // カメラがないなら通常ロードへ逃げる
            LoadScene(sceneName);
            yield break;
        }

        float startSize = cam.orthographicSize;
        Vector3 startCamPos = cam.transform.position;
        // ターゲットのZはカメラに合わせる
        Vector3 targetCamPos = new Vector3(targetPos.x, targetPos.y, startCamPos.z);

        Debug.Log($"【開始】ズーム開始！ 時間: {zoomDuration}秒 / 現在サイズ: {startSize}");

        fadeImage.gameObject.SetActive(true);
        float timer = 0f;

        // ★設定した「zoomDuration」を使ってループ
        while (timer < zoomDuration)
        {
            timer += Time.deltaTime;
            float t = timer / zoomDuration; // 0.0 -> 1.0

            // イージング（ゆっくり動き出し、ゆっくり止まる）
            float smoothT = t * t * (3f - 2f * t);

            // 1. カメラを動かす
            if (cam != null)
            {
                cam.transform.position = Vector3.Lerp(startCamPos, targetCamPos, smoothT);
                cam.orthographicSize = Mathf.Lerp(startSize, startSize * 0.2f, smoothT); // 0.2倍まで寄る
            }

            // 2. 画面を暗くする (後半から暗くし始める)
            // tが 0.6 (60%進行) を超えたら暗くし始める
            float alpha = 0f;
            if (t > 0.6f)
            {
                // 残りの0.4の期間で 0->1 にする
                alpha = (t - 0.6f) * 2.5f;
            }
            SetAlpha(alpha);

            yield return null;
        }

        // 念押しで最終状態へ
        if (cam != null)
        {
            cam.transform.position = targetCamPos;
            cam.orthographicSize = startSize * 0.2f;
        }
        SetAlpha(1f);

        Debug.Log("【完了】シーンを読み込みます");
        yield return SceneManager.LoadSceneAsync(sceneName);
        yield return StartCoroutine(FadeIn());
    }

    // --- 以下、既存の処理 ---

    private IEnumerator TransitionRoutine(string sceneName)
    {
        yield return StartCoroutine(FadeOut());
        yield return SceneManager.LoadSceneAsync(sceneName);
        yield return StartCoroutine(FadeIn());
    }

    private IEnumerator FadeOut()
    {
        fadeImage.gameObject.SetActive(true);
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            SetAlpha(timer / fadeDuration);
            yield return null;
        }
        SetAlpha(1f);
    }

    private IEnumerator FadeIn()
    {
        fadeImage.gameObject.SetActive(true);
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            SetAlpha(1f - (timer / fadeDuration));
            yield return null;
        }
        SetAlpha(0f);
        fadeImage.gameObject.SetActive(false);
    }

    private void SetAlpha(float alpha)
    {
        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = alpha;
            fadeImage.color = c;
        }
    }
}