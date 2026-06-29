using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// リザルト画面の「順位表示パネル」を制御するクラス。
///
/// ▼ このクラスの役割
/// ・1つの順位枠（例：1位 / 2位 / 3位 / 4位）を表示する
/// ・順位番号の表示
/// ・プレイヤーアイコンの表示
/// ・勝利数の表示
///
/// ※ 各順位ごとにこのスクリプトをアタッチして使用する想定。
/// </summary>
public class ResultRankPanelUI : MonoBehaviour
{
    // =========================================================
    // Inspector から設定するUI参照
    // =========================================================

    [Header("Assign from this panel")]

    /// <summary>
    /// 順位テキスト（例："1." や "2." を表示するText）
    /// </summary>
    [SerializeField] private Text rankText;

    /// <summary>
    /// プレイヤーのアイコン画像（キャラの顔など）
    /// </summary>
    [SerializeField] private Image playerIcon;

    /// <summary>
    /// 勝利を示すアイコン（トロフィーなど）
    /// ※ 固定画像なら特に変更処理は不要
    /// </summary>
    [SerializeField] private Image winIcon;

    /// <summary>
    /// そのプレイヤーの勝利数を表示するText
    /// </summary>
    [SerializeField] private Text winsCountText;

    // =========================================================
    // 表示更新メソッド
    // =========================================================

    /// <summary>
    /// この順位パネルの表示内容を設定する
    /// </summary>
    /// <param name="rank">
    /// 表示する順位（1, 2, 3, 4 など）
    /// </param>
    /// <param name="playerSprite">
    /// 表示するプレイヤーのアイコン画像
    /// </param>
    /// <param name="wins">
    /// そのプレイヤーの勝利数
    /// </param>
    public void Set(int rank, Sprite playerSprite, int wins)
    {
        // -------------------------
        // 順位番号の表示
        // -------------------------
        if (rankText != null)
        {
            // 例：1 → "1." という形式で表示
            rankText.text = $"{rank}.";
        }

        // -------------------------
        // プレイヤーアイコンの設定
        // -------------------------
        if (playerIcon != null && playerSprite != null)
        {
            playerIcon.sprite = playerSprite;
        }

        // -------------------------
        // 勝利数の表示
        // -------------------------
        if (winsCountText != null)
        {
            winsCountText.text = wins.ToString();
        }

        // -------------------------
        // winIcon について
        // -------------------------
        // トロフィー画像を常に固定表示するなら、
        // 特に処理を書く必要はありません。
        //
        // もし「1位だけ表示」などにしたい場合は、
        // ここで winIcon.enabled = (rank == 1);
        // のように制御できます。
    }
}