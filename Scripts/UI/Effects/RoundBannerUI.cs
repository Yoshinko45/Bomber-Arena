using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ラウンド開始時に
/// 「Round X」→「GO!」を表示するUI制御クラス
/// </summary>
public class RoundBannerUI : MonoBehaviour
{
    // UI全体の透明度を制御するための CanvasGroup
    [SerializeField] private CanvasGroup canvasGroup;

    // 「Round X」を表示するテキスト
    [SerializeField] private Text roundText;

    // 「GO!」を表示するテキスト
    [SerializeField] private Text goText;

    /// <summary>
    /// オブジェクト生成時に一度だけ呼ばれる
    /// </summary>
    private void Awake()
    {
        // InspectorでCanvasGroupが設定されていなければ、自分自身から取得
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        // 開始時はUIを非表示にする
        HideImmediate();
    }

    /// <summary>
    /// 「Round X」→「GO!」を順番に表示するコルーチン
    /// </summary>
    /// <param name="roundNumber">現在のラウンド番号</param>
    public IEnumerator ShowRoundThenGo(int roundNumber)
    {
        // UIを完全表示状態にする
        canvasGroup.alpha = 2f;

        // Roundテキストを表示
        if (roundText != null)
        {
            roundText.gameObject.SetActive(true);
            roundText.text = $"Round {roundNumber}";
        }

        // GOテキストはまだ表示しない
        if (goText != null)
            goText.gameObject.SetActive(false);

        // 実時間で2秒待機（Time.timeScaleの影響を受けない）
        yield return new WaitForSecondsRealtime(2.0f);

        // Round表示を消す
        if (roundText != null)
            roundText.gameObject.SetActive(false);

        // GO表示を出す
        if (goText != null)
        {
            goText.gameObject.SetActive(true);
            goText.text = "GO!";
        }

        // GOを2秒表示
        yield return new WaitForSecondsRealtime(1.0f);

        // フェードアウト処理
        float t = 0f;
        float duration = 0.25f; // フェードにかかる時間

        while (t < duration)
        {
            // 経過時間を加算（TimeScale無視）
            t += Time.unscaledDeltaTime;

            // alphaを徐々に下げていく
            canvasGroup.alpha = Mathf.Lerp(2f, 0f, t / duration);
            yield return null;
        }

        // 完全に非表示にする
        HideImmediate();
    }

    /// <summary>
    /// UIを即座に非表示にする
    /// </summary>
    private void HideImmediate()
    {
        // 全体を透明に
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;

        // 各テキストを非表示
        if (roundText != null)
            roundText.gameObject.SetActive(false);

        if (goText != null)
            goText.gameObject.SetActive(false);
    }
}