using UnityEngine;

/// <summary>
/// 손패 카드를 부채꼴로 배치하고, 호버한 카드를 떠오르게 한다.
/// Horizontal Layout Group은 일렬 균등 배치라 부채꼴과 맞지 않으므로 직접 계산한다.
/// (HandContainer의 Layout Group 컴포넌트는 반드시 제거할 것)
///
/// 배치 기준은 "몇 번째 자식인가"가 아니라 CardView.Index(손패 인덱스)다.
/// 그래야 호버한 카드를 맨 앞으로 끌어올려도(SetAsLastSibling) 위치가 흐트러지지 않는다.
/// </summary>
public class HandLayout : MonoBehaviour
{
    [Header("부채꼴")]
    public float spacing = 90f;        // 카드 간 가로 간격(px)
    public float maxWidth = 700f;      // 이 너비를 넘으면 간격을 줄여 겹치게
    public float anglePerCard = 6f;    // 카드 한 장당 기울기(도)
    public float maxTotalAngle = 40f;  // 부채 전체가 이 각도를 넘지 않게
    public float arcDrop = 8f;         // 바깥쪽 카드가 아래로 처지는 정도

    [Header("호버")]
    public float hoverLift = 45f;      // 호버 시 떠오르는 높이(px)
    public float hoverScale = 1.15f;   // 호버 시 확대 배율

    private CardView _hovered;
    private CardView _dragging;   // 드래그 중인 카드는 배치에서 제외

    /// <summary>CardView가 드래그 시작·종료를 알려준다.</summary>
    public void SetDragging(CardView view, bool on)
    {
        _dragging = on ? view : (_dragging == view ? null : _dragging);
        if (!on) Arrange();   // 드래그 끝나면 재정렬
    }

    /// <summary>CardView가 마우스 진입·이탈을 알려준다.</summary>
    public void SetHovered(CardView view, bool on)
    {
        if (on) _hovered = view;
        else if (_hovered == view) _hovered = null;
        Arrange();
    }

    /// <summary>손패를 다시 그린 뒤 GameUI가 호출한다.</summary>
    public void Arrange()
    {
        int n = transform.childCount;
        if (n == 0) return;

        // 카드가 많으면 간격과 각도를 자동으로 좁힌다.
        float step = spacing;
        if ((n - 1) * step > maxWidth && n > 1) step = maxWidth / (n - 1);

        float angleStep = anglePerCard;
        if ((n - 1) * angleStep > maxTotalAngle && n > 1) angleStep = maxTotalAngle / (n - 1);

        float center = (n - 1) / 2f;

        for (int i = 0; i < n; i++)
        {
            var rt = transform.GetChild(i) as RectTransform;
            if (rt == null) continue;

            var view = rt.GetComponent<CardView>();
            if (view != null && view == _dragging) continue;   // 드래그 중인 카드는 손을 따라가게 둔다

            // 자식 순서가 아니라 손패 인덱스를 기준으로 자리를 정한다.
            float slot = (view != null) ? view.Index : i;
            float off = slot - center;

            float angle = -off * angleStep;                       // 바깥으로 갈수록 기울어짐
            Vector2 pos = new Vector2(off * step, -Mathf.Abs(off) * arcDrop);
            bool hovered = (view != null && view == _hovered);

            if (hovered)
            {
                pos.y += hoverLift;   // 위로 떠오르고
                angle = 0f;           // 똑바로 서서 잘 보이게
            }

            rt.anchoredPosition = pos;
            rt.localRotation = Quaternion.Euler(0f, 0f, angle);
            rt.localScale = hovered ? Vector3.one * hoverScale : Vector3.one;
        }

        // 떠오른 카드가 옆 카드에 가리지 않도록 맨 앞으로.
        if (_hovered != null) _hovered.transform.SetAsLastSibling();
    }
}