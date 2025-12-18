using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CommentListElement : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI contentText;
    [SerializeField] private Image iconImage;
    [SerializeField] private LayoutElement layoutElement;

    public void Setup(string text, Color color)
    {
        if (contentText != null)
        {
            contentText.text = text;
        }

        if (iconImage != null)
        {
            iconImage.color = color;
        }

        // 念の為、高さ確保をコードからも強制する
        if (layoutElement != null)
        {
            layoutElement.minHeight = 100f; // 最小高さを確保
        }
    }
}