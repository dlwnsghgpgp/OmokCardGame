using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>대전 모드. PvP는 아직 미구현(선택만 가능하게 두고 비활성).</summary>
public enum PlayMode { VersusAI, VersusPlayer }

/// <summary>
/// 씬이 바뀌어도 살아남는 전역 관리자.
///  - 카드 데이터베이스 참조(어느 씬에서든 ID로 카드 조회)
///  - 씬 간 전달 데이터(선택한 덱 ID 목록, 테마, 대전 모드)
///  - 씬 전환 기능
///
/// 어느 씬에서 플레이를 시작하든 Instance 접근 시 자동 생성되므로,
/// 개발 중 GameScene을 직접 열어 테스트해도 문제없다.
/// </summary>
public class GameSession : MonoBehaviour
{
    // ── 싱글턴 ──
    private static GameSession _instance;

    public static GameSession Instance
    {
        get
        {
            if (_instance != null) return _instance;

            // 씬에 이미 놓여 있으면 그걸 쓴다.
            _instance = FindObjectOfType<GameSession>();
            if (_instance == null)
            {
                // 없으면 스스로 만든다(게임 씬 직접 실행 대비).
                var go = new GameObject("GameSession");
                _instance = go.AddComponent<GameSession>();
                Debug.Log("[GameSession] 자동 생성됨(씬에 배치된 것이 없어서).");
            }
            _instance.Init();
            return _instance;
        }
    }

    [Header("데이터")]
    public CardDatabase cardDatabase;   // 씬에 배치해 연결하거나, Resources에서 자동 로드
    public List<ThemeData> themes = new List<ThemeData>();   // 선택 가능한 테마들

    // ── 씬 간 전달 데이터 ──
    public PlayMode Mode { get; set; } = PlayMode.VersusAI;

    /// <summary>
    /// 선택된 테마의 ID(타이틀의 테마 선택에서 설정).
    /// 실제 테마 객체는 이 ID로 조회하는 SelectedTheme 프로퍼티로 얻는다.
    /// </summary>
    public string SelectedThemeId { get; set; }

    /// <summary>선택된 덱(카드 ID 목록). 비어 있으면 게임 씬이 기본 덱으로 시작한다.</summary>
    public List<string> SelectedDeckIds { get; private set; } = new List<string>();

    /// <summary>덱이 선택되어 있는가. 없으면 게임 씬이 기본 덱으로 시작한다.</summary>
    public bool HasDeck => SelectedDeckIds != null && SelectedDeckIds.Count > 0;

    public void SetDeck(IEnumerable<string> cardIds)
    {
        SelectedDeckIds = new List<string>();
        if (cardIds != null) SelectedDeckIds.AddRange(cardIds);
    }

    private bool _initialized;

    void Awake()
    {
        // 씬에 직접 배치된 경우: 중복이면 자신을 지운다.
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        Init();
    }

    private void Init()
    {
        if (_initialized) return;
        _initialized = true;
        DontDestroyOnLoad(gameObject);

        // 카드 DB가 연결되지 않았으면 Resources에서 찾아본다.
        if (cardDatabase == null)
        {
            cardDatabase = Resources.Load<CardDatabase>("CardDatabase");
            if (cardDatabase == null)
                Debug.LogWarning("[GameSession] CardDatabase가 없습니다. " +
                                 "Resources 폴더에 두거나 씬의 GameSession에 연결하세요.");
        }

        // 테마도 비어 있으면 Resources/Themes 폴더에서 전부 불러온다.
        if (themes == null || themes.Count == 0)
        {
            var loaded = Resources.LoadAll<ThemeData>("Themes");
            themes = new List<ThemeData>(loaded);
            if (themes.Count == 0)
                Debug.LogWarning("[GameSession] 테마가 없습니다. " +
                                 "Assets/Resources/Themes 에 테마 에셋을 두세요.");
        }
    }

    /// <summary>선택된 덱 ID를 실제 카드 목록으로 변환한다(덱이 없으면 빈 목록).</summary>
    public List<CardData> BuildDeckCards()
    {
        if (cardDatabase == null || !HasDeck) return new List<CardData>();
        return cardDatabase.GetMany(SelectedDeckIds);
    }

    /// <summary>
    /// 선택된 테마. SelectedThemeId로 찾고, 없으면 첫 번째 테마(그것도 없으면 null).
    /// 게임 씬이 이 값으로 필드 덱·등장 규칙을 결정한다.
    /// </summary>
    public ThemeData SelectedTheme
    {
        get
        {
            if (themes == null || themes.Count == 0) return null;
            if (!string.IsNullOrEmpty(SelectedThemeId))
            {
                foreach (var t in themes)
                    if (t != null && t.id == SelectedThemeId) return t;
                Debug.LogWarning($"[GameSession] 테마 ID '{SelectedThemeId}'를 찾을 수 없어 첫 테마를 씁니다.");
            }
            return themes[0];
        }
    }

    // ── 씬 전환 ──
    public const string TitleScene = "TitleScene";
    public const string DeckEditScene = "DeckEditScene";
    public const string GameScene = "GameScene";

    public void LoadTitle()    => SceneManager.LoadScene(TitleScene);
    public void LoadDeckEdit() => SceneManager.LoadScene(DeckEditScene);
    public void LoadGame()     => SceneManager.LoadScene(GameScene);

    /// <summary>게임 종료(에디터에서는 플레이 모드 중지).</summary>
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}