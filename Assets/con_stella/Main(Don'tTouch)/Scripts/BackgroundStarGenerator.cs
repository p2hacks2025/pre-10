using UnityEngine;

public class BackgroundStarGenerator : MonoBehaviour
{
    [Header("連携設定")]
    [SerializeField] private GradientBackground targetBackground;
    [SerializeField] private Transform starRoot;

    [Header("星の設定")]
    [SerializeField] private GameObject starPrefab;
    [SerializeField] private int starAmount = 200;

    [SerializeField] private Vector2 manualSpawnArea = new Vector2(60f, 60f);

    [Header("サイズ設定")]
    [SerializeField] private float minSingleStarSize = 0.5f;
    [SerializeField] private float maxSingleStarSize = 1.5f;

    [Header("明るさ設定 (Bloom)")]
    [SerializeField] private float minBgStarBrightness = 0.5f;
    [SerializeField] private float maxBgStarBrightness = 1.5f;

    void Start()
    {
        if (targetBackground != null)
        {
            // 背景サイズを計算させてから星を作る
            targetBackground.FitBackground();
            GenerateStars();
        }
        else
        {
            GenerateStars();
        }
    }

    public void GenerateStars()
    {
        if (starPrefab == null)
        {
            Debug.LogError("★エラー: StarPrefab が設定されていません！Inspectorを確認してください。");
            return;
        }

        Vector2 area = manualSpawnArea;

        // 背景があるならそのサイズをもらう
        if (targetBackground != null)
        {
            area = targetBackground.CurrentSize / 2f;
        }

        // 掃除
        if (starRoot == null) starRoot = this.transform;
        foreach (Transform child in starRoot) Destroy(child.gameObject);

        // 生成ループ
        for (int i = 0; i < starAmount; i++)
        {
            // 座標決定
            float x = Random.Range(-area.x, area.x);
            float y = Random.Range(-area.y, area.y);
            Vector3 spawnPos = new Vector3(x, y, 0);

            // ★修正1: 生成と同時に親を指定する (SetParentのトラブル回避)
            GameObject star = Instantiate(starPrefab, starRoot);

            // ★修正2: ローカル座標としてセットする
            star.transform.localPosition = spawnPos;

            // ★修正3: サイズのランダム化（これが抜けていました）
            float randomScale = Random.Range(minSingleStarSize, maxSingleStarSize);
            star.transform.localScale = Vector3.one * randomScale;

            // ★修正4: 「starPrefab」ではなく、生成した「star」の色を変える
            SpriteRenderer sr = star.GetComponent<SpriteRenderer>(); // ←ここを直しました
            if (sr != null)
            {
                float randomIntensity = Random.Range(minBgStarBrightness, maxBgStarBrightness);
                Color hdrColor = new Color(randomIntensity, randomIntensity, randomIntensity, 1f);
                sr.color = hdrColor;
            }
        }

        Debug.Log($"【StarGenerator】星を {starAmount} 個 生成しました (範囲: {area})");
    }
}