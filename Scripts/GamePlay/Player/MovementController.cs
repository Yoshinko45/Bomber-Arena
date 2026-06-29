using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// プレイヤーの移動と向き（4方向）を管理するクラス
/// ・Input System の Move(Vector2) を受け取る
/// ・Bomberman風に斜め移動は禁止
/// ・移動方向に応じてアニメ（Sprite）を切り替える
///
/// ★CPU対応：
/// ・SetMoveInput(Vector2) で InputSystem を介さずに移動入力を渡せる
/// ・public SetDirection(Vector2) は “生direction代入” をしないようにし、
///   SetMoveInput に委譲してアニメが必ず出るようにした
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class MovementController : MonoBehaviour
{
    // Rigidbody2D（物理移動用）
    public Rigidbody2D rb { get; private set; }

    // 移動速度（Inspectorで調整）
    [SerializeField] private float speed = 5f;

    // 現在の移動方向（Vector2.zeroなら停止）
    private Vector2 direction = Vector2.zero;

    // 各方向用の AnimatedSpriteRenderer（Inspectorで割り当て）
    public AnimatedSpriteRenderer spriteRendererUp;
    public AnimatedSpriteRenderer spriteRendererDown;
    public AnimatedSpriteRenderer spriteRendererLeft;
    public AnimatedSpriteRenderer spriteRendererRight;

    // 死亡アニメ用
    public AnimatedSpriteRenderer spriteRendererDeath;

    // HUD表示用：スピードアイテム取得数
    public int speedIncreaseCount = 0;

    // 今表示しているスプライト（idle制御に使う）
    private AnimatedSpriteRenderer activeSpriteRenderer;

    // 死亡処理コルーチン参照（多重実行を防ぐ）
    private Coroutine deathRoutine;

    // 死亡中フラグ（入力・移動・二重死亡を防ぐ）
    private bool isDead = false;

    // ラウンドリセット用：初期速度を保持
    private float initialSpeed;

    /// <summary>
    /// 初期化（生成時に1回）
    /// </summary>
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        initialSpeed = speed;

        // 初期状態：停止＆下向きidle
        direction = Vector2.zero;
        SetDirectionInternal(Vector2.zero, spriteRendererDown);
    }

    /// <summary>
    /// 復活・再表示時にも呼ばれる（gameObject.SetActive(true) のタイミング）
    /// </summary>
    private void OnEnable()
    {
        CancelInvoke();

        if (deathRoutine != null)
        {
            StopCoroutine(deathRoutine);
            deathRoutine = null;
        }

        isDead = false;
        direction = Vector2.zero;

        if (spriteRendererDeath != null) spriteRendererDeath.enabled = false;
        SetDirectionInternal(Vector2.zero, spriteRendererDown);

        enabled = true;
    }

    /// <summary>
    /// ラウンド開始時に呼ばれる想定：プレイヤー状態を初期化する
    /// （MatchManager側から各プレイヤーに対して呼ぶ）
    /// </summary>
    public void ResetForNewRound()
    {
        CancelInvoke();

        if (deathRoutine != null)
        {
            StopCoroutine(deathRoutine);
            deathRoutine = null;
        }

        isDead = false;
        direction = Vector2.zero;

        speed = initialSpeed;
        speedIncreaseCount = 0;

        if (spriteRendererDeath != null) spriteRendererDeath.enabled = false;
        SetDirectionInternal(Vector2.zero, spriteRendererDown);

        enabled = true;

        // 爆弾も復帰させる（死亡時に無効化している場合がある）
        var bomb = GetComponent<BombController>();
        if (bomb != null) bomb.enabled = true;
    }

    /// <summary>
    /// Input System の Move アクションから呼ばれる
    /// </summary>
    public void OnMove(InputValue value)
    {
        SetMoveInput(value.Get<Vector2>());
    }

    /// <summary>
    /// ★CPU用：Input System を介さずに移動入力を渡すための口。
    /// OnMove() と同じロジックで4方向化＋アニメ切替を行う。
    /// </summary>
    public void SetMoveInput(Vector2 input)
    {
        if (MatchManager.InputLocked) return;
        if (isDead) return;

        if (input.sqrMagnitude < 0.01f)
        {
            direction = Vector2.zero;

            // 現在の向きのまま idle
            if (activeSpriteRenderer != null)
                activeSpriteRenderer.idle = true;

            return;
        }

        // 斜め禁止：大きい軸だけ採用
        if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
        {
            if (input.x > 0)
                SetDirectionInternal(Vector2.right, spriteRendererRight);
            else
                SetDirectionInternal(Vector2.left, spriteRendererLeft);
        }
        else
        {
            if (input.y > 0)
                SetDirectionInternal(Vector2.up, spriteRendererUp);
            else
                SetDirectionInternal(Vector2.down, spriteRendererDown);
        }
    }

    /// <summary>
    /// 速度を増加させる（アイテム取得時に呼ばれる）
    /// </summary>
    public void IncreaseSpeed(float amount = 1f)
    {
        speed += amount;
        speedIncreaseCount++;
    }

    /// <summary>
    /// 物理更新：Rigidbody2Dで移動する
    /// </summary>
    private void FixedUpdate()
    {
        if (MatchManager.InputLocked) return;
        if (isDead) return;

        rb.MovePosition(rb.position + direction * speed * Time.fixedDeltaTime);
    }

    /// <summary>
    /// 方向と表示スプライトをまとめて切り替える（内部用）
    /// </summary>
    private void SetDirectionInternal(Vector2 newDirection, AnimatedSpriteRenderer renderer)
    {
        if (isDead) return;

        direction = newDirection;

        if (spriteRendererUp != null)    spriteRendererUp.enabled    = (renderer == spriteRendererUp);
        if (spriteRendererDown != null)  spriteRendererDown.enabled  = (renderer == spriteRendererDown);
        if (spriteRendererLeft != null)  spriteRendererLeft.enabled  = (renderer == spriteRendererLeft);
        if (spriteRendererRight != null) spriteRendererRight.enabled = (renderer == spriteRendererRight);

        activeSpriteRenderer = renderer;

        // 停止中なら idle、移動中なら idle=false
        if (activeSpriteRenderer != null)
            activeSpriteRenderer.idle = (direction == Vector2.zero);
    }

    /// <summary>
    /// 爆風に触れたら死亡（Explosionレイヤーに反応）
    /// </summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isDead) return;

        if (other.gameObject.layer == LayerMask.NameToLayer("Explosion"))
        {
            DeathSequence();
        }
    }

    /// <summary>
    /// 死亡シーケンス：移動/爆弾無効化 → 死亡アニメ → 一定時間後に非表示
    /// </summary>
    private void DeathSequence()
    {
        if (isDead) return;
        isDead = true;

        enabled = false;

        var bomb = GetComponent<BombController>();
        if (bomb != null) bomb.enabled = false;

        if (spriteRendererUp != null)    spriteRendererUp.enabled = false;
        if (spriteRendererDown != null)  spriteRendererDown.enabled = false;
        if (spriteRendererLeft != null)  spriteRendererLeft.enabled = false;
        if (spriteRendererRight != null) spriteRendererRight.enabled = false;

        if (spriteRendererDeath != null)
        {
            spriteRendererDeath.enabled = true;
            spriteRendererDeath.idle = false;
        }

        if (deathRoutine != null) StopCoroutine(deathRoutine);
        deathRoutine = StartCoroutine(DeathEndRoutine());
    }

    /// <summary>
    /// ★互換用：外部から方向だけ渡したい場合の口。
    /// “生代入”するとアニメが出ないので、SetMoveInput に委譲する。
    /// </summary>
    public void SetDirection(Vector2 dir)
    {
        SetMoveInput(dir);
    }

    /// <summary>
    /// 死亡演出の終わり：一定時間待ってから非表示にする
    /// </summary>
    private IEnumerator DeathEndRoutine()
    {
        yield return new WaitForSeconds(1.25f);
        gameObject.SetActive(false);
    }
}