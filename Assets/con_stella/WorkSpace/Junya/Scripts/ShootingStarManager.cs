#define new
using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System;

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

    [Header("設定")]
    public GameObject prefab;      // 流れ星のプレハブ
    [SerializeField] private Transform starRoot; // 流れ星を配置する親（Canvasとか）
    [SerializeField] private float speed = 5f;   // 速度
    [SerializeField] private float minSize = 0.5f; //最小の大きさ
    [SerializeField] private float maxSize = 1.0f;  //最大の大きさ

    [Header("マップ範囲設定")]
    public Vector2 mapSize = new Vector2(150f, 150f);

    // シングルトン化
    public static ShootingStarManager instance;

    void Awake()
    {
        if (instance == null) instance = this;
    }

    void Start()
    {
        // テスト用：3秒後にデモデータを流す
        StartCoroutine(DemoSpawnRoutine());
    }

    // デモ用
    IEnumerator DemoSpawnRoutine()
    {
        yield return new WaitForSeconds(1f);
        SpawnStar(new ShootingStarData("願い事が叶いますように！"));

        yield return new WaitForSeconds(2f);
        SpawnStar(new ShootingStarData("Unity完全に理解した"));
    }

    // 外部（UIやFireBase受信時）から呼ぶ関数
    public void SpawnStar(ShootingStarData data)
    {
        // 右端より少し右
        float spawnX = (mapSize.x / 2f) + 10f;

        // 高さランダム
        float spawnY = UnityEngine.Random.Range(-mapSize.y / 2f, mapSize.y / 2f) * 0.9f;

        Vector3 startPos = new Vector3(spawnX, spawnY, 0);

        // 速度ベクトルの計算
        // 左方向を中心に、少し角度を散らす(±10度)
        float angle = UnityEngine.Random.Range(170f, 190f);
        Vector3 velocity = Quaternion.Euler(0, 0, angle) * Vector3.right * speed * Time.deltaTime;

        // 親オブジェクトの決定
        Transform parent = starRoot != null ? starRoot : this.transform;

        new ShootingStar(data.message, startPos, velocity, parent);
    }
}

// チームメンバー作成のクラス（微修正版）
[Serializable]
public class ShootingStar
{
    private readonly GameObject gameObject;
    public string content;
    private bool isWaste;

    // コンストラクタに parent 引数を追加
    public ShootingStar(string content, Vector3 initial, Vector3 velocity, Transform parent)
    {
        // 仕様チェック
        if (velocity.y > 0f || velocity.z != 0f)
        {
            // 上向き禁止仕様ならYを反転させる等の安全策をとっても良い
            // throw new InvalidShootingException(); 
            Debug.LogWarning("上向きの流れ星は禁止されています。補正します。");
            velocity.y = -Mathf.Abs(velocity.y);
        }

        // 生成
        this.gameObject = MonoBehaviour.Instantiate(ShootingStarManager.instance.prefab, initial, Quaternion.identity);

        // 角度調整
        this.gameObject.transform.eulerAngles = new Vector3(0f, 0f, Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg + 180f);

        // 指定された親（Canvasなど）の下に配置
        this.gameObject.transform.SetParent(parent, false);
        // ワールド座標を再設定（SetParentでずれるかもしれないから）
        this.gameObject.transform.position = initial;

        // テキストセット
        // "Content"という名前の子オブジェクトにTextがある前提
        var textComp = this.gameObject.transform.Find("Content")?.GetComponent<Text>();
        if (textComp != null)
        {
            textComp.text = content;
        }
        else
        {
            // TextMeshProを使っている場合はここを変える必要があります
            //this.gameObject.GetComponentInChildren<TextMeshProUGUI>().text = content;
            Debug.LogWarning("Textコンポーネントが見つかりません");
        }

        // 移動開始
        ShootingStarManager.instance.StartCoroutine(ShootCoroutine(velocity));

        //5秒後に自動消滅（メモリリーク防止）
        MonoBehaviour.Destroy(this.gameObject, 15f);
    }

    private IEnumerator ShootCoroutine(Vector3 velocity)
    {
        // マップの境界線を取得 (Managerの設定を使う)
        Vector2 limit = ShootingStarManager.instance.mapSize / 2f;
        float destroyMargin = 20f; // 画面外にこれくらい出たら消す

        // 左端、上端、下端の限界ライン
        float minX = -limit.x - destroyMargin;
        float maxY = limit.y + destroyMargin;
        float minY = -limit.y - destroyMargin;

        while (this.gameObject != null && !this.isWaste)
        {
            this.gameObject.transform.position += velocity;

            Vector3 pos = this.gameObject.transform.position;

            // 左に突き抜けた or 上下に突き抜けた 場合に消滅
            if (pos.x < minX || pos.y > maxY || pos.y < minY)
            {
                Stop(); // 自爆
                break;
            }

            yield return null;
        }
    }

    public void Stop()
    {
        this.isWaste = true;
        if (this.gameObject != null) MonoBehaviour.Destroy(this.gameObject);
    }

    private class InvalidShootingException : Exception { }
}
#endif
