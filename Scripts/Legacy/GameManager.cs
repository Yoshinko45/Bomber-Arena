using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement; // シーンの切り替えに必要

// 2026年/02/11　から一時的に使用停止中
// ゲーム全体の進行を管理するクラス
public class GameManager : MonoBehaviour
{
    // ゲームに参加しているプレイヤー全員を格納する配列
    // InspectorからプレイヤーのGameObjectを登録する
    public GameObject[] players;

    // 勝敗判定を行うメソッド
    public void CheckWinState()
    {
        // 生き残っているプレイヤーの数
        int aliveCount = 0;

        // 全プレイヤーを順番にチェック
        foreach (GameObject player in players)
        {
            // プレイヤーがアクティブ（＝生存）しているか確認
            if (player.activeSelf)
            {
                aliveCount++;
            }
        }

        // 生存プレイヤーが1人以下になったら勝敗確定
        if (aliveCount <= 1)
        {
            // 3秒後にNewRoundメソッドを呼び出す
            // 勝利演出などを見せるための待ち時間
            Invoke(nameof(NewRound), 3.0f);
        }
    }

    // 新しいラウンドを開始する処理
    private void NewRound()
    {
        // 現在のシーンを再読み込みしてゲームをリセット
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}