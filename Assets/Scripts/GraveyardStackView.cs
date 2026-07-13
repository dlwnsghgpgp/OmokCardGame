using UnityEngine;

/// <summary>
/// 묘지를 board 옆 한 자리에 "살짝 어긋난 겹침 더미"로 3D 표시한다.
/// 맨 위(가장 최근) 카드만 앞면 프리팹, 그 아래는 뒷면 프리팹으로 쌓는다.
/// 순수 시각 담당 — 장수만 받아 그린다. 클릭/목록은 GraveyardView(9-2b)에서.
///
/// 회전은 각 프리팹에 저장된 값을 그대로 쓴다(바닥에 눕히려면 프리팹에서 X=90).
/// </summary>
public class GraveyardStackView : MonoBehaviour
{
    [Header("카드 프리팹")]
    public GameObject cardFrontPrefab;   // 맨 위 한 장(앞면·일러스트). 없으면 뒷면으로 대체.
    public GameObject cardBackPrefab;     // 아래에 겹치는 카드(뒷면). 8d-1의 CardBack 재활용.

    [Header("겹침 간격")]
    public float stepXZ = 0.03f;   // 한 장마다 XZ로 살짝 밀어 겹쳐 보이게
    public float stepY = 0.01f;    // 한 장마다 살짝 높여 Z-파이팅 방지
    public int maxVisible = 12;    // 실제로 그릴 최대 장수(너무 많으면 성능·시야 낭비)

    private GameObject[] _cards = new GameObject[0];

    /// <summary>묘지 장수가 바뀌면 GameManager가 호출한다.</summary>
    public void SetCount(int count)
    {
        ClearCards();
        if (count <= 0) return;

        int visible = Mathf.Min(count, maxVisible);   // 아래쪽 오래된 카드는 어차피 가려지므로 생략
        _cards = new GameObject[visible];

        for (int i = 0; i < visible; i++)
        {
            bool isTop = (i == visible - 1);   // 마지막에 그리는 것이 맨 위(앞면)
            GameObject prefab = isTop && cardFrontPrefab != null ? cardFrontPrefab : cardBackPrefab;
            if (prefab == null) continue;

            GameObject card = Instantiate(prefab, transform);
            // 아래에서 위로 갈수록 조금씩 어긋나게 쌓는다.
            card.transform.localPosition = new Vector3(stepXZ * i, stepY * i, stepXZ * i);
            // 회전은 프리팹 값을 유지(덮어쓰지 않음).
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