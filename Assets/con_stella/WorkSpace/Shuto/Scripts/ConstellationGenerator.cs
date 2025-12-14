using UnityEngine;
using System.Collections.Generic;

public class ConstellationGenerator : MonoBehaviour
{
    [Header("設定")]
    [SerializeField] private GameObject starPrefab;
    [SerializeField] private Transform constellationRoot;
    [SerializeField] private LineRenderer lineRenderer;

    private List<GameObject> spawnedStars = new List<GameObject>();

    public void GenerateFromContours(List<List<Vector2>> contours, float scaleFactor)
    {
        ClearConstellation();
        // UIの上に描画させるためOrderを大きく
        lineRenderer.sortingOrder = 100;

        List<Vector3> allLinePoints = new List<Vector3>();
        int totalStars = 0;

        foreach (var contour in contours)
        {
            // contourには、すでに「いい感じにカクカクした頂点」が入っている
            if (contour.Count < 3) continue;

            // 一筆書きの線を作る
            for (int i = 0; i < contour.Count; i++)
            {
                // Z座標を0にする（Canvas設定がCameraならこれでOK）
                Vector3 currentPos = new Vector3(contour[i].x * scaleFactor, contour[i].y * scaleFactor, 0f);

                SpawnStar(currentPos);
                allLinePoints.Add(currentPos);
                totalStars++;
            }

            // 最後の点から最初の点へ線を戻して「閉じた形」にする（星座っぽくする）
            Vector3 firstPos = new Vector3(contour[0].x * scaleFactor, contour[0].y * scaleFactor, 0f);
            allLinePoints.Add(firstPos);

            // ★重要：次の輪郭へ飛ぶときに変な線が出ないようにする工夫
            // LineRendererの頂点数を操作する代わりに、
            // 「同じ場所にもう一度点を打つ」と線が切れることがあるが、
            // LineRenderer単体では完全な分割は難しい。
            // 見た目を重視するなら、ここで「透明な線」にするなどの工夫が必要だが、
            // 今回はシンプルに、次の輪郭の始点も追加して強引につなぐ（または許容する）。
            // もっと綺麗にするなら、1輪郭につき1つのLineRendererオブジェクトを生成するべき。
            // ↓
            // 簡易対応：ここではそのまま次の輪郭へ繋がります。
            // もし「線がつながるのが嫌」なら、Prefab化してLineRendererを複数生成する改修が必要ですが、
            // まずはこの「ポリゴン星座」の見た目を確認してください。
        }

        lineRenderer.positionCount = allLinePoints.Count;
        lineRenderer.SetPositions(allLinePoints.ToArray());
    }

    private void SpawnStar(Vector3 pos)
    {
        GameObject star = Instantiate(starPrefab, constellationRoot);
        star.transform.localPosition = pos;
        star.transform.localScale = Vector3.one * 0.5f;
        spawnedStars.Add(star);
    }

    public void ClearConstellation()
    {
        foreach (Transform child in constellationRoot) Destroy(child.gameObject);
        spawnedStars.Clear();
        lineRenderer.positionCount = 0;
    }
}