#define new

using System;
using System.Collections.Generic;
using System.Collections;

using UnityEngine;
using UnityEngine.UI;

using System.Linq;

/*

using System;
using System.Collections.Generic;
//using Junya;

[Serializable]
public class ConstellationData
{
    public string constellationName;
    public string createdAt;
    public List<StarData> stars = new List<StarData>();
    public List<ConnectionData> connections = new List<ConnectionData>();
    //public Node<String> root = new Node<String>();
    //public int likeCount;

}

[Serializable]
public class StarData
{
    public int id;
    public float x;
    public float y;
    public float scale;
}

[Serializable]
public class ConnectionData
{
    public int fromStarId;
    public int toStarId;
}

[Serializable]
public class ConstellationListWrapper
{
    public List<ConstellationData> list = new List<ConstellationData>();
} 

 */

namespace Junya
{
#if new

    [Serializable] public sealed class Comment : IAttachable
    {
        public IAttachable parent;
        public List<Comment> children;

        private string content;
        public string Content
        {
            get
            {
                return this.content ??= "";
            }
            set
            {
                //ゲームオブジェクトの設定
                this.content = value;
                this.gameObject.transform.Find("Canvas").Find("Content").GetComponent<Text>().text = value;
            }
        }

        public readonly GameObject gameObject;

        public Comment(string content)
        {
            this.children = new();

            //ゲームオブジェクトの設定
            this.gameObject = MonoBehaviour.Instantiate(CommentManager.instance.prefab, Vector3.zero, Quaternion.identity);
            this.Content = content;
            this.gameObject.name = content;

            this.isActive = true;

            CommentManager.instance.StartCoroutine(InternalCoroutine());
        }

        public void Attach(ref Comment comment)
        {
            //親子関係の構築
            comment.gameObject.transform.SetParent(this.gameObject.transform);
            this.children.Add(comment);
            comment.parent = this;

            //子の座標設定
            comment.gameObject.transform.position = this.gameObject.transform.position;
        }

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

        private const float interval = 2f;
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