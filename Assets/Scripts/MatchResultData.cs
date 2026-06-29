using UnityEngine;

/// <summary>
/// マッチ（試合）終了時の結果データを、
/// ゲームシーン → リザルトシーンへ受け渡すための静的クラス。
///
/// static クラスにしている理由：
/// ・シーンをまたいでもデータを保持できる
/// ・インスタンス生成が不要
///
/// 使い方：
/// 1. ゲーム終了時に SetFrom() を呼ぶ
/// 2. リザルトシーン側で wins や playerCount を参照する
/// </summary>
public static class MatchResultData
{
    // =========================================================
    // 公開フィールド
    // =========================================================

    /// <summary>
    /// 参加人数（2～4人）
    /// MatchManager から設定される。
    /// </summary>
    public static int playerCount;

    /// <summary>
    /// 各プレイヤーの勝利数
    /// 配列サイズは常に4固定（P1～P4想定）
    /// 実際に使用するのは playerCount 分だけ。
    /// </summary>
    public static int[] wins = new int[4];


    // =========================================================
    // データ設定
    // =========================================================

    /// <summary>
    /// MatchManager から結果を受け取って保存する。
    /// </summary>
    /// <param name="count">
    /// 実際の参加人数（2～4を想定）
    /// </param>
    /// <param name="srcWins">
    /// 各プレイヤーの勝利数配列
    /// </param>
    public static void SetFrom(int count, int[] srcWins)
    {
        // 念のため 2～4 の範囲に制限する
        playerCount = Mathf.Clamp(count, 2, 4);

        // wins配列が未初期化 or サイズ違いなら作り直す
        if (wins == null || wins.Length != 4)
        {
            wins = new int[4];
        }

        // 安全にコピー（null対策＋長さ対策）
        for (int i = 0; i < 4; i++)
        {
            wins[i] =
                (srcWins != null && i < srcWins.Length)
                ? srcWins[i]   // 有効な値があればコピー
                : 0;           // なければ 0 にする
        }
    }


    // =========================================================
    // データ初期化
    // =========================================================

    /// <summary>
    /// データを初期状態に戻す。
    /// タイトルへ戻るときなどに使用。
    /// </summary>
    public static void Clear()
    {
        // デフォルトは2人対戦
        playerCount = 2;

        // 念のため配列の存在チェック
        if (wins == null || wins.Length != 4)
        {
            wins = new int[4];
        }

        // 勝利数を全て0にリセット
        for (int i = 0; i < 4; i++)
        {
            wins[i] = 0;
        }
    }
}