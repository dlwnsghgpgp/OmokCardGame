using UnityEngine;

/// <summary>
/// 10-1 검증용 임시 스크립트. 빈 GameObject에 붙이고 database를 연결한 뒤 플레이하면,
/// 카드 DB가 ID로 조회되는지 Console로 확인한다. 확인 후 삭제해도 된다.
/// </summary>
public class CardDatabaseTester : MonoBehaviour
{
    public CardDatabase database;

    // 확정한 ID들. DB에 다 들어갔는지 확인용.
    private readonly string[] _expected =
    {
        "active_remove_stone",
        "active_extra_stone",
        "active_remove_block",
        "counter_nullify",
        "passive_double_move",
        "passive_guard",
        "field_zombie",
        "field_test",
    };

    void Start()
    {
        if (database == null)
        {
            Debug.LogError("[테스트] Database가 연결되지 않았습니다.");
            return;
        }

        Debug.Log($"[테스트] 총 카드 {database.AllCards.Count}종.");
        foreach (var id in _expected)
        {
            var card = database.Get(id);
            if (card != null)
                Debug.Log($"[테스트] O  {id} → {card.cardName} (Type: {card.Type})");
            else
                Debug.LogWarning($"[테스트] X  {id} → 찾을 수 없음(카드 ID 미입력 또는 DB 미등록)");
        }
    }
}