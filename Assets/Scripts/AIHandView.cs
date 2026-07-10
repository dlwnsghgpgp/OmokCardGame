using UnityEngine;

/// <summary>
/// AI(상대) 손패를 보드 반대편 바닥에 카드 뒷면(3D)으로 나열한다.
/// 순수 시각 담당 — 장수만 받아서 그린다. 카드 내용은 알지 못한다.
///
/// Layout Group은 UI 전용이라 3D에는 못 쓰므로, 간격 배치를 코드로 한다.
/// 카드가 늘 중앙 정렬되도록 총 너비의 절반만큼 왼쪽에서 시작한다.
/// </summary>
public class AIHandView : MonoBehaviour
{
    [Header("카드")]
    public GameObject cardBackPrefab;   // 바닥에 눕힌 Quad(뒷면). 콜라이더 없음 권장.

    [Header("배치")]
    public float spacing = 1.2f;        // 카드 중심 간 거리
    public float maxWidth = 12f;        // 이 너비를 넘으면 카드가 겹치도록 간격 자동 축소

    private GameObject[] _cards = new GameObject[0];

    /// <summary>AI 손패 장수가 바뀌면 GameManager가 호출한다.</summary>
    public void SetHandCount(int count)
    {
        if (cardBackPrefab == null) return;
        if (count < 0) count = 0;

        // 개수가 바뀌었으면 다시 만든다(장수만 다루므로 단순 재생성이 안전하고 충분).
        ClearCards();
        _cards = new GameObject[count];
        if (count == 0) return;

        // 카드가 많아 총 너비를 넘으면 간격을 줄여 겹치게 한다.
        float step = spacing;
        float totalWidth = (count - 1) * step;
        if (totalWidth > maxWidth && count > 1)
        {
            step = maxWidth / (count - 1);
            totalWidth = maxWidth;
        }

        float startX = -totalWidth / 2f;   // 중앙 정렬

        for (int i = 0; i < count; i++)
        {
            Vector3 localPos = new Vector3(startX + step * i, 0f, 0f);
            GameObject card = Instantiate(cardBackPrefab, transform);
            card.transform.localPosition = localPos;
            // 회전은 프리팹에 저장된 값을 그대로 쓴다(예: 바닥에 눕히려 X=90).
            // 여기서 Quaternion.identity로 덮어쓰면 프리팹 회전이 사라진다.
            _cards[i] = card;
        }
    }

    private void ClearCards()
    {
        for (int i = 0; i < _cards.Length; i++)
            if (_cards[i] != null) Destroy(_cards[i]);
        _cards = new GameObject[0];
    }
}