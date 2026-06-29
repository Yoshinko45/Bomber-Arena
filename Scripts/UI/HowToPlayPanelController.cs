using UnityEngine;
using UnityEngine.UI;

public class HowToPlayPanelController : MonoBehaviour
{
    [Header("Manager")]
    [SerializeField] private TitleManager titleManager; // ★TitleManagerを入れる

    [Header("Pages")]
    [SerializeField] private GameObject pageControls; // 1ページ目
    [SerializeField] private GameObject pageRules;    // 2ページ目

    [Header("Buttons")]
    [SerializeField] private Button nextButton;       // Next（1ページ目で使う）
    [SerializeField] private Button backButton;       // Back（タイトルへ or 前ページへ）

    [Header("First Selected per page")]
    [SerializeField] private GameObject controlsFirstSelected; // 例：NextButton
    [SerializeField] private GameObject rulesFirstSelected;    // 例：BackButton

    private int pageIndex = 0; // 0=Controls, 1=Rules

    private void OnEnable()
    {
        // 開いたら必ず1ページ目
        pageIndex = 0;
        ApplyPageAndSelection();
    }

    // -------------------------
    // Next：Controls -> Rules
    // -------------------------
    public void NextPage()
    {
        if (pageIndex == 0)
        {
            pageIndex = 1;
            ApplyPageAndSelection();
        }
    }

    // -------------------------
    // Back：
    // Rulesなら Controlsに戻る
    // Controlsなら Titleに戻る
    // -------------------------
    public void BackOrClose()
    {
        if (pageIndex == 1)
        {
            pageIndex = 0;
            ApplyPageAndSelection();
        }
        else
        {
            // ★閉じる処理はTitleManagerに委譲（非アクティブ問題回避）
            if (titleManager != null)
                titleManager.CloseHowToPlayToTitle();
        }
    }

    // -------------------------
    // UI適用＆「そのページで最初に選ぶ」ものをセット
    // -------------------------
    private void ApplyPageAndSelection()
    {
        if (pageControls != null) pageControls.SetActive(pageIndex == 0);
        if (pageRules != null) pageRules.SetActive(pageIndex == 1);

        // Nextは1ページ目だけ表示（2ページ目では不要）
        if (nextButton != null) nextButton.gameObject.SetActive(pageIndex == 0);

        // 選択したいボタンをページごとに変える
        GameObject target = (pageIndex == 0) ? controlsFirstSelected : rulesFirstSelected;

        // ★次フレームで確実に選択（キーマウ/Pad切替の違和感減る）
        if (titleManager != null && target != null)
            titleManager.SetSelected(target);
    }
}