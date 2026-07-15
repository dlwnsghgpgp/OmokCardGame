using UnityEngine;

/// <summary>
/// 인프라 검증용 테스트 필드 카드. 실제 효과 없이 훅 호출만 로그로 남긴다.
/// 9-3b에서 좀비 카드를 만들면 필드 덱에서 빼도 된다.
/// </summary>
[CreateAssetMenu(menuName = "Omok/Cards/Field-Test (테스트 필드)", fileName = "TestFieldCard")]
public class TestFieldCardData : FieldCardData
{
    private int _turns;

    public override void OnActivated(FieldContext ctx)
    {
        _turns = 0;
        Debug.Log($"[필드] {cardName} 발동!");
    }

    public override void OnTurnBegin(FieldContext ctx)
    {
        _turns++;
        Debug.Log($"[필드] {cardName} 턴 훅 {_turns}회째");
    }
}