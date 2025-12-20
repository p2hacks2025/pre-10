using UnityEngine;
using UnityEngine.UI;

public class Frame : MonoBehaviour
{
    public ConstellationData data;
    [System.NonSerialized]public GameObject constellationParent;
    public Button button;

    public bool isTryExpand;

    public GameObject Anchor
    {
        get => this.gameObject.transform.Find("Scaler").Find("Anchor").gameObject;
    }
    public GameObject Text
    {
        get => this.gameObject.transform.Find("Scaler").Find("Canvas").Find("Text").gameObject;
    }

    public string Date
    {
        get
        {
            Debug.Log(data?.createdAt);
            return this.data.createdAt.Split(" ")[0].Replace("/", ".").Trim();
        }
    }

    public static Frame ConstructFrame(ConstellationData data)
    {
        Frame result = Instantiate(MyPageManager.instance.framePrefab, MyPageManager.instance.transform.position, Quaternion.identity).transform.GetComponent<Frame>();

        result.data = data;

        result.isTryExpand = false;
        result.gameObject.name = $"Frame for \"{data.constellationName}\"";
        result.Text.transform.GetComponent<Text>().text = result.Date;

        result.constellationParent = MySkyProject2D.instance.GetConstellationObject(ref data, result.Anchor.transform.position);
        result.constellationParent.transform.SetParent(result.Anchor.transform);

        return result;
    }

    public void Expand()
    {
        this.isTryExpand = true;
    }

}
