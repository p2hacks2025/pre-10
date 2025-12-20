using UnityEngine;
using System.Collections.Generic;
using Junya;

public class SkyProject2D : MonoBehaviour
{
    public static SkyProject2D instance;

    [Header("プレハブ")]
    [SerializeField] private GameObject singleStarPrefab;
    [SerializeField] private Transform skyRoot;

    [Header("配置設定")]
    [SerializeField] private int starAmount = 200;
    [SerializeField] private Vector2 spawnArea = new Vector2(60f, 60f);
    [SerializeField] private float maxDisplayScale = 0.05f;
    [SerializeField] private float minDisplayScale = 0.01f;
    [SerializeField] private float minSingleStarSize = 0.5f;
    [SerializeField] private float maxSingleStarSize = 1.5f;
    [SerializeField] private float minBgStarBrightness = 0.5f; // 暗め
    [SerializeField] private float maxBgStarBrightness = 1.5f; // 少し光る

    [Header("UI連携")]
    [SerializeField] private SkyCameraController cameraController;

    [Header("生成設定")]
    [SerializeField] private float collisionCheckRadius = 10f; // この半径内に他の星座があったら配置し直す
    [SerializeField] private int maxRetryCount = 10; // 配置場所が見つからない時の最大再試行回数
    [SerializeField] new private ConstellationRenderer renderer; // 星座の生成スクリプト参照

    [Header("いいね演出 (Bloom用HDR設定)")]
    [SerializeField] private float baseIntensity = 1.0f;     // 通常時の明るさ (1.0 = そのまま)
    [SerializeField] private float intensityPerLike = 0.2f;  // 1いいねごとの加算値
    [SerializeField] private float maxIntensity = 4.0f;      // 明るさの限界値

    // 読み込んだ全データをここに保持しておく
    public ConstellationListWrapper currentWrapper;

    void Awake()
    {
        // 他からアクセスできるように自分を登録
        if (instance == null) instance = this;
    }

    void Start()
    {
        GenerateSingleStar();
        StartCoroutine(LoadFromCloudSequence());  //FireBaseから読み込み

    }

    private System.Collections.IEnumerator FocusOnNewConstellation()
    {
        // 1. キーがあるか確認
        if (PlayerPrefs.HasKey("NextFocusGUID"))
        {
            string targetGuid = PlayerPrefs.GetString("NextFocusGUID");

            // 読み終わったらすぐ消す（次回以降は反応させないため）
            PlayerPrefs.DeleteKey("NextFocusGUID");
            PlayerPrefs.Save();

            // 生成などが完全に終わるまで少し待つ（念のため）
            yield return null;

            // GUID名でオブジェクトを探す
            // (前回の修正でオブジェクト名をGUIDにしている前提です)
            GameObject targetObj = GameObject.Find(targetGuid);

            // 見つからなければ名前でも探してみる（古いデータ対策）
            if (targetObj == null)
            {
                // currentWrapperから該当データをデータ検索
                var data = currentWrapper.list.Find(x => x.guid == targetGuid);
                if (data != null)
                {
                    targetObj = GameObject.Find(data.constellationName);
                }
            }

            // カメラを移動させる
            if (targetObj != null && cameraController != null)
            {
                Debug.Log($"【カメラ移動】ターゲット発見: {targetObj.name} 位置: {targetObj.transform.position}");

                // カメラコントローラーに移動命令を出す
                cameraController.FocusOnTarget(targetObj.transform.position);
            }
        }
    }

    // クラウド読み込み用コルーチン
    private System.Collections.IEnumerator LoadFromCloudSequence()
    {
        // FirebaseManagerの準備ができるまで少し待つ
        yield return new WaitForSeconds(1.0f);

        if (FirebaseManager.instance != null)
        {
            Debug.Log("【Sky】クラウドからデータ取得を開始...");

            // データの読み込みを依頼
            FirebaseManager.instance.LoadAllConstellations((List<ConstellationData> dataList) =>
            {
                // データが返ってきたら実行される場所
                OnDataLoaded(dataList);
                SetupShootingStarListener();
            });
        }
        else
        {
            Debug.LogError("FirebaseManagerが見つかりません");
        }
    }

    // データ受け取り後の処理
    private void OnDataLoaded(List<ConstellationData> dataList)
    {
        if (dataList == null) return;

        // 保存されている「いいね済みリスト」を取得
        string likedGuidsString = PlayerPrefs.GetString("LikedGuids", "");
        // 判定しやすいようにHashSetに入れる（リストでも可）
        HashSet<string> likedGuids = new HashSet<string>(likedGuidsString.Split(','));

        // 既存のラッパーに入れておく（検索などで使うため）
        if (currentWrapper == null) currentWrapper = new ConstellationListWrapper();
        currentWrapper.list = dataList;

        Debug.Log($"【Sky】{dataList.Count}個の星座を生成します");

        // 全データを生成
        foreach (var data in dataList)
        {
            //修正、追加の過程で壊れた星座データをガン無視
            if (data == null || string.IsNullOrEmpty(data.guid) || data.stars == null || data.stars.Count == 0)
            {
                Debug.LogWarning($"不完全な星座データ（星リスト空など）のためスキップしました: {data?.constellationName ?? "Unknown"}");
                continue;
            }

            // もしリストの中にIDがあれば、いいね済みにする
            if (likedGuids.Contains(data.guid))
            {
                data.isLiked = true;
            }

            // 座標を決めて生成
            Vector3 spawnPos = FindSafePosition();
            GenerateConstellationObject(data, spawnPos);
        }

        // カメラ位置合わせ（投稿直後の演出）
        StartCoroutine(FocusOnNewConstellation());
    }

    public void DecideConstellationPos()
    {
        DataManager.instance.LoadAllLocalData();
        // 生成ループ
        foreach (var data in currentWrapper.list)
        {
            // 位置を決める
            float randomX = Random.Range(-spawnArea.x, spawnArea.x);
            float randomY = Random.Range(-spawnArea.y, spawnArea.y);
            Vector3 spawnPos = new Vector3(randomX, randomY, 0);

            GenerateConstellationObject(data, spawnPos);
        }
    }

    // 特定の星座オブジェクトの輝き（Bloom）を現在のデータに基づいて更新します
    public void UpdateBloom(Transform target, ConstellationData data)
    {
        if (target == null || data == null) return;

        // インスペクターの設定値を使用して計算
        float intensity = baseIntensity + (data.likeCount * intensityPerLike);

        // 上限キャップを適用
        intensity = Mathf.Min(intensity, maxIntensity);

        // HDRカラーを作成
        Color hdrColor = new Color(intensity, intensity, intensity, 1f);

        // 星（SpriteRenderer）をすべて取得して色をセット
        SpriteRenderer[] stars = target.GetComponentsInChildren<SpriteRenderer>();
        foreach (var starSprite in stars)
        {
            starSprite.color = hdrColor;
        }
    }
        
    // GUIDまたは名前からオブジェクトを探して輝きを更新します
    public void UpdateConstellationBloom(ConstellationData data)
    {
        if (skyRoot == null) return;

        string targetName = string.IsNullOrEmpty(data.guid) ? data.constellationName : data.guid;
        Transform targetTransform = skyRoot.Find(targetName);

        if (targetTransform != null)
        {
            // 上記の共通メソッドを呼び出す
            UpdateBloom(targetTransform, data);
        }
    }

    // 重ならない位置を探すロジック
    private Vector3 FindSafePosition()
    {
        for (int i = 0; i < maxRetryCount; i++)
        {
            float randomX = Random.Range(-spawnArea.x, spawnArea.x);
            float randomY = Random.Range(-spawnArea.y, spawnArea.y);
            Vector2 candidatePos = new Vector2(randomX, randomY);

            // その位置にコライダーがあるかチェック (LayerMaskはDefault等を想定)
            Collider2D hit = Physics2D.OverlapCircle(candidatePos, collisionCheckRadius);

            // 誰もいなければ決定
            if (hit == null)
            {
                return new Vector3(randomX, randomY, 0);
            }
        }

        // 諦めてランダムな位置を返す
        return new Vector3(Random.Range(-spawnArea.x, spawnArea.x), Random.Range(-spawnArea.y, spawnArea.y), 0);
    }

    public static void StaticGenerate(ConstellationData data, Vector3 position) => GameObject.Find("SkyManager").
        GetComponent<SkyProject2D>().GenerateConstellationObject(data, position);

    private void GenerateConstellationObject(ConstellationData data, Vector3 position)
    {
        float currentScale = Random.Range(minDisplayScale, maxDisplayScale);
        // レンダラースクリプトに依頼
        renderer.Render(data, skyRoot, position, currentScale, cameraController);
    }

    private void ScaleConstellationObject(in ConstellationData data, in float frameWidth, in float frameHeight)
    {
        float up = data.stars[0].y;
        float down = data.stars[0].y;
        float right = data.stars[0].x;
        float left = data.stars[0].x;

        foreach(StarData star in data.stars)
        {
            if (up < star.y) up = star.y;
            if(down > star.y) down = star.y;
            if(left > star.x) left = star.x;
            if(right < star.x) right = star.x;
        }

        float width = Mathf.Abs(right - left);
        float height = Mathf.Abs(up - down);

        string targetName = string.IsNullOrEmpty(data.guid) ? data.constellationName : data.guid;
        GameObject targetObj = GameObject.Find(targetName);

        if (targetObj != null)
        {
            targetObj.transform.localScale /= Mathf.Max(width / frameWidth, height / frameHeight);
        }
    }

    private void SetupShootingStarListener()
    {
        if (FirebaseManager.instance != null)
        {
            // Firebase側のリスナーを起動（すでに動いていれば無視される）
            FirebaseManager.instance.InitShootingStarListener();

            // データが届いたときの処理を登録
            FirebaseManager.instance.OnShootingStarReceived += HandleShootingStar;
        }
    }

    private void GenerateSingleStar()
    {
        if (singleStarPrefab == null) return;

        for (int i = 0; i < starAmount; i++)
        {
            // 位置をランダムに決定（画面端にも生成できるように、spawnAreaを調整）
            float randomX = Random.Range(-spawnArea.x - 10, spawnArea.x + 10);
            float randomY = Random.Range(-spawnArea.y - 10, spawnArea.y + 10);
            Vector3 spawnPos = new Vector3(randomX, randomY, 0);

            // 生成 (skyRootがあればその子にする)
            GameObject starObj = Instantiate(singleStarPrefab, spawnPos, Quaternion.identity);
            if (skyRoot != null) starObj.transform.SetParent(skyRoot);

            // 名前を変えておく
            starObj.name = $"BgStar_{i}";

            // ランダムな大きさを適用
            float randomScale = Random.Range(minSingleStarSize, maxSingleStarSize);
            starObj.transform.localScale = Vector3.one * randomScale;

            // ランダムな輝き（Bloom）を適用
            SpriteRenderer sr = starObj.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                // 背景用の明るさをランダム決定
                float randomIntensity = Random.Range(minBgStarBrightness, maxBgStarBrightness);

                // HDRカラーを作成 (RGB > 1.0 で光る)
                Color hdrColor = new Color(randomIntensity, randomIntensity, randomIntensity, 1f);
                sr.color = hdrColor;
            }
        }

        singleStarPrefab.transform.localScale = Vector3.one;
    }

    //　実際に星を受け取ったときの処理
    private void HandleShootingStar(ShootingStarData data)
    {
        // 自分以外の星が飛んできたら表示
        if (data.senderName != "自分")
        {
            if (ShootingStarManager.instance != null)
            {
                ShootingStarManager.instance.SpawnStar(data);
            }
        }
    }

    void OnDestroy()
    {
        if (FirebaseManager.instance != null)
        {
            FirebaseManager.instance.OnShootingStarReceived -= HandleShootingStar;
        }
    }
}