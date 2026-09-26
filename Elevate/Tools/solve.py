"""BFS solver mirroring Board.cs rules. Usage: python3 solve.py [levels.txt]"""
import sys
from collections import deque

DIRS = {'U': (0, 1), 'D': (0, -1), 'L': (-1, 0), 'R': (1, 0)}
ARROWS = {'>': (1, 0), '<': (-1, 0), '^': (0, 1), 'v': (0, -1)}
CYCLE = [0, 1, 2, 1]


def parse(path):
    levels, cur, in_map = [], None, False
    for raw in open(path):
        line = raw.rstrip('\n')
        if in_map:
            if line.strip() == 'end':
                in_map = False
                levels.append(cur)
            else:
                cur['map'].append(line)
            continue
        if line.startswith('level:'):
            cur = {'title': line[6:].strip(), 'map': [], 'par': 0}
        elif line.startswith('par:'):
            cur['par'] = int(line[4:])
        elif line.strip() == 'map:':
            in_map = True
    return levels


class Board:
    def __init__(self, rows):
        self.h = len(rows)
        self.w = max(len(r) for r in rows)
        self.t = {}
        self.pieces = []
        self.teleports = []
        for r, row in enumerate(rows):
            for c in range(self.w):
                ch = row[c] if c < len(row) else ' '
                p = (c, self.h - 1 - r)
                if ch == 'P':
                    self.pieces.append(p)
                    ch = '.'
                if ch == 'T':
                    self.teleports.append(p)
                self.t[p] = ch

    def height(self, p, lift, turn):
        ch = self.t.get(p, ' ')
        if ch in '123':
            return int(ch)
        if ch == 'a':
            return 2 if lift else 0
        if ch == 'A':
            return 0 if lift else 2
        if ch == 'c':
            return CYCLE[turn % 4]
        if ch == 'C':
            return CYCLE[(turn + 2) % 4]
        return 0

    def solid(self, p):
        return self.t.get(p, ' ') in '# '

    def move(self, state, d, jump):
        pos, lift, turn = list(state[0]), state[1], state[2]
        dx, dy = DIRS[d]
        order = sorted(range(len(pos)), key=lambda i: -(pos[i][0] * dx + pos[i][1] * dy))
        moved = False
        occ = lambda q, i: any(pos[j] == q for j in range(len(pos)) if j != i)
        H = lambda q: self.height(q, lift, turn)
        for i in order:
            p = pos[i]
            hc = H(p)
            if jump:
                m = (p[0] + dx, p[1] + dy)
                l = (p[0] + 2 * dx, p[1] + 2 * dy)
                if hc < 1 or self.solid(m) or self.solid(l) or H(m) > hc or H(l) > hc or occ(l, i):
                    continue
                pos[i] = l
            else:
                n = (p[0] + dx, p[1] + dy)
                if self.solid(n) or H(n) > hc + 1 or occ(n, i):
                    continue
                pos[i] = n
            moved = True
            cd = (dx, dy)
            for _ in range(64):
                q = pos[i]
                ch = self.t[q]
                if ch == 'x':
                    return 'dead'
                if ch == 'S':
                    lift = not lift
                    break
                if ch == 'T':
                    other = self.teleports[1] if q == self.teleports[0] else self.teleports[0]
                    if not occ(other, i):
                        pos[i] = other
                    break
                if ch == 'i':
                    n = (q[0] + cd[0], q[1] + cd[1])
                    if not self.solid(n) and H(n) <= H(q) and not occ(n, i):
                        pos[i] = n
                        continue
                    break
                if ch in ARROWS:
                    cd = ARROWS[ch]
                    n = (q[0] + cd[0], q[1] + cd[1])
                    if not self.solid(n) and H(n) <= H(q) + 1 and not occ(n, i):
                        pos[i] = n
                        continue
                    break
                break
        if not moved:
            return None
        return (tuple(pos), lift, (turn + 1) % 4)

    def won(self, state):
        return all(self.t[p] == 'G' for p in state[0])

    def solve(self):
        start = (tuple(self.pieces), False, 0)
        prev = {start: None}
        dq = deque([start])
        while dq:
            s = dq.popleft()
            if self.won(s):
                path = []
                while prev[s]:
                    s, mv = prev[s]
                    path.append(mv)
                return path[::-1]
            for d in 'UDLR':
                for jump in (False, True):
                    n = self.move(s, d, jump)
                    if n and n != 'dead' and n not in prev:
                        prev[n] = (s, ('J' if jump else '') + d)
                        dq.append(n)
        return None


if __name__ == '__main__':
    path = sys.argv[1] if len(sys.argv) > 1 else 'Assets/Resources/levels.txt'
    ok = True
    for i, lv in enumerate(parse(path), 1):
        b = Board(lv['map'])
        sol = b.solve()
        nojump = None
        if sol is None:
            ok = False
        print(f"{i:2} {lv['title']:<18} par={lv['par']:<3} optimal={len(sol) if sol else 'UNSOLVABLE'} {' '.join(sol) if sol else ''}")
    sys.exit(0 if ok else 1)
