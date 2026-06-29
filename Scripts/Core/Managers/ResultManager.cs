using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// リザルト（結果）画面の表示を管理するクラス。
///
/// ▼ 主な役割
/// ・勝利数に基づいてプレイヤー順位を決定
/// ・順位順にUIパネルへ表示
/// ・リプレイ／タイトルへ戻るボタン処理
///
/// ※ MatchResultData に保存された情報を元に表示を構築する
/// </summary>
public class ResultManager : MonoBehaviour
{
    // =========================================================
    // Inspector設定
    // =========================================================

    [Header("Panels (Top to Bottom)")]
    [SerializeField]
    private ResultRankPanelUI[] panels;
    // ↑ 上から順に P1RankPanel, P2RankPanel ... を登録
    //   配列の 0 が「1位表示パネル」になる

    [Header("Player Sprites (index 0 = Player1)")]
    [SerializeField]
    private Sprite[] playerSprites = new Sprite[4];
    // ↑ プレイヤーごとのアイコン画像
    //   index 0 = Player1, index 1 = Player2 ...

    [Header("Scenes")]
    [SerializeField]
    private string titleSceneName = "TitleScene";
    // タイトルシーン名

    [SerializeField]
    private string gameSceneName = "GameScene";
    // ゲームシーン名

    // =========================================================
    // 初期化処理
    // =========================================================
    private void Start()
    {
        // 念のため timeScale を通常状態に戻す
        // （試合終了時に 0 にしている可能性があるため）
        Time.timeScale = 1f;

        // 勝利数に基づいて順位を作成し、UIへ反映
        BuildRanking();
    }

    // =========================================================
    // 順位作成処理
    // =========================================================

    /// <summary>
    /// MatchResultData に保存された勝利数を元に
    /// プレイヤー順位を計算し、UIパネルへ反映する。
    /// </summary>
    private void BuildRanking()
    {
        // プレイヤー人数を 2～4 の範囲に制限
        int count = Mathf.Clamp(MatchResultData.playerCount, 2, 4);

        // プレイヤー番号リスト（0,1,2,3）を作成
        var idx = new List<int>();
        for (int i = 0; i < count; i++)
            idx.Add(i);

        // ▼ 並び替えルール
        // 1. 勝利数の多い順（降順）
        // 2. 同点の場合はプレイヤー番号の小さい順（昇順）
        idx.Sort((a, b) =>
        {
            // 勝利数を比較（降順）
            int w = MatchResultData.wins[b].CompareTo(MatchResultData.wins[a]);
            if (w != 0) return w;

            // 同点ならプレイヤー番号昇順
            return a.CompareTo(b);
        });

        // UIパネルに順位を反映
        for (int rank = 0; rank < panels.Length; rank++)
        {
            // パネル未設定の場合はスキップ
            if (panels[rank] == null)
                continue;

            // 表示する順位がプレイヤー人数以内なら表示
            if (rank < count)
            {
                int playerIndex = idx[rank]; // 実際のプレイヤー番号（0～3）
                int wins = MatchResultData.wins[playerIndex];

                // プレイヤーアイコン取得（範囲チェック付き）
                Sprite sp =
                    (playerSprites != null && playerIndex < playerSprites.Length)
                    ? playerSprites[playerIndex]
                    : null;

                // パネル表示
                panels[rank].gameObject.SetActive(true);

                // パネルへデータを渡す
                // rank + 1 → 表示用順位（1位,2位,3位…）
                panels[rank].Set(rank + 1, sp, wins);
            }
            else
            {
                // 使わない順位パネルは非表示
                panels[rank].gameObject.SetActive(false);
            }
        }
    }


    // =========================================================
    // ボタン処理
    // =========================================================

    /// <summary>
    /// 「もう一度プレイ」ボタンが押されたとき
    /// ゲームシーンを再読み込みする
    /// </summary>
    public void OnClickReplay()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(gameSceneName);
    }

    /// <summary>
    /// 「タイトルへ戻る」ボタンが押されたとき
    /// 設定を初期化し、タイトルシーンへ遷移する
    /// </summary>
    public void OnClickTitle()
    {
        Time.timeScale = 1f;

        // ゲーム設定を初期化
        GameSettings.ResetDefaults();

        // 勝利データをクリア
        MatchResultData.Clear();

        // タイトルへ戻る
        SceneManager.LoadScene(titleSceneName);
    }
}