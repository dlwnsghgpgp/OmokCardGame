using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 좀비 아포칼립스 (필드 카드).
///  - 발동 시: 보드 위 돌 중 무작위 하나가 숙주가 되어 좀비(초록)로 변한다.
///  - 매 턴: 그 시점의 좀비들이 각자 자기 3×3 안의 "멀쩡한 돌" 하나를 감염시킨다.
///           (이번 턴에 새로 감염된 돌은 다음 턴부터 감염을 퍼뜨린다 — 스냅샷 방식)
///           숙주만은 예외로, 3×3에 대상이 없으면 보드 전체에서 가장 가까운 돌을 감염시킨다.
///  - 승패: 한쪽의 돌이 전부 좀비가 되면 그 사람 패배, 돌이 남은 쪽 승리(기존 종료 조건 무시).
///
/// 상태(숙주 위치)는 ScriptableObject에 남으므로 OnActivated에서 반드시 초기화한다.
/// 감염 정보 자체는 BoardState가 들고 있어 재시작 시 자동으로 지워진다.
/// </summary>
[CreateAssetMenu(menuName = "Omok/Cards/Field-Zombie (좀비 아포칼립스)", fileName = "ZombieApocalypseCard")]
public class ZombieApocalypseFieldCardData : FieldCardData
{
    /// <summary>이 카드가 깔리면 기존 종료 조건(턴 수·목표 점수)을 무시하고 전멸로만 승패를 가린다.</summary>
    public override bool SuppressNormalEnd => true;

    private int _hostCol = -1;
    private int _hostRow = -1;

    public override void OnActivated(FieldContext ctx)
    {
        _hostCol = -1;
        _hostRow = -1;   // 에셋에 남은 이전 게임 상태를 반드시 리셋

        var stones = AllStones(ctx);
        if (stones.Count == 0)
        {
            Debug.Log("[좀비] 보드에 돌이 없어 숙주를 정하지 못했습니다. 다음 턴에 재시도합니다.");
            return;
        }

        var host = stones[Random.Range(0, stones.Count)];
        _hostCol = host.x;
        _hostRow = host.y;
        ctx.InfectStone(_hostCol, _hostRow);
        Debug.Log($"[좀비] 숙주 발생 → ({_hostCol},{_hostRow})");
    }

    public override void OnTurnBegin(FieldContext ctx)
    {
        // 발동 시 돌이 없었다면 이제라도 숙주를 정한다.
        if (_hostCol < 0)
        {
            OnActivated(ctx);
            return;
        }

        // 이번 턴 시작 시점의 좀비 목록을 고정한다(새로 감염된 돌이 같은 턴에 또 퍼뜨리지 않게).
        List<Vector2Int> zombies = AllInfected(ctx);
        int infectedThisTurn = 0;

        foreach (var z in zombies)
        {
            // 1) 자기 3×3 안의 멀쩡한 돌 하나를 감염
            if (TryFindHealthyIn3x3(ctx, z.x, z.y, out Vector2Int target))
            {
                if (ctx.InfectStone(target.x, target.y)) infectedThisTurn++;
                continue;
            }

            // 2) 숙주만 예외: 주변에 대상이 없으면 가장 가까운 멀쩡한 돌까지 손을 뻗는다.
            bool isHost = (z.x == _hostCol && z.y == _hostRow);
            if (isHost && TryFindNearestHealthy(ctx, z.x, z.y, out Vector2Int near))
            {
                if (ctx.InfectStone(near.x, near.y)) infectedThisTurn++;
                Debug.Log($"[좀비] 숙주가 멀리 있는 ({near.x},{near.y})를 감염시켰습니다.");
            }
        }

        if (infectedThisTurn > 0)
            Debug.Log($"[좀비] 이번 턴 {infectedThisTurn}개 감염. " +
                      $"멀쩡한 돌 — 흑 {ctx.Board.CountHealthy(CellState.Black)} / " +
                      $"백 {ctx.Board.CountHealthy(CellState.White)}");
    }

    public override bool CheckWin(FieldContext ctx, out string result)
    {
        result = null;
        if (_hostCol < 0) return false;   // 아직 시작 안 됨

        int black = ctx.Board.CountHealthy(CellState.Black);
        int white = ctx.Board.CountHealthy(CellState.White);

        if (black == 0 && white == 0)
        {
            result = "무승부 — 모든 돌이 좀비가 되었습니다";
            return true;
        }
        if (black == 0)
        {
            result = "백 승리 — 흑의 돌이 모두 좀비가 되었습니다";
            return true;
        }
        if (white == 0)
        {
            result = "흑 승리 — 백의 돌이 모두 좀비가 되었습니다";
            return true;
        }
        return false;
    }

    // ── 보조 함수 ──

    private static List<Vector2Int> AllStones(FieldContext ctx)
    {
        var list = new List<Vector2Int>();
        int size = ctx.Board.Size;
        for (int c = 0; c < size; c++)
        for (int r = 0; r < size; r++)
            if (ctx.Board.GetCell(c, r) != CellState.Empty)
                list.Add(new Vector2Int(c, r));
        return list;
    }

    private static List<Vector2Int> AllInfected(FieldContext ctx)
    {
        var list = new List<Vector2Int>();
        int size = ctx.Board.Size;
        for (int c = 0; c < size; c++)
        for (int r = 0; r < size; r++)
            if (ctx.Board.IsInfected(c, r))
                list.Add(new Vector2Int(c, r));
        return list;
    }

    private static bool IsHealthyStone(FieldContext ctx, int c, int r)
        => ctx.Board.InBounds(c, r)
        && ctx.Board.GetCell(c, r) != CellState.Empty
        && !ctx.Board.IsInfected(c, r);

    // 3×3(자기 칸 제외) 안의 멀쩡한 돌 중 하나를 무작위로 고른다.
    private static bool TryFindHealthyIn3x3(FieldContext ctx, int col, int row, out Vector2Int found)
    {
        var candidates = new List<Vector2Int>();
        for (int dc = -1; dc <= 1; dc++)
        for (int dr = -1; dr <= 1; dr++)
        {
            if (dc == 0 && dr == 0) continue;
            int c = col + dc, r = row + dr;
            if (IsHealthyStone(ctx, c, r)) candidates.Add(new Vector2Int(c, r));
        }

        if (candidates.Count == 0) { found = default; return false; }
        found = candidates[Random.Range(0, candidates.Count)];
        return true;
    }

    // 보드 전체에서 가장 가까운 멀쩡한 돌(숙주 전용 예외 규칙).
    private static bool TryFindNearestHealthy(FieldContext ctx, int col, int row, out Vector2Int found)
    {
        found = default;
        int size = ctx.Board.Size;
        int bestDist = int.MaxValue;

        for (int c = 0; c < size; c++)
        for (int r = 0; r < size; r++)
        {
            if (!IsHealthyStone(ctx, c, r)) continue;
            int dc = c - col, dr = r - row;
            int dist = dc * dc + dr * dr;   // 제곱 거리로 비교(제곱근 불필요)
            if (dist < bestDist)
            {
                bestDist = dist;
                found = new Vector2Int(c, r);
            }
        }
        return bestDist != int.MaxValue;
    }
}