using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>이 카드 뷰가 어떤 용도로 쓰이는가. 클릭 처리를 이걸로 구분한다.</summary>
public enum CardViewMode
{
    Hand,        // 손패 — 클릭하면 카드 사용
    Display,     // 열람 전용(묘지 목록) — 클릭 무시, 호버 포커스만
    FieldChoice, // 필드 카드 선택 — 클릭하면 그 카드를 선택
}

/// <summary>
/// 카드 한 장을 그리고, 마우스 진입·이탈·클릭을 감지해 GameUI에 알린다.
/// 이미지(artIcon)가 있으면 그걸, 없으면 빈 카드(단색) + 이름 텍스트로 표시한다.
/// 손패·묘지 목록·필드 선택에서 모두 재사용되며, 용도는 CardViewMode로 구분한다.
/// </summary>
public class CardView : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("표시 요소")]
    public Image iconImage;      // 카드 그림(비어 있으면 단색 = 빈 카드)
    public TMP_Text nameLabel;   // 카드 이름(이미지가 없어도 항상 표시)

    private CardData _card;
    private int _index;
    private GameUI _owner;
    private CardViewMode _mode;

    /// <summary>GameUI가 카드 데이터·인덱스·주인·용도를 넣어준다.</summary>
    public void Setup(CardData card, int index, GameUI owner, CardViewMode mode = CardViewMode.Hand)
    {
        _card = card;
        _index = index;
        _owner = owner;
        _mode = mode;

        if (iconImage != null)
        {
            Sprite icon = card.artIcon != null ? card.artIcon : card.artFull;
            iconImage.sprite = icon;   // null이면 Image가 단색 사각형으로 렌더(빈 카드)
        }
        if (nameLabel != null) nameLabel.text = card.cardName;
    }

    public void OnPointerEnter(PointerEventData e) { _owner?.ShowCardFocus(_card); }
    public void OnPointerExit(PointerEventData e)  { _owner?.HideCardFocus(); }
    public void OnPointerClick(PointerEventData e) { _owner?.OnCardViewClicked(_index, _mode); }
}