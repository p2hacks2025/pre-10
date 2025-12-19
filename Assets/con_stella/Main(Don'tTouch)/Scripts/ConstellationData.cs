using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Junya;

/// <summary>
/// (コメントを添付したいインスタンス).Attach((添付したいコメント); でコメントを添付できる
/// 
/// ただし、
/// (コメントを添付したいインスタンス)の型は、ConstellationDataクラスでもCommentクラスでも動作する
/// (添付したいコメント)はCommentクラスのみ許容
/// 
/// 要はなんかにコメントを添付したいときは(それ).Attach((コメント))って書いてみて、エラーでたら俺に言って
/// </summary>
[Serializable]
public class ConstellationData : IAttachable
{
    public string guid; // 固有ID
    public string constellationName;
    public string createdAt;
    public string description;
    public List<StarData> stars = new List<StarData>();
    public List<ConnectionData> connections = new List<ConnectionData>();
    [HideInInspector]
    public Comment root;
    public int likeCount;
    public bool isLiked = false;  //いいね済みかどうか

    [NonSerialized] public GameObject gameObject;

    //データの紐づけのみを行うように変更。（表示は別でやる）
    public void Attach(ref Comment comment)
    {
        // rootがまだなければ作る
        if (this.root == null) root = new Comment("Root");
        {
            root.Attach(ref comment);
        }

        //this.root.children.Add(comment);
    }

    public ConstellationData()
    {

    }
}

[Serializable]
public class StarData
{
    public int id;
    public float x;
    public float y;
    public float scale;

    public StarData() { }

    public StarData(int id, float x, float y, float scale)
    {
        this.id = id;
        this.x = x;
        this.y = y;
        this.scale = scale;
    }
}

[Serializable]
public class ConnectionData
{
    public int fromStarId;
    public int toStarId;

    public ConnectionData() { }

    public ConnectionData(int from, int to)
    {
        this.fromStarId = from;
        this.toStarId = to;
    }
}

[Serializable]
public class ConstellationListWrapper
{
    public List<ConstellationData> list;

    public static ConstellationData GetConstellationByGuid(string guid)
    {
        foreach (ConstellationData data in MyPageManager.GetLocalData())
        {
            if (data.guid == guid) return data;
        }

        throw new UndefinedGuidException();
    }

    private class UndefinedGuidException : Exception { }
}