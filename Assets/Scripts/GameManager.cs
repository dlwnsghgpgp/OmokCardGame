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
/// 사람·AI·네트워크 누구든 IPlayerAgent 로 다루므로 이 클래스는 바꿀 필요가 없다.
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

    private BoardState _board;
    private IPlayerAgent _blackPlayer;
    private IPlayerAgent _whitePlayer;
    private IPlayerAgent _current;      // 지금 차례인 에이전트(Tick 대상)
    private CellState _currentColor;
    private bool _gameOver;

    void Start()
    {
        _board = new BoardState(boardView.boardSize); // 보드 크기는 BoardView 기준

        // 흑 = 사람, 백 = AI. (온라인 단계에서 한쪽을 NetworkPlayer로 바꾸면 됨)
        _blackPlayer = new HumanPlayer(boardView);
        _whitePlayer = new AIPlayer(aiThinkDelay);

        StartGame();
    }

    void Update()
    {
        // 현재 차례 에이전트에게만 시간을 흘려보낸다(AI 생각 딜레이 처리).
        if (!_gameOver) _current?.Tick(Time.deltaTime);
    }

    public void StartGame()
    {
        _board.Reset();
        boardView.ClearAll();
        _gameOver = false;
        _currentColor = CellState.Black;
        Debug.Log("게임 시작! 흑(사람)부터.");
        BeginTurn();
    }

    private void BeginTurn()
    {
        if (_gameOver) return;
        _current = (_currentColor == CellState.Black) ? _blackPlayer : _whitePlayer;
        _current.RequestMove(_board, _currentColor, OnMoveChosen);
    }

    // 현재 플레이어가 수를 확정하면 호출된다(사람=클릭, AI=계산 결과).
    private void OnMoveChosen(int col, int row)
    {
        var result = _board.PlaceStone(col, row, _currentColor);
        if (!result.Success)
        {
            Debug.LogWarning($"잘못된 수({result.Error}) — 다시 두세요.");
            BeginTurn();   // 같은 플레이어에게 다시 요청
            return;
        }

        boardView.PlaceStoneVisual(col, row, _currentColor);

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

    // 종료 판정을 한 곳에 모아둠 — 방식을 바꾸려면 여기(또는 Inspector)만 손대면 된다.
    private bool CheckGameEnd()
    {
        if (_board.IsBoardFull) return true;   // 어떤 조건이든 판이 꽉 차면 종료

        switch (endCondition)
        {
            case EndCondition.FixedMoves:
                return _board.StoneCount >= maxMovesPerPlayer * 2;
            case EndCondition.TargetScore:
                return _board.BlackScore >= targetScore || _board.WhiteScore >= targetScore;
            case EndCondition.BoardFull:
                return false; // 위 IsBoardFull 에서 처리
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
        Debug.Log($"게임 종료!  흑 {_board.BlackScore} : 백 {_board.WhiteScore}  →  {winner}");
    }
}