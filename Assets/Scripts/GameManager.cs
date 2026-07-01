using System;
using UnityEngine;

/// <summary>게임을 끝내는 방식. 새 조건을 추가하기 쉽게 한 곳에 모았다.</summary>
public enum EndCondition
{
    FixedMoves,   // 각자 정해진 수만큼 두면 종료 (우리가 정한 기본)
    TargetScore,  // 누군가 목표 점수에 먼저 도달하면 종료
    BoardFull,    // 판이 꽉 차면 종료
}

/// <summary>
/// 게임 진행을 총괄한다. BoardState(데이터)와 BoardView(화면)를 들고,
/// 두 플레이어에게 번갈아 수를 요청하고, 결과를 적용하고, 종료를 판정한다.
/// UI는 전혀 모른다 — 상태가 바뀌면 이벤트만 쏘고, GameUI가 그걸 듣는다.
/// </summary>
public class GameManager : MonoBehaviour
{
    [Header("참조")]
    public BoardView boardView;

    [Header("종료 조건")]
    public EndCondition endCondition = EndCondition.FixedMoves;
    public int maxMovesPerPlayer = 30;  // FixedMoves: 각자 둘 수 있는 수
    public int targetScore = 5;         // TargetScore: 목표 점수

    [Header("AI 설정")]
    public float aiThinkDelay = 0.4f;   // AI가 두기 전 잠깐 멈추는 시간(초)

    // ── UI가 구독하는 이벤트들 (GameManager는 UI를 직접 알지 못한다) ──
    public event Action<int, int> ScoreChanged;   // (흑 점수, 백 점수)
    public event Action<CellState> TurnChanged;    // 지금 차례인 색
    public event Action<string> GameOver;          // 결과 문구

    private BoardState _board;
    private IPlayerAgent _blackPlayer;
    private IPlayerAgent _whitePlayer;
    private IPlayerAgent _current;      // 지금 차례 에이전트(Tick 대상)
    private CellState _currentColor;
    private bool _gameOver;

    void Start()
    {
        _board = new BoardState(boardView.boardSize);

        // 미리보기와 착수가 같은 규칙을 쓰도록, 보드의 IsPlayable 을 View에 연결.
        boardView.CanPlace = _board.IsPlayable;

        _blackPlayer = new HumanPlayer(boardView);
        _whitePlayer = new AIPlayer(aiThinkDelay);

        StartGame();
    }

    void Update()
    {
        if (!_gameOver) _current?.Tick(Time.deltaTime);
    }

    public void StartGame()
    {
        _board.Reset();
        boardView.ClearAll();
        _gameOver = false;
        _currentColor = CellState.Black;

        ScoreChanged?.Invoke(_board.BlackScore, _board.WhiteScore); // 0 : 0 으로 초기화
        Debug.Log("게임 시작! 흑(사람)부터.");
        BeginTurn();
    }

    private void BeginTurn()
    {
        if (_gameOver) return;
        TurnChanged?.Invoke(_currentColor);
        _current = (_currentColor == CellState.Black) ? _blackPlayer : _whitePlayer;
        _current.RequestMove(_board, _currentColor, OnMoveChosen);
    }

    private void OnMoveChosen(int col, int row)
    {
        var result = _board.PlaceStone(col, row, _currentColor);
        if (!result.Success)
        {
            Debug.LogWarning($"잘못된 수({result.Error}) — 다시 두세요.");
            BeginTurn();
            return;
        }

        boardView.PlaceStoneVisual(col, row, _currentColor);
        ScoreChanged?.Invoke(_board.BlackScore, _board.WhiteScore);

        if (result.PointsScored > 0)
            Debug.Log($"{_currentColor} +{result.PointsScored}점!  " +
                      $"흑 {_board.BlackScore} : 백 {_board.WhiteScore}");

        if (CheckGameEnd())
        {
            EndGame();
            return;
        }

        _currentColor = (_currentColor == CellState.Black) ? CellState.White : CellState.Black;
        BeginTurn();
    }

    private bool CheckGameEnd()
    {
        if (_board.IsBoardFull) return true;

        switch (endCondition)
        {
            case EndCondition.FixedMoves:
                return _board.StoneCount >= maxMovesPerPlayer * 2;
            case EndCondition.TargetScore:
                return _board.BlackScore >= targetScore || _board.WhiteScore >= targetScore;
            case EndCondition.BoardFull:
                return false;
        }
        return false;
    }

    private void EndGame()
    {
        _gameOver = true;
        _blackPlayer.Cancel();
        _whitePlayer.Cancel();

        string winner = _board.BlackScore > _board.WhiteScore ? "흑 승리"
                      : _board.WhiteScore > _board.BlackScore ? "백 승리"
                      : "무승부";
        string result = $"{winner}\n흑 {_board.BlackScore} : 백 {_board.WhiteScore}";

        Debug.Log($"게임 종료!  {result.Replace("\n", "  ")}");
        GameOver?.Invoke(result);
    }
}