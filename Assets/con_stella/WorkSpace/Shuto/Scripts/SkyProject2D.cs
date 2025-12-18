using UnityEngine;
using System.Collections.Generic;

public class SkyProject2D : MonoBehaviour
{
    public static SkyProject2D instance; // シングルトン化

    [Header("プレハブ")]
    [SerializeField] private GameObject starPrefab;
    [SerializeField] private GameObject linePrefab;
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
    [SerializeField] private float baseLineWidth = 2.0f;

    [Header("UI連携")]
    [SerializeField] private SkyCameraController cameraController;

    [SerializeField] private float collisionCheckRadius = 10f; // この半径内に他の星座があったら配置し直す
    [SerializeField] private int maxRetryCount = 10; // 配置場所が見つからない時の最大再試行回数

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
        CheckSavedData();
        LoadLocalData();
        GenerateSingleStar();
    }

    public void LoadLocalData()
    {
        // 1. キーがあるか確認
        if (!PlayerPrefs.HasKey("LocalSaveList"))
        {
            Debug.LogError("【捜査エラー】セーブデータ 'LocalSaveList' が見つかりません！保存ボタンを押しましたか？");

            // もし古いキー(TestSaveData)が残っているなら、教えてあげる
            if (PlayerPrefs.HasKey("TestSaveData"))
            {
                Debug.LogWarning("※ 'TestSaveData' は見つかりました。保存側のコードが古い（リスト保存になっていない）可能性があります。");
            }
            return;
        }

        // 2. JSONの中身を確認
        string json = PlayerPrefs.GetString("LocalSaveList");
        Debug.Log("【捜査2】JSONデータを発見: " + json);

        // 3. リストに復元できるか確認
        currentWrapper = JsonUtility.FromJson<ConstellationListWrapper>(json);

        if (currentWrapper == null)
        {
            Debug.LogError("【捜査エラー】JSONの解析に失敗しました。データが壊れています。");
            return;
        }

        if (currentWrapper.list == null || currentWrapper.list.Count == 0)
        {
            Debug.LogError("【捜査エラー】リストの中身が空っぽ(0件)です！保存処理がうまくいっていません。");
            return;
        }

        Debug.Log($"【捜査3】{currentWrapper.list.Count} 件のデータを確認。生成を開始します...");

        // 4. 生成ループ
        foreach (var data in currentWrapper.list)
        {
            // 位置を決める
            float randomX = Random.Range(-spawnArea.x, spawnArea.x);
            float randomY = Random.Range(-spawnArea.y, spawnArea.y);
            Vector3 spawnPos = new Vector3(randomX, randomY, 0);

            GenerateConstellationObject(data, spawnPos);
        }
    }

    public void SaveLocalData()
    {
        if (currentWrapper == null) return;

        // 現在のデータをJSONに変換
        string json = JsonUtility.ToJson(currentWrapper);

        // PlayerPrefsに保存
        PlayerPrefs.SetString("LocalSaveList", json);
        PlayerPrefs.Save();

        Debug.Log("【保存完了】データを保存しました: " + json);
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

    public static void StaticGenerate(ConstellationData data, Vector3 position) => new GameObject().transform.GetComponent<SkyProject2D>().GenerateConstellationObject(data, position);

    private void GenerateConstellationObject(ConstellationData data, Vector3 position)
    {
        // 5. プレハブチェック
        if (starPrefab == null || linePrefab == null)
        {
            Debug.LogError("【捜査エラー】Inspectorで StarPrefab か LinePrefab がセットされていません！");
            return;
        }
        if (skyRoot == null)
        {
            Debug.LogWarning("※ SkyRoot がセットされていません（生成はされますが整理されません）");
        }

        GameObject rootObj = new GameObject(data.constellationName);
        if (skyRoot != null) rootObj.transform.SetParent(skyRoot);
        rootObj.transform.localPosition = position;

        Debug.Log($"【捜査4】オブジェクト '{data.constellationName}' を生成しました。位置: {position}");

        // 以下、中身の生成
        float currentScale = Random.Range(minDisplayScale, maxDisplayScale);

        BoxCollider2D col = rootObj.AddComponent<BoxCollider2D>();
        col.size = new Vector2(300f * currentScale, 300f * currentScale);
        col.isTrigger = true;

        ConstellationClickTrigger trigger = rootObj.AddComponent<ConstellationClickTrigger>();
        trigger.Setup(data, cameraController);

        //中身の生成
        Dictionary<int, GameObject> idToObjMap = new Dictionary<int, GameObject>();

        // 星
        foreach (var sData in data.stars)
        {
            GameObject star = Instantiate(starPrefab, rootObj.transform);
            Vector3 starPos = new Vector3(sData.x * currentScale, sData.y * currentScale, 0);
            star.transform.localPosition = starPos;
            star.transform.localScale = Vector3.one * sData.scale * currentScale;

            var sprite = star.GetComponent<SpriteRenderer>();
            if (sprite != null) sprite.sortingOrder = 100;

            idToObjMap[sData.id] = star;
        }

        // 線
        foreach (var cData in data.connections)
        {
            if (idToObjMap.ContainsKey(cData.fromStarId) && idToObjMap.ContainsKey(cData.toStarId))
            {
                CreateLine(idToObjMap[cData.fromStarId].transform.localPosition,
                           idToObjMap[cData.toStarId].transform.localPosition,
                           rootObj.transform,
                           currentScale);
            }
        }
        UpdateConstellationBloom(data);  //星の輝き更新
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

        GameObject.Find(data.constellationName).transform.localScale /= Mathf.Max(width/frameWidth, height/frameHeight);
    }

    private void CreateLine(Vector3 startLocal, Vector3 endLocal, Transform parent, float scale)
    {
        GameObject line = Instantiate(linePrefab, parent);
        LineRenderer lr = line.GetComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.positionCount = 2;
        lr.SetPosition(0, startLocal);
        lr.SetPosition(1, endLocal);
        lr.widthMultiplier = baseLineWidth * scale;
        lr.sortingOrder = 90;
    }

    private void GenerateSingleStar()
    {
        if (starPrefab == null) return;

        for (int i = 0; i < starAmount; i++)
        {
            // 1. 位置をランダムに決定（画面端にも生成できるように、spawnAreaを調整）
            float randomX = Random.Range(-spawnArea.x - 10, spawnArea.x + 10);
            float randomY = Random.Range(-spawnArea.y - 10, spawnArea.y + 10);
            Vector3 spawnPos = new Vector3(randomX, randomY, 0);

            // 2. 生成 (skyRootがあればその子にする)
            GameObject starObj = Instantiate(starPrefab, spawnPos, Quaternion.identity);
            if (skyRoot != null) starObj.transform.SetParent(skyRoot);

            // 名前を変えておくとわかりやすい（任意）
            starObj.name = $"BgStar_{i}";

            // 3. ランダムな大きさを適用
            float randomScale = Random.Range(minSingleStarSize, maxSingleStarSize);
            starObj.transform.localScale = Vector3.one * randomScale;

            // 4. ランダムな輝き（Bloom）を適用
            SpriteRenderer sr = starObj.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                // 背景用の明るさをランダム決定
                float randomIntensity = Random.Range(minBgStarBrightness, maxBgStarBrightness);

                // HDRカラーを作成 (RGB > 1.0 で光る)
                Color hdrColor = new Color(randomIntensity, randomIntensity, randomIntensity, 1f);
                sr.color = hdrColor;

                // 背景用の星なので、星座より奥に描画されるようにSortingOrderを下げる
                //sr.sortingOrder = -10;
            }
        }

        starPrefab.transform.localScale = Vector3.one;
    }

    //星の輝きを更新する関数
    public void UpdateConstellationBloom(ConstellationData data)
    {
        if (skyRoot == null) return;
        Transform targetTransform = skyRoot.Find(data.constellationName);
        if (targetTransform == null) return;

        // 計算: いいね数が多いほど値が大きくなる (例: 1.0 -> 1.2 -> 1.4 ...)
        float intensity = baseIntensity + (data.likeCount * intensityPerLike);
        // 上限キャップ
        intensity = Mathf.Min(intensity, maxIntensity);

        // HDRカラーを作成 (RGBすべてを1.0以上にすると白く光る)
        Color hdrColor = new Color(intensity, intensity, intensity, 1f);

        // 星（SpriteRenderer）をすべて取得して色をセット
        SpriteRenderer[] stars = targetTransform.GetComponentsInChildren<SpriteRenderer>();
        foreach (var starSprite in stars)
        {
            starSprite.color = hdrColor;
        }

        Debug.Log($"[{data.constellationName}] Bloom強度更新: {intensity}");
    }
    void CheckSavedData()
    {
        if (PlayerPrefs.HasKey("LocalSaveList"))
        {
            string json = PlayerPrefs.GetString("LocalSaveList");
            Debug.Log("【保存データの中身】: " + json);
        }
        else
        {
            Debug.Log("保存されたデータはありません。");
        }
    }
}