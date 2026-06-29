using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

/// <summary>
/// 爆弾の設置・爆発・爆風生成・破壊可能タイル破壊を管理するクラス（ボンバーマン系）
///
/// さらにこの版では、ラウンド切替時に「残った爆弾・爆風・破壊演出」などが
/// シーン内に残留しないように、StageResetter の RuntimeRoot 配下に生成物をまとめる。
/// → ラウンド開始時に RuntimeRoot を一括削除すれば、残留物を確実に消せる設計。
/// </summary>
public class BombController : MonoBehaviour
{
    // =========================
    // 爆弾の基本設定
    // =========================
    [Header("Bomb")]
    public GameObject bombPrefab;      // 爆弾プレハブ
    public float bombFuseTime = 3.0f;  // 爆弾を置いてから爆発するまでの秒数
    public int bombAmount = 1;         // 同時に置ける爆弾の最大数（上限）

    // =========================
    // 爆風の設定
    // =========================
    [Header("Explosion")]
    public Explosion explosionPrefab;      // 爆風プレハブ（start/middle/endなどの見た目を切替える想定）
    public LayerMask explosionLayerMask;   // 爆風を遮るレイヤー（壁・破壊ブロックなど）
    public float explosionDuration = 1.0f; // 爆風が表示される時間（秒）
    public int explosionRadius = 1;        // 爆風が伸びる距離（マス数）

    // =========================
    // 破壊可能オブジェクト設定（Tilemap）
    // =========================
    [Header("Destructible")]
    public Tilemap destructibleTiles;       // 破壊可能タイルが置かれているTilemap
    public Distructible destructiblePrefab; // 破壊演出（瓦礫/エフェクト等）のプレハブ

    // =========================
    // HUD 表示用カウント
    // =========================
    [Header("HUD Count (Read Only)")]
    public int extraBombCount = 1;    // ExtraBomb を取った数（初期値をどう見せたいかで調整）
    public int blastRadiusCount = 0;  // BlastRadius を取った数

    // =========================
    // 内部状態
    // =========================
    private int bombsRemaining;  // 現在「置ける残り爆弾数」
    private bool bombPressed;    // Bombボタンが押された瞬間フラグ（Updateで処理するため）

    // =========================
    // Round Reset 用（初期値保存）
    // =========================
    private int initialBombAmount;      // Inspectorで設定した初期爆弾数を保存
    private int initialExplosionRadius; // Inspectorで設定した初期爆風距離を保存

    // =========================
    // ステージリセッター（RuntimeRoot / ItemsRoot 用）
    // =========================
    private StageResetter stageResetter; // 生成物をまとめる親（RuntimeRoot）を持つ想定

    /// <summary>
    /// 生成直後に1回だけ呼ばれる（初期値保存＆参照取得）
    /// </summary>
    private void Awake()
    {
        // Inspector設定を「初期値」として保存しておく（ラウンド開始時にここへ戻す）
        initialBombAmount = bombAmount;
        initialExplosionRadius = explosionRadius;

        // StageResetter を1回だけ探してキャッシュする（Find連打を避ける）
        stageResetter = FindObjectOfType<StageResetter>();
    }

    /// <summary>
    /// ラウンド開始時に状態を初期化する（MatchManagerなどから呼ぶ想定）
    /// </summary>
    public void ResetForNewRound()
    {
        // ★爆弾設置コルーチンが残っていると、ラウンドを跨いで爆発する事故が起きるため止める
        StopAllCoroutines();

        // ゲームバランス用の値を初期値に戻す
        bombAmount = initialBombAmount;
        explosionRadius = initialExplosionRadius;

        // HUD表示用のカウントも初期化
        // ※仕様として「初期表示を1にしたい」なら 1 固定でOK
        extraBombCount = 1;
        blastRadiusCount = 0;

        // 入力フラグ初期化
        bombPressed = false;

        // 「置ける残り数」も整合させる
        bombsRemaining = bombAmount;
    }

    /// <summary>
    /// オブジェクトが有効化された時に呼ばれる
    ///（例：リスポーン、シーン開始、SetActive(true) など）
    /// </summary>
    private void OnEnable()
    {
        // 現在置ける爆弾数を初期化
        bombsRemaining = bombAmount;
        bombPressed = false;

        // Awakeで取れなかったケースにも対応（念のため）
        if (stageResetter == null)
            stageResetter = FindObjectOfType<StageResetter>();
    }

    /// <summary>
    /// オブジェクトが無効化された時に呼ばれる
    /// </summary>
    private void OnDisable()
    {
        // 死亡時にコルーチンが止まる → 爆弾が残ったままになる原因にもなる。
        // ただしこのプロジェクトでは「次ラウンド開始時にStageResetterでまとめて消す」設計なので、
        // ここではコルーチン停止だけして、残留物の削除はラウンド管理側に任せる。
        StopAllCoroutines();
    }

    /// <summary>
    /// Input System の Bomb アクションから呼ばれる関数。
    /// PlayerInput の Behavior が「Send Messages」の場合、
    /// Action名が "Bomb" なら OnBomb が自動的に呼ばれる。
    /// </summary>
    public void OnBomb(InputValue value)
    {
        // ラウンド開始演出中など、入力を無視したい時はここで弾く
        if (MatchManager.InputLocked) return;

        // ボタンが押された瞬間だけフラグを立てる
        if (value.isPressed) bombPressed = true;
    }

    /// <summary>
    /// 毎フレーム呼ばれる
    /// </summary>
    private void Update()
    {
        // ラウンド演出中は入力無視
        if (MatchManager.InputLocked) return;

        // 押された瞬間フラグが無ければ何もしない
        if (!bombPressed) return;

        // フラグを戻す（押しっぱなしで多重発火しないようにする）
        bombPressed = false;

        // 残弾があるなら爆弾設置
        if (bombsRemaining > 0)
        {
            StartCoroutine(PlaceBomb());
        }
    }

    /// <summary>
    /// 爆弾を設置し、一定時間後に爆発させる処理（コルーチン）
    /// </summary>
    private IEnumerator PlaceBomb()
    {
        // プレイヤー位置を取得し、グリッドに合わせて丸める（マス目に揃える）
        Vector2 position = transform.position;
        position.x = Mathf.Round(position.x);
        position.y = Mathf.Round(position.y);

        // 爆弾生成
        GameObject bomb = Instantiate(bombPrefab, position, Quaternion.identity);

        // ★爆弾は RuntimeRoot の子に入れる（ラウンド切替で一括削除できるように）
        if (stageResetter != null && stageResetter.RuntimeRoot != null)
        {
            bomb.transform.SetParent(stageResetter.RuntimeRoot, true);
        }

        // 置いたので残弾を減らす
        bombsRemaining--;

        // 爆発まで待機
        yield return new WaitForSeconds(bombFuseTime);

        // 待機中にラウンドリセット等で爆弾が消されていた場合は中止
        // （RuntimeRoot削除などで Destroy 済みの可能性がある）
        if (bomb == null)
        {
            // 置ける数だけ戻して終了（整合性維持）
            bombsRemaining++;
            yield break;
        }

        // 爆弾位置を再取得し、丸め直す（微小ズレ対策）
        position = bomb.transform.position;
        position.x = Mathf.Round(position.x);
        position.y = Mathf.Round(position.y);

        // 中心の爆風を生成（start）
        Explosion center = Instantiate(explosionPrefab, position, Quaternion.identity);

        // ★中心爆風も RuntimeRoot 配下へ（残留防止）
        if (stageResetter != null && stageResetter.RuntimeRoot != null)
        {
            center.transform.SetParent(stageResetter.RuntimeRoot, true);
        }

        center.SetActiveRenderer(center.start);
        center.DestroyAfter(explosionDuration);

        // 四方向に爆風を伸ばす
        Explode(position, Vector2.up, explosionRadius);
        Explode(position, Vector2.down, explosionRadius);
        Explode(position, Vector2.left, explosionRadius);
        Explode(position, Vector2.right, explosionRadius);

        // 爆弾を削除し、残弾を回復
        Destroy(bomb);
        bombsRemaining++;
    }

    /// <summary>
    /// 指定方向に爆風を再帰的に伸ばす処理
    /// </summary>
    private void Explode(Vector2 position, Vector2 direction, int length)
    {
        // 伸ばせる長さが無ければ終了
        if (length <= 0) return;

        // 次のマスへ進める
        position += direction;

        // 壁等に当たったら止まる（破壊可能なら破壊だけして終了）
        if (Physics2D.OverlapBox(position, Vector2.one / 2f, 0f, explosionLayerMask))
        {
            // 破壊可能タイルがあれば削除（壁なら何も起きない）
            ClearDestructible(position);
            return;
        }

        // 爆風生成
        Explosion explosion = Instantiate(explosionPrefab, position, Quaternion.identity);

        // ★爆風も RuntimeRoot 配下へ（ラウンド切替で残らないようにする）
        if (stageResetter != null && stageResetter.RuntimeRoot != null)
        {
            explosion.transform.SetParent(stageResetter.RuntimeRoot, true);
        }

        // 見た目：途中なら middle、最後なら end
        explosion.SetActiveRenderer(length > 1 ? explosion.middle : explosion.end);

        // 向き設定（縦/横の表示切替などに使用）
        explosion.SetDirection(direction);

        // 一定時間後に削除
        explosion.DestroyAfter(explosionDuration);

        // 次のマスへ爆風を伸ばす
        Explode(position, direction, length - 1);
    }

    /// <summary>
    /// 破壊可能タイルを削除し、破壊演出を生成する処理
    /// </summary>
    private void ClearDestructible(Vector2 position)
    {
        // ★Tilemapが未設定なら何もしない（Inspector設定忘れ対策）
        if (destructibleTiles == null)
        {
            Debug.LogWarning($"{nameof(BombController)}: destructibleTiles が未設定です。");
            return;
        }

        // ワールド座標をタイル座標へ変換
        Vector3Int cell = destructibleTiles.WorldToCell(position);

        // そのセルにタイルがあるか確認
        TileBase tile = destructibleTiles.GetTile(cell);

        // タイルが存在すれば破壊
        if (tile != null)
        {
            // 破壊演出生成
            var fx = Instantiate(destructiblePrefab, position, Quaternion.identity);

            // ★破壊演出も RuntimeRoot 配下へ（残留防止）
            if (stageResetter != null && stageResetter.RuntimeRoot != null)
            {
                fx.transform.SetParent(stageResetter.RuntimeRoot, true);
            }

            // タイル削除（null をセットすると消える）
            destructibleTiles.SetTile(cell, null);
        }
    }

    /// <summary>
    /// 爆弾所持数（同時設置数）を増やす。
    /// アイテム取得時に ItemPickUp 等から呼ぶ想定。
    /// </summary>
    public void AddBomb()
    {
        // 最大数を増やす
        bombAmount++;

        // 現在置ける数も増やす（取得直後から恩恵が出る）
        bombsRemaining++;

        // HUD表示用の取得数も増やす
        extraBombCount++;
    }

    /// <summary>
    /// 爆風範囲を増やす。
    /// アイテム取得時に ItemPickUp 等から呼ぶ想定。
    /// </summary>
    public void AddBlastRadius(int amount = 1)
    {
        // 爆風の伸びる距離を増やす
        explosionRadius += amount;

        // HUD表示用の取得数も増やす
        blastRadiusCount += amount;
    }

    /// <summary>
    /// プレイヤーが爆弾から離れた瞬間に当たり判定を有効化する処理。
    /// 目的：設置直後はプレイヤーが爆弾に引っかからないようにし、
    ///       離れたら爆弾を「壁」として機能させる。
    ///
    /// 前提：爆弾は設置直後 isTrigger=true になっていること。
    /// </summary>
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Bomb"))
        {
            // Trigger を解除して通常Colliderに戻す（通り抜け不可にする）
            other.isTrigger = false;
        }
    }
}