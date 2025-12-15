using UnityEngine;
using TMPro; // ★これがないと文字機能が使えません！

public class CommentManager : MonoBehaviour
{
    // Unityの画面でセットする箱を用意
    public TMP_InputField inputField;  // 入力欄
    public TextMeshProUGUI resultText; // 結果を表示するテキスト

    // ボタンが押されたら動く機能
    public void OnPost()
    {
        // 1. 入力された文字を取得する
        string message = inputField.text;

        // もし空っぽだったら何もしない
        if (message == "") return;

        // 2. 結果テキストに表示する（投稿した感）
        resultText.text = "投稿内容: " + message;

        // 3. 入力欄を空っぽに戻す（次の入力をしやすくする）
        inputField.text = "";

        // 4. コンソールにも表示（確認用）
        Debug.Log("コメントを保存しました: " + message);
    }
}