using UnityEngine;
using System.Collections.Generic;

public class ConstellationRenderer : MonoBehaviour
{
    [Header("設定")]
    [SerializeField] private GameObject starPrefab;
    [SerializeField] private GameObject linePrefab;
    [SerializeField] private float colliderSizeRate = 2f;
    [SerializeField] private float baseLineWidth = 2.0f;

    // 星座データをワールドに構築する
    public GameObject Render(ConstellationData data, Transform parent, Vector3 position, float scale, SkyCameraController cam)
    {
        // データの整合性を保証（後述のConstellationDataの修正と連動）
        data.EnsureIntegrity();

        string objectName = string.IsNullOrEmpty(data.guid) ? data.constellationName : data.guid;
        GameObject rootObj = new GameObject(objectName);
        rootObj.transform.SetParent(parent);
        rootObj.transform.localPosition = position;

        // コライダーとクリック判定の追加
        BoxCollider2D col = rootObj.AddComponent<BoxCollider2D>();
        col.size = new Vector2(300f * scale * colliderSizeRate, 300f * scale * colliderSizeRate);
        col.isTrigger = true;

        ConstellationClickTrigger trigger = rootObj.AddComponent<ConstellationClickTrigger>();
        trigger.Setup(data, cam);

        Dictionary<int, GameObject> idToObjMap = new Dictionary<int, GameObject>();

        // 星の生成
        foreach (var sData in data.stars)
        {
            GameObject star = Instantiate(starPrefab, rootObj.transform);
            star.transform.localPosition = new Vector3(sData.x * scale, sData.y * scale, 0);
            star.transform.localScale = Vector3.one * sData.scale * scale;
            idToObjMap[sData.id] = star;
        }

        // 線の生成
        foreach (var cData in data.connections)
        {
            if (idToObjMap.ContainsKey(cData.fromStarId) && idToObjMap.ContainsKey(cData.toStarId))
            {
                CreateLine(idToObjMap[cData.fromStarId].transform.localPosition,
                           idToObjMap[cData.toStarId].transform.localPosition,
                           rootObj.transform, scale);
            }
        }

        // SkyProject2Dのインスタンスが存在する場合のみ、輝きを更新する
        if (SkyProject2D.instance != null)
        {
            SkyProject2D.instance.UpdateBloom(rootObj.transform, data);
        }

        return rootObj;
    }

    private void CreateLine(Vector3 start, Vector3 end, Transform parent, float scale)
    {
        GameObject line = Instantiate(linePrefab, parent);
        LineRenderer lr = line.GetComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.positionCount = 2;
        lr.SetPositions(new Vector3[] { start, end });
        lr.widthMultiplier = baseLineWidth * scale;
    }
}