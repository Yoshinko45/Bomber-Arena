/// <summary>
/// ゲーム全体で共有する設定情報を管理するクラス。
///
/// タイトル画面で決定した以下の情報を
/// シーンをまたいで保持するために使用する。
///
/// ・プレイヤー人数
/// ・各プレイヤーが「人間 or CPU」
/// ・操作方法（キーボード / ゲームパッド）
/// ・CPUの難易度
///
/// static クラスのため、インスタンス化せず
/// GameSettings.playerCount のように直接アクセスする。
/// </summary>
public static class GameSettings
{
    /// <summary>
    /// プレイヤーの操作方法を表す列挙型。
    /// </summary>
    public enum ControlType
    {
        Keyboard,  // キーボード操作
        Gamepad    // ゲームパッド操作
    }

    /// <summary>
    /// プレイヤースロットの種類（人間かCPUか）を表す列挙型。
    /// </summary>
    public enum SlotType
    {
        Human, // 人間プレイヤー
        CPU    // CPUプレイヤー
    }

    /// <summary>
    /// CPUの難易度を表す列挙型。
    /// AIの思考レベルなどに使用する想定。
    /// </summary>
    public enum CpuDifficulty
    {
        Easy,    // 簡単（初心者向け）
        Normal,  // 標準
        Hard     // 難しい（高性能AI）
    }

    /// <summary>
    /// 現在のプレイヤー人数（2～4人）。
    /// タイトル画面で変更される。
    /// </summary>
    public static int playerCount = 2;

    /// <summary>
    /// 各プレイヤー枠の種類を保持する配列。
    /// インデックス 0～3 が Player1～Player4 に対応する。
    ///
    /// 例：
    /// slotTypes[0] → Player1 が Human か CPU か
    /// </summary>
    public static SlotType[] slotTypes = new SlotType[4]
    {
        SlotType.Human,
        SlotType.Human,
        SlotType.Human,
        SlotType.Human
    };

    /// <summary>
    /// 各プレイヤーの操作方法を保持する配列。
    /// インデックス 0～3 が Player1～Player4 に対応する。
    ///
    /// 例：
    /// controlTypes[1] → Player2 が Gamepad 操作
    ///
    /// ※CPUの場合はこの値は実質的に使用されない。
    /// </summary>
    public static ControlType[] controlTypes = new ControlType[4]
    {
        ControlType.Keyboard, // Player1
        ControlType.Gamepad,  // Player2
        ControlType.Gamepad,  // Player3
        ControlType.Gamepad   // Player4
    };

    /// <summary>
    /// 各プレイヤー枠のCPU難易度を保持する配列。
    /// インデックス 0～3 が Player1～Player4 に対応する。
    ///
    /// ※そのプレイヤーが CPU の場合のみ意味を持つ。
    /// </summary>
    public static CpuDifficulty[] cpuDifficulties = new CpuDifficulty[4]
    {
        CpuDifficulty.Normal,
        CpuDifficulty.Normal,
        CpuDifficulty.Normal,
        CpuDifficulty.Normal
    };

    /// <summary>
    /// すべての設定を初期状態に戻す。
    /// タイトル画面へ戻ったときなどに呼び出す。
    ///
    /// 初期状態：
    /// ・プレイヤー人数：2人
    /// ・全員 Human
    /// ・P1はKeyboard、他はGamepad
    /// ・CPU難易度はすべて Normal
    /// </summary>
    public static void ResetDefaults()
    {
        // プレイヤー人数を初期値に戻す
        playerCount = 2;

        // 全プレイヤーを Human に戻す
        slotTypes[0] = SlotType.Human;
        slotTypes[1] = SlotType.Human;
        slotTypes[2] = SlotType.Human;
        slotTypes[3] = SlotType.Human;

        // 操作方法を初期値に戻す
        controlTypes[0] = ControlType.Keyboard;
        controlTypes[1] = ControlType.Gamepad;
        controlTypes[2] = ControlType.Gamepad;
        controlTypes[3] = ControlType.Gamepad;

        // CPU難易度をすべて Normal に戻す
        cpuDifficulties[0] = CpuDifficulty.Normal;
        cpuDifficulties[1] = CpuDifficulty.Normal;
        cpuDifficulties[2] = CpuDifficulty.Normal;
        cpuDifficulties[3] = CpuDifficulty.Normal;
    }
}