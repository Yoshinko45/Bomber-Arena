using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Tilemapステージをラウンド切り替え時に「完全に初期状態」に戻すクラス。
///
/// ▼ 主な役割
/// 1. 破壊可能タイル（Destructibles）を初期配置に復元する
/// 2. ラウンド中に生成されたアイテムを全削除する
/// 3. 爆弾・爆風・破壊演出などのランタイム生成オブジェクトを全削除する
///
/// ※ MatchManager からラウンド開始前に ResetStage() を呼び出す想定
/// </summary>
public class StageResetter : MonoBehaviour
{
    // =========================================================
    // Inspector 設定項目
    // =========================================================

    [Header("Tilemaps")]
    [SerializeField] private Tilemap destructiblesTilemap;
    // 破壊可能ブロック用の Tilemap
    // 爆弾で壊されるブロックを管理しているタイルマップ

    [Header("Runtime Objects Roots")]
    [SerializeField] private Transform itemsRoot;
    // ラウンド中に生成されるアイテムの親オブジェクト

    [SerializeField] private Transform runtimeRoot;
    // 爆弾・爆風・破壊エフェクトなど
    // ゲーム中に生成されるオブジェクトの親

    // =========================================================
    // 初期タイル保存用データ
    // =========================================================

    private BoundsInt savedBounds;
    // Tilemapの保存範囲（どの範囲のタイルを保存するか）

    private TileBase[] savedTiles;
    // 初期状態のタイルデータを保持する配列

    // =========================================================
    // 外部参照用プロパティ
    // =========================================================

    /// <summary>
    /// アイテム生成時に親として使うための公開プロパティ
    /// </summary>
    public Transform ItemsRoot => itemsRoot;

    /// <summary>
    /// 爆弾・爆風などの生成時に親として使うための公開プロパティ
    /// </summary>
    public Transform RuntimeRoot => runtimeRoot;

    // =========================================================
    // 初期化
    // =========================================================

    private void Awake()
    {
        // ▼ 破壊可能タイルマップの「初期状態」を保存しておく
        //    これにより、ラウンド終了後に元の状態へ戻せる

        if (destructiblesTilemap != null)
        {
            // 現在のタイル範囲を取得
            savedBounds = destructiblesTilemap.cellBounds;

            // その範囲内のタイルをまとめて取得
            savedTiles = destructiblesTilemap.GetTilesBlock(savedBounds);
        }
    }

    // =========================================================
    // ステージ初期化処理
    // =========================================================

    /// <summary>
    /// ラウンド開始前に呼び出す。
    /// ステージを完全な初期状態へ戻す。
    /// </summary>
    public void ResetStage()
    {
        // -----------------------------------------------------
        // 1) 破壊可能タイルを復元
        // -----------------------------------------------------
        // 爆弾で壊されたブロックをすべて元に戻す

        if (destructiblesTilemap != null && savedTiles != null)
        {
            // 現在のタイルをすべて削除
            destructiblesTilemap.ClearAllTiles();

            // 保存しておいた初期タイルを再配置
            destructiblesTilemap.SetTilesBlock(savedBounds, savedTiles);

            // タイルの表示を更新
            destructiblesTilemap.RefreshAllTiles();
        }

        // -----------------------------------------------------
        // 2) アイテムを全削除
        // -----------------------------------------------------
        // ラウンド中に出現したアイテムを完全に消す

        if (itemsRoot != null)
        {
            ClearChildren(itemsRoot);
        }

        // -----------------------------------------------------
        // 3) 爆弾・爆風・演出を全削除
        // -----------------------------------------------------
        // ラウンド終了直後に爆弾が残っている問題を防ぐため、
        // ランタイム生成オブジェクトをすべて削除する

        if (runtimeRoot != null)
        {
            ClearChildren(runtimeRoot);
        }
    }

    // =========================================================
    // 子オブジェクト一括削除処理
    // =========================================================

    /// <summary>
    /// 指定された親オブジェクトの子をすべて削除する。
    /// ※ 後ろから削除することで安全に消せる。
    /// </summary>
    private void ClearChildren(Transform root)
    {
        for (int i = root.childCount - 1; i >= 0; i--)
        {
            Destroy(root.GetChild(i).gameObject);
        }
    }
}