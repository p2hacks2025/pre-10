using UnityEngine;
using UnityEngine.UI;

public class Frame
{
    public readonly ConstellationData data;
    public readonly GameObject gameObject;

    private string Date
    {
        get => this.data.createdAt;
        set
        {
            this.data.createdAt = value;
            this.gameObject.transform.Find("Scaler").Find("Canvas").Find("Text").gameObject.GetComponent<Text>().text = value.Split(" ")[0].Trim();
        }
    }


    public Frame(ConstellationData data)
    {
        this.data = data;

        this.gameObject = MonoBehaviour.Instantiate(MyPageManager.instance.prefab, MyPageManager.instance.transform.position, Quaternion.identity);
        this.Date = this.data.createdAt;

    }

    public void OptimizeConstellationScale()
    {
        if(GameObject.Find(this.data.constellationName) != null)
        {
            (float width, float height) scale = this.ConstellationScale;

            GameObject.Find(this.data.constellationName).transform.localScale /= Mathf.Max(scale.width, scale.height);
        }
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
