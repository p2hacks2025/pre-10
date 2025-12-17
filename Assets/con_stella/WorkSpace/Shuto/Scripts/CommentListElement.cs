using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class CommentListElement : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private Text contentText;

    // データをセットして表示を更新する
    public void Setup(string content, Color iconColor)
    {
        if (contentText != null) contentText.text = content;
        if (iconImage != null) iconImage.color = iconColor;
    }
}