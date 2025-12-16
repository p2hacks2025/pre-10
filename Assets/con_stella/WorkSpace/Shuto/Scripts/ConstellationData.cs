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
    public string constellationName;
    public string createdAt;
    public List<StarData> stars = new List<StarData>();
    public List<ConnectionData> connections = new List<ConnectionData>();
    public Comment root = new(null);
    //public int likeCount;

    public void Attach(ref Comment comment)
    {
        comment.gameObject.transform.SetParent(GameObject.Find(this.constellationName).transform);
        this.root.children.Add(comment);
        comment.parent = this;
        comment.gameObject.transform.position = GameObject.Find(this.constellationName).transform.position;

    }

    public ConstellationData()
    {

    }
}

/* 俺がいじる前のConstellationDataクラス
[Serializable]
public class ConstellationData
{
    public string constellationName;
    public string createdAt;
    public List<StarData> stars = new List<StarData>();
    public List<ConnectionData> connections = new List<ConnectionData>();
    //public Node<string> root = new Node<string>();
    //public int likeCount;

} 
 */

[Serializable]
public class StarData
{
    public int id;
    public float x;
    public float y;
    public float scale;
}

[Serializable]
public class ConnectionData
{
    public int fromStarId;
    public int toStarId;
}

[Serializable]
public class ConstellationListWrapper
{
    public List<ConstellationData> list = new List<ConstellationData>();
}