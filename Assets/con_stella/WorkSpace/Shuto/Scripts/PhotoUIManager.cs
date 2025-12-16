using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class PhotoUIManager : UIBaseManager
{
    [Header("UIパーツ")]
    [SerializeField] private RectTransform conSettingPanel; //星座設定パネル

    protected override void Start()
    {
        base.Start();

        // 最初は隠しておく（念の為）
        conSettingPanel.anchoredPosition = new Vector2(0, -panelHeight);
    }

    public void OnOKButtonClicked()
    {

    }
}