using UnityEngine;
using System.Collections.Generic;

public class MyPageManager : MonoBehaviour
{
    public GameObject prefab;
    public static MyPageManager instance;
    public static Dictionary<string, ConstellationData> MyCollections { get; private set; }

    private const int lineLength = 3;

    private ConstellationData this[int ]

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        instance = this;
        MyCollections = GetLocalData();

    }

    // Update is called once per frame
    void Update()
    {

    }

    private Dictionary<string, ConstellationData> GetLocalData()
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

        Dictionary<string, ConstellationData> result = new();

        foreach(ConstellationData data in wrapper.list)
        {
            result.Add(data.constellationName, data);

            Debug.Log(data.constellationName);
        }

        return result;
    }
}
