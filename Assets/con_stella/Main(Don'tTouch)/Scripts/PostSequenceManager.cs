using UnityEngine;
using System.Collections;
using System;

public class PostSequenceManager : MonoBehaviour
{
    public static PostSequenceManager instance;
    private const string MyGuidsKey = "MyConstellationGuids"; // キーを定数化して間違いを防ぐ

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

    /// <summary>
    /// 投稿完了後の共通フェード遷移
    /// </summary>
    public void GoToSky()
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