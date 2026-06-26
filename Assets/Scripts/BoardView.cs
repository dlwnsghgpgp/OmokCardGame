using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 보드의 "겉모습과 입력"만 담당하는 컴포넌트.
/// - 마우스 클릭을 격자 좌표(col,row)로 변환해 CellClicked 이벤트로 알린다.
/// - 외부(GameManager)가 요청하면 해당 좌표에 돌 프리팹을 놓는다.
/// 돌을 둬도 되는지/누구 차례인지/점수는 전혀 모른다. 그건 GameManager의 일.
///
/// 이 스크립트는 1단계에서 만든 Board 오브젝트(원점 0,0,0)에 붙인다.
/// transform.position 을 격자의 원점으로 사용하므로 BoardGizmo 와 좌표가 일치한다.
/// </summary>
public class BoardView : MonoBehaviour
{
    [Header("바둑알 프리팹")]
    public GameObject stoneBlackPrefab;
    public GameObject stoneWhitePrefab;

    [Header("격자 설정 (1단계와 동일하게)")]
    public int boardSize = 15;
    public float spacing = 1f;
    public float stoneY = 0.3f;   // 보드 윗면 위로 돌이 살짝 떠 보이게 하는 높이

    /// <summary>플레이어가 격자 교차점을 클릭했을 때 발생. 인자는 (col, row).</summary>
    public event Action<int, int> CellClicked;

    private GameObject[,] _stones;   // 놓인 돌 오브젝트 추적(재시작 시 정리용)
    private Camera _cam;

    private float Half => (boardSize - 1) / 2f;   // 15면 7

    void Awake()
    {
        _stones = new GameObject[boardSize, boardSize];
        _cam = Camera.main;
    }

    void Update()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            TryClick();
    }

    private void TryClick()
    {
        if (_cam == null) _cam = Camera.main;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = _cam.ScreenPointToRay(mousePos);
        if (!Physics.Raycast(ray, out RaycastHit hit, 1000f))
            return;

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
        if (_stones[col, row] != null) return;          // 이미 시각적으로 놓여 있음
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