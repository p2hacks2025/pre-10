using UnityEngine;

public class ConstellationClickTrigger : MonoBehaviour
{
    private ConstellationData myData;
    private SkyCameraController camController;

    public void Setup(ConstellationData data, SkyCameraController cam)
    {
        myData = data;
        camController = cam;
    }

    public ConstellationData GetData()
    {
        return myData;
    }
}