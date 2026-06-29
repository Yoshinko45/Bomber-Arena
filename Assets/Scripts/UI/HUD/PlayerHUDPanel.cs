using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// プレイヤー1人分のHUD（アイコン・勝利数・アイテム所持数）を管理するクラス
/// </summary>
public class PlayerHUDPanel : MonoBehaviour
{
    // =========================
    // 対象プレイヤー
    // =========================

    [Header("Target Player (assign in Inspector)")]
    // このHUDが表示内容を参照するプレイヤー
    [SerializeField] private GameObject targetPlayer;

    // =========================
    // UI参照
    // =========================

    [Header("UI References")]
    // プレイヤーアイコン
    [SerializeField] private Image playerIcon;
    // 勝利アイコン
    [SerializeField] private Image winIcon;

    // 勝利数テキスト
    [SerializeField] private Text winsCountText;

    // アイテムアイコン
    [SerializeField] private Image itemBlastRadiusIcon;
    [SerializeField] private Image itemExtraBombIcon;
    [SerializeField] private Image itemSpeedIncreaseIcon;

    // アイテム個数表示テキスト
    [SerializeField] private Text itemBlastCountText;
    [SerializeField] private Text itemExtraBombCountText;
    [SerializeField] private Text itemSpeedIncreaseCountText;

    // =========================
    // プレイヤー側コンポーネント参照
    // =========================

    // 爆弾関連（所持数・爆風範囲など）
    private BombController bomb;
    // 移動関連（スピードアップ回数など）
    private MovementController move;

    // =========================
    // 勝利数管理
    // =========================

    // このプレイヤーの勝利数
    private int wins = 0;

    /// <summary>
    /// オブジェクト生成時に一度だけ呼ばれる
    /// </summary>
    private void Awake()
    {
        // インスペクターで指定されたプレイヤーを紐づけ
        Bind(targetPlayer);

        // UIを初期状態に更新
        RefreshAll();
    }

    /// <summary>
    /// HUDとプレイヤーを紐づける
    /// </summary>
    public void Bind(GameObject player)
    {
        targetPlayer = player;

        // プレイヤーが存在しない場合は参照をクリア
        if (targetPlayer == null)
        {
            bomb = null;
            move = null;
            return;
        }

        // プレイヤーから必要なコンポーネントを取得
        bomb = targetPlayer.GetComponent<BombController>();
        move = targetPlayer.GetComponent<MovementController>();
    }

    /// <summary>
    /// 勝利数を設定（MatchManagerなどから呼ばれる）
    /// </summary>
    public void SetWins(int value)
    {
        // 勝利数は0～3の範囲に制限
        wins = Mathf.Clamp(value, 0, 3);

        // UIテキストに反映
        if (winsCountText != null)
            winsCountText.text = wins.ToString();
    }

    /// <summary>
    /// 毎フレーム呼ばれる
    /// </summary>
    private void Update()
    {
        // 対象プレイヤーがいなければ何もしない
        if (targetPlayer == null) return;

        // アイテム数は変動するので毎フレーム更新
        RefreshItemCounts();
    }

    /// <summary>
    /// HUD全体をまとめて更新
    /// </summary>
    private void RefreshAll()
    {
        RefreshItemCounts();
        SetWins(wins);
    }

    /// <summary>
    /// アイテム所持数の表示を更新
    /// </summary>
    private void RefreshItemCounts()
    {
        // =========================
        // BombController関連
        // =========================
        if (bomb != null)
        {
            // 追加爆弾数
            if (itemExtraBombCountText != null)
                itemExtraBombCountText.text = bomb.extraBombCount.ToString();

            // 爆風範囲アップ数
            if (itemBlastCountText != null)
                itemBlastCountText.text = bomb.blastRadiusCount.ToString();
        }
        else
        {
            // BombControllerが無い場合は0表示
            if (itemExtraBombCountText != null) itemExtraBombCountText.text = "0";
            if (itemBlastCountText != null) itemBlastCountText.text = "0";
        }

        // =========================
        // MovementController関連
        // =========================
        if (move != null)
        {
            // スピードアップ回数
            if (itemSpeedIncreaseCountText != null)
                itemSpeedIncreaseCountText.text = move.speedIncreaseCount.ToString();
        }
        else
        {
            // MovementControllerが無い場合は0表示
            if (itemSpeedIncreaseCountText != null)
                itemSpeedIncreaseCountText.text = "0";
        }
    }
}