using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 보드의 겉모습과 입력 담당. 평소엔 클릭을 CellClicked로 알리고, CanPlace로 미리보기를 칠한다.
/// 카드 타겟 모드(BeginTargeting)일 땐, 카드가 준 규칙으로 미리보기를 칠하고
/// 다음 클릭(유효 칸)을 카드에게 돌려준다. 우클릭은 취소.
/// </summary>
public class BoardView : MonoBehaviour
{
    [Header("바둑알 프리팹")]
    public GameObject stoneBlackPrefab;
    public GameObject stoneWhitePrefab;

    [Header("미리보기")]
    public GameObject previewPrefab;
    public Color playableColor = new Color(1f, 1f, 0f, 0.6f);
    public Color blockedColor  = new Color(1f, 0f, 0f, 0.6f);

    [Header("막힌 칸 표시")]
    public GameObject blockedMarkerPrefab;   // 막힌 칸에 놓을 표식(없으면 표시 생략)

    [Header("격자 설정 (1단계와 동일하게)")]
    public int boardSize = 15;
    public float spacing = 1f;
    public float stoneY = 0.3f;

    public event Action<int, int> CellClicked;
    public Func<int, int, bool> CanPlace;   // 평소 미리보기/착수 가능 판정(보통 board.IsPlayable)

    /// <summary>타겟 모드 진입(true)/이탈(false)을 알린다. UI 안내에 사용.</summary>
    public event Action<bool> TargetingChanged;

    private GameObject[,] _stones;
    private GameObject[,] _blockedMarkers;
    private Camera _cam;

    private GameObject _preview;
    private Renderer _previewRenderer;

    // 타겟 모드 상태
    private bool _targeting;
    private Func<int, int, bool> _targetValid;
    private Action<int, int> _onTargetPicked;
    private Action _onTargetCancel;

    private float Half => (boardSize - 1) / 2f;

    void Awake()
    {
        _stones = new GameObject[boardSize, boardSize];
        _blockedMarkers = new GameObject[boardSize, boardSize];
        _cam = Camera.main;

        if (previewPrefab != null)
        {
            _preview = Instantiate(previewPrefab, transform);
            _previewRenderer = _preview.GetComponentInChildren<Renderer>();
            _preview.SetActive(false);
        }
    }

    void Update()
    {
        UpdateHover();
        if (Mouse.current == null) return;

        if (_targeting)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame) TryPickTarget();
            else if (Mouse.current.rightButton.wasPressedThisFrame) CancelTargeting();
        }
        else
        {
            if (Mouse.current.leftButton.wasPressedThisFrame) TryClick();
        }
    }

    private void UpdateHover()
    {
        if (_preview == null) return;
        if (_cam == null) _cam = Camera.main;
        if (Mouse.current == null) return;

        Ray ray = _cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f) &&
            WorldToGrid(hit.point, out int col, out int row))
        {
            _preview.transform.position = GridToWorld(col, row);
            if (!_preview.activeSelf) _preview.SetActive(true);

            // 타겟 모드면 카드가 준 규칙, 아니면 평소 CanPlace로 색 결정.
            Func<int, int, bool> rule = _targeting ? _targetValid : CanPlace;
            bool ok = (rule != null) ? rule(col, row) : (_stones[col, row] == null);
            SetPreviewColor(ok ? playableColor : blockedColor);
        }
        else
        {
            if (_preview.activeSelf) _preview.SetActive(false);
        }
    }

    private void SetPreviewColor(Color c)
    {
        if (_previewRenderer == null) return;
        var mat = _previewRenderer.material;   // 인스턴스화되어 공유 에셋은 안 건드림
        mat.color = c;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
    }

    // ── 평소 클릭 ──
    private void TryClick()
    {
        if (_cam == null) _cam = Camera.main;
        Ray ray = _cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (!Physics.Raycast(ray, out RaycastHit hit, 1000f)) return;
        if (WorldToGrid(hit.point, out int col, out int row))
            CellClicked?.Invoke(col, row);
    }

    // ── 카드 타겟 모드 ──
    public void BeginTargeting(Func<int, int, bool> isValid, Action<int, int> onPicked, Action onCancel)
    {
        _targeting = true;
        _targetValid = isValid;
        _onTargetPicked = onPicked;
        _onTargetCancel = onCancel;
        TargetingChanged?.Invoke(true);
    }

    private void EndTargeting()
    {
        _targeting = false;
        _targetValid = null;
        _onTargetPicked = null;
        _onTargetCancel = null;
        TargetingChanged?.Invoke(false);
    }

    private void TryPickTarget()
    {
        if (_cam == null) _cam = Camera.main;
        Ray ray = _cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (!Physics.Raycast(ray, out RaycastHit hit, 1000f)) return;
        if (!WorldToGrid(hit.point, out int col, out int row)) return;
        if (_targetValid != null && !_targetValid(col, row)) return;   // 유효 대상만

        var cb = _onTargetPicked;
        EndTargeting();
        cb?.Invoke(col, row);
    }

    private void CancelTargeting()
    {
        var cb = _onTargetCancel;
        EndTargeting();
        cb?.Invoke();
    }

    // ── 좌표 변환 ──
    public Vector3 GridToWorld(int col, int row)
    {
        return transform.position +
            new Vector3((col - Half) * spacing, stoneY, (row - Half) * spacing);
    }

    public bool WorldToGrid(Vector3 world, out int col, out int row)
    {
        Vector3 local = world - transform.position;
        col = Mathf.RoundToInt(local.x / spacing + Half);
        row = Mathf.RoundToInt(local.z / spacing + Half);
        return col >= 0 && col < boardSize && row >= 0 && row < boardSize;
    }

    // ── 돌 시각 오브젝트 ──
    public void PlaceStoneVisual(int col, int row, CellState color)
    {
        if (col < 0 || col >= boardSize || row < 0 || row >= boardSize) return;
        if (_stones[col, row] != null) return;
        if (color == CellState.Empty) return;

        GameObject prefab = (color == CellState.Black) ? stoneBlackPrefab : stoneWhitePrefab;
        if (prefab == null) { Debug.LogWarning("BoardView: 바둑알 프리팹이 비어 있습니다."); return; }

        _stones[col, row] = Instantiate(prefab, GridToWorld(col, row), Quaternion.identity, transform);
    }

    public void RemoveStoneVisual(int col, int row)
    {
        if (col < 0 || col >= boardSize || row < 0 || row >= boardSize) return;
        if (_stones[col, row] != null)
        {
            Destroy(_stones[col, row]);
            _stones[col, row] = null;
        }
    }

    public void MarkBlockedVisual(int col, int row)
    {
        if (col < 0 || col >= boardSize || row < 0 || row >= boardSize) return;
        if (blockedMarkerPrefab == null) return;              // 프리팹 없으면 표시 생략
        if (_blockedMarkers[col, row] != null) return;
        _blockedMarkers[col, row] =
            Instantiate(blockedMarkerPrefab, GridToWorld(col, row), Quaternion.identity, transform);
    }

    public void ClearBlockedVisual(int col, int row)
    {
        if (col < 0 || col >= boardSize || row < 0 || row >= boardSize) return;
        if (_blockedMarkers[col, row] != null)
        {
            Destroy(_blockedMarkers[col, row]);
            _blockedMarkers[col, row] = null;
        }
    }

    public void ClearAll()
    {
        for (int c = 0; c < boardSize; c++)
        for (int r = 0; r < boardSize; r++)
        {
            if (_stones[c, r] != null) Destroy(_stones[c, r]);
            _stones[c, r] = null;
            if (_blockedMarkers[c, r] != null) Destroy(_blockedMarkers[c, r]);
            _blockedMarkers[c, r] = null;
        }
    }
}