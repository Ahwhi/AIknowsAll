using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game Settings")]
    public int totalRounds     = 10;
    public int chancesPerRound = 5;
    public int undoPerRound    = 1;

    [Header("Classes (49개)")]
    public string[] allClasses = {
        "cat","dog","fish","bird","rabbit",
        "horse","cow","pig","duck","frog",
        "lion","elephant","shark","snake","butterfly",
        "car","airplane","bicycle","bus","train",
        "helicopter","ambulance",
        "apple","banana","pizza","cake","ice cream",
        "hamburger","grapes","watermelon",
        "tree","sun","cloud","flower","mushroom","mountain",
        "house","star","umbrella","clock","guitar",
        "cup","book","pencil",
        "hand","eye","face","smiley face","crown"
    };

    [Header("References")]
    public DrawingCanvas  drawingCanvas;
    public ModelInference modelInference;
    public UIManager      uiManager;

    // ── 게임 상태 ────────────────────────────────────────
    private List<string> _roundClasses   = new();
    private int          _currentRound   = 0;
    private int          _chancesLeft    = 0;
    private int          _undoLeft       = 0;
    private float        _totalScore     = 0f;
    private bool         _gameActive     = false;
    private bool         _roundAnswered  = false;

    // 현재 라운드 제시어
    public string CurrentWord  => _currentRound < _roundClasses.Count ? _roundClasses[_currentRound] : "";
    public int    ChancesLeft  => _chancesLeft;
    public int    UndoLeft     => _undoLeft;
    public int    CurrentRound => _currentRound;
    public int    TotalRounds  => totalRounds;
    public float  AverageScore => _currentRound > 0 ? _totalScore / _currentRound : 0f;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        StartGame();
    }

    // ── 게임 시작 ─────────────────────────────────────────
    public void StartGame()
    {
        // 49개 중 10개 랜덤 추출
        List<string> pool = new(allClasses);
        _roundClasses.Clear();
        for (int i = 0; i < totalRounds && pool.Count > 0; i++)
        {
            int idx = Random.Range(0, pool.Count);
            _roundClasses.Add(pool[idx]);
            pool.RemoveAt(idx);
        }

        _currentRound  = 0;
        _totalScore    = 0f;
        _gameActive    = true;
        StartRound();
    }

    // ── 라운드 시작 ───────────────────────────────────────
    void StartRound()
    {
        _chancesLeft   = chancesPerRound;
        _undoLeft      = undoPerRound;
        _roundAnswered = false;
        drawingCanvas.ClearCanvas();
        uiManager.UpdateHUD();
        uiManager.ShowWord(CurrentWord);
    }

    // ── 마우스 업 시 호출 (DrawingCanvas에서 호출) ────────
    public void OnDrawingSubmitted(float topProb, string topClass) {
        if (!_gameActive || _chancesLeft <= 0) return;

        _chancesLeft--;

        uiManager.UpdateHUD();

        // 기회 다 소진 시 라운드 종료
        if (_chancesLeft <= 0) {
            _roundAnswered = true;

            bool correct = topClass.ToLower() == CurrentWord.ToLower();
            float score = correct ? topProb * 100f : 0f;
            _totalScore += score;

            uiManager.ShowRoundResult(correct, score);
            Invoke(nameof(NextRound), 1.5f);
        }
    }

    // ── 다음 라운드 ───────────────────────────────────────
    void NextRound()
    {
        _currentRound++;
        if (_currentRound >= totalRounds)
        {
            EndGame();
            return;
        }
        StartRound();
    }

    // ── 게임 종료 ─────────────────────────────────────────
    void EndGame()
    {
        _gameActive = false;
        float avg = _totalScore / totalRounds;
        uiManager.ShowEndPopup(avg);
    }

    // ── Ctrl+Z 언두 ───────────────────────────────────────
    public bool TryUndo()
    {
        if (_undoLeft <= 0 || !_gameActive || _roundAnswered) return false;
        _undoLeft--;
        uiManager.UpdateHUD();
        return true;
    }

    public void SubmitCurrentResult(float topProb, string topClass) {
        if (!_gameActive || _roundAnswered) return;
        _roundAnswered = true;
        _chancesLeft = 0;
        bool correct = topClass.ToLower() == CurrentWord.ToLower();
        float score = correct ? topProb * 100f : 0f;
        _totalScore += score;
        uiManager.ShowRoundResult(correct, score);
        uiManager.UpdateHUD();
        Invoke(nameof(NextRound), 1.5f);
    }

    public bool IsRoundAnswered => _roundAnswered;
    public bool IsGameActive    => _gameActive;
}
