using UnityEngine;

/// <summary>
/// 一定時間が経過したら、このGameObjectを自動で削除（Destroy）するスクリプト。
/// 例：爆発エフェクト、煙、破片、演出用オブジェクトなど「時間が経ったら消したいもの」に使う。
///
/// ※このクラスは「破壊ブロック」などにも付いている想定で、
///   Destroyされたときに一定確率でアイテムを出現させる。
/// </summary>
public class Distructible : MonoBehaviour
{
    /// <summary>何秒後に消すか</summary>
    public float destructionTime = 1.0f;

    /// <summary>アイテム出現確率（0〜1の範囲）</summary>
    [Range(0f, 1f)]
    public float itemSpawnChance = 0.2f;

    /// <summary>出現させるアイテムのプレハブ一覧</summary>
    public GameObject[] spawnableItems;

    public void Start()
    {
        // destructionTime 秒後に削除
        Destroy(gameObject, destructionTime);
    }

    /// <summary>
    /// オブジェクトがDestroyされる直前に呼ばれる
    /// </summary>
    private void OnDestroy()
    {
        // アイテム出現処理
        if (spawnableItems == null || spawnableItems.Length == 0) return;
        // ランダム出現判定
        if (Random.value >= itemSpawnChance) return;

        // ランダムにアイテムを1つ選んで出現させる
        int randomIndex = Random.Range(0, spawnableItems.Length);
        GameObject item = Instantiate(spawnableItems[randomIndex], transform.position, Quaternion.identity);

        // ★アイテムは ItemsRoot の子に入れて、ラウンド開始時に一括削除できるようにする
        var stage = FindObjectOfType<StageResetter>();
        if (stage != null && stage.ItemsRoot != null)
        {
            item.transform.SetParent(stage.ItemsRoot, true);
        }
    }
}