using System;

[Serializable]
public class ShootingStarData
{
    public string message;      // メッセージ内容
    public string senderName;   // 送信者（任意）
    public string createdAt;    // 作成日時（ソート用）

    // コンストラクタ
    public ShootingStarData(string msg, string name = "Anonymous")
    {
        this.message = msg;
        this.senderName = name;
        this.createdAt = DateTime.Now.ToString();
    }
}