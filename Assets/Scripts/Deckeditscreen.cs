using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 덱 편집 화면.
///  - 왼쪽(카드 풀): 소지한 카드 전체. 클릭하면 현재 덱에 추가.
///  - 오른쪽(현재 덱): 지금 만드는 덱. 클릭하면 덱에서 제거.
///  - 규칙(DeckRules)에 어긋나면 상단에 잠깐 토스트로 알린다.
///  - 저장하면 GameSession의 저장 데이터에 반영되어 JSON에 기록된다.
///
/// 지금은 덱 하나만 편집(여러 덱 관리는 이후 단계).
/// </summary>
public class DeckEditScreen : MonoBehaviour
{
    [Header("카드 프리팹 · 컨테이너")]
    public GameObject cardButtonPrefab;   // CardView가 붙은 프리팹(게임 씬과 동일)
    public Transform poolContainer;       // 소지 카드 풀(Grid/Horizontal Layout Group)
    public Transform deckContainer;       // 현재 덱(Grid/Horizontal Layout Group)

    [Header("호버 포커스(선택)")]
    public GameUI focusUI;   // 있으면 카드 호버 시 큰 정보 표시. 없으면 생략.

    [Header("상태 표시")]
    public TMP_Text deckCountText;        // "덱 5/10장" 상시 표시
    public TMP_Text toastText;            // 규칙 위반 등 잠깐 뜨는 메시지
    public float toastSeconds = 1.5f;

    [Header("버튼")]
    public Button saveButton;
    public Button clearButton;   // 덱 비우기
    public Button backButton;    // 타이틀로

    private readonly List<CardData> _deck = new List<CardData>();
    private CardDatabase _db;
    private Coroutine _toastRoutine;

    void Start()
    {
        _db = GameSession.Instance.cardDatabase;

        if (saveButton != null)  saveButton.onClick.AddListener(OnSave);
        if (clearButton != null) clearButton.onClick.AddListener(OnClear);
        if (backButton != null)  backButton.onClick.AddListener(() => GameSession.Instance.LoadTitle());

        if (toastText != null) toastText.gameObject.SetActive(false);

        LoadCurrentDeck();
        BuildPool();
        RefreshDeck();
    }

    // 저장된 선택 덱을 편집 대상으로 불러온다.
    private void LoadCurrentDeck()
    {
        _deck.Clear();
        var saved = GameSession.Instance.SaveData?.GetSelectedDeck();
        if (saved == null || _db == null) return;

        foreach (var id in saved.cardIds)
        {
            var card = _db.Get(id);
            if (card != null) _deck.Add(card);
        }
    }

    // 소지한 카드(필드 카드 제외)를 풀에 그린다. 풀은 한 번만 그리면 된다.
    private void BuildPool()
    {
        if (poolContainer == null || cardButtonPrefab == null || _db == null) return;

        for (int i = poolContainer.childCount - 1; i >= 0; i--)
            Destroy(poolContainer.GetChild(i).gameObject);

        foreach (var card in _db.AllCards)
        {
            if (card == null) continue;
            if (card.Type == CardType.Field) continue;          // 필드 카드는 덱에 못 넣음
            if (!GameSession.Instance.OwnsCard(card.id)) continue;   // 소지한 카드만

            var go = Instantiate(cardButtonPrefab);
            go.transform.SetParent(poolContainer, false);
            var view = go.GetComponent<CardView>();
            if (view != null)
                view.SetupSimple(card, focusUI, CardViewMode.DeckPool, OnPoolCardClicked);
        }
    }

    // 현재 덱을 다시 그린다(추가/제거 때마다 호출).
    private void RefreshDeck()
    {
        if (deckContainer != null && cardButtonPrefab != null)
        {
            for (int i = deckContainer.childCount - 1; i >= 0; i--)
            {
                Transform child = deckContainer.GetChild(i);
                child.SetParent(null, false);
                Destroy(child.gameObject);
            }

            foreach (var card in _deck)
            {
                var go = Instantiate(cardButtonPrefab);
                go.transform.SetParent(deckContainer, false);
                var view = go.GetComponent<CardView>();
                if (view != null)
                    view.SetupSimple(card, focusUI, CardViewMode.DeckMember, OnDeckCardClicked);
            }
        }

        UpdateStatus();
    }

    private void UpdateStatus()
    {
        if (deckCountText != null)
            deckCountText.text = $"덱 {_deck.Count}/{DeckRules.MaxCards}장";

        // 저장 가능 여부로 저장 버튼 활성화
        if (saveButton != null)
            saveButton.interactable = DeckRules.Validate(_deck).Valid;
    }

    // 풀 카드 클릭 → 덱에 추가(규칙 확인)
    private void OnPoolCardClicked(CardData card)
    {
        var can = DeckRules.CanAdd(_deck, card);
        if (!can.Valid) { Toast(can.Reason); return; }

        _deck.Add(card);
        RefreshDeck();
    }

    // 덱 카드 클릭 → 덱에서 제거
    private void OnDeckCardClicked(CardData card)
    {
        // 같은 카드가 여러 장이면 한 장만 제거.
        int idx = _deck.FindIndex(c => c != null && c.id == card.id);
        if (idx >= 0)
        {
            _deck.RemoveAt(idx);
            RefreshDeck();
        }
    }

    private void OnClear()
    {
        _deck.Clear();
        RefreshDeck();
    }

    private void OnSave()
    {
        var result = DeckRules.Validate(_deck);
        if (!result.Valid) { Toast(result.Reason); return; }

        // 편집 결과를 저장 데이터의 선택 덱에 반영.
        var save = GameSession.Instance.SaveData;
        var target = save?.GetSelectedDeck();
        if (target == null)
        {
            Toast("저장할 덱을 찾지 못했습니다.");
            return;
        }

        target.cardIds.Clear();
        foreach (var card in _deck) target.cardIds.Add(card.id);

        GameSession.Instance.SaveToDisk();               // JSON 저장
        GameSession.Instance.ApplySelectedDeckFromSave();// 현재 세션에도 즉시 반영
        Toast("저장되었습니다.");
    }

    // ── 토스트(상단에 잠깐 뜨는 메시지) ──
    private void Toast(string message)
    {
        if (toastText == null) { Debug.Log($"[덱편집] {message}"); return; }
        if (_toastRoutine != null) StopCoroutine(_toastRoutine);
        _toastRoutine = StartCoroutine(ToastRoutine(message));
    }

    private IEnumerator ToastRoutine(string message)
    {
        toastText.text = message;
        toastText.gameObject.SetActive(true);
        yield return new WaitForSeconds(toastSeconds);
        toastText.gameObject.SetActive(false);
    }
}