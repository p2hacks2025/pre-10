using UnityEngine;
using System.Collections;
using System;

public class PostSequenceManager : MonoBehaviour
{
    public static PostSequenceManager instance;

    void Awake()
    {
        if (instance == null) { instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }
    }

    /// <summary>
    /// 指定秒後にアクションを実行する（カメラやアルバム起動用）
    /// </summary>
    public void DelayedAction(float delay, Action action)
    {
        StartCoroutine(ExecuteDelayed(delay, action));
    }

    private IEnumerator ExecuteDelayed(float delay, Action action)
    {
        yield return new WaitForSeconds(delay);
        action?.Invoke();
    }


    // GUIDを自分のリストに保存する共通ロジック
    public void SaveMyConstellationGuid(string guid)
    {
        string key = "MyConstellationGuids";
        string currentSaved = PlayerPrefs.GetString(key, "");

        if (!currentSaved.Contains(guid))
        {
            if (string.IsNullOrEmpty(currentSaved)) currentSaved = guid;
            else currentSaved += "," + guid;

            PlayerPrefs.SetString(key, currentSaved);
            PlayerPrefs.Save();
        }
    }

    // 投稿完了後のフェード演出とシーン遷移
    public IEnumerator PlayPostSequence(ConstellationData data, CanvasGroup messageGroup, float fadeDuration)
    {
        Debug.Log($"投稿完了演出開始: {data.constellationName}");

        // データの永続化
        PlayerPrefs.SetString("NextFocusGUID", data.guid);
        SaveMyConstellationGuid(data.guid);
        PlayerPrefs.Save();

        // フェード演出
        if (messageGroup != null)
        {
            messageGroup.gameObject.SetActive(true);
            messageGroup.alpha = 0f;

            float timer = 0f;
            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                messageGroup.alpha = Mathf.Lerp(0f, 1f, timer / fadeDuration);
                yield return null;
            }
            messageGroup.alpha = 1f;
            yield return new WaitForSeconds(0.5f);
        }

        yield return new WaitForSeconds(1.0f);

        // シーン遷移
        FinalizeAndGoToSky();
    }

    /// <summary>
    /// 投稿完了後の共通フェード遷移
    /// </summary>
    public void FinalizeAndGoToSky()
    {
        if (SceneTransitionManager.instance != null)
        {
            SceneTransitionManager.instance.LoadScene("SkyScene");
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("SkyScene");
        }
    }
}