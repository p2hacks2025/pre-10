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
        // これがないと、受信時に「Unityの機能が使えません」というエラーになります
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

    // =================================================================
    // 1. 星座を保存 (Manual / Photo / Reply 共通)
    // =================================================================
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
                    onComplete?.Invoke(true);
                }
                else
                {
                    Debug.LogError("【Firebase】保存失敗: " + task.Exception);
                    onComplete?.Invoke(false);
                }
            });
    }

    // =================================================================
    // 2. 全データを読み込み (SkyProject2D用)
    // =================================================================
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

    // =================================================================
    // 3. いいね数の更新 (SkyUIManager用)
    // =================================================================
    public void AddLike(string guid, int currentCount)
    {
        if (!isFirebaseReady) return;

        // guid/likeCount だけを直接書き換える
        reference.Child("constellations").Child(guid).Child("likeCount").SetValueAsync(currentCount);
    }

    // =================================================================
    // 4. 流れ星の保存 (ShootingStar)
    // =================================================================
    public void SaveShootingStar(ShootingStarData data)
    {
        if (!isFirebaseReady) return;

        // "shooting_stars" という新しいフォルダを作って保存
        // Push()を使うと、ユニークなIDを自動で振ってくれる（履歴として残る）
        string json = JsonUtility.ToJson(data);
        reference.Child("shooting_stars").Push().SetRawJsonValueAsync(json);
    }

    // =================================================================
    // 5. 流れ星の受信 (SkyProject2D用)
    // =================================================================
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