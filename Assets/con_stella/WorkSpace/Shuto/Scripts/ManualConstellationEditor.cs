using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System; // Serializableに必要

public class ManualConstellationEditor : MonoBehaviour
{
    [Header("設定")]
    [SerializeField] private GameObject starPrefab;
    [SerializeField] private GameObject linePrefab;
    [SerializeField] private Transform workAreaRoot; // 星座を作る親オブジェクト

    [Header("描画範囲の設定")]
    // このRectTransformの中でしか描けないように制限する
    [SerializeField] private RectTransform drawingArea;

    [Header("星のサイズ設定")]
    [SerializeField] private float minStarScale = 3.0f; // 最小サイズ（大きめに設定）
    [SerializeField] private float maxStarScale = 5.0f; // 最大サイズ

    [Header("操作パラメータ")]
    //[SerializeField] private float touchRadius = 50f; // 星をタップしたと判定する距離(px)

    // 管理用リスト
    private List<GameObject> myStars = new List<GameObject>(); //星
    private List<LineData> myLines = new List<LineData>();     //線
    private GameObject selectedStar = null; // 今選んでいる星

    /// <summary>
    /// 線データの定義（このクラスで線と星の関係を覚えます）
    /// </summary>
    private class LineData
    {
        public GameObject lineObject; // 線の実体
        public GameObject starA;      // つながっている星1
        public GameObject starB;      // つながっている星2

    }
    void Update()
    {
        // 画面タップ（クリック）を検知
        if (Pointer.current != null && Pointer.current.press.wasPressedThisFrame)
        {
            Vector2 screenPos = Pointer.current.position.ReadValue();

            // 指定した範囲の外なら無視する
            if (drawingArea != null)
            {
                // Canvasが "Screen Space - Camera" の場合はカメラを渡す必要がある
                if (!RectTransformUtility.RectangleContainsScreenPoint(drawingArea, screenPos, Camera.main))
                {
                    return; // 範囲外なので何もしない
                }
            }

            // UI（ボタンなど）の上なら無視する
            if (IsPointerOverUI())
            {
                return;
            }

            HandleInput();
        }
    }

    //ボタン上かどうか
    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;
        return EventSystem.current.IsPointerOverGameObject();
    }

    private void HandleInput()
    {
        // カーソル位置の取得
        Vector2 screenPos = Pointer.current.position.ReadValue();

        // タップした画面座標を、ワールド座標(Z=0)に変換
        Vector3 mousePos = new Vector3(screenPos.x, screenPos.y, 10f); // Zはカメラ距離
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);
        worldPos.z = 0f; // Zは0に固定

        // 近くに既存の星があるかチェック
        GameObject hitStar = FindNearestStar(worldPos);

        if (hitStar != null)
        {
            // --- 星をタップした場合 ---
            OnTapStar(hitStar);
        }
        else
        {
            // --- 何もない場所をタップした場合 ---
            CreateStar(worldPos);
        }
    }

    // 近くの星を探す関数
    private GameObject FindNearestStar(Vector3 pos)
    {
        GameObject nearest = null;
        float minDist = float.MaxValue;

        foreach (var star in myStars)
        {
            float dist = Vector3.Distance(pos, star.transform.position);
            // スクリーン座標ではなくワールド座標での距離判定になるので注意
            // ここでは簡易的に判定
            if (dist < 1.0f) // 判定範囲（適宜調整）
            {
                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = star;
                }
            }
        }
        return nearest;
    }

    // 星を作って配置
    private void CreateStar(Vector3 pos)
    {
        GameObject newStar = Instantiate(starPrefab, workAreaRoot);
        newStar.transform.position = pos;
        //設定した範囲内でランダムな大きさを適用
        float randomScale = UnityEngine.Random.Range(minStarScale, maxStarScale);
        newStar.transform.localScale = Vector3.one * randomScale;
        myStars.Add(newStar);

        // 作った星を自動選択する
        SelectStar(newStar);
    }

    // 星をタップした時の処理
    private void OnTapStar(GameObject star)
    {
        if (selectedStar == null)
        {
            // 選択されていないなら選択する
            SelectStar(star);
        }
        else if (selectedStar == star)
        {
            // 同じ星なら選択解除
            DeselectStar();
        }
        else
        {
            // 違う星なら「線を引く」！
            CreateLine(selectedStar, star);
            // 連続で線を引けるように、今の星を選択状態にする
            SelectStar(star);
        }
    }

    // 線を引く
    private void CreateLine(GameObject starA, GameObject starB)
    {
        // 既に同じ線があるかチェック（二重線防止）
        if (HasConnection(starA, starB)) return;

        GameObject newLine = Instantiate(linePrefab, workAreaRoot);
        LineRenderer lr = newLine.GetComponent<LineRenderer>();

        // 線の設定
        lr.positionCount = 2;
        lr.useWorldSpace = true; // ここではWorldSpaceの方が管理しやすい
        lr.SetPosition(0, starA.transform.position);
        lr.SetPosition(1, starB.transform.position);
        lr.sortingOrder = 90;

        // リストに「この線はAとBをつないでいる」と記録する
        LineData data = new LineData();
        data.lineObject = newLine;
        data.starA = starA;
        data.starB = starB;
        myLines.Add(data);
    }

    // 既に繋がっているかチェックする便利関数
    private bool HasConnection(GameObject a, GameObject b)
    {
        foreach (var line in myLines)
        {
            if ((line.starA == a && line.starB == b) || (line.starA == b && line.starB == a))
            {
                return true;
            }
        }
        return false;
    }

    // 選択状態の見た目変更
    private void SelectStar(GameObject star)
    {
        DeselectStar(); // 前の選択を解除
        selectedStar = star;
        // 色を赤くするなど（SpriteRendererを取得して色変更）
        var renderer = star.GetComponent<SpriteRenderer>();
        if (renderer != null) renderer.color = Color.red;
    }

    private void DeselectStar()
    {
        if (selectedStar != null)
        {
            // 色を白に戻す
            var renderer = selectedStar.GetComponent<SpriteRenderer>();
            if (renderer != null) renderer.color = Color.white;
            selectedStar = null;
        }
    }

    /// <summary>
    /// 選択中の星と、それにつながる線を削除する（ボタン用）
    /// </summary>
    public void ClearSelectedStar()
    {
        if (selectedStar == null) return;

        GameObject targetStar = selectedStar;

        // 1. この星につながっている線を全て探して削除
        // リストを逆順に回して削除していく（ループ中の削除対策）
        for (int i = myLines.Count - 1; i >= 0; i--)
        {
            LineData line = myLines[i];

            // 星A か 星B のどちらかが削除対象なら、この線も道連れにする
            if (line.starA == targetStar || line.starB == targetStar)
            {
                if (line.lineObject != null) Destroy(line.lineObject);
                myLines.RemoveAt(i);
            }
        }

        // 2. 星自体を削除
        if (myStars.Contains(targetStar))
        {
            myStars.Remove(targetStar);
        }
        Destroy(targetStar);

        // 3. 選択状態を解除
        selectedStar = null;
    }

    /// <summary>
    /// 全消去機能（ボタン用）
    /// </summary>
    public void ClearAll()
    {
        foreach (Transform child in workAreaRoot) Destroy(child.gameObject);
        myStars.Clear();
        myLines.Clear(); // 線リストもクリア
        selectedStar = null;
    }

    /// <summary>
    /// 【テスト用】現在の星座をJSONにしてローカル保存（PlayerPrefs）
    /// </summary>
    public void TestSaveLocal()
    {
        // 1. 保存用データの器を作る
        ConstellationData newData = new ConstellationData();
        newData.constellationName = "Manual Constellation";
        newData.createdAt = System.DateTime.Now.ToString();
        //newData.likeCount = 0;
        //newData.commentRoot = new CommentNode("ROOT");

        // 2. 星を保存データに変換 & ID割り振り
        // 「GameObject」と「ID(0,1,2...)」の対応表を作る
        Dictionary<GameObject, int> objToIdMap = new Dictionary<GameObject, int>();
        int currentId = 0;

        foreach (var starObj in myStars)
        {
            if (starObj == null) continue;

            StarData sData = new StarData();
            sData.id = currentId;
            // 親オブジェクト(workAreaRoot)からの相対座標で保存するのが安全
            sData.x = starObj.transform.localPosition.x;
            sData.y = starObj.transform.localPosition.y;
            sData.scale = starObj.transform.localScale.x;

            newData.stars.Add(sData);

            // マップに記録
            objToIdMap[starObj] = currentId;
            currentId++;
        }

        // 3. 線を保存データに変換
        foreach (var line in myLines)
        {
            if (line.lineObject == null) continue;

            // 両端の星が正しくID管理されているかチェック
            if (objToIdMap.ContainsKey(line.starA) && objToIdMap.ContainsKey(line.starB))
            {
                ConnectionData cData = new ConnectionData();
                cData.fromStarId = objToIdMap[line.starA];
                cData.toStarId = objToIdMap[line.starB];
                newData.connections.Add(cData);
            }
        }

        ConstellationListWrapper wrapper = new ConstellationListWrapper();
        if (PlayerPrefs.HasKey("LocalSaveList"))
        {
            string json = PlayerPrefs.GetString("LocalSaveList");
            wrapper = JsonUtility.FromJson<ConstellationListWrapper>(json);
        }

        // 2. リストに追加
        wrapper.list.Add(newData);

        // 3. 保存
        string newJson = JsonUtility.ToJson(wrapper);
        PlayerPrefs.SetString("LocalSaveList", newJson);
        PlayerPrefs.Save();

        Debug.Log($"手書き星座をリストに追加保存しました！(全{wrapper.list.Count}件)");
    }

    /// <summary>
    /// 【テスト用】ローカル保存されたJSONを読み込んで復元
    /// </summary>
    public void TestLoadLocal()
    {
        // 1. 保存データがあるか確認
        if (!PlayerPrefs.HasKey("TestSaveData"))
        {
            Debug.LogWarning("保存されたデータがありません");
            return;
        }

        // 2. JSONを取得してクラスに復元
        string json = PlayerPrefs.GetString("TestSaveData");
        Debug.Log("【JSON読み込み】\n" + json);

        ConstellationData loadedData = JsonUtility.FromJson<ConstellationData>(json);

        // 3. 画面をクリアして再構築開始
        ClearAll();

        // 復元用の「ID -> 生成されたGameObject」対応表
        Dictionary<int, GameObject> idToObjMap = new Dictionary<int, GameObject>();

        // 星を復元
        foreach (var sData in loadedData.stars)
        {
            GameObject newStar = Instantiate(starPrefab, workAreaRoot);
            newStar.transform.localPosition = new Vector3(sData.x, sData.y, 0);
            newStar.transform.localScale = Vector3.one * sData.scale;

            myStars.Add(newStar);

            // IDと実体を紐付ける
            idToObjMap[sData.id] = newStar;
        }

        // 線を復元
        foreach (var cData in loadedData.connections)
        {
            if (idToObjMap.ContainsKey(cData.fromStarId) && idToObjMap.ContainsKey(cData.toStarId))
            {
                GameObject starA = idToObjMap[cData.fromStarId];
                GameObject starB = idToObjMap[cData.toStarId];

                // 既存の線引きメソッドを使って線を引く
                // （CreateLine関数は既存のコードにある前提）
                CreateLineUsingExistingLogic(starA, starB);
            }
        }
    }

    // CreateLineはprivateかもしれないので、ロード用のラップ関数か、
    // 既存のCreateLineをpublic/internalにするか、同じ処理を書く
    private void CreateLineUsingExistingLogic(GameObject starA, GameObject starB)
    {
        // 前回のCreateLineの中身と同じ処理
        GameObject newLine = Instantiate(linePrefab, workAreaRoot);
        LineRenderer lr = newLine.GetComponent<LineRenderer>();
        if (lr != null)
        {
            lr.positionCount = 2;
            lr.useWorldSpace = true;
            lr.SetPosition(0, starA.transform.position);
            lr.SetPosition(1, starB.transform.position);
            lr.sortingOrder = 90;
        }

        LineData data = new LineData();
        data.lineObject = newLine;
        data.starA = starA;
        data.starB = starB;
        myLines.Add(data);
    }
}