using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
public class Frame
{
    public ConstellationData data;
    public GameObject gameObject;

    public bool isClicked;@

    public GameObject Anchor
    {
        get => this.gameObject.transform.Find("Scaler").Find("Anchor").gameObject;
    }
    public GameObject Text
    {
        get
        {
            Debug.Log(this.gameObject.name);
            return this.gameObject.transform.Find("Scaler").Find("Canvas").Find("Text").gameObject;

        }
    }

    public string Date
    {
        get => this.data.createdAt;
        set
        {
            //Debug.Log(value);
            string formatted = value.Split(" ")?[0].Replace("/", ".").Trim();
            //Debug.Log(formatted);

            this.data.createdAt = value;
            this.Text.transform.GetComponent<Text>().text = formatted;
        }
    }


    public Frame(ConstellationData data)
    {
        this.data = data;
        this.isClicked = false;

        //this.gameObject = MonoBehaviour.Instantiate(MyPageManager.instance.prefab, MyPageManager.instance.transform.position, Quaternion.identity);

        //GameObject.Find("MySkyManager").transform.GetComponent<MySkyProject2D>().GetConstellationObject(ref data, this.Anchor.transform.position);
    }
    
    public async UniTask Expand()
    {
        await UniTask.Yield();
        Debug.Log("expanded");
        //this.gameObject.
    }

    public void OptimizeConstellationScale()
    {
        if(data.gameObject != null)
        {
            (float width, float height) scale = this.ConstellationScale;

            data.gameObject.transform.localScale /= Mathf.Max(scale.width, scale.height);
        }
    }


    public void Log()
    {
        Debug.Log("clicked");
    }


    private (float width, float height) ConstellationScale
    {
        get
        {

            float right = this.data.stars[0].x;
            float left = right;
            float up = this.data.stars[0].y;
            float down = up;
            foreach (StarData star in this.data.stars)
            {
                if (right < star.x) right = star.x;
                if (left > star.x) left = star.x;
                if(up < star.y) up = star.y;
                if(down > star.y) down = star.y;
            }

            float width = Mathf.Abs(right - left);
            float height = Mathf.Abs(up - down);

            return (width, height);

        }
    }
}
