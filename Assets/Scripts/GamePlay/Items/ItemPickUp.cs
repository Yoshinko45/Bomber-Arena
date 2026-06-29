using UnityEngine;

/// <summary>
/// プレイヤーが触れたときに効果を発動するアイテムクラス。
/// ・ボム数増加
/// ・爆風範囲拡大
/// ・移動速度上昇
/// などの強化アイテムを管理する。
/// </summary>
public class ItemPickUp : MonoBehaviour
{
    /// <summary>
    /// アイテムの種類を定義する列挙型（enum）。
    /// Inspector から選択できるようにしている。
    /// </summary>
    public enum ItemType
    {
        ExtraBomb,     // 設置可能ボム数を増やす
        BlastRadius,   // 爆風の範囲を広げる
        SpeedIncrease, // 移動速度を上げる
    }

    /// <summary>
    /// このアイテムがどの種類なのかを指定する変数。
    /// UnityのInspector上で設定する。
    /// </summary>
    public ItemType Type;

    /// <summary>
    /// プレイヤーがアイテムを取得したときに呼ばれる処理。
    /// 対応する能力を強化し、その後アイテムを削除する。
    /// </summary>
    /// <param name="player">アイテムを取得したプレイヤー</param>
    private void OnItemPickup(GameObject player)
    {
        // プレイヤーに付いている各コンポーネントを取得
        // （無い場合は null になるため注意）
        BombController bomb = player.GetComponent<BombController>();
        MovementController movement = player.GetComponent<MovementController>();

        // アイテムの種類に応じて処理を分岐
        switch (Type)
        {
            // ----------------------------------------
            // ボム設置可能数を増やす
            // ----------------------------------------
            case ItemType.ExtraBomb:
                if (bomb != null)
                {
                    bomb.AddBomb(); // BombController側の処理を呼ぶ
                }
                break;

            // ----------------------------------------
            // 爆風範囲を拡大する
            // ----------------------------------------
            case ItemType.BlastRadius:
                if (bomb != null)
                {
                    bomb.AddBlastRadius(1);
                    // 爆風範囲を1段階拡大
                    // 内部でカウントや制限処理を行う想定
                }
                break;

            // ----------------------------------------
            // 移動速度を上げる
            // ----------------------------------------
            case ItemType.SpeedIncrease:
                if (movement != null)
                {
                    movement.IncreaseSpeed();
                    // MovementController側で速度を増加させる
                }
                break;
        }

        // アイテム取得後、このオブジェクトを削除する
        // （重複取得を防ぐため）
        Destroy(gameObject);
    }

    /// <summary>
    /// トリガーに他のコライダーが入ったときに呼ばれる。
    /// Playerタグを持つオブジェクトのみアイテム取得処理を行う。
    /// </summary>
    /// <param name="other">接触したコライダー</param>
    private void OnTriggerEnter2D(Collider2D other)
    {
        // タグが「Player」の場合のみ反応
        if (other.CompareTag("Player"))
        {
            OnItemPickup(other.gameObject);
        }
    }
}