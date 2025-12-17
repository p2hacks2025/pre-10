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
    public string description;
    public List<StarData> stars = new List<StarData>();
    public List<ConnectionData> connections = new List<ConnectionData>();
    /*最初からnullだと、参照するときに、ぬるぽを吐くから最初は初期化しない
     * 代わりに、必要になったら初期化する（Attach()の部分で）*/
    [NonSerialized] public Comment root;
    public int likeCount;

    //データの紐づけのみを行うように変更。（表示は別でやる）
    public void Attach(ref Comment comment)
    {
        // rootがまだなければ作る
        if (this.root == null)
        {
            // CommentManager が存在している時限定
            this.root = new Comment(null);
        }
        /*
        comment.gameObject.transform.SetParent(GameObject.Find(this.constellationName).transform);
        this.root.children.Add(comment);
        comment.parent = this;
        comment.gameObject.transform.position = GameObject.Find(this.constellationName).transform.position;
        */
        this.root.children.Add(comment);
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
<<<<<<< HEAD
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