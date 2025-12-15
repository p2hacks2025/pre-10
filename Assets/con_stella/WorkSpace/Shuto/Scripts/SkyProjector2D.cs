using UnityEngine;
using System.Collections.Generic;

public class SkyProjector2D : MonoBehaviour
{
    [Header("プレハブ")]
    [SerializeField] private GameObject starPrefab;
    [SerializeField] private GameObject linePrefab;
    [SerializeField] private Transform skyRoot;

    [Header("配置設定")]
    [SerializeField] private Vector2 spawnArea = new Vector2(50f, 50f);
    // 配置される星座全体のスケール倍率（0.1など小さい値を推奨）
    [SerializeField] private float maxDisplayScale = 0.01f;
    [SerializeField] private float minDisplayScale = 0.03f;
    private float displayScale;
    [Header("線の太さ調整")]
    // 線の基本の太さ。これにdisplayScaleが掛け合わされます。
    [SerializeField] private float baseLineWidth = 2.0f;

    [Header("UI連携")]
    [SerializeField] private SkyCameraController cameraController;

    void Start()
    {
        LoadLocalData();
    }

    public void LoadLocalData()
    {
        if (PlayerPrefs.HasKey("TestSaveData"))
        {
            string json = PlayerPrefs.GetString("TestSaveData");
            ConstellationData data = JsonUtility.FromJson<ConstellationData>(json);

            float randomX = Random.Range(-spawnArea.x, spawnArea.x);
            float randomY = Random.Range(-spawnArea.y, spawnArea.y);
            Vector3 spawnPos = new Vector3(randomX, randomY, 0);

            GenerateConstellationObject(data, spawnPos);
        }
    }

    private void GenerateConstellationObject(ConstellationData data, Vector3 position)
    {
        GameObject rootObj = new GameObject(data.constellationName);
        rootObj.transform.SetParent(skyRoot);
        rootObj.transform.localPosition = position;

        // 修正点1：親オブジェクトのスケールは (1,1,1) のままにする
        // rootObj.transform.localScale = Vector3.one * displayScale; // ←これはやめる

        // コライダー（タップ判定）のサイズもスケールに合わせて調整
        BoxCollider2D col = rootObj.AddComponent<BoxCollider2D>();
        // 元のデータがだいたい500〜800pxの範囲で作られていると仮定して調整
        displayScale = Random.Range(minDisplayScale, maxDisplayScale);

        col.size = new Vector2(500f * displayScale, 500f * displayScale);
        col.isTrigger = true;

        ConstellationClickTrigger trigger = rootObj.AddComponent<ConstellationClickTrigger>();
        trigger.Setup(data, cameraController);

        Dictionary<int, GameObject> idToObjMap = new Dictionary<int, GameObject>();

        foreach (var sData in data.stars)
        {
            GameObject star = Instantiate(starPrefab, rootObj.transform);

            // 修正点2：座標にスケールを掛ける
            Vector3 starPos = new Vector3(sData.x * displayScale, sData.y * displayScale, 0);
            star.transform.localPosition = starPos;

            // 修正点3：星の大きさにもスケールを掛ける
            // sData.scale は保存時の相対的な大きさ(3~5など)なので、それに全体の縮小率を掛ける
            star.transform.localScale = Vector3.one * sData.scale * displayScale;

            var sprite = star.GetComponent<SpriteRenderer>();
            if (sprite != null)
            {
                sprite.sortingOrder = 100;
            }

            idToObjMap[sData.id] = star;
        }

        foreach (var cData in data.connections)
        {
            if (idToObjMap.ContainsKey(cData.fromStarId) && idToObjMap.ContainsKey(cData.toStarId))
            {
                CreateLine(idToObjMap[cData.fromStarId].transform.localPosition,
                           idToObjMap[cData.toStarId].transform.localPosition,
                           rootObj.transform);
            }
        }
    }

    private void CreateLine(Vector3 startLocal, Vector3 endLocal, Transform parent)
    {
        GameObject line = Instantiate(linePrefab, parent);
        LineRenderer lr = line.GetComponent<LineRenderer>();

        lr.useWorldSpace = false;
        lr.positionCount = 2;
        lr.SetPosition(0, startLocal);
        lr.SetPosition(1, endLocal);

        // 修正点4：線の太さにもスケールを掛ける
        // 基本の太さ * 全体のスケール で調整
        lr.widthMultiplier = baseLineWidth * displayScale;

        lr.sortingOrder = 90;
    }
}