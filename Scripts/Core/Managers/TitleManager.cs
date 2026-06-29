using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// タイトル周りのパネル切り替えと「UI選択状態」を安定させる管理クラス。
///
/// できること：
/// ・Title / Offline / HowToPlay の表示切替
/// ・各画面で最初に選ぶUIを設定（GamePad/Keyboard対応）
/// ・画面を戻った時に「直前に選んでいたボタン」を復元（UX向上）
/// ・SetActive直後に選択が外れる問題を、次フレームで確実に解決
/// </summary>
public class TitleManager : MonoBehaviour
{
    // =========================
    // パネル参照
    // =========================
    [Header("Panels")]
    [SerializeField] private GameObject titlePanel;
    [SerializeField] private GameObject offlinePanel;
    [SerializeField] private GameObject howToPlayPanel;

    // =========================
    // 各画面の「最初に選択するUI」
    // =========================
    [Header("First Selected (Fallback)")]
    [SerializeField] private GameObject titleFirstSelected;     // 例：OfflineButton
    [SerializeField] private GameObject offlineFirstSelected;   // 例：PlayerCountButton2
    [SerializeField] private GameObject howToFirstSelected;     // 例：NextButton or BackButton

    // =========================
    // 戻る時に「直前に選んでたUI」を復元するための記憶
    // =========================
    private GameObject lastSelectedOnTitle;
    private GameObject lastSelectedOnOffline;
    private GameObject lastSelectedOnHowTo;

    private enum PanelType { Title, Offline, HowTo }
    private PanelType current = PanelType.Title;

    private void Start()
    {
        // 起動時はタイトルだけONにする（念のため）
        ShowOnly(PanelType.Title);

        // 起動直後は選択が外れやすいので次フレームで確実に入れる
        StartCoroutine(SetSelectedNextFrame(titleFirstSelected));
    }

    public void SetSelected(GameObject target)
    {
        StartCoroutine(SetSelectedNextFrame(target));
    }

    // =========================================================
    // 外部（ボタン）から呼ぶAPI
    // =========================================================

    /// <summary>オフライン設定を開く（Title -> Offline）</summary>
    public void OpenOfflinePanel()
    {
        RememberCurrentSelection();

        ShowOnly(PanelType.Offline);

        // 以前選んでたものがあればそれ、無ければ初期選択
        var target = (lastSelectedOnOffline != null) ? lastSelectedOnOffline : offlineFirstSelected;
        StartCoroutine(SetSelectedNextFrame(target));
    }

    /// <summary>タイトルへ戻る（Offline -> Title）</summary>
    public void BackToTitlePanel()
    {
        RememberCurrentSelection();

        ShowOnly(PanelType.Title);

        var target = (lastSelectedOnTitle != null) ? lastSelectedOnTitle : titleFirstSelected;
        StartCoroutine(SetSelectedNextFrame(target));
    }

    /// <summary>HowToPlay を開く（Title -> HowTo）</summary>
    public void OpenHowToPlayPanel()
    {
        RememberCurrentSelection();

        ShowOnly(PanelType.HowTo);

        var target = (lastSelectedOnHowTo != null) ? lastSelectedOnHowTo : howToFirstSelected;
        StartCoroutine(SetSelectedNextFrame(target));
    }

    /// <summary>HowToPlay を閉じてタイトルへ戻る（HowTo -> Title）</summary>
    public void CloseHowToPlayToTitle()
    {
        RememberCurrentSelection();

        ShowOnly(PanelType.Title);

        var target = (lastSelectedOnTitle != null) ? lastSelectedOnTitle : titleFirstSelected;
        StartCoroutine(SetSelectedNextFrame(target));
    }

    // =========================================================
    // 内部処理
    // =========================================================

    /// <summary>
    /// 指定パネルだけを表示し、それ以外を非表示にする
    /// </summary>
    private void ShowOnly(PanelType type)
    {
        current = type;

        if (titlePanel != null)     titlePanel.SetActive(type == PanelType.Title);
        if (offlinePanel != null)   offlinePanel.SetActive(type == PanelType.Offline);
        if (howToPlayPanel != null) howToPlayPanel.SetActive(type == PanelType.HowTo);
    }

    /// <summary>
    /// 今の画面で選択されているUIを記憶する
    /// </summary>
    private void RememberCurrentSelection()
    {
        if (EventSystem.current == null) return;

        GameObject currentSelected = EventSystem.current.currentSelectedGameObject;

        // 何も選択されてない時は記憶しない（nullで上書きしない）
        if (currentSelected == null) return;

        switch (current)
        {
            case PanelType.Title:
                lastSelectedOnTitle = currentSelected;
                break;
            case PanelType.Offline:
                lastSelectedOnOffline = currentSelected;
                break;
            case PanelType.HowTo:
                lastSelectedOnHowTo = currentSelected;
                break;
        }
    }

    /// <summary>
    /// 1フレーム待ってからUIの選択状態を確実に設定する
    /// （SetActive直後は選択できないことがあるため）
    /// </summary>
    private IEnumerator SetSelectedNextFrame(GameObject target)
    {
        yield return null;

        if (EventSystem.current == null) yield break;

        // targetが無い場合は fallback を選ぶ（事故防止）
        if (target == null)
        {
            target = current switch
            {
                PanelType.Title  => titleFirstSelected,
                PanelType.Offline => offlineFirstSelected,
                PanelType.HowTo  => howToFirstSelected,
                _ => titleFirstSelected
            };
        }

        // 念のため、選択をリセットしてから入れる
        EventSystem.current.SetSelectedGameObject(null);

        if (target != null)
            EventSystem.current.SetSelectedGameObject(target);
    }

    /// <summary>ゲームを終了する（Exitボタン用）</summary>
    public void QuitGame()
    {
    #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // エディタ上での停止
    #else
        Application.Quit(); // ビルド版での終了
    #endif
    }
}