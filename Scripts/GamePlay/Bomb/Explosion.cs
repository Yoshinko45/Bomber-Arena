using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 爆風オブジェクトを制御するクラス
/// 爆風の見た目（始点・中間・終点）の切り替え、
/// 向きの回転、一定時間後の削除を担当する
/// </summary>
public class Explosion : MonoBehaviour
{
    // 爆風の始点用スプライト
    public AnimatedSpriteRenderer start;

    // 爆風の中間用スプライト
    public AnimatedSpriteRenderer middle;

    // 爆風の終点用スプライト
    public AnimatedSpriteRenderer end;

    /// <summary>
    /// 指定された AnimatedSpriteRenderer のみを有効化する
    /// 爆風の種類（始点・中間・終点）を切り替えるために使用
    /// </summary>
    /// <param name="renderer">
    /// 表示したい AnimatedSpriteRenderer
    /// </param>
    public void SetActiveRenderer(AnimatedSpriteRenderer renderer)
    {
        // 指定された renderer と一致するものだけを有効にする
        start.enabled  = renderer == start;
        middle.enabled = renderer == middle;
        end.enabled    = renderer == end;
    }

    /// <summary>
    /// 爆風の向きを指定された方向に回転させる
    /// </summary>
    /// <param name="direction">
    /// 爆風を伸ばしたい方向（例：Vector2.up, Vector2.right など）
    /// </param>
    public void SetDirection(Vector2 direction)
    {
        // 指定された方向ベクトルから角度（ラジアン）を計算
        float angle = Mathf.Atan2(direction.y, direction.x);

        // ラジアンを度数に変換し、Z軸回転として適用
        transform.rotation =
            Quaternion.AngleAxis(angle * Mathf.Rad2Deg, Vector3.forward);
    }

    /// <summary>
    /// 指定した秒数後にこの爆風オブジェクトを削除する
    /// </summary>
    /// <param name="seconds">
    /// 削除までの時間（秒）
    /// </param>
    public void DestroyAfter(float seconds)
    {
        // 指定時間後にゲームオブジェクトを破棄
        Destroy(gameObject, seconds);
    }

}