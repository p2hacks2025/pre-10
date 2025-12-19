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

    // メインスレッドで処理を実行するためのキュー
    private Queue<Action> _executionQueue = new Queue<Action>();

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // シーン遷移しても消えないようにする
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        // メインスレッド（Update）でキューに溜まった処理を実行する
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
        // 1. Firebaseの初期化確認
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            var dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                // 準備OK
                reference = FirebaseDatabase.DefaultInstance.RootReference;
                isFirebaseReady = true;
                Debug.Log("【Firebase】接続成功！準備完了です。");
            }
            else
            {
                Debug.LogError($"【Firebase】接続失敗: {dependencyStatus}");
            }
        });
    }

    /// <summary>
    ///  星座を保存 (Manual / Photo / Reply 共通)
    /// </summary>
    /// <param name="data"></param>
    /// <param name="onComplete"></param>
    public void SaveConstellation(ConstellationData data, Action<bool> onComplete = null)
    {
        if (!isFirebaseReady)
        {
            Debug.LogWarning("Firebaseの準備ができていません");
            onComplete?.Invoke(false);
            return;
        }

        // JSONに変換
        string json = JsonUtility.ToJson(data);

        // "constellations" というフォルダの下に、GUIDを名前として保存する
        reference.Child("constellations").Child(data.guid).SetRawJsonValueAsync(json)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted)
                {
                    Debug.Log($"【Firebase】保存完了: {data.constellationName}");
                    CleanUpOldConstellations();  //古いものを削除するかを見る
                    onComplete?.Invoke(true);
                }
                else
                {
                    Debug.LogError("【Firebase】保存失敗: " + task.Exception);
                    onComplete?.Invoke(false);
                }
            });
    }

    /// <summary>
    /// 星座データの削除
    /// </summary>
    private void CleanUpOldConstellations()
    {
        // データの作成順（キー順）に取得
        reference.Child("constellations").OrderByKey().GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || !task.IsCompleted) return;

            DataSnapshot snapshot = task.Result;
            long currentCount = snapshot.ChildrenCount;

            // 上限を超えているかチェック
            if (currentCount > maxConstellationCount)
            {
                long deleteCount = currentCount - maxConstellationCount;
                Debug.Log($"【Firebase】容量オーバー: {currentCount}/{maxConstellationCount} 件。古い {deleteCount} 件を削除します。");

                int deleted = 0;
                foreach (DataSnapshot child in snapshot.Children)
                {
                    if (deleted >= deleteCount) break;

                    // 古いデータを削除
                    reference.Child("constellations").Child(child.Key).RemoveValueAsync();
                    deleted++;
                }
            }
        });
    }

    /// <summary>
    /// 全データ読み込み
    /// </summary>
    /// <param name="onSuccess"></param>
    public void LoadAllConstellations(Action<List<ConstellationData>> onSuccess)
    {
        if (!isFirebaseReady) return;

        reference.Child("constellations").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogError("【Firebase】読み込み失敗");
                return;
            }

            DataSnapshot snapshot = task.Result;
            List<ConstellationData> dataList = new List<ConstellationData>();

            // 取得したデータをリストに変換
            foreach (DataSnapshot child in snapshot.Children)
            {
                string json = child.GetRawJsonValue();
                if (!string.IsNullOrEmpty(json))
                {
                    ConstellationData data = JsonUtility.FromJson<ConstellationData>(json);
                    dataList.Add(data);
                }
            }

            Debug.Log($"【Firebase】{dataList.Count}件のデータを取得しました");
            onSuccess?.Invoke(dataList);
        });
    }

    /// <summary>
    /// いいね数更新
    /// </summary>
    /// <param name="guid"></param>
    /// <param name="currentCount"></param>
    public void AddLike(string guid, int currentCount)
    {
        if (!isFirebaseReady) return;

        // guid/likeCount だけを直接書き換える
        reference.Child("constellations").Child(guid).Child("likeCount").SetValueAsync(currentCount);
    }

    /// <summary>
    /// 流れ星データの保存関数
    /// </summary>
    /// <param name="data"></param>
    public void SaveShootingStar(ShootingStarData data)
    {
        if (!isFirebaseReady) return;

        string json = JsonUtility.ToJson(data);

        // Push()で作った場所の参照（リファレンス）を保持しておく
        DatabaseReference newStarRef = reference.Child("shooting_stars").Push();

        newStarRef.SetRawJsonValueAsync(json).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                // 30秒後にこのデータを削除するタイマーを開始
                StartCoroutine(DeleteShootingStarDelayed(newStarRef, 30f));
            }
        });
    }

    /// <summary>
    /// 時間差でデータを消すコルーチン
    /// </summary>
    /// <param name="targetRef"></param>
    /// <param name="delay"></param>
    /// <returns></returns>
    private IEnumerator DeleteShootingStarDelayed(DatabaseReference targetRef, float delay)
    {
        // 指定秒数待つ（相手に届くための猶予時間）
        yield return new WaitForSeconds(delay);

        // 削除実行
        targetRef.RemoveValueAsync();
        Debug.Log("【Firebase】流れ星データをクリーンアップしました");
    }

    /// <summary>
    /// 流れ星を受信する関数
    /// </summary>
    /// <param name="onStarReceived"></param>
    public void ListenForShootingStars(Action<ShootingStarData> onStarReceived)
    {
        if (!isFirebaseReady) return;

        // データが追加された瞬間を検知
        reference.Child("shooting_stars").LimitToLast(1).ChildAdded += (object sender, ChildChangedEventArgs args) =>
        {
            if (args.Snapshot != null && args.Snapshot.Value != null)
            {
                string json = args.Snapshot.GetRawJsonValue();
                if (!string.IsNullOrEmpty(json))
                {
                    ShootingStarData data = JsonUtility.FromJson<ShootingStarData>(json);

                    // 受信はバックグラウンドで行われるため、メインスレッド(Update)で実行するように予約する
                    lock (_executionQueue)
                    {
                        _executionQueue.Enqueue(() => {
                            onStarReceived?.Invoke(data);
                        });
                    }
                }
            }
        };
    }
}