using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// UIボタンに「拡大演出」を付けるためのスクリプト。
///
/// 機能：
/// ・マウスカーソルが乗ったときにボタンを拡大する
/// ・ゲームパッドやキーボードで選択されたときにも拡大する
/// ・選択が外れたら元のサイズに戻す
/// ・マウスでクリックした場合は、そのボタンをUI選択状態にする
///
/// 対応しているインターフェース：
/// IPointerEnterHandler  → マウスが乗ったとき
/// IPointerExitHandler   → マウスが離れたとき
/// ISelectHandler        → UI選択されたとき（Pad操作など）
/// IDeselectHandler      → UI選択が外れたとき
/// IPointerDownHandler   → マウスクリック時
/// </summary>
public class ButtonScaleEffect : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    ISelectHandler, IDeselectHandler,
    IPointerDownHandler
{
    // -------------------------
    // スケール設定
    // -------------------------

    /// <summary>
    /// 通常時のスケール（初期サイズ）
    /// </summary>
    [SerializeField] private Vector3 normalScale = Vector3.one;

    /// <summary>
    /// ホバー（選択）時のスケール（少し拡大）
    /// </summary>
    [SerializeField] private Vector3 hoverScale = new Vector3(1.1f, 1.1f, 1f);

    /// <summary>
    /// オブジェクトが有効化されたときに呼ばれる。
    /// ボタンを必ず通常サイズに戻す。
    /// （シーン切り替えや再表示時のズレ防止）
    /// </summary>
    private void OnEnable()
    {
        transform.localScale = normalScale;
    }

    // -------------------------
    // マウス操作
    // -------------------------

    /// <summary>
    /// マウスカーソルがボタンの上に乗ったときに呼ばれる。
    /// ボタンを拡大表示する。
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        transform.localScale = hoverScale;
    }

    /// <summary>
    /// マウスカーソルがボタンから離れたときに呼ばれる。
    /// ただし「現在選択中」の場合はサイズを戻さない。
    /// （ゲームパッド操作では選択状態が残るため）
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        // 現在このボタンが選択中なら、サイズを戻さない
        if (EventSystem.current != null &&
            EventSystem.current.currentSelectedGameObject == gameObject)
        {
            return;
        }

        // 選択されていない場合のみ通常サイズに戻す
        transform.localScale = normalScale;
    }

    // -------------------------
    // キーボード / ゲームパッド操作
    // -------------------------

    /// <summary>
    /// UIとして選択されたときに呼ばれる。
    /// （キーボードやゲームパッドでフォーカスが当たった場合）
    /// ボタンを拡大表示する。
    /// </summary>
    public void OnSelect(BaseEventData eventData)
    {
        transform.localScale = hoverScale;
    }

    /// <summary>
    /// UI選択が外れたときに呼ばれる。
    /// ボタンを通常サイズに戻す。
    /// </summary>
    public void OnDeselect(BaseEventData eventData)
    {
        transform.localScale = normalScale;
    }

    // -------------------------
    // クリック時の処理
    // -------------------------

    /// <summary>
    /// マウスでボタンを押したときに呼ばれる。
    ///
    /// マウス操作でも「UI選択状態」をこのボタンに合わせることで、
    /// キーボード／ゲームパッド操作と挙動を統一する。
    ///
    /// これにより、
    /// ・クリック後も拡大状態が維持される
    /// ・方向キーやパッド操作に自然につながる
    /// </summary>
    public void OnPointerDown(PointerEventData eventData)
    {
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(gameObject);
        }
    }
}