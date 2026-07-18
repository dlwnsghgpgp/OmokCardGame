using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>이 카드 뷰가 어떤 용도로 쓰이는가. 입력 처리를 이걸로 구분한다.</summary>
public enum CardViewMode
{
    Hand,        // 손패 — 위로 드래그하면 카드 사용
    Display,     // 열람 전용(묘지 목록) — 호버 포커스만
    FieldChoice, // 필드 카드 선택 — 클릭하면 그 카드를 선택
}

/// <summary>
/// 카드 한 장을 그리고, 호버 포커스·부채꼴 떠오름·드래그 발동을 처리한다.
/// 손패(Hand): 위로 끌어올렸다 놓으면 사용. 필드 선택(FieldChoice): 클릭하면 선택.
/// 묘지(Display): 클릭·드래그 없이 호버 포커스만.
/// </summary>
public class CardView : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("표시 요소")]
    public Image iconImage;
    public TMP_Text nameLabel;

    [Header("드래그 발동")]
    public float activateLift = 180f;   // 시작 위치보다 이만큼 위로 올리고 놓으면 발동(px)

    private CardData _card;
    private int _index;
    private GameUI _owner;
    private CardViewMode _mode;
    private HandLayout _layout;

    private RectTransform _rt;
    private Canvas _canvas;
    private Vector2 _dragStartAnchored;
    private bool _dragging;
    private bool _armed;   // 임계 높이를 넘어 "놓으면 발동" 상태

    public int Index => _index;

    void Awake()
    {
        _rt = transform as RectTransform;
        _canvas = GetComponentInParent<Canvas>();
    }

    public void Setup(CardData card, int index, GameUI owner, CardViewMode mode = CardViewMode.Hand)
    {
        _card = card;
        _index = index;
        _owner = owner;
        _mode = mode;
        _layout = (mode == CardViewMode.Hand) ? GetComponentInParent<HandLayout>() : null;

        if (iconImage != null)
        {
            Sprite icon = card.artIcon != null ? card.artIcon : card.artFull;
            iconImage.sprite = icon;
        }
        if (nameLabel != null) nameLabel.text = card.cardName;
    }

    // ── 호버 ──
    public void OnPointerEnter(PointerEventData e)
    {
        if (_dragging) return;
        _layout?.SetHovered(this, true);
        _owner?.ShowCardFocus(_card);
    }

    public void OnPointerExit(PointerEventData e)
    {
        if (_dragging) return;
        _layout?.SetHovered(this, false);
        _owner?.HideCardFocus();
    }

    // ── 클릭 (필드 선택 전용) ──
    public void OnPointerClick(PointerEventData e)
    {
        if (_mode == CardViewMode.FieldChoice)
            _owner?.OnCardViewClicked(_index, _mode);
        // 손패는 클릭이 아니라 드래그로 발동한다. 묘지는 아무 동작 없음.
    }

    // ── 드래그 (손패 전용) ──
    public void OnBeginDrag(PointerEventData e)
    {
        if (_mode != CardViewMode.Hand || _rt == null) return;
        _dragging = true;
        _armed = false;
        _dragStartAnchored = _rt.anchoredPosition;

        _owner?.HideCardFocus();          // 드래그 중엔 큰 포커스 오버레이는 접는다
        _layout?.SetHovered(this, false);
        _layout?.SetDragging(this, true); // 레이아웃이 이 카드를 제자리로 되돌리지 않게
        transform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData e)
    {
        if (!_dragging || _rt == null) return;

        float scale = (_canvas != null) ? _canvas.scaleFactor : 1f;
        _rt.anchoredPosition += e.delta / scale;   // 마우스를 따라 이동

        float lift = _rt.anchoredPosition.y - _dragStartAnchored.y;
        bool armed = lift >= activateLift;
        if (armed != _armed)
        {
            _armed = armed;
            _rt.localScale = armed ? Vector3.one * 1.15f : Vector3.one;   // 발동 대기 피드백
        }
    }

    public void OnEndDrag(PointerEventData e)
    {
        if (!_dragging) return;
        _dragging = false;
        _rt.localScale = Vector3.one;
        _layout?.SetDragging(this, false);

        if (_armed)
        {
            _owner?.OnCardViewClicked(_index, _mode);   // 발동(손패 사용)
            // 발동되면 손패가 곧 다시 그려지며 이 오브젝트는 교체된다.
        }
        else
        {
            _layout?.Arrange();   // 취소 — 부채꼴 제자리로 복귀
        }
    }
}