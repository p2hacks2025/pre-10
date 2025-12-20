using UnityEngine;
using UnityEngine.UI;

public class Frame2 : MonoBehaviour
{
    public ConstellationData data;
    [System.NonSerialized]public GameObject constellationParent;

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

    public static Frame2 ConstructFrame(ConstellationData data)
    {
        Frame2 result = Instantiate(MyPageManager.instance.framePrefab, MyPageManager.instance.transform.position, Quaternion.identity).transform.GetComponent<Frame2>();

        result.data = data;

        result.isExpanded = false;
        result.gameObject.name = $"Frame for \"{data.constellationName}\"";
        result.Text.transform.GetComponent<Text>().text = result.Date;

        result.constellationParent = MySkyProject2D.instance.GetConstellationObject(ref data, result.Anchor.transform.position);
        result.constellationParent.transform.SetParent(result.Anchor.transform);

        return result;
    }

    private bool isExpanded;

    public void Expand()
    {
        this.isExpanded = true;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    async void Update()
    {
        if (this.isExpanded)
        {
            
        }
    }
}
