using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 보드의 "겉모습과 입력"만 담당. 클릭을 격자 좌표로 바꿔 CellClicked 로 알리고,
/// 요청받은 좌표에 돌을 놓는다. 추가로, 마우스가 가리키는 칸에 미리보기 돌을 띄운다.
/// 둘 수 있으면 노랑, 없으면 빨강 — 가능 여부는 CanPlace(보드 규칙)에게 물어본다.
/// 이 스크립트는 Board 오브젝트(원점 0,0,0)에 붙인다.
/// </summary>
public class BoardView : MonoBehaviour
{
    [Header("바둑알 프리팹")]
    public GameObject stoneBlackPrefab;
    public GameObject stoneWhitePrefab;

    [Header("미리보기")]
    public GameObject previewPrefab;                       // 반투명 돌(콜라이더 없음)
    public Color playableColor = new Color(1f, 1f, 0f, 0.6f); // 노랑(가능)
    public Color blockedColor  = new Color(1f, 0f, 0f, 0.6f); // 빨강(불가)

    [Header("격자 설정 (1단계와 동일하게)")]
    public int boardSize = 15;
    public float spacing = 1f;
    public float stoneY = 0.3f;

    /// <summary>플레이어가 격자 교차점을 클릭했을 때 발생. 인자는 (col, row).</summary>
    public event Action<int, int> CellClicked;

    /// <summary>
    /// "이 칸에 둘 수 있는가?"를 외부(GameManager)가 알려주는 판정 함수.
    /// 보통 GameManager가 board.IsPlayable 을 꽂아준다.
    /// 안 꽂혀 있으면 화면상 돌이 없는 칸을 가능으로 본다(단독 실행 대비).
    /// </summary>
    public Func<int, int, bool> CanPlace;

    private GameObject[,] _stones;
    private Camera _cam;

    private GameObject _preview;
    private Renderer _previewRenderer;

    private float Half => (boardSize - 1) / 2f;

    void Awake()
    {
        _stones = new GameObject[boardSize, boardSize];
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
        UpdateHover();   // 매 프레임 미리보기 갱신

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            TryClick();
    }

    // 마우스가 가리키는 격자에 미리보기 돌을 띄우고 색을 정한다.
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

            bool ok = (CanPlace != null) ? CanPlace(col, row) : (_stones[col, row] == null);
            SetPreviewColor(ok ? playableColor : blockedColor);
        }
        else
        {
            if (_preview.activeSelf) _preview.SetActive(false); // 보드 밖이면 숨김
        }
    }

    private void SetPreviewColor(Color c)
    {
        if (_previewRenderer == null) return;
        // .material 접근 시 인스턴스가 생기므로 공유 에셋은 건드리지 않는다.
        var mat = _previewRenderer.material;
        mat.color = c;                                       // 내장 셰이더용 메인 컬러
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c); // URP Lit 대비
    }

    private void TryClick()
    {
        if (_cam == null) _cam = Camera.main;

        Ray ray = _cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (!Physics.Raycast(ray, out RaycastHit hit, 1000f)) return;

        if (WorldToGrid(hit.point, out int col, out int row))
            CellClicked?.Invoke(col, row);
    }

    /// <summary>격자 좌표 → 월드 위치 (1단계 변환 공식과 동일).</summary>
    public Vector3 GridToWorld(int col, int row)
    {
        return transform.position +
            new Vector3((col - Half) * spacing, stoneY, (row - Half) * spacing);
    }

    /// <summary>월드 위치 → 가장 가까운 격자 좌표. 보드 범위를 벗어나면 false.</summary>
    public bool WorldToGrid(Vector3 world, out int col, out int row)
    {
        Vector3 local = world - transform.position;
        col = Mathf.RoundToInt(local.x / spacing + Half);
        row = Mathf.RoundToInt(local.z / spacing + Half);
        return col >= 0 && col < boardSize && row >= 0 && row < boardSize;
    }

    /// <summary>해당 좌표에 색에 맞는 돌을 시각적으로 놓는다. (GameManager가 호출)</summary>
    public void PlaceStoneVisual(int col, int row, CellState color)
    {
        if (col < 0 || col >= boardSize || row < 0 || row >= boardSize) return;
        if (_stones[col, row] != null) return;
        if (color == CellState.Empty) return;

        GameObject prefab = (color == CellState.Black) ? stoneBlackPrefab : stoneWhitePrefab;
        if (prefab == null)
        {
            Debug.LogWarning("BoardView: 바둑알 프리팹이 비어 있습니다.");
            return;
        }

        GameObject stone = Instantiate(prefab, GridToWorld(col, row), Quaternion.identity, transform);
        _stones[col, row] = stone;
    }

    /// <summary>놓인 돌 시각 오브젝트를 모두 제거(재시작용).</summary>
    public void ClearAll()
    {
        for (int c = 0; c < boardSize; c++)
        for (int r = 0; r < boardSize; r++)
        {
            if (_stones[c, r] != null) Destroy(_stones[c, r]);
            _stones[c, r] = null;
        }
    }
}