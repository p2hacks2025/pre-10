using UnityEngine;
using UnityEngine.UI;

public class Frame
{
    private ConstellationData data;
    private GameObject gameObject;

    private string Date
    {
        get => this.data.createdAt;
        set
        {
            this.data.createdAt = value;
            this.gameObject.transform.Find("Canvas").Find("Text").gameObject.GetComponent<Text>().text = value;
        }
    }


    public Frame(ConstellationData data)
    {
        this.data = data;

        this.gameObject = MonoBehaviour.Instantiate(MyPageManager.instance.prefab, MyPageManager.instance.transform.position, Quaternion.identity);
        this.Date = this.data.createdAt;

        
    }
}
