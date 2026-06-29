using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// UIの選択フォーカスが外れたときに、自動で復元するためのクラス。
///
/// 【目的】
/// ・キーボードやゲームパッド操作中に選択状態が消えるのを防ぐ
/// ・常に何かしらのUIボタンが選択されている状態を維持する
///
/// 例：
/// ・シーン遷移直後に選択が外れる
/// ・マウスクリック後にフォーカスが消える
/// ・Gamepad操作時に選択がnullになる
///
/// 上記のような問題を自動で補正する。
/// </summary>
public class UIFocusKeeper : MonoBehaviour
{
    // フォーカスが外れたときに戻す対象のUIオブジェクト
    [SerializeField] private GameObject fallbackSelected;

    /// <summary>
    /// Startはゲーム開始時に1回だけ呼ばれる
    /// 初期フォーカスを安全に設定する
    /// </summary>
    private void Start()
    {
        // fallbackSelected が未設定の場合、
        // EventSystem の firstSelectedGameObject を自動で取得する
        if (fallbackSelected == null && EventSystem.current != null)
            fallbackSelected = EventSystem.current.firstSelectedGameObject;

        // 現在何も選択されていない場合、
        // fallbackSelected を強制的に選択状態にする
        if (EventSystem.current != null &&
            EventSystem.current.currentSelectedGameObject == null &&
            fallbackSelected != null)
        {
            EventSystem.current.SetSelectedGameObject(fallbackSelected);
        }
    }

    /// <summary>
    /// 毎フレーム実行される処理
    /// 入力があったのに選択が消えている場合は復元する
    /// </summary>
    private void Update()
    {
        // EventSystem または fallback が無い場合は何もしない
        if (EventSystem.current == null || fallbackSelected == null) return;

        // -------------------------
        // キーボード入力の検出
        // -------------------------
        bool keyboardInput =
            Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame;

        // -------------------------
        // ゲームパッド入力の検出
        // -------------------------
        bool gamepadInput = false;

        if (Gamepad.current != null)
        {
            gamepadInput =
                // 決定ボタン（Xボタンなど）
                Gamepad.current.buttonSouth.wasPressedThisFrame ||

                // 十字キー入力
                Gamepad.current.dpad.ReadValue() != Vector2.zero ||

                // 左スティック入力
                Gamepad.current.leftStick.ReadValue() != Vector2.zero;
        }

        // -------------------------
        // 入力があったのに選択がnullの場合
        // -------------------------
        if ((keyboardInput || gamepadInput) &&
            EventSystem.current.currentSelectedGameObject == null)
        {
            // fallbackSelected を再選択する
            EventSystem.current.SetSelectedGameObject(fallbackSelected);
        }
    }
}