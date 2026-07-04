using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 손패에서 카드 한 장을 그리고, 마우스 진입·이탈·클릭을 감지해 GameUI에 알린다.
/// 이미지(artIcon)가 있으면 그걸, 없으면 빈 카드(단색) + 이름 텍스트로 표시한다.
/// 카드 한 장의 "표현"을 여기로 분리해, 카드가 늘어도 GameUI는 그대로 둔다.
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

    /// <summary>GameUI가 손패를 그릴 때 카드 데이터·인덱스·주인을 넣어준다.</summary>
    public void Setup(CardData card, int index, GameUI owner)
    {
        _card = card;
        _index = index;
        _owner = owner;

        // 아이콘 우선순위: artIcon → artFull → (없으면 스프라이트 없이 단색으로 남김)
        if (iconImage != null)
        {
            Sprite icon = card.artIcon != null ? card.artIcon : card.artFull;
            iconImage.sprite = icon;   // null이면 Image가 단색 사각형으로 렌더(빈 카드)
        }
        if (nameLabel != null) nameLabel.text = card.cardName;
    }

    public void OnPointerEnter(PointerEventData e) { _owner?.ShowCardFocus(_card); }
    public void OnPointerExit(PointerEventData e)  { _owner?.HideCardFocus(); }
    public void OnPointerClick(PointerEventData e) { _owner?.OnCardViewClicked(_index); }
}