using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class GameManager : MonoBehaviour
{
    const string UnlockedKey = "elevate.unlocked";
    const string StarsKey = "elevate.stars.";

    List<LevelData> levels;
    Board board;
    BoardView view;
    GameUI ui;
    Camera cam;
    int levelIndex;
    bool playing, pendingWin, hintsDirty, jumpArmed;
    Vector2Int? bufferedDir;
    bool bufferedJump;
    Vector2 lastScreen;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (FindAnyObjectByType<GameManager>() == null) new GameObject("Elevate").AddComponent<GameManager>();
    }

    void Awake()
    {
        Application.targetFrameRate = 60;
        cam = Camera.main;
        if (cam == null) cam = new GameObject("Main Camera", typeof(Camera)) { tag = "MainCamera" }.GetComponent<Camera>();
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Palette.Background;
        cam.transform.position = new Vector3(0, 0, -10);

        levels = LevelData.LoadAll();
        view = new GameObject("Board").AddComponent<BoardView>();
        ui = new GameUI
        {
            OnSelectLevel = LoadLevel,
            OnUndo = Undo,
            OnRestart = () => LoadLevel(levelIndex),
            OnJump = ToggleJump,
            OnMenu = ShowMenu,
            OnNext = Next,
            OnReplay = () => LoadLevel(levelIndex)
        };
        ShowMenu();
    }

    int Unlocked => Mathf.Clamp(PlayerPrefs.GetInt(UnlockedKey, 0), 0, levels.Count - 1);

    int StarsFor(int i) => PlayerPrefs.GetInt(StarsKey + i, 0);

    void ShowMenu()
    {
        playing = false;
        view.gameObject.SetActive(false);
        ui.ShowMenu(Unlocked, StarsFor);
    }

    void LoadLevel(int index)
    {
        levelIndex = index;
        board = new Board(levels[index]);
        view.gameObject.SetActive(true);
        view.Build(board);
        ui.ShowLevel(index, levels[index]);
        ui.SetMoves(0, levels[index].Par);
        SetJumpArmed(false);
        playing = true;
        pendingWin = false;
        bufferedDir = null;
        hintsDirty = true;
        FitCamera();
    }

    void Next()
    {
        if (levelIndex + 1 < levels.Count) LoadLevel(levelIndex + 1);
        else ShowMenu();
    }

    void Undo()
    {
        if (!playing || ui.WinVisible || view.Busy || !board.Undo()) return;
        view.SnapAll();
        ui.SetMoves(board.Moves, levels[levelIndex].Par);
        hintsDirty = true;
    }

    void ToggleJump() => SetJumpArmed(!jumpArmed);

    void SetJumpArmed(bool armed)
    {
        jumpArmed = armed;
        ui.SetJumpArmed(armed);
    }

    void FitCamera()
    {
        float aspect = (float)Screen.width / Mathf.Max(1, Screen.height);
        float topMargin = 2.4f, bottomMargin = 2.0f;
        float h = board.Rows + topMargin + bottomMargin;
        float w = board.Cols + 1.2f;
        cam.orthographicSize = Mathf.Max(h * 0.5f, w * 0.5f / aspect);
        cam.transform.position = new Vector3(0, (topMargin - bottomMargin) * 0.5f, -10);
        lastScreen = new Vector2(Screen.width, Screen.height);
    }

    void Update()
    {
        ui.Tick(Time.deltaTime);
        if (!playing) return;
        if (lastScreen.x != Screen.width || lastScreen.y != Screen.height) FitCamera();

        if (ui.WinVisible)
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space)) Next();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape)) { ShowMenu(); return; }
        if (Input.GetKeyDown(KeyCode.R)) { LoadLevel(levelIndex); return; }
        if (Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.Backspace) || Input.GetKeyDown(KeyCode.U)) Undo();
        if (Input.GetKeyDown(KeyCode.Space)) ToggleJump();

        bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        Vector2Int? dir = null;
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) dir = Vector2Int.up;
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) dir = Vector2Int.down;
        else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) dir = Vector2Int.left;
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) dir = Vector2Int.right;

        bool jump = shift || jumpArmed;
        if (dir == null && Input.GetMouseButtonDown(0) && !PointerOverUI()) dir = DirFromClick(out jump);

        if (dir != null)
        {
            bufferedDir = dir;
            bufferedJump = jump;
        }

        if (view.Busy) return;

        if (pendingWin)
        {
            pendingWin = false;
            CompleteLevel();
            return;
        }

        if (bufferedDir != null)
        {
            var d = bufferedDir.Value;
            bufferedDir = null;
            DoMove(d, bufferedJump);
        }

        if (hintsDirty && !view.Busy)
        {
            hintsDirty = false;
            view.ShowHints(board.ValidTargets());
        }
    }

    static bool PointerOverUI()
    {
        if (EventSystem.current == null) return false;
        if (EventSystem.current.IsPointerOverGameObject()) return true;
        for (int i = 0; i < Input.touchCount; i++)
            if (EventSystem.current.IsPointerOverGameObject(Input.GetTouch(i).fingerId)) return true;
        return false;
    }

    Vector2Int? DirFromClick(out bool jump)
    {
        jump = false;
        var world = cam.ScreenToWorldPoint(Input.mousePosition);
        if (!view.TryPickCell(world, out var cell)) return null;
        foreach (var p in board.Pieces)
        {
            var delta = cell - p;
            foreach (var d in Board.Directions)
            {
                if (delta == d) return d;
                if (delta == d * 2)
                {
                    jump = true;
                    return d;
                }
            }
        }
        return null;
    }

    void DoMove(Vector2Int dir, bool jump)
    {
        view.ClearHints();
        var result = board.Move(dir, jump);
        if (jump) SetJumpArmed(false);
        view.Play(result);
        view.RefreshHeights(false);
        hintsDirty = true;

        if (!result.Moved)
        {
            switch (result.Reason)
            {
                case BlockReason.TooHigh: ui.Toast("Too high! Climb one level at a time.", Palette.Accent); break;
                case BlockReason.JumpNeedsHeight: ui.Toast("Jumps need a raised platform to leap from.", Palette.Accent); break;
                case BlockReason.JumpBlocked: ui.Toast("Can't land there: too high or blocked.", Palette.Accent); break;
            }
            return;
        }

        if (result.Died)
        {
            ui.Toast("Whoops, a hole! You're back where you were.", Palette.Danger);
            return;
        }

        if (result.Toggled) ui.Toast("Click! The purple lifts moved.", Palette.Lift);
        ui.SetMoves(board.Moves, levels[levelIndex].Par);
        if (result.Won) pendingWin = true;
    }

    void CompleteLevel()
    {
        var level = levels[levelIndex];
        int stars = board.Moves <= level.Par ? 3 : board.Moves <= level.Par + 4 ? 2 : 1;
        if (stars > StarsFor(levelIndex)) PlayerPrefs.SetInt(StarsKey + levelIndex, stars);
        if (levelIndex + 1 > PlayerPrefs.GetInt(UnlockedKey, 0)) PlayerPrefs.SetInt(UnlockedKey, Mathf.Min(levelIndex + 1, levels.Count - 1));
        PlayerPrefs.Save();
        view.ClearHints();
        ui.ShowWin(board.Moves, level.Par, stars, levelIndex == levels.Count - 1);
    }
}
