using UnityEngine;
using System.Collections.Generic;

public class MyPageManager : MonoBehaviour
{
    public GameObject prefab;
    public static MyPageManager instance;

    private static List<ConstellationData> MyConstellations;

    private static List<Frame> frames;

    public int lineLength;

    public Vector3 scale;

    public int scrollSpeed;

    private Vector3 GetAnchor(ConstellationData data)
    {
        float x = (MyConstellations.IndexOf(data) % lineLength) * scale.x;
        float y = -(MyConstellations.IndexOf(data) / lineLength) * scale.y;

        return new(x, y, 0);
    }

    private void Locate()
    {
        Vector3 criterion = MyPageManager.instance.transform.position;

        for (int i = 0; i < frames.Count; ++i)
        {
            frames[i].gameObject.transform.position = criterion + GetAnchor(frames[i].data);
        }
        
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        instance = this;

        MyConstellations = GetLocalData();

        frames = new();
        for(int i = 0; i < MyConstellations.Count; ++i)
        {
            frames.Add(new Frame(MyConstellations[i]));
        }

        Locate();//optimize locations of frames given their indexes

        foreach(Frame frame in frames)
        {
            SkyProject2D.StaticGenerate(frame.data, frame.gameObject.transform.Find("Scaler").Find("Anchor").position);

            frame.OptimizeConstellationScale();
        }
    }

    // Update is called once per frame
    void Update()
    {
        var tmp = Camera.main.transform.position;
        tmp.y += Input.GetAxis("Mouse ScrollWheel") * scrollSpeed;
        Camera.main.transform.position = tmp;
    }

    private List<ConstellationData> GetLocalData()
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
            return null;
        }

        // 2. JSONの中身を確認
        string json = PlayerPrefs.GetString("LocalSaveList");
        Debug.Log("【捜査2】JSONデータを発見: " + json);

        // 3. リストに復元できるか確認
        ConstellationListWrapper wrapper = JsonUtility.FromJson<ConstellationListWrapper>(json);

        if (wrapper == null)
        {
            Debug.LogError("【捜査エラー】JSONの解析に失敗しました。データが壊れています。");
            return null;
        }

        if (wrapper.list == null || wrapper.list.Count == 0)
        {
            Debug.LogError("【捜査エラー】リストの中身が空っぽ(0件)です！保存処理がうまくいっていません。");
            return null;
        }

        Debug.Log($"【捜査3】{wrapper.list.Count} 件のデータを確認。生成を開始します...");


        return wrapper.list;
    }
}


/*
string name = data.constellationName;
while (GameObject.Find(name) != null) name += "_";
*/