#define new

using System;
using System.Collections.Generic;
using System.Collections;

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cysharp.Threading.Tasks;

namespace Junya
{
#if new

    [Serializable] public sealed class Comment : IAttachable, IAddCommentable
    {
        public IAttachable parent;
        public List<Comment> children;

        private readonly List<Color> iconColors = new() {new(255f, 187f, 0f)/*yellow*/, new(45f, 73f, 98f)/*light blue*/, new(15f, 44f, 68f)/*deep blue*/};

        //iconColorsからランダムに選んだ色(参照する度に再抽選)
        private Color randomColor
        {
            get => CommentManager.instance.iconColors[new System.Random().Next(0, CommentManager.instance.iconColors.Count)];
        }

        //星座に直接ついているコメントならtrueそうでなければfalseを返す変数
        private bool isDirectlyAttached
        {
            get => this.parent != null && this.parent is ConstellationData;
        }

        //内容
        private string content;
       
        private string Content
        {
            get
            {
                return this.content ??= "";
            }
            set
            {
                //ゲームオブジェクトの設定
                this.content = value;
                //Canvasを使わない形に変えたため、Canvasを探さないようにする
                /*
                this.gameObject.transform.Find("Canvas").Find("Content").GetComponent<Text>().text = value;
                */
                var textObj = this.gameObject.transform.Find("Content");
                if (textObj != null)
                {
                    // TextMeshProUGUI か Text か、プレハブにつけたコンポーネントに合わせてください
                    // ここでは新しいUIに合わせてTextMeshProUGUIとしています
                    var tmp = textObj.GetComponent<TextMeshProUGUI>();
                    if (tmp != null) tmp.text = value;
                    else textObj.GetComponent<Text>().text = value; // 念のため旧Textもケア
                }
            }
        }

        public string GetContent()
        {
            return this.content;
        }

        public readonly GameObject gameObject;
        public GameObject iconObject
        {
            get => this.gameObject.transform.Find("Icon").gameObject;
        }

        public Comment(string content)
        {
            this.children = new();

            //ゲームオブジェクトの設定
            this.gameObject = MonoBehaviour.Instantiate(CommentManager.instance.prefab, Vector3.zero, Quaternion.identity);
            this.gameObject.name = content;
            this.Content = content;
            //ScrollViewを使うため、SpriteRendererから、Imageに変更
            //this.gameObject.transform.Find("Icon").GetComponent<SpriteRenderer>().color = randomColor;
            var iconTrans = this.gameObject.transform.Find("Icon");
            if (iconTrans != null)
            {
                var img = iconTrans.GetComponent<Image>();
                if (img != null)
                {
                    img.color = randomColor;
                }
            }

            this.isActive = true;

            CommentManager.instance.StartCoroutine(InternalCoroutine());
        }

        public async UniTask<Comment> AddComment()
        {
            Comment reply = new Comment(await CommentManager.AllowWrite(CommentManager.maxLetter));
            this.Attach(ref reply);
            return reply;
        }

        public void Attach(ref Comment comment)
        {
            //親子関係の構築
            comment.gameObject.transform.SetParent(this.gameObject.transform);
            this.children.Add(comment);
            comment.parent = this;

            Debug.Log("reply count = " + this.children.Count);

            //子の座標設定
            comment.gameObject.transform.position = this.gameObject.transform.position + Vector3.down * (this.children.Count) * CommentManager.interval;

        }
        /*
        public int GetDescendantsCount()
        {
            int result = 0;


            if (this.children.Count != 0)
            {
                result += this.children.Count;
                foreach (Comment child in this.children) result += child.GetDescendantsCount();
            }

            return result;
        }
        */
        public void ActivateChildren()
        {
            foreach (Comment child in this.children) child.Activate();
        }
        public void DeactivateChildren()
        {
            foreach (Comment child in this.children) child.Deactivate();
        }

        private void Activate()//表示にする
        {
            this.isActive = true;
        }
        private void Deactivate()//非表示にする
        {
            this.isActive = false;
        }

        private void Update()
        {
            //Debug.Log("update was called");
            //Debug.Log("children.Count = " + children.Count);
            
            /*
            for (int i = 0; i < this.children.Count; ++i)
            {
                this.children[i].gameObject.transform.position = this.gameObject.transform.position + Vector3.down * interval * (i + 1);
                i += this.children[i].GetDescendantsCount();
            }
            */
        }

        private void OnActivate()
        {
            Display(true);
        }
        private void OnDeactivate()
        {
            Display(false);
        }

        private void Display(bool mode)
        {

            if (mode)
            {
                this.gameObject.SetActive(true);
                //コメントを表示する際に呼ばれる
                
                //
            }
            else
            {
                //コメントを非表示にする際に呼ばれる

                //
                this.gameObject.SetActive(false);
            }
        }

        public bool isActive { get; private set; }

        private IEnumerator InternalCoroutine()
        {
            bool p = this.isActive;

            while (true)
            {
                if (this.isActive && !p)
                {
                    p = true;
                    OnActivate();
                }
                else if (!this.isActive && p)
                {
                    p = false;
                    OnDeactivate();
                }

                if (this.isActive)
                {
                    this.Update();
                }

                yield return null;
            }
        }

    }

    public interface IAttachable
    {
        public abstract void Attach(ref Comment comment);
    }
    public interface IAddCommentable
    {
        public abstract UniTask<Comment> AddComment();
    }

#elif old    
    public class Comment
    {
        public Vector3 coordinate;


        private GameObject gameObject;
        private TMP_InputField inputField
        {
            get => this.gameObject.transform.Find("Canvas").Find("InputField").gameObject.GetComponent<TMP_InputField>();
        }

        
        public Comment(Vector3 coordinate)
        {
            this.coordinate = coordinate;
            
            this.gameObject = MonoBehaviour.Instantiate(CommentManager.Prefab, this.coordinate, Quaternion.identity);

        }

        public void StartWrite()
        {
            CommentManager.manager.StartCoroutine(WriteCoroutine());
        }

        private IEnumerator<string> WriteCoroutine()//Allow writing on InputField and return the content the user wrote when the enter key is pressed.
        {
            inputField.ActivateInputField();

            while (true)
            {
                if (Input.GetKeyDown(KeyCode.Return))
                {
                    break;
                }

                Debug.Log("running");
                yield return null;
            }

            Debug.Log(inputField.text);

            yield return inputField.text;
            this.inputField.DeactivateInputField();

            yield break;
        }

        /*
        public delegate void MyDelegate();

        private MyDelegate _handler;
        public event MyDelegate handler
        {
            add
            {
                this._handler += value;
            }
            remove
            {
                this._handler -= value;
            }
        }
        */

    }


    public partial class CommentManager
    {
        [SerializeField] private GameObject prefab;

        public static CommentManager manager
        {
            get => GameObject.Find("CommentManager").transform.GetComponent<CommentManager>();
        }

        public static GameObject Prefab
        {
            get => manager.prefab;
        }
    }
#endif
}