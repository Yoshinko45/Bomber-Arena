using UnityEngine;

/// <summary>
/// スプライトを一定時間ごとに切り替えて
/// 簡易的なフレームアニメーションを行うクラス
/// ・idle中は待機スプライトを表示
/// ・有効化／無効化に強く、復活処理にも対応
/// </summary>
public class AnimatedSpriteRenderer : MonoBehaviour
{
    // このオブジェクトに付いている SpriteRenderer への参照
    private SpriteRenderer spriteRenderer;

    // 待機状態（idle）のときに表示するスプライト
    public Sprite idleSprite;

    // アニメーション用のスプライト配列
    public Sprite[] animationSprites;

    // 1フレームあたりの表示時間（秒）
    public float animationTime = 0.25f;

    // 現在のアニメーションフレーム番号
    private int animationFrame;

    // true の場合、アニメーションをループ再生する
    public bool loop = true;

    // true の場合、idle状態として待機スプライトを表示する
    public bool idle = true;

    /// <summary>
    /// オブジェクト生成時に一度だけ呼ばれる
    /// 必要なコンポーネントの取得を行う
    /// </summary>
    private void Awake()
    {
        // 同じ GameObject に付いている SpriteRenderer を取得
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    /// <summary>
    /// このコンポーネントが有効化されたときに呼ばれる
    /// （復活・再表示時など）
    /// </summary>
    private void OnEnable()
    {
        // スプライト描画を有効にする
        spriteRenderer.enabled = true;

        // 復活時にアニメが途中で止まる問題の対策
        // フレーム番号をリセット
        animationFrame = 0;

        // 念のため、以前の Invoke をすべて停止
        CancelInvoke();

        // animationTime 秒ごとに NextFrame を繰り返し呼び出す
        InvokeRepeating(nameof(NextFrame), animationTime, animationTime);

        // ★ 復活直後の1フレーム目を即座に更新
        // 表示が一瞬おかしくなるのを防ぐ
        NextFrame();
    }

    /// <summary>
    /// このコンポーネントが無効化されたときに呼ばれる
    /// </summary>
    private void OnDisable()
    {
        // スプライト描画を無効にする
        spriteRenderer.enabled = false;

        // InvokeRepeating を確実に停止
        CancelInvoke();
    }

    /// <summary>
    /// アニメーションの次のフレームを表示する処理
    /// </summary>
    private void NextFrame()
    {
        // アニメーション用スプライトが未設定の場合
        if (animationSprites == null || animationSprites.Length == 0)
        {
            // idleスプライトを表示して処理終了
            spriteRenderer.sprite = idleSprite;
            return;
        }

        // idle状態の場合はアニメを進めず、待機スプライトを表示
        if (idle)
        {
            spriteRenderer.sprite = idleSprite;
            return;
        }

        // ---- アニメーション進行処理 ----

        // フレーム番号を進める
        animationFrame++;

        // ループありの場合：最後まで行ったら最初に戻る
        if (loop && animationFrame >= animationSprites.Length)
            animationFrame = 0;

        // ループなしの場合：最後のフレームで停止
        if (!loop && animationFrame >= animationSprites.Length)
            animationFrame = animationSprites.Length - 1;

        // 現在のフレームのスプライトを表示
        spriteRenderer.sprite = animationSprites[animationFrame];
    }
}