using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GameUI
{
    public Action<int> OnSelectLevel;
    public Action OnUndo, OnRestart, OnJump, OnMenu, OnNext, OnReplay;

    readonly Font font;
    readonly RectTransform root, hud, menu, win;
    Text levelLabel, levelTitle, movesText, hintText, toastText, controlsText;
    Image toastBg, jumpImage;
    Text jumpText;
    Text winTitle, winMoves;
    Image[] winStars;
    Button nextButton;
    readonly List<(Button button, Image image, Text label, Image[] stars)> levelButtons = new List<(Button, Image, Text, Image[])>();
    float toastTimer;

    public GameUI()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var canvasGo = new GameObject("UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600, 1000);
        scaler.matchWidthOrHeight = 0.5f;
        root = canvasGo.GetComponent<RectTransform>();

        if (UnityEngine.Object.FindAnyObjectByType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        hud = Panel("HUD", root);
        menu = Panel("Menu", root);
        win = Panel("Win", root);
        BuildHud();
        BuildMenu();
        BuildWin();
    }

    static RectTransform Panel(string name, Transform parent)
    {
        var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return rt;
    }

    static RectTransform Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    Text Label(Transform parent, string text, int size, Color color, TextAnchor align, FontStyle style = FontStyle.Normal)
    {
        var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var t = go.GetComponent<Text>();
        t.font = font;
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.fontStyle = style;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }

    static Image Box(Transform parent, Sprite sprite, Color color)
    {
        var go = new GameObject("Box", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.type = Image.Type.Sliced;
        img.color = color;
        return img;
    }

    Button MakeButton(Transform parent, string label, Color color, Action onClick, out Image image, out Text text, int fontSize = 26)
    {
        image = Box(parent, Shapes.Pill, color);
        var button = image.gameObject.AddComponent<Button>();
        var colors = button.colors;
        colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f);
        colors.pressedColor = new Color(0.85f, 0.85f, 0.85f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        button.onClick.AddListener(() => onClick?.Invoke());
        text = Label(image.transform, label, fontSize, Palette.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
        var tr = text.rectTransform;
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.offsetMin = tr.offsetMax = Vector2.zero;
        return button;
    }

    void BuildHud()
    {
        levelLabel = Label(hud, "LEVEL 1", 20, Palette.Accent, TextAnchor.UpperLeft, FontStyle.Bold);
        Place(levelLabel.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(48, -36), new Vector2(600, 30));
        levelTitle = Label(hud, "", 44, Palette.Text, TextAnchor.UpperLeft, FontStyle.Bold);
        Place(levelTitle.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(48, -62), new Vector2(700, 60));

        movesText = Label(hud, "", 26, Palette.Text, TextAnchor.UpperRight, FontStyle.Bold);
        Place(movesText.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-48, -40), new Vector2(400, 80));

        hintText = Label(hud, "", 24, Palette.TextDim, TextAnchor.UpperCenter);
        Place(hintText.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -130), new Vector2(1000, 70));

        var bar = new GameObject("Buttons", typeof(RectTransform), typeof(HorizontalLayoutGroup)).GetComponent<RectTransform>();
        bar.SetParent(hud, false);
        Place(bar, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 70), new Vector2(760, 64));
        var layout = bar.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 16;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;
        layout.childControlWidth = layout.childControlHeight = true;
        MakeButton(bar, "Levels", Palette.Button, () => OnMenu?.Invoke(), out _, out _);
        MakeButton(bar, "Undo  Z", Palette.Button, () => OnUndo?.Invoke(), out _, out _);
        MakeButton(bar, "Restart  R", Palette.Button, () => OnRestart?.Invoke(), out _, out _);
        MakeButton(bar, "Jump  Space", Palette.Button, () => OnJump?.Invoke(), out jumpImage, out jumpText);

        controlsText = Label(hud, "Move: WASD / Arrows / tap a dot     Jump: hold Shift + direction, or tap a ring", 18,
            Palette.WithAlpha(Palette.TextDim, 0.8f), TextAnchor.MiddleCenter);
        Place(controlsText.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 26), new Vector2(1200, 30));

        toastBg = Box(hud, Shapes.Pill, Palette.WithAlpha(Palette.Danger, 0.95f));
        Place(toastBg.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 160), new Vector2(620, 56));
        toastBg.raycastTarget = false;
        toastText = Label(toastBg.transform, "", 24, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
        var tr = toastText.rectTransform;
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.offsetMin = tr.offsetMax = Vector2.zero;
        toastBg.gameObject.SetActive(false);
    }

    void BuildMenu()
    {
        var title = Label(menu, "ELEVATE", 120, Palette.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
        Place(title.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -120), new Vector2(1000, 140));
        var sub = Label(menu, "Guide your piece to the green square. Climb, jump, don't fall.", 28, Palette.TextDim, TextAnchor.MiddleCenter);
        Place(sub.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -265), new Vector2(1200, 40));

        var grid = new GameObject("Levels", typeof(RectTransform), typeof(GridLayoutGroup)).GetComponent<RectTransform>();
        grid.SetParent(menu, false);
        Place(grid, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -60), new Vector2(5 * 150 + 4 * 24, 2 * 150 + 24));
        var gl = grid.GetComponent<GridLayoutGroup>();
        gl.cellSize = new Vector2(150, 150);
        gl.spacing = new Vector2(24, 24);
        gl.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gl.constraintCount = 5;

        for (int i = 0; i < 10; i++)
        {
            int index = i;
            var img = Box(grid, Shapes.Rounded, Palette.Button);
            var btn = img.gameObject.AddComponent<Button>();
            btn.onClick.AddListener(() => OnSelectLevel?.Invoke(index));
            var num = Label(img.transform, (i + 1).ToString(), 52, Palette.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            Place(num.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 14), new Vector2(150, 80));
            var stars = new Image[3];
            for (int s = 0; s < 3; s++)
            {
                stars[s] = Box(img.transform, Shapes.Star, Palette.Button);
                stars[s].type = Image.Type.Simple;
                stars[s].raycastTarget = false;
                Place(stars[s].rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2((s - 1) * 30, 18), new Vector2(26, 26));
            }
            levelButtons.Add((btn, img, num, stars));
        }

        var foot = Label(menu, "Tip: you can always Undo. Holes just bounce you back.", 20, Palette.WithAlpha(Palette.TextDim, 0.7f), TextAnchor.MiddleCenter);
        Place(foot.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 60), new Vector2(1000, 30));
    }

    void BuildWin()
    {
        var dim = Box(win, Shapes.Rounded, new Color(0.03f, 0.04f, 0.08f, 0.7f));
        dim.type = Image.Type.Simple;
        var dr = dim.rectTransform;
        dr.anchorMin = Vector2.zero;
        dr.anchorMax = Vector2.one;
        dr.offsetMin = new Vector2(-50, -50);
        dr.offsetMax = new Vector2(50, 50);

        var card = Box(win, Shapes.Rounded, Palette.Panel);
        Place(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560, 420));
        winTitle = Label(card.transform, "Level complete", 44, Palette.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
        Place(winTitle.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -40), new Vector2(520, 60));

        winStars = new Image[3];
        for (int s = 0; s < 3; s++)
        {
            winStars[s] = Box(card.transform, Shapes.Star, Palette.Piece);
            winStars[s].type = Image.Type.Simple;
            Place(winStars[s].rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 0.5f), new Vector2((s - 1) * 96, -160), new Vector2(84, 84));
        }

        winMoves = Label(card.transform, "", 24, Palette.TextDim, TextAnchor.MiddleCenter);
        Place(winMoves.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -230), new Vector2(520, 40));

        var replay = MakeButton(card.transform, "Replay", Palette.Button, () => OnReplay?.Invoke(), out _, out _);
        Place(replay.GetComponent<RectTransform>(), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-120, 44), new Vector2(210, 70));
        nextButton = MakeButton(card.transform, "Next  ⏎", Palette.Goal, () => OnNext?.Invoke(), out _, out var nextText);
        nextText.text = "Next";
        nextText.color = Palette.Background;
        Place(nextButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(120, 44), new Vector2(210, 70));
    }

    public void ShowMenu(int unlocked, Func<int, int> starsFor)
    {
        hud.gameObject.SetActive(false);
        win.gameObject.SetActive(false);
        menu.gameObject.SetActive(true);
        for (int i = 0; i < levelButtons.Count; i++)
        {
            var (button, image, label, stars) = levelButtons[i];
            bool open = i <= unlocked;
            button.interactable = open;
            image.color = open ? (i == unlocked ? Palette.Accent : Palette.Button) : Palette.WithAlpha(Palette.Button, 0.4f);
            label.color = open ? Palette.Text : Palette.WithAlpha(Palette.TextDim, 0.4f);
            int got = starsFor(i);
            for (int s = 0; s < 3; s++)
                stars[s].color = s < got ? Palette.Piece : Palette.WithAlpha(Color.black, open ? 0.25f : 0.1f);
        }
    }

    public void ShowLevel(int index, LevelData level)
    {
        menu.gameObject.SetActive(false);
        win.gameObject.SetActive(false);
        hud.gameObject.SetActive(true);
        levelLabel.text = $"LEVEL {index + 1} / 10";
        levelTitle.text = level.Title;
        hintText.text = level.Hint;
        toastBg.gameObject.SetActive(false);
    }

    public void SetMoves(int moves, int par)
    {
        movesText.text = $"<size=40>{moves}</size>\n<color=#8C93B3>MOVES · PAR {par}</color>";
        movesText.supportRichText = true;
    }

    public void SetJumpArmed(bool armed)
    {
        jumpImage.color = armed ? Palette.Accent : Palette.Button;
        jumpText.text = armed ? "Jump ready!" : "Jump  Space";
    }

    public void Toast(string message, Color color)
    {
        toastBg.color = color;
        toastText.text = message;
        toastBg.gameObject.SetActive(true);
        toastTimer = 2.2f;
    }

    public void ShowWin(int moves, int par, int stars, bool isLast)
    {
        win.gameObject.SetActive(true);
        winTitle.text = isLast ? "You reached the summit!" : "Level complete";
        winMoves.text = $"{moves} moves · par {par}";
        for (int s = 0; s < 3; s++) winStars[s].color = s < stars ? Palette.Piece : Palette.WithAlpha(Color.white, 0.08f);
        nextButton.GetComponentInChildren<Text>().text = isLast ? "Levels" : "Next";
    }

    public bool WinVisible => win.gameObject.activeSelf;

    public void Tick(float dt)
    {
        if (toastTimer <= 0f) return;
        toastTimer -= dt;
        var c = toastBg.color;
        float a = Mathf.Clamp01(toastTimer / 0.3f);
        toastBg.color = new Color(c.r, c.g, c.b, 0.95f * a);
        toastText.color = new Color(1, 1, 1, a);
        if (toastTimer <= 0f) toastBg.gameObject.SetActive(false);
    }
}
