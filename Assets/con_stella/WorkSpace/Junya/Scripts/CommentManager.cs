using UnityEngine;
using System.Threading.Tasks;

namespace Junya
{

    public class CommentManager : MonoBehaviour
    {
        
        private async Task Test()
        {
            
            Comment c1 = new("c1");
            Comment c1_1 = new("c1_1");
            c1.Attach(ref c1_1);
            Comment c1_2 = new("c1_2");
            c1.Attach(ref c1_2);
            Comment c1_3 = new("c1_3");
            c1.Attach(ref c1_3);
            Comment c1_1_1 = new("c1_1_1");
            c1_1.Attach(ref c1_1_1);

            Debug.Log(c1.GetDescendantsCount());
            
            
            /*
            Comment parent = new("parent");
            parent.gameObject.transform.position = Vector3.up * 5f;

            await Task.Delay(1000);

            Comment child = new("child");

            await Task.Delay(1000);

            parent.Attach(ref child);

            await Task.Delay(1000);

            Comment child2 = new("child2");

            await Task.Delay(1000);

            parent.Attach(ref child2);

            await Task.Delay(1000);

            Comment child3 = new("child3");

            await Task.Delay(1000);

            parent.Attach(ref child3);

            await Task.Delay(1000);

            parent.DeactivateChildren();

            await Task.Delay(1000);

            parent.ActivateChildren();
            */
        }

        public static CommentManager instance;

        public GameObject prefab;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {

            instance = GameObject.Find("CommentManager").transform.GetComponent<CommentManager>();

            Test();
        }

        // Update is called once per frame
        void Update()
        {

        }
    }

}