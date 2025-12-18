using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

using TMPro;

using Cysharp.Threading.Tasks;

namespace Junya
{

    public class CommentManager : MonoBehaviour
    {
        
        /// <summary>
        /// 
        /// グロバール変数inputFieldにシーン共通のInputFieldをinspectorからアタッチしてくれ
        /// 
        /// AllowWrite関数を呼ぶとInputFieldが選択状態になり、キーボード入力が許可される。
        /// Send関数が呼ばれた時点でのInputFieldの内容がAllowWrite関数の戻り値として返される。(Update関数内の適切なタイミングでSend関数を呼ぶようにしておく必要がある)
        /// AllowWrite関数が戻り値を返すまで処理を待ちたいときは、関数の直前にawaitと加えて、関数を呼び出す関数のシグネチャにasyncと加える
        /// 
        /// </summary> 

        //この変数に共通のInputFieldをアタッチして
        public TMP_InputField inputField;

        //コメントの最大文字数(デフォルトは60)
        public const int maxLetter = 60;

        //コメント間の間隔
        public const float interval = 2f;

        //アイコンに使う色のリスト inspectorから割り当て済み
        public List<Color> iconColors;

        public static CommentManager instance;

        //キーボード入力を開始する
        public static UniTask<string> AllowWrite(int maxLetter) => instance._AllowWrite(maxLetter);

        private async UniTask<string> _AllowWrite(int maxLetter)
        {
            //選択状態にする
            inputField.ActivateInputField();
            
            //Send関数が呼ばれるまで繰り返し
            while (!this.sendFlag)
            {

                if (maxLetter <= inputField.text.Length) inputField.text = inputField.text[0..maxLetter];

                //1フレーム待機
                await UniTask.Yield();
            }

            //フラグのリセット
            this.sendFlag = false;

            //選択状態から外す
            inputField.DeactivateInputField();

            //InputFieldの内容を返す
            return inputField.text;
        }

        //内容の確定
        public void Send()
        {
            this.sendFlag = true;
        }
        private bool sendFlag;

        /*
        public static void StartWrite()
        {
            instance.StartCoroutine(instance.WriteCoroutine());
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
        
        private async Task Test()
        {
            
        }
        */

        public GameObject prefab;

        Comment test;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        /*
         * 中で awaitされてなかったから、asyncを消した
         */
        void Start()
        {
            instance = GameObject.Find("CommentManager").transform.GetComponent<CommentManager>();

            //(サンプル) この一行は「キーボード入力が開始され、確定されたらその内容をもつコメントを生成する」ということをしてる
            //new Comment(await AllowWrite());
            test = new("test");

        }

        /*
        // Update is called once per frame
        async void Update()
        {
            if (Input.GetKeyDown(KeyCode.R)) await test.AddComment();

            //(サンプル) エンターキーを押したときにInputFieldを確定する場合
            if (Input.GetKeyDown(KeyCode.Return)) Send();
        }*/
    }

}