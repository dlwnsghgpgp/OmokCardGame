using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 하나의 테마. 성격이 맞는 필드 카드들을 묶고, 그 등장 규칙을 함께 정한다.
/// 예: '세계 멸망' — 승리 조건을 바꾸는 카드들, 게임당 딱 1회만 등장(교체 없음).
///     다른 테마 — 가벼운 효과들, N턴마다 반복 등장(이전 카드는 교체되어 사라짐).
///
/// 에셋으로 만들어(Create → Omok/Theme) 필드 덱과 규칙을 설정한다.
/// </summary>
[CreateAssetMenu(menuName = "Omok/Theme", fileName = "Theme")]
public class ThemeData : ScriptableObject
{
    [Header("식별 · 표시")]
    public string id = "";              // 예: apocalypse (GameSession이 이 ID로 테마를 고른다)
    public string themeName = "테마";
    [TextArea] public string description = "";
    public Sprite icon;                 // 테마 선택 화면용(선택)

    [Header("필드 덱")]
    [Tooltip("이 테마에서 등장할 수 있는 필드 카드들.")]
    public List<CardData> fieldDeck = new List<CardData>();

    [Tooltip("한 번에 제시할 후보 장수(점수가 낮은 플레이어가 이 중 하나를 고른다).")]
    public int choiceCount = 3;

    [Header("등장 규칙")]
    [Tooltip("첫 필드 카드가 등장하는 턴(양쪽 합산 턴 수).")]
    public int firstTurn = 10;

    [Tooltip("체크하면 주기적으로 반복 등장한다(이전 카드는 교체되어 사라짐). 끄면 게임당 1회만.")]
    public bool repeating = false;

    [Tooltip("반복 주기(턴). repeating이 켜져 있을 때만 사용.")]
    public int repeatInterval = 5;

    /// <summary>
    /// 이 턴에 필드 카드가 등장해야 하는가.
    /// 반복이 아니면 firstTurn에 딱 한 번, 반복이면 firstTurn부터 repeatInterval마다.
    /// </summary>
    public bool ShouldTrigger(int turn)
    {
        if (turn < firstTurn) return false;
        if (!repeating) return turn == firstTurn;
        if (repeatInterval <= 0) return turn == firstTurn;
        return (turn - firstTurn) % repeatInterval == 0;
    }
}