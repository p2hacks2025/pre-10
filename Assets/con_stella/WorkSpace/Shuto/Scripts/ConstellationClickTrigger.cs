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

    void OnMouseDown() // 2Dコライダーをクリックした時に呼ばれるUnity標準機能
    {
        Debug.Log("星座がタップされました: " + myData.constellationName);

        // 1. カメラをここに寄せる
        if (camController != null)
        {
            camController.FocusOnTarget(transform.position);
        }

        // 2. 詳細UIを表示する（吹き出しなど）
        // UIManager.Instance.ShowDetail(myData.constellationName, ...); 
        // ※ここは後でUIを作りましょう
    }
}