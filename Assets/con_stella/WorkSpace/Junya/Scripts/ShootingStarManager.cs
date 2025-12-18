#define new
using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System;
using TMPro; // TMPを使うため追加

public class ShootingStarManager : MonoBehaviour
{
#if old
    /// <summary>
    /// 
    /// 前提 : ゲーム上のどこでもいいからprefab "ShootingStarManager" を一個だけ置いてほしい。名前は変えないで
    /// 
    /// ShootingStarManagerクラスのメンバ変数 "speed" で流れ星の速度をinspectorから調整可
    ///
    /// 流れ星を流すにはShootingStarクラスのコンストラクタを呼ぶ。
    /// new ShootingStar([流れ星に乗せたい文章(string)], [初期位置(Vector3)], [速度ベクトル(Vector3)]);
    /// 
    /// 流れ星はStop関数を呼ぶと一応止めれる(使わなくてもいいけど、うまく使えば重さ軽減できるかも)
    /// 
    /// </summary>

    public GameObject prefab;
    [NonSerialized] public static ShootingStarManager instance;

    [SerializeField] private float speed;

    void Start()
    {
        instance = GameObject.Find("ShootingStarManager").transform.GetComponent<ShootingStarManager>();

        new ShootingStar("あああああああああああああああ", Vector3.right * 10f + Vector3.up * 10f, new Vector3(-1.3f,-1f,0f) * Time.deltaTime * this.speed); //テスト用
    }
}

[Serializable] public class ShootingStar
{
    private readonly GameObject gameObject;

    public string content;

    public ShootingStar(string content, Vector3 initial, Vector3 velocity)
    {
        if (velocity.y > 0f || velocity.z != 0f) throw new InvalidShootingException();

        this.gameObject = MonoBehaviour.Instantiate(ShootingStarManager.instance.prefab, initial, Quaternion.identity);

        this.gameObject.transform.eulerAngles = new(0f, 0f, Mathf.Atan2(velocity.y, velocity.x) * 180f / Mathf.PI + 180f);

        this.gameObject.transform.SetParent(GameObject.Find("Canvas").transform);
        this.gameObject.transform.Find("Content").GetComponent<Text>().text = content;

        ShootingStarManager.instance.StartCoroutine(ShootCoroutine(velocity));
    }
    private IEnumerator ShootCoroutine(Vector3 velocity)
    {
        while (true)
        {
            if (this.isWaste) break;

            this.gameObject.transform.position += velocity;

            yield return null;
        }

        yield break;
    }

    private bool isWaste;
    public void Stop()
    {
        this.isWaste = true;
    }

    private class InvalidShootingException : Exception{ }
}

#elif new

    // シングルトン化
    public static ShootingStarManager instance;

    [Header("設定")]
    public GameObject prefab;       // プレハブ
    [SerializeField] private Transform starRoot;

    [Header("パラメータ")]
    //速度をランダムに
    [SerializeField] private float minSpeed = 10f;
    [SerializeField] private float maxSpeed = 25f;
    [SerializeField] private float minSize = 0.5f;
    [SerializeField] private float maxSize = 1.0f;

    [Header("演出用：自動生成設定")]
    // テキスト無し流れ星を自動で流す設定
    [SerializeField] private bool enableAutoSpawn = true;
    [SerializeField] private float minInterval = 5f;  // 最短何秒で次が流れるか
    [SerializeField] private float maxInterval = 15f; // 最長何秒待つか
    [SerializeField] private float minNonTextSpeed = 15f; // 少し速めにするなど
    [SerializeField] private float maxNonTextSpeed = 30f;
    [SerializeField] private float minNonTextSize = 0.3f; // 小さめにするなど
    [SerializeField] private float maxNonTextSize = 0.8f;

    [Header("Bloom設定 (HDR)")]
    [SerializeField] private float bloomIntensity = 2.5f;

    [Header("マップ範囲")]
    public Vector2 mapSize = new Vector2(150f, 150f);

    void Awake()
    {
        if (instance == null) instance = this;
    }

    void Start()
    {
        if (enableAutoSpawn)
        {
            StartCoroutine(AutoSpawnCoroutine());
        }
    }

    // 投稿ボタンから呼ばれる
    public void SpawnStar(ShootingStarData data)
    {
        // ★修正点: テキストの有無でパラメータを切り替える
        bool hasText = !string.IsNullOrEmpty(data.message);

        float targetMinSpeed = hasText ? minSpeed : minNonTextSpeed;
        float targetMaxSpeed = hasText ? maxSpeed : maxNonTextSpeed;

        float targetMinSize = hasText ? minSize : minNonTextSize;
        float targetMaxSize = hasText ? maxSize : maxNonTextSize;

        // 1. 位置と速度の計算 (右→左)
        float spawnX = (mapSize.x / 2f) + 10f;
        float spawnY = UnityEngine.Random.Range(-mapSize.y / 3f, mapSize.y / 3f);
        Vector3 startPos = new Vector3(spawnX, spawnY, 0);

        float angle = UnityEngine.Random.Range(165f, 195f);

        float currentSpeed = UnityEngine.Random.Range(targetMinSpeed, targetMaxSpeed);
        Vector3 velocity = Quaternion.Euler(0, 0, angle) * Vector3.right * currentSpeed * Time.deltaTime;
        // 左方向へのベクトル (165度〜195度)
        Transform parent = starRoot != null ? starRoot : this.transform;

        // クラス生成
        new ShootingStar(data, startPos, velocity, parent, targetMinSize, targetMaxSize, bloomIntensity);
    }

    // 環境演出として、定期的にテキスト無しの流れ星を流す
    private IEnumerator AutoSpawnCoroutine()
    {
        while (true)
        {
            // ランダムな時間待機
            float waitTime = UnityEngine.Random.Range(minInterval, maxInterval);
            yield return new WaitForSeconds(waitTime);

            // 空のデータを作成して流す
            ShootingStarData emptyData = new ShootingStarData("", "System");
            SpawnStar(emptyData);
        }
    }
}

// 流れ星の制御クラス
[Serializable]
public class ShootingStar
{
    private GameObject gameObject;
    private bool isWaste;
    private string messageContent;

    public ShootingStar(ShootingStarData data, Vector3 initial, Vector3 velocity, Transform parent, float minSize, float maxSize, float bloomIntensity)
    {
        this.messageContent = data.message;
        // メッセージがある時だけログを出す（自動生成でログが埋まるのを防ぐ）
        if (!string.IsNullOrEmpty(data.message))
        {
            Debug.Log($"【ShootingStar】生成開始: '{data.message}'");
        }

        if (ShootingStarManager.instance.prefab == null) return;

        // 1. 生成
        this.gameObject = MonoBehaviour.Instantiate(ShootingStarManager.instance.prefab, initial, Quaternion.identity);
        this.gameObject.transform.SetParent(parent, false);
        this.gameObject.transform.position = initial; // 親設定後に再配置

        // 2. 角度調整 (本体は進行方向に向ける: 左向きなど)
        float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
        this.gameObject.transform.rotation = Quaternion.Euler(0, 0, angle);

        // 「進行方向の傾き」は維持しつつ、「上下反転」だけを直して文字を読めるようにします。
        // Canvasを探して、テキストがあるかどうかで表示/非表示を切り替える
        Canvas worldCanvas = this.gameObject.GetComponentInChildren<Canvas>();
        TextMeshProUGUI tmp = this.gameObject.GetComponentInChildren<TextMeshProUGUI>();
        if (string.IsNullOrEmpty(data.message))
        {
            // ★テキストが無い(空)なら、Canvasごと非表示にする（星とTrailだけ見える）
            if (worldCanvas != null) worldCanvas.gameObject.SetActive(false);
        }
        else
        {
            // ★テキストがあるなら、角度を直してテキストセット
            if (worldCanvas != null)
            {
                worldCanvas.gameObject.SetActive(true);
                worldCanvas.transform.localRotation = Quaternion.Euler(0, 0, 180f);
            }

            if (tmp != null)
            {
                tmp.text = data.message;
            }
        }
        // 3. サイズランダム
        float scale = UnityEngine.Random.Range(minSize, maxSize);
        this.gameObject.transform.localScale = Vector3.one * scale;

        // 4. テキストセット (★修正点2: 文字化け/未反映の確認)
        if (tmp != null)
        {
            tmp.text = data.message;
            // Debug.Log("テキストセット完了: " + tmp.text);
        }
        else
        {
            Debug.LogError("【エラー】ShootingStarプレハブの中にTextMeshProUGUIが見つかりません！構造を確認してください。");
        }

        // 5. Bloom設定 & Trail表示修正 (★修正点3)
        Color hdrColor = new Color(bloomIntensity, bloomIntensity, bloomIntensity, 1f);

        // Sprite
        SpriteRenderer sr = this.gameObject.GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = hdrColor;
            sr.sortingOrder = 100; // 星を手前に
        }

        // Trail
        TrailRenderer tr = this.gameObject.GetComponentInChildren<TrailRenderer>();
        if (tr != null)
        {
            tr.startColor = hdrColor;
            tr.endColor = new Color(bloomIntensity, bloomIntensity, bloomIntensity, 0f); // 最後は透明

            // 重要: Trailが見えない原因の多くはLayer/Order問題
            tr.sortingLayerName = "Default";
            tr.sortingOrder = 90; // 星(100)より少し後ろ、背景(-10)より手前

            // 幅の設定（念の為）
            tr.widthMultiplier = 0.5f * scale;
            tr.time = 0.8f; // 軌跡が残る時間
            tr.emitting = true;
        }

        // 6. 移動開始
        ShootingStarManager.instance.StartCoroutine(ShootCoroutine(velocity));
    }

    private IEnumerator ShootCoroutine(Vector3 velocity)
    {
        float boundaryX = -(ShootingStarManager.instance.mapSize.x / 2f) - 30f;

        while (this.gameObject != null && !this.isWaste)
        {
            // 移動
            this.gameObject.transform.position += velocity;

            // 左端を超えたら消す
            if (this.gameObject.transform.position.x < boundaryX)
            {
                Stop();
                break;
            }

            yield return null;
        }
    }

    public void Stop()
    {
        this.isWaste = true;

        // メッセージがあった場合のみ「削除」ログを出す
        if (!string.IsNullOrEmpty(this.messageContent))
        {
            Debug.Log($"【DB削除予定】消滅: {this.messageContent}");
        }

        if (this.gameObject != null)
        {
            MonoBehaviour.Destroy(this.gameObject);
        }
    }
}
#endif
