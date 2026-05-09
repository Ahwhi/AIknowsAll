using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class UIManager : MonoBehaviour
{
    // ── 결과 표시 ─────────────────────────────────────────
    [Header("Result UI")]
    public TextMeshProUGUI resultText;
    public TextMeshProUGUI titleText;
    public Image[]             probBars;
    public TextMeshProUGUI[]   probLabels;

    // ── HUD (좌측 상단) ───────────────────────────────────
    [Header("HUD")]
    public TextMeshProUGUI hudRoundText;     // 남은 라운드
    public TextMeshProUGUI hudScoreText;     // 평균 점수
    public TextMeshProUGUI hudChancesText;   // 남은 기회
    public TextMeshProUGUI hudUndoText;      // 남은 Ctrl+Z

    // ── 제시어 ────────────────────────────────────────────
    [Header("Word Display")]
    public TextMeshProUGUI wordText;         // 현재 제시어

    // ── 라운드 결과 (정답/오답 피드백) ───────────────────
    [Header("Round Result")]
    public GameObject          roundResultPanel;
    public TextMeshProUGUI     roundResultText;

    // ── 게임 종료 팝업 ────────────────────────────────────
    [Header("End Popup")]
    public GameObject          endPopupPanel;
    public TextMeshProUGUI     endScoreText;
    public Button              restartButton;
    public Button              quitButton;

    void Start()
    {
        ClearResult();
        if (titleText        != null) titleText.text = "AI knows All";
        if (roundResultPanel != null) roundResultPanel.SetActive(false);
        if (endPopupPanel    != null) endPopupPanel.SetActive(false);

        if (restartButton != null)
            restartButton.onClick.AddListener(() =>
            {
                endPopupPanel.SetActive(false);
                GameManager.Instance.StartGame();
            });

        if (quitButton != null)
            quitButton.onClick.AddListener(() => Application.Quit());
    }

    // ── HUD 업데이트 ──────────────────────────────────────
    public void UpdateHUD()
    {
        if (GameManager.Instance == null) return;

        if (hudRoundText   != null)
            hudRoundText.text   = $"라운드:  {GameManager.Instance.CurrentRound + 1} / {GameManager.Instance.TotalRounds}";

        if (hudScoreText   != null)
            hudScoreText.text   = $"평균 점수:  {GameManager.Instance.AverageScore:F1}점";

        if (hudChancesText != null)
            hudChancesText.text = $"남은 기회:  {GameManager.Instance.ChancesLeft}번";

        if (hudUndoText    != null)
            hudUndoText.text    = $"Ctrl+Z:  {GameManager.Instance.UndoLeft}회";
    }

    // ── 제시어 표시 ───────────────────────────────────────
    public void ShowWord(string word)
    {
        if (wordText != null)
            wordText.text = $"제시어: {word}";

        if (roundResultPanel != null)
            roundResultPanel.SetActive(false);

        ClearResult();
        UpdateHUD();
    }

    // ── AI 예측 결과 표시 ─────────────────────────────────
    public void ShowResult(List<(string label, float prob)> top3) {
        string output = "";

        string[] colors = { "#FFD700", "#C0C0C0", "#CD7F32" }; // 금, 은, 동
        string[] medals = { "1.", "2.", "3." };

        for (int i = 0; i < top3.Count; i++) {
            float prob = top3[i].prob * 100f;
            int barLen = Mathf.RoundToInt(prob / 5f);
            string bar = new string('█', barLen) + new string('░', 20 - barLen);
            output += $"<color={colors[i]}>{medals[i]} {top3[i].label,-12} {prob:F1}%\n";
            output += $"   {bar}</color>\n\n";
        }

        if (resultText != null) resultText.text = output;

        if (probBars != null) {
            for (int i = 0; i < probBars.Length && i < top3.Count; i++) {
                if (probBars[i] != null) probBars[i].fillAmount = top3[i].prob;
                if (probLabels != null && i < probLabels.Length && probLabels[i] != null)
                    probLabels[i].text = $"{top3[i].label} {top3[i].prob * 100f:F1}%";
            }
        }
    }

    public void ClearResult()
    {
        if (resultText != null) resultText.text = "그림을 그려보세요!";
        if (probBars   != null) foreach (var b in probBars)   if (b != null) b.fillAmount = 0f;
        if (probLabels != null) foreach (var l in probLabels) if (l != null) l.text = "";
    }

    // ── 라운드 결과 피드백 ────────────────────────────────
    public void ShowRoundResult(bool correct, float score) {
        if (roundResultPanel == null) return;
        roundResultPanel.SetActive(true);

        if (roundResultText != null) {
            if (correct) {
                roundResultText.text =
                    $"<color=#4CFF7A>정답!</color>\n" +
                    $"<color=#FFFFFF>{score:F1}점</color>";
            } else {
                roundResultText.text =
                    $"<color=#FF5A5A>아쉬워요!</color>\n" +
                    $"정답: <color=#FFD700>{GameManager.Instance.CurrentWord}</color>";
            }
        }
    }

    // ── 게임 종료 팝업 ────────────────────────────────────
    public void ShowEndPopup(float avgScore)
    {
        if (endPopupPanel == null) return;
        endPopupPanel.SetActive(true);

        if (endScoreText != null)
            endScoreText.text = $"게임 종료!\n\n총 평균 점수\n{avgScore:F1}점";
    }
}
