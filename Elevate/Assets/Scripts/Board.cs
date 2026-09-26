using System.Collections.Generic;
using UnityEngine;

public enum StepKind { Walk, Jump, Slide, Push, Teleport, Fall, Snap, Bump }

public enum BlockReason { None, Wall, TooHigh, Occupied, JumpNeedsHeight, JumpBlocked }

public struct Step
{
    public Vector2Int Pos;
    public StepKind Kind;

    public Step(Vector2Int pos, StepKind kind)
    {
        Pos = pos;
        Kind = kind;
    }
}

public class MoveResult
{
    public bool Moved, Died, Won, Toggled;
    public BlockReason Reason;
    public List<Step>[] Paths;
}

public class Board
{
    static readonly int[] PulseCycle = { 0, 1, 2, 1 };
    public const int WallHeight = 99;

    public readonly int Cols, Rows;
    public Vector2Int[] Pieces;
    public bool LiftFlipped;
    public int Turn;
    public int Moves;

    readonly char[,] tiles;
    readonly List<Vector2Int> teleports = new List<Vector2Int>();
    readonly Stack<Snapshot> history = new Stack<Snapshot>();

    struct Snapshot
    {
        public Vector2Int[] Pieces;
        public bool Lift;
        public int Turn, Moves;
    }

    public static readonly Vector2Int[] Directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

    public Board(LevelData level)
    {
        Rows = level.Map.Count;
        foreach (var row in level.Map) Cols = Mathf.Max(Cols, row.Length);
        tiles = new char[Cols, Rows];
        var pieces = new List<Vector2Int>();
        for (int r = 0; r < Rows; r++)
        for (int c = 0; c < Cols; c++)
        {
            char ch = c < level.Map[r].Length ? level.Map[r][c] : ' ';
            var p = new Vector2Int(c, Rows - 1 - r);
            if (ch == 'P')
            {
                pieces.Add(p);
                ch = '.';
            }
            if (ch == 'T') teleports.Add(p);
            tiles[c, p.y] = ch;
        }
        Pieces = pieces.ToArray();
    }

    public bool InBounds(Vector2Int p) => p.x >= 0 && p.y >= 0 && p.x < Cols && p.y < Rows;

    public char At(Vector2Int p) => InBounds(p) ? tiles[p.x, p.y] : ' ';

    public bool Solid(Vector2Int p)
    {
        char c = At(p);
        return c == '#' || c == ' ';
    }

    public int HeightAt(Vector2Int p)
    {
        char c = At(p);
        switch (c)
        {
            case '1': case '2': case '3': return c - '0';
            case 'a': return LiftFlipped ? 2 : 0;
            case 'A': return LiftFlipped ? 0 : 2;
            case 'c': return PulseCycle[Turn % 4];
            case 'C': return PulseCycle[(Turn + 2) % 4];
            case '#': case ' ': return WallHeight;
            default: return 0;
        }
    }

    public bool IsWon
    {
        get
        {
            foreach (var p in Pieces)
                if (At(p) != 'G') return false;
            return true;
        }
    }

    public bool CanUndo => history.Count > 0;

    bool Occupied(Vector2Int p, int self)
    {
        for (int i = 0; i < Pieces.Length; i++)
            if (i != self && Pieces[i] == p) return true;
        return false;
    }

    public BlockReason CheckWalk(int i, Vector2Int d, out Vector2Int target)
    {
        var p = Pieces[i];
        target = p + d;
        if (Solid(target)) return BlockReason.Wall;
        if (HeightAt(target) > HeightAt(p) + 1) return BlockReason.TooHigh;
        if (Occupied(target, i)) return BlockReason.Occupied;
        return BlockReason.None;
    }

    public BlockReason CheckJump(int i, Vector2Int d, out Vector2Int target)
    {
        var p = Pieces[i];
        int h = HeightAt(p);
        var mid = p + d;
        target = p + d * 2;
        if (h < 1) return BlockReason.JumpNeedsHeight;
        if (Solid(mid) || Solid(target) || HeightAt(mid) > h || HeightAt(target) > h) return BlockReason.JumpBlocked;
        if (Occupied(target, i)) return BlockReason.Occupied;
        return BlockReason.None;
    }

    public List<(Vector2Int pos, bool jump)> ValidTargets()
    {
        var result = new List<(Vector2Int, bool)>();
        for (int i = 0; i < Pieces.Length; i++)
        foreach (var d in Directions)
        {
            if (CheckWalk(i, d, out var w) == BlockReason.None) result.Add((w, false));
            if (CheckJump(i, d, out var j) == BlockReason.None) result.Add((j, true));
        }
        return result;
    }

    Snapshot Capture() => new Snapshot { Pieces = (Vector2Int[])Pieces.Clone(), Lift = LiftFlipped, Turn = Turn, Moves = Moves };

    void Restore(Snapshot s)
    {
        Pieces = (Vector2Int[])s.Pieces.Clone();
        LiftFlipped = s.Lift;
        Turn = s.Turn;
        Moves = s.Moves;
    }

    public bool Undo()
    {
        if (history.Count == 0) return false;
        Restore(history.Pop());
        return true;
    }

    public MoveResult Move(Vector2Int d, bool jump)
    {
        var snapshot = Capture();
        var result = new MoveResult { Paths = new List<Step>[Pieces.Length], Reason = BlockReason.None };
        for (int i = 0; i < Pieces.Length; i++) result.Paths[i] = new List<Step>();

        var order = new List<int>();
        for (int i = 0; i < Pieces.Length; i++) order.Add(i);
        order.Sort((a, b) => Dot(Pieces[b], d).CompareTo(Dot(Pieces[a], d)));

        foreach (int i in order)
        {
            var reason = jump ? CheckJump(i, d, out var target) : CheckWalk(i, d, out target);
            if (reason != BlockReason.None)
            {
                if (result.Reason == BlockReason.None) result.Reason = reason;
                result.Paths[i].Add(new Step(Pieces[i] + d, StepKind.Bump));
                continue;
            }
            result.Moved = true;
            Pieces[i] = target;
            result.Paths[i].Add(new Step(target, jump ? StepKind.Jump : StepKind.Walk));
            if (Resolve(i, d, result))
            {
                result.Died = true;
                break;
            }
        }

        if (!result.Moved) return result;

        if (result.Died)
        {
            Restore(snapshot);
            for (int i = 0; i < Pieces.Length; i++) result.Paths[i].Add(new Step(Pieces[i], StepKind.Snap));
            return result;
        }

        history.Push(snapshot);
        Turn++;
        Moves++;
        result.Won = IsWon;
        return result;
    }

    bool Resolve(int i, Vector2Int dir, MoveResult result)
    {
        for (int guard = 0; guard < 64; guard++)
        {
            var q = Pieces[i];
            switch (At(q))
            {
                case 'x':
                    result.Paths[i].Add(new Step(q, StepKind.Fall));
                    return true;
                case 'S':
                    LiftFlipped = !LiftFlipped;
                    result.Toggled = true;
                    return false;
                case 'T':
                    var other = teleports[0] == q ? teleports[1] : teleports[0];
                    if (!Occupied(other, i))
                    {
                        Pieces[i] = other;
                        result.Paths[i].Add(new Step(other, StepKind.Teleport));
                    }
                    return false;
                case 'i':
                {
                    var n = q + dir;
                    if (Solid(n) || HeightAt(n) > HeightAt(q) || Occupied(n, i)) return false;
                    Pieces[i] = n;
                    result.Paths[i].Add(new Step(n, StepKind.Slide));
                    continue;
                }
                case '>': case '<': case '^': case 'v':
                {
                    dir = ArrowDir(At(q));
                    var n = q + dir;
                    if (Solid(n) || HeightAt(n) > HeightAt(q) + 1 || Occupied(n, i)) return false;
                    Pieces[i] = n;
                    result.Paths[i].Add(new Step(n, StepKind.Push));
                    continue;
                }
                default:
                    return false;
            }
        }
        return false;
    }

    public static Vector2Int ArrowDir(char c)
    {
        switch (c)
        {
            case '>': return Vector2Int.right;
            case '<': return Vector2Int.left;
            case '^': return Vector2Int.up;
            default: return Vector2Int.down;
        }
    }

    static int Dot(Vector2Int a, Vector2Int b) => a.x * b.x + a.y * b.y;
}
