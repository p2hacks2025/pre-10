#define frame2

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.UI;

using System.Linq;

using Cysharp.Threading.Tasks;

#if frame
public class MyPageManager : MonoBehaviour
{
    private static Frame focused;

    public GameObject mainCamera;
    public GameObject prefab;
    public GameObject mask;
    public GameObject pointer;
    public GameObject button;

    public static MyPageManager instance;
    
    private static List<ConstellationData> MyConstellations;

    private static List<Frame> frames;
    private static List<Button> buttons;

    public int lineLength;

    public Vector3 scale;

    public int scrollSpeed;

    private Vector3 GetAnchor(ConstellationData data)
    {
        float x = (MyConstellations.IndexOf(data) % lineLength) * scale.x;
        float y = -(MyConstellations.IndexOf(data) / lineLength) * scale.y;

        return new(x, y, 0);
    }

    private void Locate()
    {
        Vector3 criterion = MyPageManager.instance.transform.position;

        for (int i = 0; i < frames.Count; ++i)
        {
            frames[i].gameObject.transform.position = criterion + GetAnchor(frames[i].data);
            buttons[i].gameObject.transform.position = criterion + GetAnchor(frames[i].data);
        }
        
    }



    private async UniTask MoveCameraSmoothly(GameObject targetAnchor)
    {
        Vector3 defaultPosition = this.mainCamera.transform.position;
        Vector3 orbit = targetAnchor.transform.position - this.mainCamera.transform.position;

        float duration = 0f;
        while(duration <= 1f)
        {
            this.mainCamera.transform.position = defaultPosition + orbit * Curve(duration);

            duration += Time.deltaTime;
            await UniTask.Yield();
        }

        this.mainCamera.transform.position = targetAnchor.transform.position;
    }

    private static float Curve(float x)
    {
        if (x < 0) throw new System.Exception();
        else if (x < 1) return (-Mathf.Cos(Mathf.PI * x) + 1) / 2;
        else return 1;
    }

    private async UniTask FocusTo(Frame target)
    {
        focused = target;

        Vector3 defaultCameraPosition = mainCamera.transform.position;

        IEnumerable<Frame> others = from frame in frames where frame != target select frame;

        foreach(Frame other in others)
        {
            other.gameObject.SetActive(false);
        }

        //カメラ位置調整
        await MoveCameraSmoothly(target.Anchor);
        
        //展開
        await target.Expand();

        foreach (Frame other in others)
        {
            other.gameObject.SetActive(true);
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        instance = this;

        MyConstellations = GetLocalData();

        frames = new();
        buttons = new();
        for(int i = 0; i < MyConstellations.Count; ++i)
        {
            frames.Add(new Frame(MyConstellations[i]));
            frames[i].gameObject = MySkyProject2D.instance.GetConstellationObject(ref frames[i].data, Vector3.zero);

            buttons.Add(Instantiate(this.button, Vector3.zero, Quaternion.identity).transform.GetComponent<Button>());
            buttons[i].gameObject.transform.GetComponent<Button>().onClick.AddListener(() => frames[i].isClicked = true);
            frames[i].Date = frames[i].data.createdAt;
        }

        Locate();//optimize locations of frames given their indexes

        for (int i = 0; i < MyConstellations.Count; ++i)
        {

            
            //frame.gameObject.transform.SetParent(this.mask.transform);
            //frame.OptimizeConstellationScale();
        }
    }

    private Vector3 PointerPosition
    {
        get => Camera.main.ScreenToWorldPoint(Pointer.current.position.ReadValue());
    }

    private Vector3 pPointerPosition;
    
    private float DeltaY
    {
        get => this.PointerPosition.y - this.pPointerPosition.y;
    }

    // Update is called once per frame
    async void Update()
    {
        if (focused == null)
        {

            pointer.transform.position = PointerPosition;
            if (Input.GetMouseButton(0))
            {
                Debug.Log("mouseing");

                mainCamera.transform.position += Vector3.down * DeltaY;

            }
            this.pPointerPosition = PointerPosition;

        }
        else
        {

        }


    }

    public static List<ConstellationData> GetLocalData()
    {
        // 1. キーがあるか確認
        if (!PlayerPrefs.HasKey("LocalSaveList"))
        {
            Debug.LogError("【捜査エラー】セーブデータ 'LocalSaveList' が見つかりません！保存ボタンを押しましたか？");

            // もし古いキー(TestSaveData)が残っているなら、教えてあげる
            if (PlayerPrefs.HasKey("TestSaveData"))
            {
                Debug.LogWarning("※ 'TestSaveData' は見つかりました。保存側のコードが古い（リスト保存になっていない）可能性があります。");
            }
            return null;
        }

        // 2. JSONの中身を確認
        string json = PlayerPrefs.GetString("LocalSaveList");
        Debug.Log("【捜査2】JSONデータを発見: " + json);

        // 3. リストに復元できるか確認
        ConstellationListWrapper wrapper = JsonUtility.FromJson<ConstellationListWrapper>(json);

        if (wrapper == null)
        {
            Debug.LogError("【捜査エラー】JSONの解析に失敗しました。データが壊れています。");
            return null;
        }

        if (wrapper.list == null || wrapper.list.Count == 0)
        {
            Debug.LogError("【捜査エラー】リストの中身が空っぽ(0件)です！保存処理がうまくいっていません。");
            return null;
        }

        Debug.Log($"【捜査3】{wrapper.list.Count} 件のデータを確認。生成を開始します...");


        return wrapper.list;
    }
}


/*
string name = data.constellationName;
while (GameObject.Find(name) != null) name += "_";
*/

#elif frame2

public class MyPageManager : MonoBehaviour
{
    public static MyPageManager instance;
    [Header("Prefabs")]
    public GameObject framePrefab;
    public GameObject buttonPrefab;

    [Header("RawObjects")]
    public GameObject pointer;

    [Header("Preferences")]
    public Vector2 space;
    public int row;

    private Rigidbody2D rigidbody;

    private List<Frame2> frames;
    private List<Button> buttons;

    async void Start()
    {
        instance = this;

        List<ConstellationData> datas = GetLocalData();

        this.frames = new();
        this.buttons = new();
        for (int index = 0; index < datas.Count; ++index)
        {
            this.frames.Add(Frame2.ConstructFrame(this, datas[index]));
            this.buttons.Add(Instantiate(this.buttonPrefab, this.transform.position, Quaternion.identity).transform.GetComponent<Button>());
            this.frames[index].transform.SetParent(this.transform);
            this.buttons[index].transform.SetParent(frames[index].transform);

            frames[index].transform.position = this.transform.position + new Vector3(index % row * space.x, -index / row * space.y, 0f);
            buttons[index].transform.position = frames[index].transform.position;

            buttons[index].onClick.AddListener(() => FocusOn(frames[index]));
        }

        this.rigidbody = this.transform.GetComponent<Rigidbody2D>();
    }


    private float pvalue;
    async void Update()
    {
        float value = Camera.main.ScreenToWorldPoint(Pointer.current.position.ReadValue()).y;
        float delta = value - pvalue;

        ////
        Debug.Log("delta = " + delta);

        if (Input.GetMouseButton(0))
        {
            this.rigidbody.AddForce(Vector3.up * delta * 1000f);
            //this.transform.position += Vector3.up * delta;
        }
        this.rigidbody.AddForce(-Vector3.up * this.rigidbody.linearVelocity.y);
        ////
        
        pvalue = value;
    }

    private void FocusOn(Frame2 frame)
    {

    }



    public static List<ConstellationData> GetLocalData()
    {
        // 1. キーがあるか確認
        if (!PlayerPrefs.HasKey("LocalSaveList"))
        {
            Debug.LogError("【捜査エラー】セーブデータ 'LocalSaveList' が見つかりません！保存ボタンを押しましたか？");

            // もし古いキー(TestSaveData)が残っているなら、教えてあげる
            if (PlayerPrefs.HasKey("TestSaveData"))
            {
                Debug.LogWarning("※ 'TestSaveData' は見つかりました。保存側のコードが古い（リスト保存になっていない）可能性があります。");
            }
            return null;
        }

        // 2. JSONの中身を確認
        string json = PlayerPrefs.GetString("LocalSaveList");
        Debug.Log("【捜査2】JSONデータを発見: " + json);

        // 3. リストに復元できるか確認
        ConstellationListWrapper wrapper = JsonUtility.FromJson<ConstellationListWrapper>(json);

        if (wrapper == null)
        {
            Debug.LogError("【捜査エラー】JSONの解析に失敗しました。データが壊れています。");
            return null;
        }

        if (wrapper.list == null || wrapper.list.Count == 0)
        {
            Debug.LogError("【捜査エラー】リストの中身が空っぽ(0件)です！保存処理がうまくいっていません。");
            return null;
        }

        Debug.Log($"【捜査3】{wrapper.list.Count} 件のデータを確認。生成を開始します...");


        return wrapper.list;
    }
}

#endif