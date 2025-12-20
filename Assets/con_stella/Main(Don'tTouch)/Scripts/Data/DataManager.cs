using UnityEngine;
using System.Collections.Generic;

public class DataManager : MonoBehaviour
{
    public static DataManager instance;

    // ★全てのキーを一括管理（定数化）
    public const string LocalSaveListKey = "LocalSaveList";
    public const string MyConstellationGuidsKey = "MyConstellationGuids";
    public const string LikedGuidsKey = "LikedGuids";
    public const string NextFocusGUIDKey = "NextFocusGUID";

    void Awake()
    {
        if (instance == null) { instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }
    }

    // --- 星座データのセーブ（ローカル & クラウド） ---
    public void SaveConstellation(ConstellationData data)
    {
        SaveConstellationLocal(data);
        SaveConstellationFirebase(data);
    }

    // --- 星座データのローカル保存 ---
    public void SaveConstellationLocal(ConstellationData newData)
    {
        ConstellationListWrapper wrapper = LoadAllLocalData();
        wrapper.list.Add(newData);
        string json = JsonUtility.ToJson(wrapper);
        PlayerPrefs.SetString(LocalSaveListKey, json);
        PlayerPrefs.Save();
    }

    //Firebaseへの保存
    public void SaveConstellationFirebase(ConstellationData data)
    {
        FirebaseManager.instance?.SaveConstellation(data, (success) => {
            if (success) Debug.Log("【Data】クラウド保存/更新完了");
        });
    }

    // --- 自分の作成したGUIDリストへの保存 ---
    public void SaveMyConstellationGuid(string guid)
    {
        if (string.IsNullOrEmpty(guid)) return;
        string current = PlayerPrefs.GetString(MyConstellationGuidsKey, "");
        if (!current.Contains(guid))
        {
            current = string.IsNullOrEmpty(current) ? guid : current + "," + guid;
            PlayerPrefs.SetString(MyConstellationGuidsKey, current);
        }
    }


    // --- いいね済みの保存 ---
    public void SaveLikedGuid(string guid)
    {
        string current = PlayerPrefs.GetString(LikedGuidsKey, "");
        if (!current.Contains(guid))
        {
            current = string.IsNullOrEmpty(current) ? guid : current + "," + guid;
            PlayerPrefs.SetString(LikedGuidsKey, current);
            PlayerPrefs.Save();
        }
    }

    // --- フォーカス対象の保存 ---
    public void SaveNextFocusGuid(string guid) => PlayerPrefs.SetString(NextFocusGUIDKey, guid);

    // --- データの読み込み ---
    public ConstellationListWrapper LoadAllLocalData()
    {
        if (PlayerPrefs.HasKey(LocalSaveListKey))
        {
            string json = PlayerPrefs.GetString(LocalSaveListKey);
            return JsonUtility.FromJson<ConstellationListWrapper>(json) ?? new ConstellationListWrapper();
        }
        return new ConstellationListWrapper { list = new List<ConstellationData>() };
    }
}