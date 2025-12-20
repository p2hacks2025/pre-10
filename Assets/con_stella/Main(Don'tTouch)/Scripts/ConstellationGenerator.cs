using UnityEngine;
using System.Collections.Generic;
using System;

public class ConstellationGenerator : MonoBehaviour
{
    [Header("設定")]
    [SerializeField] private GameObject starPrefab;
    [SerializeField] private Transform constellationRoot;
    [SerializeField] private GameObject linePrefab;

    [Header("星のサイズ設定")]
    [SerializeField] private float minStarScale = 1.0f; // 最小サイズ
    [SerializeField] private float maxStarScale = 3.0f; // 最大サイズ

    [Header("間引き設定")]
    //前の星からこの距離(px)以内なら、星を置かずにスキップする
    [SerializeField] private float minDistanceBetweenStars = 30f;

    private List<GameObject> spawnedObjects = new List<GameObject>();

    // List<List<GameObject>> = 「1つの輪郭に含まれる星たちのリスト」のリスト
    private List<List<GameObject>> generatedContoursData = new List<List<GameObject>>();

    public void GenerateFromContours(List<List<Vector2>> contours, float scaleFactor)
    {
        ClearConstellation();

        // 基本は設定値を使う
        float currentMinDist = minDistanceBetweenStars;

        //輪郭が多い（複雑な）場合は、間引き距離を広げてスッキリさせる
        if (contours.Count > 10)
        {
            currentMinDist = 50f;
            Debug.Log($"輪郭が多い({contours.Count})ため、間引き距離を {currentMinDist} に広げました");
        }
        foreach (var contour in contours)
        {
            if (contour.Count < 3) continue;

            // --- 1. 間引き処理（近すぎる点を削除） ---
            List<Vector3> filteredPoints = new List<Vector3>();

            // 最初の点は必ず採用
            Vector3 lastAddedPoint = new Vector3(contour[0].x * scaleFactor, contour[0].y * scaleFactor, 0f);
            filteredPoints.Add(lastAddedPoint);

            for (int i = 1; i < contour.Count; i++)
            {
                Vector3 currentPoint = new Vector3(contour[i].x * scaleFactor, contour[i].y * scaleFactor, 0f);

                // 「前の点」との距離を測る
                float dist = Vector3.Distance(lastAddedPoint, currentPoint);

                // 設定した距離以上離れている場合のみ採用！
                if (dist >= minDistanceBetweenStars)
                {
                    filteredPoints.Add(currentPoint);
                    lastAddedPoint = currentPoint; // 基準点を更新
                }
            }

            // 点が減りすぎて、線にならなくなった場合は描画しない
            if (filteredPoints.Count < 2) continue;

            // --- 2. 星と線の生成 ---

            List<GameObject> currentContourStars = new List<GameObject>();

            // 星を置く
            foreach (var p in filteredPoints)
            {
                // 星を生成し、リストに追加
                GameObject starObj = SpawnStar(p);
                currentContourStars.Add(starObj);
            }

            // 線を引く（間引かれた点同士をつなぐ）
            SpawnLine(filteredPoints);

            // 保存用にデータを記録
            generatedContoursData.Add(currentContourStars);
        }
    }

    /// <summary>
    /// 線を生成する関数
    /// </summary>
    /// <param name="points"></param>
    private void SpawnLine(List<Vector3> points)
    {
        // 線のプレハブを生成
        GameObject lineObj = Instantiate(linePrefab, constellationRoot);

        // リストに追加（あとで消すため）
        spawnedObjects.Add(lineObj);

        LineRenderer lr = lineObj.GetComponent<LineRenderer>();
        if (lr != null)
        {
            lr.useWorldSpace = false; // Canvas内で使うため
            lr.sortingOrder = 90;     // 星(Order 100想定)より少し奥、背景より手前
            lr.loop = true;           // 自動で始点と終点をつなぐ
            lr.positionCount = points.Count;
            lr.SetPositions(points.ToArray());
        }
    }

    /// <summary>
    /// 星の生成をする関数
    /// </summary>
    /// <param name="pos"></param>
    private GameObject SpawnStar(Vector3 pos)
    {
        GameObject star = Instantiate(starPrefab, constellationRoot);
        star.transform.localPosition = pos;
        float randomScale = UnityEngine.Random.Range(minStarScale, maxStarScale);
        star.transform.localScale = Vector3.one * randomScale;

        spawnedObjects.Add(star);
        return star; // 生成した星を返すように変更
    }

    /// <summary>
    /// 星座の削除
    /// </summary>
    public void ClearConstellation()
    {
        // Root以下の全オブジェクトを削除（星も線も消える）
        foreach (Transform child in constellationRoot)
        {
            if (child != null) Destroy(child.gameObject);
        }

        spawnedObjects.Clear();
        generatedContoursData.Clear();
    }
    
    public ConstellationData RegisterConstellationData(string name, string description)
    {
        // 生成された星座データがない場合は中断
        if (generatedContoursData == null || generatedContoursData.Count == 0) return null;

        ConstellationData saveData = new ConstellationData();

        // UIから受け取ったデータをセット
        saveData.guid = System.Guid.NewGuid().ToString();
        saveData.constellationName = name;
        saveData.description = description;

        saveData.createdAt = System.DateTime.Now.ToString();
        saveData.likeCount = 0;

        // 星と線の保存処理
        Dictionary<GameObject, int> objToIdMap = new Dictionary<GameObject, int>();
        int currentId = 0;

        // 星の座標を保存
        foreach (var contourStars in generatedContoursData)
        {
            foreach (var starObj in contourStars)
            {
                if (starObj == null) continue;

                StarData sData = new StarData();
                sData.id = currentId;
                sData.x = starObj.transform.localPosition.x;
                sData.y = starObj.transform.localPosition.y;
                sData.scale = starObj.transform.localScale.x;

                saveData.stars.Add(sData);
                objToIdMap[starObj] = currentId;
                currentId++;
            }
        }

        // 線のつながりを保存
        foreach (var contourStars in generatedContoursData)
        {
            System.Collections.Generic.List<GameObject> validStars = new System.Collections.Generic.List<GameObject>();
            foreach (var s in contourStars) if (s != null) validStars.Add(s);

            int count = validStars.Count;
            if (count < 2) continue;

            for (int i = 0; i < count; i++)
            {
                GameObject currentStar = validStars[i];
                GameObject nextStar = validStars[(i + 1) % count];

                if (objToIdMap.ContainsKey(currentStar) && objToIdMap.ContainsKey(nextStar))
                {
                    ConnectionData cData = new ConnectionData();
                    cData.fromStarId = objToIdMap[currentStar];
                    cData.toStarId = objToIdMap[nextStar];
                    saveData.connections.Add(cData);
                }
            }
        }
        //作成したデータを返す（UI側でGUIDを使うため）
        return saveData;
    }
}