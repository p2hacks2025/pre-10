using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System;

public class ShootingStarManager : MonoBehaviour
{

    /// <summary>
    /// 
    /// 前提 : ゲーム上のどこでもいいからprefab "ShootingStarManager" を一個だけ置いてほしい。名前は変えないで
    /// 
    /// ShootingStarManagerクラスのメンバ変数 "speed" で流れ星の速度をinspectorから調整可
    ///
    /// 流れ星を流すにはShootingStarクラスのコンストラクタを呼ぶ。
    /// new ShootingStar([流れ星に乗せたい文章(string)], [初期位置(Vector3)], [速度ベクトル(Vector3)]);
    /// 
    /// 流れ星はStop関数を呼ぶと一応止めれる(使わなくてもいいけど、うまく使えば重さ軽減できるかも)
    /// 
    /// </summary>

    public GameObject prefab;
    [NonSerialized] public static ShootingStarManager instance;

    [SerializeField] private float speed;

    void Start()
    {
        instance = GameObject.Find("ShootingStarManager").transform.GetComponent<ShootingStarManager>();

        new ShootingStar("あああああああああああああああ", Vector3.right * 10f + Vector3.up * 10f, new Vector3(-1.3f,-1f,0f) * Time.deltaTime * this.speed); //テスト用
    }
}

public class ShootingStar
{
    private readonly GameObject gameObject;

    public ShootingStar(string content, Vector3 initial, Vector3 velocity)
    {
        if (velocity.y > 0f || velocity.z != 0f) throw new InvalidShootingException();

        this.gameObject = MonoBehaviour.Instantiate(ShootingStarManager.instance.prefab, initial, Quaternion.identity);

        this.gameObject.transform.eulerAngles = new(0f, 0f, Mathf.Atan2(velocity.y, velocity.x) * 180f / Mathf.PI + 180f);

        this.gameObject.transform.SetParent(GameObject.Find("Canvas").transform);
        this.gameObject.transform.Find("Content").GetComponent<Text>().text = content;

        ShootingStarManager.instance.StartCoroutine(ShootCoroutine(velocity));
    }
    private IEnumerator ShootCoroutine(Vector3 velocity)
    {
        while (true)
        {
            if (this.isWaste) break;

            this.gameObject.transform.position += velocity;

            yield return null;
        }

        yield break;
    }

    private bool isWaste;
    public void Stop()
    {
        this.isWaste = true;
    }

    private class InvalidShootingException : Exception{ }
}