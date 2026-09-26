using System.Collections.Generic;
using UnityEngine;

public class BoardView : MonoBehaviour
{
    public const float Lift = 0.17f;
    const float TileSize = 0.92f;
    const float BaseDepth = 0.09f;
    const float WallVisualHeight = 2.4f;

    Board board;
    TileView[,] tiles;
    PieceView[] pieces;
    readonly List<SpriteRenderer> hintMarks = new List<SpriteRenderer>();

    public bool Busy
    {
        get
        {
            if (pieces == null) return false;
            foreach (var p in pieces)
                if (p.Busy) return true;
            return false;
        }
    }

    class TileView
    {
        public Vector2Int Cell;
        public char Type;
        public SpriteRenderer Side, Top, Decor, Decor2;
        public SpriteRenderer[] Pips;
        public float AnimHeight, TargetHeight;
    }

    class PieceView
    {
        public Transform Root;
        public SpriteRenderer Body, Shine, Shadow;
        public readonly Queue<Step> Queue = new Queue<Step>();
        public Vector2Int Cell;
        public Vector2Int From, To;
        public StepKind Kind;
        public float T, Duration;
        public bool Active;
        public bool Busy => Active || Queue.Count > 0;
    }

    public Vector3 CellCenter(Vector2Int c) => new Vector3(c.x - (board.Cols - 1) * 0.5f, c.y - (board.Rows - 1) * 0.5f, 0f);

    public void Build(Board b)
    {
        board = b;
        for (int i = transform.childCount - 1; i >= 0; i--) Destroy(transform.GetChild(i).gameObject);
        hintMarks.Clear();

        tiles = new TileView[b.Cols, b.Rows];
        for (int x = 0; x < b.Cols; x++)
        for (int y = 0; y < b.Rows; y++)
        {
            var cell = new Vector2Int(x, y);
            char type = b.At(cell);
            if (type == ' ') continue;
            tiles[x, y] = CreateTile(cell, type);
        }

        pieces = new PieceView[b.Pieces.Length];
        for (int i = 0; i < pieces.Length; i++) pieces[i] = CreatePiece(b.Pieces[i]);
        SnapAll();
    }

    TileView CreateTile(Vector2Int cell, char type)
    {
        var root = new GameObject($"Tile {cell.x},{cell.y}").transform;
        root.SetParent(transform, false);
        var t = new TileView { Cell = cell, Type = type };
        t.Side = MakeSprite(root, "Side", Shapes.Rounded, Color.white, true);
        t.Top = MakeSprite(root, "Top", Shapes.Rounded, Color.white, true);
        t.Top.size = Vector2.one * TileSize;
        t.Pips = new SpriteRenderer[3];
        for (int i = 0; i < 3; i++)
        {
            t.Pips[i] = MakeSprite(root, "Pip", Shapes.Circle, new Color(1, 1, 1, 0.55f), false);
            t.Pips[i].transform.localScale = Vector3.one * 0.1f;
        }

        Color top = Palette.Floor;
        switch (type)
        {
            case '#': top = Palette.WallTop; break;
            case 'x': top = Palette.Hole; break;
            case 'G': top = Palette.Goal; break;
            case '1': case '2': case '3': top = Palette.PlatformTop(type - '0'); break;
            case 'a': case 'A': top = Palette.Lift; break;
            case 'c': case 'C': top = Palette.Pulse; break;
            case 'i': top = Palette.Ice; break;
        }
        t.Top.color = top;
        t.Side.color = type == '#' ? Palette.WallSide : Palette.Side(top);

        switch (type)
        {
            case 'x':
                t.Side.enabled = false;
                t.Top.size = Vector2.one * (TileSize - 0.06f);
                t.Decor = MakeSprite(root, "Glow", Shapes.Ring, Palette.WithAlpha(Palette.Danger, 0.55f), false);
                t.Decor.transform.localScale = Vector3.one * 0.5f;
                t.Decor2 = MakeSprite(root, "Core", Shapes.Circle, Palette.WithAlpha(Palette.Danger, 0.18f), false);
                t.Decor2.transform.localScale = Vector3.one * 0.3f;
                break;
            case 'G':
                t.Decor = MakeSprite(root, "Ring", Shapes.Ring, new Color(1, 1, 1, 0.8f), false);
                t.Decor.transform.localScale = Vector3.one * 0.55f;
                break;
            case 'S':
                t.Decor = MakeSprite(root, "Button", Shapes.Circle, Palette.Lift, false);
                t.Decor.transform.localScale = Vector3.one * 0.5f;
                t.Decor2 = MakeSprite(root, "ButtonRing", Shapes.Ring, Palette.WithAlpha(Palette.Lift, 0.6f), false);
                t.Decor2.transform.localScale = Vector3.one * 0.72f;
                break;
            case 'T':
                t.Decor = MakeSprite(root, "Portal", Shapes.Ring, Palette.Portal, false);
                t.Decor.transform.localScale = Vector3.one * 0.66f;
                t.Decor2 = MakeSprite(root, "PortalCore", Shapes.Circle, Palette.WithAlpha(Palette.Portal, 0.35f), false);
                t.Decor2.transform.localScale = Vector3.one * 0.4f;
                break;
            case 'i':
                t.Decor = MakeSprite(root, "Shine", Shapes.Pill, new Color(1, 1, 1, 0.7f), true);
                t.Decor.size = new Vector2(0.36f, 0.08f);
                t.Decor.transform.localRotation = Quaternion.Euler(0, 0, 35f);
                break;
            case '>': case '<': case '^': case 'v':
                t.Decor = MakeSprite(root, "Arrow", Shapes.Chevron, Palette.Arrow, false);
                t.Decor.transform.localScale = Vector3.one * 0.6f;
                var d = Board.ArrowDir(type);
                t.Decor.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
                break;
        }

        t.TargetHeight = t.AnimHeight = VisualHeight(t);
        return t;
    }

    PieceView CreatePiece(Vector2Int cell)
    {
        var root = new GameObject("Piece").transform;
        root.SetParent(transform, false);
        var p = new PieceView { Root = root, Cell = cell, From = cell, To = cell };
        p.Shadow = MakeSprite(root, "Shadow", Shapes.Shadow, new Color(0, 0, 0, 0.55f), false);
        p.Body = MakeSprite(root, "Body", Shapes.Circle, Palette.Piece, false);
        p.Shine = MakeSprite(root, "Shine", Shapes.Circle, Palette.PieceLight, false);
        return p;
    }

    static SpriteRenderer MakeSprite(Transform parent, string name, Sprite sprite, Color color, bool sliced)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        if (sliced) sr.drawMode = SpriteDrawMode.Sliced;
        return sr;
    }

    float VisualHeight(TileView t)
    {
        if (t.Type == '#') return WallVisualHeight;
        if (t.Type == 'x') return 0f;
        return board.HeightAt(t.Cell);
    }

    float TileHeight(Vector2Int c)
    {
        if (!board.InBounds(c) || tiles[c.x, c.y] == null) return 0f;
        var t = tiles[c.x, c.y];
        return t.Type == '#' ? 0f : t.AnimHeight;
    }

    int RowOrder(float y) => (board.Rows - Mathf.FloorToInt(y + 0.001f)) * 20;

    public void SnapAll()
    {
        for (int i = 0; i < pieces.Length; i++)
        {
            var p = pieces[i];
            p.Queue.Clear();
            p.Active = false;
            p.Cell = p.From = p.To = board.Pieces[i];
            p.Root.localScale = Vector3.one;
        }
        RefreshHeights(true);
    }

    public void RefreshHeights(bool instant)
    {
        foreach (var t in tiles)
        {
            if (t == null) continue;
            t.TargetHeight = VisualHeight(t);
            if (instant) t.AnimHeight = t.TargetHeight;
        }
    }

    public void Play(MoveResult result)
    {
        for (int i = 0; i < pieces.Length; i++)
            foreach (var s in result.Paths[i])
                pieces[i].Queue.Enqueue(s);
    }

    public void ShowHints(List<(Vector2Int pos, bool jump)> targets)
    {
        ClearHints();
        foreach (var (pos, jump) in targets)
        {
            var sr = MakeSprite(transform, "Hint", jump ? Shapes.Ring : Shapes.Circle,
                new Color(1, 1, 1, jump ? 0.4f : 0.22f), false);
            var c = CellCenter(pos);
            sr.transform.localPosition = c + Vector3.up * (TileHeight(pos) * Lift);
            sr.transform.localScale = Vector3.one * (jump ? 0.34f : 0.16f);
            sr.sortingOrder = RowOrder(pos.y) + 4;
            hintMarks.Add(sr);
        }
    }

    public void ClearHints()
    {
        foreach (var h in hintMarks) Destroy(h.gameObject);
        hintMarks.Clear();
    }

    public bool TryPickCell(Vector3 world, out Vector2Int cell)
    {
        var local = world - transform.position;
        for (int y = 0; y < board.Rows; y++)
        for (int x = 0; x < board.Cols; x++)
        {
            var t = tiles[x, y];
            if (t == null) continue;
            var c = CellCenter(t.Cell);
            float top = c.y + t.AnimHeight * Lift;
            if (Mathf.Abs(local.x - c.x) < 0.5f && Mathf.Abs(local.y - top) < 0.5f)
            {
                cell = t.Cell;
                return true;
            }
        }
        cell = default;
        return false;
    }

    void Update()
    {
        if (board == null) return;
        float dt = Time.deltaTime;
        float time = Time.time;

        foreach (var t in tiles)
        {
            if (t == null) continue;
            t.AnimHeight = Mathf.Lerp(t.AnimHeight, t.TargetHeight, 1f - Mathf.Exp(-dt * 14f));
            LayoutTile(t, time);
        }

        foreach (var p in pieces) UpdatePiece(p, dt);
    }

    void LayoutTile(TileView t, float time)
    {
        var c = CellCenter(t.Cell);
        float lift = t.AnimHeight * Lift;
        int order = RowOrder(t.Cell.y);
        var topPos = c + Vector3.up * lift;

        t.Top.transform.localPosition = topPos;
        t.Top.sortingOrder = order + 1;
        float sideHeight = TileSize + lift + BaseDepth;
        t.Side.size = new Vector2(TileSize, sideHeight);
        t.Side.transform.localPosition = new Vector3(c.x, topPos.y + TileSize * 0.5f - sideHeight * 0.5f, 0f);
        t.Side.sortingOrder = order;

        if (t.Decor != null)
        {
            t.Decor.transform.localPosition = topPos;
            t.Decor.sortingOrder = order + 3;
        }
        if (t.Decor2 != null)
        {
            t.Decor2.transform.localPosition = topPos;
            t.Decor2.sortingOrder = order + 2;
        }

        switch (t.Type)
        {
            case 'G':
                t.Decor.transform.localScale = Vector3.one * (0.5f + 0.06f * Mathf.Sin(time * 3f));
                break;
            case 'T':
                t.Decor2.transform.localScale = Vector3.one * (0.36f + 0.08f * Mathf.Sin(time * 4f));
                break;
            case 'x':
                t.Decor.color = Palette.WithAlpha(Palette.Danger, 0.4f + 0.2f * Mathf.Sin(time * 2.5f + t.Cell.x));
                break;
            case 'S':
                t.Decor.color = board.LiftFlipped ? Palette.Side(Palette.Lift) : Palette.Lift;
                break;
        }

        bool showPips = t.Type != '#' && t.Type != 'x';
        int pipCount = showPips ? Mathf.RoundToInt(t.TargetHeight) : 0;
        for (int i = 0; i < 3; i++)
        {
            var pip = t.Pips[i];
            pip.enabled = i < pipCount;
            if (!pip.enabled) continue;
            float offset = (i - (pipCount - 1) * 0.5f) * 0.16f;
            pip.transform.localPosition = topPos + new Vector3(offset, -0.3f, 0f);
            pip.sortingOrder = order + 2;
        }
    }

    void UpdatePiece(PieceView p, float dt)
    {
        if (!p.Active && p.Queue.Count > 0)
        {
            var step = p.Queue.Dequeue();
            p.Kind = step.Kind;
            p.From = p.Cell;
            p.To = step.Pos;
            p.T = 0f;
            p.Active = true;
            switch (step.Kind)
            {
                case StepKind.Jump: p.Duration = 0.28f; break;
                case StepKind.Slide: p.Duration = 0.08f; break;
                case StepKind.Push: p.Duration = 0.12f; break;
                case StepKind.Teleport: p.Duration = 0.3f; break;
                case StepKind.Fall: p.Duration = 0.35f; break;
                case StepKind.Snap: p.Duration = 0.2f; break;
                case StepKind.Bump: p.Duration = 0.16f; break;
                default: p.Duration = 0.13f; break;
            }
            if (step.Kind == StepKind.Snap) p.From = p.To;
            if (step.Kind == StepKind.Bump || step.Kind == StepKind.Fall) p.To = step.Pos;
        }

        float k = 1f;
        if (p.Active)
        {
            p.T += dt / p.Duration;
            k = Mathf.Clamp01(p.T);
        }

        Vector2 from = p.From, to = p.To;
        Vector2 pos;
        float arc = 0f, scale = 1f;
        float height;
        switch (p.Active ? p.Kind : StepKind.Walk)
        {
            case StepKind.Bump:
                pos = Vector2.Lerp(from, to, 0.16f * Mathf.Sin(Mathf.PI * k));
                height = TileHeight(p.From);
                break;
            case StepKind.Teleport:
                pos = k < 0.5f ? from : to;
                scale = Mathf.Abs(Mathf.Cos(Mathf.PI * k));
                height = TileHeight(k < 0.5f ? p.From : p.To);
                break;
            case StepKind.Fall:
                pos = from;
                scale = 1f - k;
                height = -k * 2f;
                break;
            case StepKind.Snap:
                pos = to;
                scale = k;
                height = TileHeight(p.To);
                break;
            default:
            {
                float e = p.Active ? Ease(k) : 1f;
                pos = Vector2.Lerp(from, to, e);
                height = Mathf.Lerp(TileHeight(p.From), TileHeight(p.To), e);
                float arcHeight = p.Kind == StepKind.Jump ? 0.6f : p.Kind == StepKind.Walk ? 0.12f : p.Kind == StepKind.Push ? 0.05f : 0f;
                if (p.Active) arc = Mathf.Sin(Mathf.PI * k) * arcHeight;
                break;
            }
        }

        if (p.Active && p.T >= 1f)
        {
            p.Active = false;
            if (p.Kind != StepKind.Bump && p.Kind != StepKind.Fall) p.Cell = p.To;
            p.From = p.To = p.Cell;
        }

        var cellPos = CellCenter(Vector2Int.zero) + new Vector3(pos.x, pos.y, 0f);
        float baseY = cellPos.y + height * Lift;
        int order = RowOrder(Mathf.Min(pos.y, Mathf.Max(from.y, to.y))) + 10;

        p.Shadow.transform.localPosition = new Vector3(cellPos.x, baseY - 0.1f, 0f);
        p.Shadow.transform.localScale = Vector3.one * (0.8f - arc * 0.4f) * scale;
        p.Shadow.sortingOrder = order;

        var bodyPos = new Vector3(cellPos.x, baseY + 0.06f + arc, 0f);
        float squash = p.Active && p.Kind == StepKind.Jump ? 1f + 0.08f * Mathf.Sin(Mathf.PI * k) : 1f;
        p.Body.transform.localPosition = bodyPos;
        p.Body.transform.localScale = new Vector3(0.62f / squash, 0.62f * squash, 1f) * scale;
        p.Body.sortingOrder = order + 1;
        p.Shine.transform.localPosition = bodyPos + new Vector3(-0.1f, 0.1f, 0f) * scale;
        p.Shine.transform.localScale = Vector3.one * 0.2f * scale;
        p.Shine.sortingOrder = order + 2;
    }

    static float Ease(float t) => 1f - (1f - t) * (1f - t);
}
