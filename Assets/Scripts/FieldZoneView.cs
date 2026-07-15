using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 필드에 깔린 카드를 board 옆에 앞면으로 3D 표시하고, 마우스 호버를 감지한다.
/// 호버하면 HoverChanged(true) → GameManager가 카드 포커스(일러스트+이름+효과)를 띄운다.
/// 자기 콜라이더만 raycast로 확인하므로 보드 입력과 섞이지 않는다(전용 레이어 사용).
/// </summary>
[RequireComponent(typeof(Collider))]
public class FieldZoneView : MonoBehaviour
{
    [Header("카드")]
    public GameObject cardFrontPrefab;   // 앞면 카드(묘지의 CardFront 재사용 가능)

    [Header("레이어")]
    public LayerMask hoverLayer;   // 필드 존 전용 레이어만 검사

    /// <summary>마우스가 필드 카드 위에 올라옴(true) / 벗어남(false).</summary>
    public event Action<bool> HoverChanged;

    private Camera _cam;
    private Collider _collider;
    private GameObject _cardObject;
    private bool _hovering;

    void Awake()
    {
        _cam = Camera.main;
        _collider = GetComponent<Collider>();
        _collider.enabled = false;   // 카드가 깔리기 전엔 호버 대상이 아님
    }

    /// <summary>필드에 카드가 깔릴 때 GameManager가 호출. null이면 비운다.</summary>
    public void SetCard(CardData card)
    {
        if (_cardObject != null) Destroy(_cardObject);
        _cardObject = null;

        if (card == null || cardFrontPrefab == null)
        {
            _collider.enabled = false;
            return;
        }

        // 회전은 프리팹 값을 그대로 사용(바닥에 눕히려면 프리팹에서 X=90).
        _cardObject = Instantiate(cardFrontPrefab, transform);
        _cardObject.transform.localPosition = Vector3.zero;
        _collider.enabled = true;
    }

    void Update()
    {
        if (!_collider.enabled || Mouse.current == null) return;
        if (_cam == null) _cam = Camera.main;

        bool over = IsPointerOver();
        if (over != _hovering)
        {
            _hovering = over;
            HoverChanged?.Invoke(over);
        }
    }

    private bool IsPointerOver()
    {
        Ray ray = _cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        int mask = hoverLayer.value == 0 ? ~0 : hoverLayer.value;
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, mask))
            return hit.collider == _collider;
        return false;
    }
}