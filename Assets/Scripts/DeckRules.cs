using System.Collections.Generic;

/// <summary>
/// 덱 구성 규칙의 단일 출처. 덱 편집 화면과 게임 시작 검증이 모두 이걸 쓴다.
///  - 덱 크기: 5~10장
///  - 같은 카드: 최대 3장
///  - 패시브: 덱 전체에 1장만(따라서 같은 패시브 중복도 자동으로 금지)
///  - 필드 카드: 플레이어 덱에 넣을 수 없음(테마가 관리)
///
/// 규칙을 바꾸려면 이 파일만 고치면 된다.
/// </summary>
public static class DeckRules
{
    public const int MinCards = 5;
    public const int MaxCards = 10;
    public const int MaxCopiesPerCard = 3;
    public const int MaxPassiveCards = 1;

    /// <summary>검증 결과. 통과하지 못하면 이유를 담는다.</summary>
    public struct Result
    {
        public bool Valid;
        public string Reason;

        public static Result Ok() => new Result { Valid = true, Reason = null };
        public static Result Fail(string reason) => new Result { Valid = false, Reason = reason };
    }

    /// <summary>완성된 덱이 규칙을 만족하는지 검사한다(게임 시작 전 최종 검증).</summary>
    public static Result Validate(IReadOnlyList<CardData> deck)
    {
        if (deck == null) return Result.Fail("덱이 없습니다.");
        if (deck.Count < MinCards) return Result.Fail($"덱은 최소 {MinCards}장이어야 합니다. (현재 {deck.Count}장)");
        if (deck.Count > MaxCards) return Result.Fail($"덱은 최대 {MaxCards}장까지입니다. (현재 {deck.Count}장)");

        var counts = new Dictionary<string, int>();
        int passiveCount = 0;

        foreach (var card in deck)
        {
            if (card == null) return Result.Fail("덱에 빈 카드가 있습니다.");
            if (card.Type == CardType.Field)
                return Result.Fail($"필드 카드({card.cardName})는 덱에 넣을 수 없습니다.");

            if (card.Type == CardType.Passive) passiveCount++;

            string id = card.id;
            counts.TryGetValue(id, out int n);
            counts[id] = n + 1;
            if (counts[id] > MaxCopiesPerCard)
                return Result.Fail($"같은 카드는 최대 {MaxCopiesPerCard}장까지입니다. ({card.cardName})");
        }

        if (passiveCount > MaxPassiveCards)
            return Result.Fail($"패시브 카드는 덱에 {MaxPassiveCards}장만 넣을 수 있습니다. (현재 {passiveCount}장)");

        return Result.Ok();
    }

    /// <summary>
    /// 편집 중 "이 카드를 한 장 더 넣어도 되는가"를 판단한다(덱 편집 UI용).
    /// 최소 장수는 편집 중엔 검사하지 않는다(만들어가는 중이므로).
    /// </summary>
    public static Result CanAdd(IReadOnlyList<CardData> current, CardData card)
    {
        if (card == null) return Result.Fail("카드가 없습니다.");
        if (card.Type == CardType.Field) return Result.Fail("필드 카드는 덱에 넣을 수 없습니다.");
        if (current == null) return Result.Ok();

        if (current.Count >= MaxCards)
            return Result.Fail($"덱이 가득 찼습니다. (최대 {MaxCards}장)");

        int copies = 0, passives = 0;
        foreach (var c in current)
        {
            if (c == null) continue;
            if (c.id == card.id) copies++;
            if (c.Type == CardType.Passive) passives++;
        }

        if (copies >= MaxCopiesPerCard)
            return Result.Fail($"이 카드는 이미 {MaxCopiesPerCard}장입니다.");

        if (card.Type == CardType.Passive && passives >= MaxPassiveCards)
            return Result.Fail($"패시브 카드는 {MaxPassiveCards}장만 넣을 수 있습니다.");

        return Result.Ok();
    }
}