using UnityEngine;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using System.Collections;
using System.Collections.Generic;
using System;

public class FirebaseManager : MonoBehaviour
{
    public static FirebaseManager instance;
    private DatabaseReference reference;
    private bool isFirebaseReady = false;

    // 星座の保存上限数
    [SerializeField] private int maxConstellationCount = 100;

    // メインスレッド実行用キュー
    private Queue<Action> _executionQueue = new Queue<Action>();

    // 流れ星受信イベント
    public event Action<ShootingStarData> OnShootingStarReceived;

    // GC対策用のクエリ保持変数
    private Query shootingStarQuery;
    private bool isListening = false;

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

    void Update()
    {
        lock (_executionQueue)
        {
            while (_executionQueue.Count > 0)
            {
                _executionQueue.Dequeue().Invoke();
            }
        }
    }

    void Start()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                reference = FirebaseDatabase.DefaultInstance.RootReference;
                isFirebaseReady = true;
                Debug.Log("【Firebase】接続成功！論理削除モードで稼働します。");

                // リスナー開始
                InitShootingStarListener();
            }
            else
            {
                Debug.LogError($"【Firebase】接続失敗: {task.Result}");
            }
        });
    }

    // =================================================
    // 流れ星 (論理削除ロジック)
    // =================================================

    /// <summary>
    /// 流れ星を保存し、30秒後にフラグを折る
    /// </summary>
    public void SaveShootingStar(ShootingStarData data)
    {
        if (!isFirebaseReady) return;

        string json = JsonUtility.ToJson(data);

        // Push()で場所を作る
        DatabaseReference newStarRef = reference.Child("shooting_stars").Push();
        string starKey = newStarRef.Key; // IDを控えておく

        // 保存実行
        newStarRef.SetRawJsonValueAsync(json).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                // ★ 30秒後に「論理削除」を実行するコルーチンを開始
                StartCoroutine(MarkShootingStarDeleted(starKey, 30f));
            }
        });
    }

    /// <summary>
    /// 指定時間後に isDeleted フラグを true にする
    /// </summary>
    private IEnumerator MarkShootingStarDeleted(string key, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (isFirebaseReady)
        {
            // データ自体は消さず、isDeleted だけ true に更新する
            reference
                .Child("shooting_stars")
                .Child(key)
                .Child("isDeleted")
                .SetValueAsync(true);

            // Debug.Log("【Firebase】論理削除しました (ID: " + key + ")");
        }
    }

    /// <summary>
    /// リスナー初期化
    /// </summary>
    public void InitShootingStarListener()
    {
        if (!isFirebaseReady || isListening) return;
        isListening = true;

        // クエリを保持
        shootingStarQuery = reference.Child("shooting_stars").LimitToLast(1);

        // ChildAddedイベント登録
        shootingStarQuery.ChildAdded += OnShootingStarAdded;

        Debug.Log("【Firebase】流れ星リスナー開始");
    }

    /// <summary>
    /// データ受信時の処理
    /// </summary>
    private void OnShootingStarAdded(object sender, ChildChangedEventArgs args)
    {
        if (args.Snapshot == null || !args.Snapshot.Exists) return;

        string json = args.Snapshot.GetRawJsonValue();
        if (string.IsNullOrEmpty(json)) return;

        ShootingStarData data = JsonUtility.FromJson<ShootingStarData>(json);

        // ★ 論理削除されているデータなら無視して終了！
        if (data.isDeleted)
        {
            // Debug.Log("古いデータなので無視しました");
            return;
        }

        // 生きているデータなら通知する
        lock (_executionQueue)
        {
            _executionQueue.Enqueue(() =>
            {
                OnShootingStarReceived?.Invoke(data);
            });
        }
    }

    // =================================================
    // 星座 (既存のまま)
    // =================================================
    public void SaveConstellation(ConstellationData data, Action<bool> onComplete = null)
    {
        if (!isFirebaseReady) { onComplete?.Invoke(false); return; }
        string json = JsonUtility.ToJson(data);
        reference.Child("constellations").Child(data.guid).SetRawJsonValueAsync(json)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted) { CleanUpOldConstellations(); onComplete?.Invoke(true); }
                else { onComplete?.Invoke(false); }
            });
    }

    private void CleanUpOldConstellations()
    {
        reference.Child("constellations").OrderByKey().GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || !task.IsCompleted) return;
            DataSnapshot snapshot = task.Result;
            long currentCount = snapshot.ChildrenCount;
            if (currentCount > maxConstellationCount)
            {
                long deleteCount = currentCount - maxConstellationCount;
                int deleted = 0;
                foreach (DataSnapshot child in snapshot.Children)
                {
                    if (deleted >= deleteCount) break;
                    reference.Child("constellations").Child(child.Key).RemoveValueAsync();
                    deleted++;
                }
            }
        });
    }

    public void LoadAllConstellations(Action<List<ConstellationData>> onSuccess)
    {
        if (!isFirebaseReady) return;
        reference.Child("constellations").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled) return;
            List<ConstellationData> dataList = new List<ConstellationData>();
            foreach (DataSnapshot child in task.Result.Children)
            {
                string json = child.GetRawJsonValue();
                if (!string.IsNullOrEmpty(json)) dataList.Add(JsonUtility.FromJson<ConstellationData>(json));
            }
            onSuccess?.Invoke(dataList);
        });
    }

    public void AddLike(string guid, int currentCount)
    {
        if (!isFirebaseReady) return;
        reference.Child("constellations").Child(guid).Child("likeCount").SetValueAsync(currentCount);
    }
}