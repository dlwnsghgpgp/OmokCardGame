using UnityEngine;

/// <summary>
/// 10-4 검증용 임시 스크립트. 빈 GameObject에 붙이고 플레이하면
/// 저장 데이터 로드·소지 목록·기본 덱·규칙 검증 결과를 Console로 확인한다.
/// 확인 후 삭제해도 된다.
///
/// [초기화 테스트] resetOnStart를 켜면 저장 파일을 지우고 새로 만든다.
/// </summary>
public class SaveSystemTester : MonoBehaviour
{
    [Tooltip("켜면 시작 시 저장 파일을 삭제하고 새로 생성한다(초기화 테스트).")]
    public bool resetOnStart = false;

    void Start()
    {
        if (resetOnStart)
        {
            SaveSystem.DeleteSave();
            Debug.Log("[테스트] 저장 파일을 삭제했습니다. 에디터를 다시 실행하면 새로 생성됩니다.");
        }

        var session = GameSession.Instance;
        var save = session.SaveData;

        if (save == null)
        {
            Debug.LogError("[테스트] SaveData가 null입니다.");
            return;
        }

        Debug.Log($"[테스트] 저장 경로: {Application.persistentDataPath}");
        Debug.Log($"[테스트] 소지 카드 {save.ownedCardIds.Count}종: {string.Join(", ", save.ownedCardIds)}");
        Debug.Log($"[테스트] 덱 {save.decks.Count}개, 선택 인덱스 {save.selectedDeckIndex}");

        var deck = save.GetSelectedDeck();
        if (deck == null)
        {
            Debug.LogWarning("[테스트] 선택된 덱이 없습니다.");
            return;
        }

        Debug.Log($"[테스트] 덱 '{deck.deckName}' ({deck.cardIds.Count}장): {string.Join(", ", deck.cardIds)}");

        // 규칙 검증
        var cards = session.BuildDeckCards();
        var result = DeckRules.Validate(cards);
        if (result.Valid)
            Debug.Log($"[테스트] 덱 규칙 통과 ({cards.Count}장)");
        else
            Debug.LogWarning($"[테스트] 덱 규칙 위반: {result.Reason}");
    }
}