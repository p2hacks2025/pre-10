#define frame2

using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static UnityEditor.IMGUI.Controls.CapsuleBoundsHandle;

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

    private async UniTask FocusOn(Frame target)
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
    public GameObject anchor;
    public GameObject description;
    public GameObject upperSpace;

    [Header("Preferences")]
    public Vector2 space;
    public int row;
    public float forceFactor;
    public float frictionFactor;

    new private Rigidbody2D rigidbody;

    private List<Frame2> frames;
    private List<Button> buttons;

    private bool isInFocusView;
    private bool isTryExit;

    private float height;
    private float defaultY;

    private float deltaY
    {
        get
        {
            return this.transform.position.y - defaultY;
        }
        set
        {
            Vector3 tmp = this.transform.position;
            tmp.y = value + defaultY;
            this.transform.position = tmp;
        }
    }

    void Start()
    {
        instance = this;

        this.rigidbody = this.transform.GetComponent<Rigidbody2D>();

        List<ConstellationData> datas = GetLocalData();

        Debug.Log("datas.Count = " + datas.Count);

        this.frames = new();
        this.buttons = new();
        for (int index = 0; index < datas.Count; ++index)
        {
            Debug.Log("datas[index].constellationName = " + datas[index].constellationName);
            Debug.Log("datas[index].descirption = " + datas[index].description);

            this.frames.Add(Frame2.ConstructFrame(datas[index]));
            this.buttons.Add(Instantiate(this.buttonPrefab, this.transform.position, Quaternion.identity).transform.GetComponent<Button>());
            this.frames[index].transform.SetParent(this.transform);
            this.buttons[index].transform.SetParent(frames[index].transform);

            frames[index].transform.position = this.transform.position + new Vector3(index % row * space.x, -index / row * space.y, 0f);
            buttons[index].transform.position = frames[index].transform.position;

            buttons[index].onClick.AddListener(() => FocusOn(frames[index]));
        }

        this.height = (int)(this.frames.Count / row) * space.y /*+ this.framePrefab.transform.localScale.y*/;
        this.defaultY = this.transform.position.y /*+ this.framePrefab.transform.localScale.y*/;

        this.isInFocusView = false;
    }

    private float pvalue;
    async void Update()
    {
        if (!this.isInFocusView)
        {

            float value = Camera.main.ScreenToWorldPoint(Pointer.current.position.ReadValue()).y;
            float delta = value - pvalue;

            foreach (Frame2 frame in frames)
            {
                if (frame.isTryExpand) FocusOn(frame).Forget();
            }

            if ((int)(this.frames.Count / 3) >= 2)
            {

                if (Input.GetMouseButton(0))
                {
                    this.rigidbody.AddForce(Vector3.up * delta * forceFactor);
                    //this.transform.position += Vector3.up * delta;
                }
                this.rigidbody.AddForce(-Vector3.up * frictionFactor * this.rigidbody.linearVelocity.y);

                pvalue = value;

                if (deltaY < -height)
                {
                    deltaY = -height;
                    this.rigidbody.linearVelocity = Vector3.zero;
                    //this.transform.position = new(this.transform.position.x, this.anchor.transform.position.y);
                }
                else if (deltaY > height)
                {
                    //this.transform.position = new(this.transform.position.x, this.anchor.transform.position.y);
                    deltaY = height;
                    this.rigidbody.linearVelocity = Vector3.zero;
                }

            }
        }

        else
        {
            if (Input.GetKeyDown(KeyCode.E)) Exit();
        }
    }

    public void Exit()
    {
        Debug.LogWarning("exit");
        this.isTryExit = true;
    }

    public async UniTask FocusOn(Frame2 target)
    {
        this.isInFocusView = true;

        for(int index = 0; index < frames.Count; ++index)
        {
            frames[index].isTryExpand = false;
        }

        await UniTask.Delay(100);

        upperSpace.transform.GetComponent<SpriteRenderer>().sortingOrder = 0;
        this.rigidbody.linearVelocity = Vector3.zero;

        MoveOthers(target).Forget();
        await UniTask.WhenAll(MoveTarget(target));
        /*
        MoveOthers(frame).Forget();
        MoveTarget(frame).Forget();
        */

        this.isInFocusView = false;
    }

    private async UniTask WaitExit()
    {
        while (!this.isTryExit)
        {
            await UniTask.Yield();
        }
    }
    private async UniTask WaitNotFocusOn()
    {
        while (this.isInFocusView)
        {
            await UniTask.Yield();
        }
    }

    private async UniTask MoveOthers(Frame2 frame)
    {
        /*
        List<Frame2> others = (from other in this.frames where other != frame select other).ToList();
        List<Vector3> defaultPositions = (from other in others select other.transform.position + new Vector3((int)(this.frames.IndexOf(other) % row) * space.x, -((int)this.frames.IndexOf(other) / row) * space.y, 0f)).ToList();
        */

        Vector3[] defaultPositions = new Vector3[this.frames.Count];
        for (int index = 0; index < this.frames.Count; ++index)
        {
            defaultPositions[index] = this.transform.position + new Vector3(index % row * space.x, -index / row * space.y, 0f) /*+ Vector3.up * this.transform.position.y*/;
        }


        float duration = 0;

        float speed = 1.5f;
        while (duration < 1.5f)
        {
            
            for (int index = 0; index < this.frames.Count(); ++index)
            {
                if (this.frames[index] != frame)
                {

                    frames[index].gameObject.transform.position = defaultPositions[index] + Vector3.right * Curve(duration * speed) * 20f;
                }
            }

            duration += Time.deltaTime;

            await UniTask.Yield();
        }

        for (int index = 0; index < this.frames.Count(); ++index)
        {
            if (this.frames[index] != frame) frames[index].transform.position = defaultPositions[index] + Vector3.right * 20f;
        }

        //exit
        await WaitNotFocusOn();

        duration = 0;

        speed = 1.8f;
        while (duration < 1.5f)
        {

            for (int index = 0; index < this.frames.Count(); ++index)
            {
                if (this.frames[index] != frame)
                {

                    frames[index].gameObject.transform.position = defaultPositions[index] + Vector3.right * (1f - Curve(duration * speed)) * 20f;
                }
            }

            duration += Time.deltaTime;

            await UniTask.Yield();
        }

        for (int index = 0; index < this.frames.Count(); ++index)
        {
            if (this.frames[index] != frame) frames[index].transform.position = defaultPositions[index];
        }
        /*
        speed = 1.8f;
        while (duration < 1f)
        {
            for (int index = 0; index < others.Count(); ++index)
            {
                others.ToList()[index].gameObject.transform.position = defaultPositions.ToList()[index] - Vector3.right * Curve(duration);
            }

            duration += Time.deltaTime * speed;

            await UniTask.Yield();
        }

        for (int index = 0; index < frames.Count(); ++index)
        {
            frames[index].transform.position = this.transform.position + new Vector3(index % row * space.x, -index / row * space.y, 0f);
        }
        */
        /*
        for (int index = 0; index < others.Count(); ++index)
        {
            others.ToList()[index].gameObject.transform.position = defaultPositions.ToList()[index] - Vector3.right;
        }
        */
    }

    public float factor1;
    public float factor2;

    private async UniTask MoveTarget(Frame2 frame)
    {
        await UniTask.Delay(200);

        float duration = 0;

        Vector3 defaultPosition = frame.transform.position;
        Vector3 defaultScale = frame.transform.localScale;

        Vector3 defaultConstellationPosition = frame.constellationParent.transform.localPosition;
        Vector3 defaultConstellationScale = frame.constellationParent.transform.localScale;

        Vector3 delta = anchor.transform.position - defaultPosition + 6f * Vector3.down;

        while (duration < 2)
        {

            frame.transform.position = defaultPosition + delta * Curve(duration);
            frame.transform.localScale = defaultScale * (1f + 7f * Curve(duration));

            //frame.constellationParent.transform.localPosition = Vector3.up * factor1 * Curve(duration);
            frame.constellationParent.transform.localScale = defaultConstellationScale / (1f + factor2 * Curve(duration));

            duration += Time.deltaTime * 1.5f;

            await UniTask.Yield();
        }

        await FadeInText(frame);

        //exit
        await WaitExit();

        this.description.transform.GetComponent<TMP_Text>().text = "";

        Debug.Log("inversed");

        duration = 0;

        defaultPosition = frame.transform.position;
        defaultScale = frame.transform.localScale;

        defaultConstellationPosition = frame.constellationParent.transform.localPosition;
        defaultConstellationScale = frame.constellationParent.transform.localScale;

        delta = -delta;

        while (duration < 1)
        {
            frame.transform.position = defaultPosition + delta * Curve(duration);
            frame.transform.localScale = defaultScale / (1 + 7 * Curve(duration));

            //frame.constellationParent.transform.localPosition = defaultConstellationPosition - Vector3.up * factor1 * Curve(duration);
            frame.constellationParent.transform.localScale = defaultConstellationScale * (1f + factor2 * Curve(duration));


            duration += Time.deltaTime * 1.5f;

            await UniTask.Yield();
        }

        this.isTryExit = false;

    }
    private async UniTask FadeInText(Frame2 frame)
    {
        int length = "<align=left>".Length;

        string fullText = "<align=left>" + frame.Date + "\0\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n" + frame.data.constellationName + " 座\n\n" + frame.data.description;
        while (length <= fullText.Length)
        {
            this.description.transform.GetComponent<TMP_Text>().text = fullText[0..length];

            if (fullText[length - 1] == '\0')
            {
                fullText = fullText.Replace("\0", "</align>");
                
                length += "</align>".Length;

                continue;
            }

            if (fullText[length - 1] == '\n')
            {
                length++;

                continue;
            }


            length++;

            await UniTask.Delay(25);
        }
    }

    private static float Curve(float x)
    {
        if (x < 0f) throw new System.Exception();
        else if (x < 1f) return (-Mathf.Cos(Mathf.PI * x) + 1f) / 2f;
        else return 1f;
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