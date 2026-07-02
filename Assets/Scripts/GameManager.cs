using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>게임을 끝내는 방식. 새 조건을 추가하기 쉽게 한 곳에 모았다.</summary>
public enum EndCondition
{
    FixedMoves,   // 각자 정해진 수만큼 두면 종료 (우리가 정한 기본)
    TargetScore,  // 누군가 목표 점수에 먼저 도달하면 종료
    BoardFull,    // 판이 꽉 차면 종료
}

/// <summary>
/// 게임 진행을 총괄한다. 데이터·화면·플레이어·덱·손패를 들고 턴을 굴린다.
/// UI는 전혀 모른다 — 상태가 바뀌면 이벤트만 쏘고, GameUI가 그걸 듣는다.
/// (8a-1) 매 턴 현재 플레이어가 공유 덱에서 한 장 뽑는다. 카드 "사용"은 8a-2에서.
/// </summary>
public class GameManager : MonoBehaviour
{
    [Header("참조")]
    public BoardView boardView;

    [Header("종료 조건")]
    public EndCondition endCondition = EndCondition.FixedMoves;
    public int maxMovesPerPlayer = 30;
    public int targetScore = 5;

    [Header("AI 설정")]
    public float aiThinkDelay = 0.4f;

    [Header("카드")]
    public List<CardData> deckCards = new List<CardData>(); // 덱 구성(같은 카드를 여러 번 넣어 빈도 조절)
    public int maxHandSize = 7;

    // ── UI가 구독하는 이벤트들 ──
    public event Action<int, int> ScoreChanged;                     // (흑 점수, 백 점수)
    public event Action<CellState> TurnChanged;                      // 지금 차례 색
    public event Action<string> GameOver;                            // 결과 문구
    public event Action<IReadOnlyList<CardData>> HumanHandChanged;   // 사람(흑) 손패 변경

    private BoardState _board;
    private IPlayerAgent _blackPlayer;
    private IPlayerAgent _whitePlayer;
    private IPlayerAgent _current;
    private CellState _currentColor;
    private bool _gameOver;

    private Deck _deck;
    private Hand _blackHand;   // 사람
    private Hand _whiteHand;   // AI

    void Start()
    {
        _board = new BoardState(boardView.boardSize);
        boardView.CanPlace = _board.IsPlayable;   // 미리보기와 착수가 같은 규칙 공유

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

        _deck = new Deck(deckCards);
        _deck.Shuffle();
        _blackHand = new Hand();
        _whiteHand = new Hand();

        _gameOver = false;
        _currentColor = CellState.Black;

        ScoreChanged?.Invoke(_board.BlackScore, _board.WhiteScore);
        HumanHandChanged?.Invoke(_blackHand.Cards);
        Debug.Log($"게임 시작! 흑(사람)부터. 덱 {_deck.Count}장.");
        BeginTurn();
    }

    private void BeginTurn()
    {
        if (_gameOver) return;

        DrawFor(_currentColor);   // 드로우 페이즈: 현재 플레이어가 한 장 뽑는다
        TurnChanged?.Invoke(_currentColor);

        _current = (_currentColor == CellState.Black) ? _blackPlayer : _whitePlayer;
        _current.RequestMove(_board, _currentColor, OnMoveChosen);
    }

    private void DrawFor(CellState color)
    {
        Hand hand = (color == CellState.Black) ? _blackHand : _whiteHand;
        if (hand.Count >= maxHandSize) return;   // 손패가 꽉 차면 스킵

        CardData card = _deck.Draw();
        if (card == null) return;                // 덱 소진

        hand.Add(card);
        Debug.Log($"{color} 드로우: {card.cardName} (덱 {_deck.Count}장 남음)");

        if (color == CellState.Black)
            HumanHandChanged?.Invoke(_blackHand.Cards);
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
            Debug.Log($"{_currentColor} +{result.PointsScored}점!  흑 {_board.BlackScore} : 백 {_board.WhiteScore}");

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