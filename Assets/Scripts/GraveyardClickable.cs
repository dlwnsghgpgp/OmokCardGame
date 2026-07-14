using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 묘지 더미(3D)의 마우스 호버·클릭을 감지한다.
/// 자기 콜라이더만 raycast로 확인하므로 보드 입력과 섞이지 않는다(전용 레이어 사용).
/// 호버 시 강조(맨 위 카드가 살짝 떠오름), 클릭 시 Clicked 이벤트 발생.
/// </summary>
[RequireComponent(typeof(Collider))]
public class GraveyardClickable : MonoBehaviour
{
    [Header("레이어")]
    public LayerMask clickLayer;   // 묘지 전용 레이어만 검사(보드와 분리)

    [Header("호버 강조")]
    public float hoverLift = 0.15f;   // 호버 시 위로 살짝 떠오르는 높이

    /// <summary>묘지가 클릭됐을 때 발생.</summary>
    public event Action Clicked;

    private Camera _cam;
    private Collider _collider;
    private Vector3 _basePos;
    private bool _hovering;

    void Awake()
    {
        _cam = Camera.main;
        _collider = GetComponent<Collider>();
        _basePos = transform.position;
    }

    void Update()
    {
        if (Mouse.current == null) return;
        if (_cam == null) _cam = Camera.main;

        bool over = IsPointerOver();

        if (over != _hovering)
        {
            _hovering = over;
            // 호버 강조: 통째로 살짝 들어올린다(프리팹 교체 없이 시각 피드백).
            transform.position = _basePos + (over ? Vector3.up * hoverLift : Vector3.zero);
        }

        if (over && Mouse.current.leftButton.wasPressedThisFrame)
            Clicked?.Invoke();
    }

    private bool IsPointerOver()
    {
        Ray ray = _cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        // 묘지 레이어만 검사. clickLayer가 비어 있으면 전체 검사로 폴백.
        int mask = clickLayer.value == 0 ? ~0 : clickLayer.value;
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, mask))
            return hit.collider == _collider;
        return false;
    }
}