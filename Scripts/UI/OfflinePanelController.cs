using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// タイトル画面の「オフライン設定パネル」を制御するクラス。
///
/// 主な役割：
/// ・プレイヤー人数（2〜4人）の切り替え
/// ・各プレイヤーを「人間 / CPU」に切り替え
/// ・人間プレイヤーのみ「キーボード / ゲームパッド」を切り替え
/// ・「CPUと対戦」モード（P1以外をCPU固定）をON/OFF
/// ・UI（画像）を現在の設定に合わせて更新
/// ・ゲームシーンへ遷移 / タイトルへ戻る
///
/// ★追加：
/// ・オフラインでは「キーボードは同時に1人だけ」を UI 側で制限
/// ・制限に引っかかったら画面上にメッセージを表示
/// </summary>
public class OfflinePanelController : MonoBehaviour
{
    // -------------------------
    // パネル参照
    // -------------------------

    [Header("Panels")]
    [SerializeField] private GameObject titlePanel;     // タイトルUI（戻る時に表示する）
    [SerializeField] private GameObject offlinePanel;   // オフライン設定パネル（この画面）

    // -------------------------
    // 各プレイヤー枠の背景（表示/非表示に使う）
    // -------------------------

    [Header("Panels (Player UI BG)")]
    [SerializeField] private GameObject player1UIBG;    // P1の枠背景
    [SerializeField] private GameObject player2UIBG;    // P2の枠背景
    [SerializeField] private GameObject player3UIBG;    // P3の枠背景
    [SerializeField] private GameObject player4UIBG;    // P4の枠背景

    // player1UIBG〜player4UIBG を配列で扱うための入れ物
    // 人数に応じた表示切り替えをループで書きやすくする
    private GameObject[] playerPanels;

    // -------------------------
    // 「人間 / CPU」切替ボタンの画像
    // -------------------------

    [Header("CPU Select Button Images (per player)")]
    [SerializeField] private Image[] cpuSelectImages = new Image[4]; // 各プレイヤーの「人間/CPU」ボタン画像
    [SerializeField] private Sprite playerSprite; // 人間の時の表示スプライト
    [SerializeField] private Sprite cpuSprite;    // CPUの時の表示スプライト

    // -------------------------
    // ★CPU Difficulty（Easy/Normal/Hard）ボタン
    // -------------------------

    [Header("CPU Difficulty Button (per player)")]
    [SerializeField] private GameObject[] cpuDifficultyButtons = new GameObject[4]; // ボタン全体(表示/非表示用)
    [SerializeField] private Image[] cpuDifficultyImages = new Image[4];            // ボタンの画像
    [SerializeField] private Sprite cpuEasySprite;
    [SerializeField] private Sprite cpuNormalSprite;
    [SerializeField] private Sprite cpuHardSprite;

    // -------------------------
    // 「キーボード / ゲームパッド」切替ボタンの画像
    // -------------------------

    [Header("Control Type Button Images (per player)")]
    [SerializeField] private Image[] controlTypeImages = new Image[4]; // 各プレイヤーの「操作方法」ボタン画像
    [SerializeField] private Sprite keyboardSprite; // キーボードの時の表示スプライト
    [SerializeField] private Sprite gamepadSprite;  // ゲームパッドの時の表示スプライト

    // -------------------------
    // ★キーボード同時使用制限（UI側）
    // -------------------------

    /// <summary>
    /// キーボードは同時に1人だけ：2人目がキーボードを選ぼうとした時の挙動を選べる
    /// </summary>
    private enum KeyboardLimitMode
    {
        DenySecond, // 2人目は選べない（拒否して元に戻す）
        Swap        // 2人目が選んだら、既存のキーボード担当と入れ替える
    }

    [Header("Keyboard Limit (Offline)")]
    [SerializeField] private KeyboardLimitMode keyboardLimitMode = KeyboardLimitMode.DenySecond;
    // ↑ Inspector で「拒否」か「入れ替え」かを選択できる

    // -------------------------
    // ★画面上メッセージ（注意表示）
    // -------------------------

    [Header("Notice UI")]
    [SerializeField] private Text noticeText;                 // 画面に表示するテキスト（UI Text）
    [SerializeField] private float noticeDuration = 1.5f;     // 表示時間（秒）
    private Coroutine noticeCoroutine;                        // 表示を消すためのコルーチン参照（多重起動防止）

    // -------------------------
    // 「CPUと対戦」モードのON/OFFボタン表示
    // -------------------------

    [Header("CPU Match Button")]
    [SerializeField] private Image cpuMatchImage;   // CPU対戦ボタンの画像
    [SerializeField] private Sprite circleSprite;   // OFFの表示（○など）
    [SerializeField] private Sprite checkSprite;    // ONの表示（☑など）

    // -------------------------
    // 遷移先シーン名
    // -------------------------

    [Header("Scene Names")]
    [SerializeField] private string gameSceneName = "GameScene"; // ゲーム本編のシーン名

    // -------------------------
    // CPU対戦モードの復元用
    // -------------------------

    // CPU対戦モードONにした時、元の「人間/CPU」状態を一時保存しておく配列
    private GameSettings.SlotType[] backupSlotTypes = new GameSettings.SlotType[4];

    // CPU対戦モードがONかどうか
    private bool cpuMatchMode = false;

    // =========================
    // Unityイベント
    // =========================

    private void Awake()
    {
        // 個別の変数を配列にまとめて扱えるようにする
        // これにより「人数に応じて表示/非表示」をループで書ける
        playerPanels = new GameObject[4]
        {
            player1UIBG, player2UIBG, player3UIBG, player4UIBG
        };

        // Notice初期化（参照がある場合は非表示に）
        // 起動直後に noticeText が表示されたままにならないようにする
        HideNoticeImmediate();
    }

    private void OnEnable()
    {
        // パネルが表示されたタイミングで設定を初期化する
        // （※毎回リセットしたくない場合は、ここを外す/条件分岐する）
        GameSettings.ResetDefaults();

        // 人数に応じてプレイヤー枠の表示/非表示を反映
        ApplyPanelsVisible();

        // 現在の設定に合わせてボタン画像を更新
        RefreshImages();

        // パネルを開くたびに注意表示は一旦消しておく
        HideNoticeImmediate();
    }

    // =========================
    // Player Count（人数変更）
    // =========================

    /// <summary>
    /// 参加人数を 2〜4 に設定する。
    /// UIボタン（2P/3P/4Pなど）から呼ぶ想定。
    /// </summary>
    public void SetPlayerCount(int count)
    {
        // 念のため2〜4に丸める（想定外の値が来ても安全）
        GameSettings.playerCount = Mathf.Clamp(count, 2, 4);

        // 人数に応じて枠の表示を更新
        ApplyPanelsVisible();

        // ボタン画像など見た目を更新
        RefreshImages();
    }

    // =========================
    // CPU Toggle（人間/CPU切替）
    // =========================

    /// <summary>
    /// 指定プレイヤーを「人間 ⇄ CPU」で切り替える。
    /// indexは 0..3（P1..P4）を想定。
    /// </summary>
    public void ToggleCPU(int index)
    {
        // 人数外の枠（例：2人プレイ中のP3/P4）は触れないようにする
        if (index >= GameSettings.playerCount) return;

        // Human と CPU を交互に切り替え
        GameSettings.slotTypes[index] =
            (GameSettings.slotTypes[index] == GameSettings.SlotType.Human)
            ? GameSettings.SlotType.CPU
            : GameSettings.SlotType.Human;

        // 見た目を更新
        RefreshImages();
    }

    public void ToggleCPUDifficulty(int index)
    {
        if (index >= GameSettings.playerCount) return;

        // CPUじゃないなら難易度いじれない（ボタンも隠す運用にする）
        if (GameSettings.slotTypes[index] != GameSettings.SlotType.CPU) return;

        // Easy → Normal → Hard → Easy...
        switch (GameSettings.cpuDifficulties[index])
        {
            case GameSettings.CpuDifficulty.Easy:
                GameSettings.cpuDifficulties[index] = GameSettings.CpuDifficulty.Normal;
                break;

            case GameSettings.CpuDifficulty.Normal:
                GameSettings.cpuDifficulties[index] = GameSettings.CpuDifficulty.Hard;
                break;

            default:
                GameSettings.cpuDifficulties[index] = GameSettings.CpuDifficulty.Easy;
                break;
        }

        RefreshImages();
    }

    // =========================
    // Control Type Toggle（操作方法切替）
    // =========================

    /// <summary>
    /// 指定プレイヤーの操作方法を「キーボード ⇄ ゲームパッド」で切り替える。
    /// ※CPUのプレイヤーは操作方法を切り替える意味がないので無効化している。
    ///
    /// ★追加：
    /// ・オフラインでは「キーボードは同時に1人だけ」を UI 側で制限する。
    /// ・制限に引っかかったら画面上にメッセージを出す。
    /// </summary>
    public void ToggleControlType(int index)
    {
        // 人数外は対象外
        if (index >= GameSettings.playerCount) return;

        // CPUは操作方法の切替が不要なので無効
        if (GameSettings.slotTypes[index] == GameSettings.SlotType.CPU) return;

        // 次にしたいタイプ（トグル：Keyboard→Gamepad, Gamepad→Keyboard）
        GameSettings.ControlType desired =
            (GameSettings.controlTypes[index] == GameSettings.ControlType.Keyboard)
            ? GameSettings.ControlType.Gamepad
            : GameSettings.ControlType.Keyboard;

        // -------------------------
        // ★キーボード同時使用制限
        // -------------------------
        if (desired == GameSettings.ControlType.Keyboard)
        {
            // 「自分以外」にすでにキーボード担当の人間がいるか探す
            int otherKeyboardUser = FindKeyboardHumanIndexExcept(index);

            // 既に他のプレイヤーがキーボードだった場合
            if (otherKeyboardUser != -1)
            {
                if (keyboardLimitMode == KeyboardLimitMode.DenySecond)
                {
                    // 【拒否モード】
                    // 2人目のキーボード選択は無効化して、注意を出す
                    ShowNotice("キーボードは1人まで！");

                    // 画像を更新してUIを正しい状態に戻す
                    // （※この時点では設定は変えていないが、念のため再描画）
                    RefreshImages();
                    return;
                }
                else // Swap
                {
                    // 【入れ替えモード】
                    // 「既存のキーボード担当」をゲームパッドへ切り替えてから
                    // いま操作しているプレイヤーをキーボードにする

                    // ただしゲームパッドが1つも無いなら入れ替え不可
                    if (Gamepad.all.Count == 0)
                    {
                        ShowNotice("ゲームパッドが無いので入れ替えできません");
                        RefreshImages();
                        return;
                    }

                    // 既存のキーボード担当を Gamepad にする
                    GameSettings.controlTypes[otherKeyboardUser] = GameSettings.ControlType.Gamepad;

                    // 入れ替えたことが分かるように通知
                    ShowNotice($"P{otherKeyboardUser + 1} をゲームパッドに変更しました");
                }
            }
        }

        // ここまで来たら desired に変更してOK
        GameSettings.controlTypes[index] = desired;

        // 見た目更新
        RefreshImages();
    }

    /// <summary>
    /// 「自分以外」でキーボード担当の Human を探して、その index を返す。
    /// 見つからないなら -1。
    ///
    /// 条件：
    /// ・slotTypes が Human
    /// ・controlTypes が Keyboard
    /// ・exceptIndex（自分）は除外する
    /// </summary>
    private int FindKeyboardHumanIndexExcept(int exceptIndex)
    {
        for (int i = 0; i < GameSettings.playerCount; i++)
        {
            if (i == exceptIndex) continue; // 自分は除外
            if (GameSettings.slotTypes[i] != GameSettings.SlotType.Human) continue; // CPUなら対象外

            if (GameSettings.controlTypes[i] == GameSettings.ControlType.Keyboard)
                return i; // 見つけたらそのindex
        }
        return -1; // 見つからない
    }

    // =========================
    // CPUと対戦ボタン（モード切替）
    // =========================

    /// <summary>
    /// CPU対戦モードをON/OFFする。
    ///
    /// ON：
    /// ・現在のslotTypesを保存（復元用）
    /// ・P1はHuman固定
    /// ・P2以降はCPU固定
    ///
    /// OFF：
    /// ・保存していたslotTypesに戻す
    /// </summary>
    public void ToggleCPUMatchMode()
    {
        // ON/OFF反転
        cpuMatchMode = !cpuMatchMode;

        if (cpuMatchMode)
        {
            // ---- ONにする ----

            // 現在の状態を保存
            for (int i = 0; i < GameSettings.playerCount; i++)
            {
                backupSlotTypes[i] = GameSettings.slotTypes[i];
            }

            // P1は人間固定
            GameSettings.slotTypes[0] = GameSettings.SlotType.Human;

            // それ以降はCPUに
            for (int i = 1; i < GameSettings.playerCount; i++)
            {
                GameSettings.slotTypes[i] = GameSettings.SlotType.CPU;
            }
        }
        else
        {
            // ---- OFFにする（元に戻す）----
            for (int i = 0; i < GameSettings.playerCount; i++)
            {
                GameSettings.slotTypes[i] = backupSlotTypes[i];
            }
        }

        // UI更新
        RefreshImages();
    }

    // =========================
    // タイトルへ戻る
    // =========================

    /// <summary>
    /// 設定パネルを閉じてタイトルパネルへ戻す。
    /// </summary>
    public void BackToTitle()
    {
        if (offlinePanel != null)
            offlinePanel.SetActive(false); // 設定パネルを非表示

        if (titlePanel != null)
            titlePanel.SetActive(true);    // タイトルを表示

        // 戻った時に初期設定に戻す
        GameSettings.ResetDefaults();
    }

    // =========================
    // Play（ゲーム開始）
    // =========================

    /// <summary>
    /// ゲームシーンへ遷移する。
    /// </summary>
    public void PlayGame()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    // =========================
    // UI更新（内部処理）
    // =========================

    /// <summary>
    /// 人数に応じて、P3/P4の枠を表示/非表示にする。
    /// </summary>
    private void ApplyPanelsVisible()
    {
        // P1とP2は常に表示（最低2人）
        playerPanels[0].SetActive(true);
        playerPanels[1].SetActive(true);

        // P3は3人以上の時だけ表示
        playerPanels[2].SetActive(GameSettings.playerCount >= 3);

        // P4は4人の時だけ表示
        playerPanels[3].SetActive(GameSettings.playerCount >= 4);
    }

    /// <summary>
    /// 現在の GameSettings の状態に合わせて、各ボタンの画像を更新する。
    /// ・人間/CPUボタン
    /// ・操作方法ボタン（CPUならCPU画像を表示）
    /// ・CPU対戦モードのON/OFF画像（○⇄☑）
    /// </summary>
    private void RefreshImages()
    {
        // 最大4人分を更新
        for (int i = 0; i < 4; i++)
        {
            // 人数外（例：2人ならP3/P4）は更新しない
            if (i >= GameSettings.playerCount) continue;

            // -------------------------
            // 人間 / CPU 画像切り替え
            // -------------------------
            if (cpuSelectImages[i] != null)
            {
                cpuSelectImages[i].sprite =
                    (GameSettings.slotTypes[i] == GameSettings.SlotType.Human)
                    ? playerSprite
                    : cpuSprite;
            }

            // -------------------------
            // 操作方法画像切り替え
            // -------------------------
            if (controlTypeImages[i] != null)
            {
                if (GameSettings.slotTypes[i] == GameSettings.SlotType.CPU)
                {
                    controlTypeImages[i].sprite = cpuSprite;
                }
                else
                {
                    controlTypeImages[i].sprite =
                        (GameSettings.controlTypes[i] == GameSettings.ControlType.Keyboard)
                        ? keyboardSprite
                        : gamepadSprite;
                }
            }

            // =========================
            // ★CPU難易度ボタン（表示/非表示 & 画像切り替え）
            // =========================
            bool isCpu = (GameSettings.slotTypes[i] == GameSettings.SlotType.CPU);

            // CPUのときだけボタン自体を表示
            if (cpuDifficultyButtons != null &&
                i < cpuDifficultyButtons.Length &&
                cpuDifficultyButtons[i] != null)
            {
                cpuDifficultyButtons[i].SetActive(isCpu);
            }

            // CPUのときだけ難易度スプライト更新
            if (isCpu &&
                cpuDifficultyImages != null &&
                i < cpuDifficultyImages.Length &&
                cpuDifficultyImages[i] != null)
            {
                switch (GameSettings.cpuDifficulties[i])
                {
                    case GameSettings.CpuDifficulty.Easy:
                        cpuDifficultyImages[i].sprite = cpuEasySprite;
                        break;

                    case GameSettings.CpuDifficulty.Normal:
                        cpuDifficultyImages[i].sprite = cpuNormalSprite;
                        break;

                    case GameSettings.CpuDifficulty.Hard:
                        cpuDifficultyImages[i].sprite = cpuHardSprite;
                        break;
                }
            }
        }

        // -------------------------
        // CPU対戦ボタン（○⇄☑）
        // -------------------------
        if (cpuMatchImage != null)
        {
            cpuMatchImage.sprite = cpuMatchMode ? checkSprite : circleSprite;
        }
    }

    // =========================
    // Notice（画面メッセージ）
    // =========================

    /// <summary>
    /// 画面に注意メッセージを表示し、一定時間後に自動で消す。
    /// 連続で呼ばれた場合は、前の表示を止めて新しいメッセージを優先する。
    /// </summary>
    private void ShowNotice(string message)
    {
        if (noticeText == null) return; // 参照が無ければ何もしない

        // メッセージ内容を更新して表示
        noticeText.text = message;
        noticeText.gameObject.SetActive(true);

        // すでに「消す処理」が動いていたら止める（多重起動防止）
        if (noticeCoroutine != null) StopCoroutine(noticeCoroutine);

        // 指定秒後に非表示にする
        noticeCoroutine = StartCoroutine(HideNoticeAfterSeconds(noticeDuration));
    }

    /// <summary>
    /// 指定秒後に Notice を消す（Time.timeScale の影響を受けない）。
    /// </summary>
    private IEnumerator HideNoticeAfterSeconds(float seconds)
    {
        // タイトル画面やUI中に timeScale を変更していても正しく動くように Realtime を使う
        yield return new WaitForSecondsRealtime(seconds);

        // 完全に非表示
        HideNoticeImmediate();
    }

    /// <summary>
    /// Notice を即座に非表示にする。
    /// すでに動いているコルーチンがあれば停止してから消す。
    /// </summary>
    private void HideNoticeImmediate()
    {
        // 自動非表示コルーチンが動いていたら止める
        if (noticeCoroutine != null)
        {
            StopCoroutine(noticeCoroutine);
            noticeCoroutine = null;
        }

        // テキストが設定されているなら非表示
        if (noticeText != null)
        {
            noticeText.gameObject.SetActive(false);
        }
    }
}