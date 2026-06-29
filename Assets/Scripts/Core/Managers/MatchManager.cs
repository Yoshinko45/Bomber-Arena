using System.Collections;
using System.Collections.Generic;   // List<T> を使うため（Gamepad一覧など）
using UnityEngine;
using UnityEngine.InputSystem;      // Input System（PlayerInput / Gamepad / Keyboard など）
using UnityEngine.SceneManagement;

/// <summary>
/// 試合（マッチ）全体の進行管理クラス。
///
/// 役割：
/// - ラウンド開始演出（Round → GO）
/// - プレイヤーのリスポーン＆初期化
/// - 生存判定（最後の1人が勝利）
/// - 勝利数管理（HUD表示）
/// - マップリセット（破壊ブロック復元、アイテム削除）
///
/// 注意：
/// - Time.timeScale = 0 の間は「ゲーム時間」が停止する。
///   そのため WaitForSeconds は止まるが、WaitForSecondsRealtime は止まらない。
///   RoundBannerUI 側で WaitForSecondsRealtime を使う想定。
/// </summary>
public class MatchManager : MonoBehaviour
{
    // -------------------------
    // Inspector から設定する参照（Unityの画面で設定する）
    // -------------------------

    [Header("Players (Player1..Player4)")]
    // プレイヤー本体（GameObject）。配列順が Player1..Player4 になる想定
    [SerializeField] private GameObject[] players;

    [Header("HUD Panels (Player1..Player4)")]
    // 各プレイヤーのHUD（勝利数、アイテム表示など）
    [SerializeField] private PlayerHUDPanel[] hudPanels;

    [Header("Round Banner")]
    // ラウンド演出表示（Round x / GO! など）
    [SerializeField] private RoundBannerUI roundBannerUI;

    [Header("Spawn Positions (Player1..Player4)")]
    // ラウンド開始時に戻すスポーン座標（playersと同じ順番で用意）
    [SerializeField] private Vector2[] spawnPositions;

    [Header("Map Reset")]
    // ステージの初期化担当（破壊ブロック復元、残ったアイテム削除など）
    [SerializeField] private StageResetter stageResetter;

    [Header("Scenes")]
    [SerializeField] private string resultSceneName = "Result";

    // -------------------------
    // 内部状態（スクリプト内部で管理する値）
    // -------------------------

    // 勝利数（wins[0]がPlayer1の勝利数）
    private int[] wins;

    // 現在のラウンド番号（1,2,3...）
    private int currentRound = 0;

    // ラウンド中に勝利判定が二重で走らないようにするフラグ
    private bool roundEnded = false;

    /// <summary>
    /// 入力ロックフラグ（他スクリプトから参照可能）。
    /// 例：MovementController / BombController 側で
    ///     「InputLocked中は入力を処理しない」ようにする。
    /// </summary>
    public static bool InputLocked { get; private set; } = false;

    private void Awake()
    {
        // 勝利数配列を用意（4人固定想定）
        // ※将来的に players.Length に合わせたいなら new int[players.Length] でもOK
        wins = new int[4];
    }

    private void Start()
    {
        // タイトル設定（人数/CPU/操作方式）を反映し、入力デバイスも割り当てる
        ApplySettings();

        // 1ラウンド目開始
        StartCoroutine(StartRoundRoutine());
    }

    /// <summary>
    /// プレイヤーごとに Input System のデバイス割り当てを行う。
    ///
    /// 仕様：
    /// - CPUスロットは入力不要なのでスキップ
    /// - Keyboard担当のプレイヤーは Keyboard（＋MouseがあればMouseも）を割り当て
    /// - Gamepad担当は接続順に1台ずつ割り当て
    /// - Keyboard は1人だけ（2人目以降は警告を出して Gamepad に切り替える）
    ///
    /// 前提：
    /// - 各プレイヤーに PlayerInput が付いている
    /// - InputActions に "Player" ActionMap がある
    /// - Control Scheme に "Keyboard&Mouse" と "Gamepad" がある
    /// </summary>
    private void ConfigurePlayerInputs()
    {
        // 現在接続されているGamepad一覧（接続順）
        var pads = new List<Gamepad>(Gamepad.all);

        // 次に割り当てるGamepadのインデックス
        int padIndex = 0;

        // キーボードは1人までの制限フラグ
        bool keyboardAlreadyAssigned = false;

        // players配列を順番に処理
        for (int i = 0; i < players.Length; i++)
        {
            // 参加人数外のスロットはスキップ
            if (i >= GameSettings.playerCount) continue;

            // プレイヤーが存在しない / 非表示（階層的に無効）ならスキップ
            // ※activeInHierarchy は親のActiveも含めた状態
            if (players[i] == null || !players[i].activeInHierarchy) continue;

            // CPUスロットは入力不要（AIで動かす想定）
            if (GameSettings.slotTypes[i] == GameSettings.SlotType.CPU) continue;

            // PlayerInputを取得
            var pi = players[i].GetComponent<PlayerInput>();
            if (pi == null) continue;

            // 念のためいったん入力OFF（切り替え中に変な入力が入るのを防ぐ）
            pi.DeactivateInput();

            // ====== どのデバイスを割り当てるか確定（Keyboard / Gamepad） ======

            // 「Keyboard指定だけど既に別プレイヤーがKeyboardを使っている」場合
            if (GameSettings.controlTypes[i] == GameSettings.ControlType.Keyboard)
            {
                if (keyboardAlreadyAssigned)
                {
                    // ここでは「強制でGamepadに切り替える」方針
                    // ※この挙動が嫌なら、ここでcontinueして入力無効のままにする等も可能
                    Debug.LogWarning($"Player{i + 1}: Keyboardは既に使用中。Gamepadへ切り替えます。");
                    GameSettings.controlTypes[i] = GameSettings.ControlType.Gamepad;
                }
            }

            // ここから実際の割り当て処理
            if (GameSettings.controlTypes[i] == GameSettings.ControlType.Keyboard)
            {
                // Keyboard担当
                var k = Keyboard.current;
                var m = Mouse.current;

                // 環境によっては Keyboard.current が null の可能性があるためチェック
                if (k == null)
                {
                    Debug.LogWarning($"Player{i + 1}: Keyboardが見つからないため入力を無効化");
                    // 入力OFFのまま次へ（ActivateInputしない）
                    continue;
                }

                // Keyboardが割り当てられたのでフラグを立てる（以降は2人目を弾く）
                keyboardAlreadyAssigned = true;

                // Control Scheme を先に確定（Keyboard&Mouse）
                // Mouseがある場合は一緒に渡す（なくても動く）
                if (m != null) pi.SwitchCurrentControlScheme("Keyboard&Mouse", k, m);
                else           pi.SwitchCurrentControlScheme("Keyboard&Mouse", k);
            }
            else
            {
                // Gamepad担当
                if (padIndex >= pads.Count)
                {
                    // Gamepad不足（割り当て不可能）
                    Debug.LogWarning($"Player{i + 1}: Gamepadが足りないため入力を無効化");
                    continue;
                }

                // 接続順に1台ずつ割り当て
                var pad = pads[padIndex++];
                pi.SwitchCurrentControlScheme("Gamepad", pad);
            }

            // ====== スキームが確定した後に ActionMap と入力を有効化する ======
            // （順番が重要：先にスキーム→次にアクションマップ→最後に入力ON）
            pi.SwitchCurrentActionMap("Player");
            pi.ActivateInput();
        }
    }

    /// <summary>
    /// GameSettings の内容を players / HUD / スクリプト有効無効 に反映する。
    ///
    /// 反映内容：
    /// - playerCount   ：参加人数（1～4）
    /// - slotTypes     ：プレイヤー or CPU
    /// - controlTypes  ：Keyboard or Gamepad（入力割り当てで使用）
    ///
    /// 最後に ConfigurePlayerInputs() を呼び、PlayerInputのデバイス割り当ても行う。
    /// ただし、PlayerInputは OnEnable の初期化があるため「次フレーム」に回して安全にする。
    /// </summary>
    private void ApplySettings()
    {
        for (int i = 0; i < players.Length; i++)
        {
            // 参加人数内なら表示、それ以外は非表示（不参加）
            bool active = i < GameSettings.playerCount;

            // プレイヤーの表示ON/OFF
            players[i].SetActive(active);

            // HUDも人数分だけ表示
            if (hudPanels != null && i < hudPanels.Length)
                hudPanels[i].gameObject.SetActive(active);

            // 不参加スロットはこれ以上設定不要
            if (!active) continue;

            // CPUかどうか
            bool isCPU = GameSettings.slotTypes[i] == GameSettings.SlotType.CPU;

            // CPUならプレイヤー操作スクリプトを無効化（入力で動かないようにする）
            var move = players[i].GetComponent<MovementController>();
            if (move != null) move.enabled = !isCPU;

            var bomb = players[i].GetComponent<BombController>();
            if (bomb != null) bomb.enabled = !isCPU;

            // HUDに対象プレイヤーを再バインド（参照先更新）
            if (hudPanels != null && i < hudPanels.Length)
                hudPanels[i].Bind(players[i]);
        }

        // ★入力割り当ては「即時」ではなく「次フレーム」に実行する
        // 理由：SetActive(true) した直後は PlayerInput の OnEnable 初期化が終わっていない場合があり、
        //       そのフレームで SwitchCurrentControlScheme などをすると不安定になりやすい。
        StopCoroutine(nameof(ConfigurePlayerInputsNextFrame));
        StartCoroutine(nameof(ConfigurePlayerInputsNextFrame));
    }

    /// <summary>
    /// 1フレーム待ってから入力割り当てを行うコルーチン。
    /// SetActive(true) 直後の PlayerInput 初期化完了を待つための安全策。
    /// </summary>
    private IEnumerator ConfigurePlayerInputsNextFrame()
    {
        yield return null; // ★重要：次フレームまで待つ
        ConfigurePlayerInputs();
    }

    /// <summary>
    /// ラウンド開始の流れ：
    /// 1) ラウンド番号更新
    /// 2) マップ初期化（ブロック復元＋アイテム削除）
    /// 3) プレイヤーを復活＆位置リセット＆初期化
    /// 4) 入力ロック
    /// 5) Round→GO演出（TimeScaleを止めたまま）
    /// 6) 入力ロック解除
    /// </summary>
    private IEnumerator StartRoundRoutine()
    {
        // 1) ラウンド数を進める
        currentRound++;

        // 2) このラウンドではまだ勝敗が決まっていない状態に戻す
        roundEnded = false;

        // 3) マップ初期化（ステージ復元＆残留物削除）
        // Inspector未設定でも動くように保険（ただし重いので頻繁なFindは避けたい）
        if (stageResetter == null) stageResetter = FindObjectOfType<StageResetter>();
        if (stageResetter != null) stageResetter.ResetStage();

        // 4) プレイヤーを復活＆座標/状態をリセット
        ResetPlayersForNewRound();

        // 5) 演出中は操作禁止
        InputLocked = true;

        // 6) ゲーム時間停止（移動・爆弾タイマー等が進まない）
        Time.timeScale = 0f;

        // Round→GO演出（UIはWaitForSecondsRealtimeを使う想定）
        if (roundBannerUI != null)
            yield return StartCoroutine(roundBannerUI.ShowRoundThenGo(currentRound));

        // 7) ゲーム再開＆操作解放
        Time.timeScale = 1f;
        InputLocked = false;
    }

    private void Update()
    {
        // すでにラウンド終了処理に入っているなら何もしない（多重実行防止）
        if (roundEnded) return;

        int aliveCount = 0;       // 生存人数
        int lastAliveIndex = -1;  // 最後に見つかった生存プレイヤーのインデックス

        // players配列を走査して activeSelf なら「生存」とみなす
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] != null && players[i].activeSelf)
            {
                aliveCount++;
                lastAliveIndex = i;
            }
        }

        // 生存が1人以下になったら勝敗確定
        // lastAliveIndex != -1 は「生存者が誰かいる」ことの確認
        // （全滅を引き分け扱いにしたいなら条件を変える）
        if (aliveCount <= 1 && lastAliveIndex != -1)
        {
            roundEnded = true;

            // 次ラウンド開始まで操作禁止
            InputLocked = true;

            // 同フレームで他の処理が進まないように停止
            Time.timeScale = 0f;

            // 勝者処理へ
            OnRoundWin(lastAliveIndex);
        }
    }

    /// <summary>
    /// ラウンド勝者が決まったときの処理。
    /// - 勝利数加算
    /// - HUD更新
    /// - 規定勝利数に到達したらマッチ勝利（ここではログのみ）
    /// - まだ続くなら次ラウンドへ
    /// </summary>
    private void OnRoundWin(int winnerIndex)
    {
        // 勝利数を加算
        wins[winnerIndex]++;

        // HUDに勝利数を反映
        if (winnerIndex < hudPanels.Length && hudPanels[winnerIndex] != null)
        {
            hudPanels[winnerIndex].SetWins(wins[winnerIndex]);
        }

       if (wins[winnerIndex] >= 3)
        {
            Debug.Log($"MATCH WINNER: Player {winnerIndex + 1}");

            // 時間停止解除（リザルトでUI動かすため）
            Time.timeScale = 1f;
            InputLocked = false;

            // 結果を保存してリザルトへ
            MatchResultData.SetFrom(GameSettings.playerCount, wins);
            SceneManager.LoadScene(resultSceneName);
            return;
        }

        // 次ラウンド開始
        StartCoroutine(NextRound());
    }

    /// <summary>
    /// 次ラウンドへ進むためのコルーチン。
    /// TimeScale=0 のままだと WaitForSeconds は止まるため、
    /// ここでは1フレーム待つだけ（yield return null）にしている。
    /// </summary>
    private IEnumerator NextRound()
    {
        // 1フレームだけ待って、処理が安定してから次ラウンド開始
        yield return null;

        StartCoroutine(StartRoundRoutine());
    }

    /// <summary>
    /// 新ラウンド開始前に全プレイヤーを復帰させる。
    ///
    /// 行うこと：
    /// - 参加人数外のプレイヤーは非アクティブ
    /// - スポーン位置へ戻す
    /// - Rigidbody2D の速度をゼロに戻す（押し出し・滑りの持ち越し防止）
    /// - BombController / MovementController のラウンド初期化を呼ぶ
    ///
    /// 重要：
    /// - active=false のプレイヤーに対して Reset を続けない（無駄処理/不具合防止）
    /// </summary>
    private void ResetPlayersForNewRound()
    {
        for (int i = 0; i < players.Length; i++)
        {
            var p = players[i];
            if (p == null) continue;

            // 参加人数内かどうか
            bool active = i < GameSettings.playerCount;

            // 参加人数外なら非表示（＝試合不参加）
            p.SetActive(active);

            // ★非参加ならここで終了（これ以降触らない）
            if (!active) continue;

            // スポーン位置へ戻す
            if (spawnPositions != null && i < spawnPositions.Length)
            {
                p.transform.position = spawnPositions[i];

                // 物理の慣性をリセット（前ラウンドの速度が残るのを防ぐ）
                var rb = p.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.velocity = Vector2.zero;
                    rb.angularVelocity = 0f;
                }
            }

            // 爆弾関連の初期化
            var bomb = p.GetComponent<BombController>();
            if (bomb != null)
            {
                // 注意：
                // ここは常に true にしているので、CPUスロットでもBombControllerが有効になる。
                // CPUに爆弾を使わせないなら、moveと同様に isCPU 条件で制御すると安全。
                bomb.enabled = true;
                bomb.ResetForNewRound();
            }

            // 移動関連の初期化
            var move = p.GetComponent<MovementController>();
            if (move != null)
            {
                // CPUならプレイヤー入力による移動は無効化
                move.enabled = GameSettings.slotTypes[i] != GameSettings.SlotType.CPU;
                move.ResetForNewRound();
            }
        }
    }
}