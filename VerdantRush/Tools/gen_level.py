"""Builds Leaf-forest style Act 1: collision paths, objects and pre-rendered terrain chunks."""
import json, math, os, random
import numpy as np
from PIL import Image
from scipy import ndimage
import art

OUT = os.path.join(os.path.dirname(__file__), '..', 'Assets', 'Resources')
CHUNK = 512
Y_MIN, Y_MAX = -520, 640
random.seed(7)


class Path:
    def __init__(self, x, y, one_way=False, loop=-1):
        self.pts = [(float(x), float(y))]
        self.one_way = one_way
        self.loop = loop

    @property
    def end(self):
        return self.pts[-1]

    def line(self, dx, dy, step=8):
        x0, y0 = self.end
        n = max(1, int(math.hypot(dx, dy) / step))
        for i in range(1, n + 1):
            t = i / n
            self.pts.append((x0 + dx * t, y0 + dy * t))
        return self

    def to(self, x, y):
        x0, y0 = self.end
        return self.line(x - x0, y - y0)

    def flat(self, w):
        return self.line(w, 0)

    def ease(self, w, dy):
        x0, y0 = self.end
        n = max(2, int(w / 6))
        for i in range(1, n + 1):
            t = i / n
            self.pts.append((x0 + w * t, y0 + dy * (1 - math.cos(math.pi * t)) / 2))
        return self

    def hill(self, w, h):
        x0, y0 = self.end
        n = max(2, int(w / 6))
        for i in range(1, n + 1):
            t = i / n
            self.pts.append((x0 + w * t, y0 + h * (1 - math.cos(2 * math.pi * t)) / 2))
        return self

    def launch(self, w, h):
        """Curve that gets steeper toward the end (ski-jump)."""
        x0, y0 = self.end
        n = max(2, int(w / 6))
        for i in range(1, n + 1):
            t = i / n
            self.pts.append((x0 + w * t, y0 + h * t ** 2.2))
        return self

    def arc(self, cx, cy, r, a0, a1, step=6):
        n = max(2, int(abs(a1 - a0) * r / step))
        for i in range(1, n + 1):
            a = a0 + (a1 - a0) * i / n
            self.pts.append((cx + r * math.cos(a), cy + r * math.sin(a)))
        return self


paths, loops, objects, platforms, decor = [], [], [], [], []


def obj(t, x, y, a=0.0, b=0.0):
    objects.append({'type': t, 'x': round(x, 1), 'y': round(y, 1), 'a': a, 'b': b})


# ---------------------------------------------------------------- geometry
g = Path(0, 620)
g.to(0, 0)
g.flat(420).hill(320, 26).flat(160)                              # 0-900
g.ease(340, -90).flat(260)                                        # 900-1500 down to -90
LOOP1 = (g.end[0] + 330, -90, 100)
g.flat(700)                                                       # loop section to 2200
g.ease(420, 130).flat(280)                                        # up to +40 at 2900
PIT1 = g.end[0]
g.to(PIT1, Y_MIN)
paths.append(g)

g = Path(PIT1 + 190, Y_MIN)
g.to(PIT1 + 190, 40)
for _ in range(3):
    g.hill(380, 64)
g.flat(200)                                                       # rolling hills
WALL = g.end[0]
g.to(WALL, 200)                                                   # step up (spring needed)
g.flat(300).hill(300, 30).flat(260)
g.ease(620, -400).flat(220)                                       # big drop to -200
LOOP2 = (g.end[0] + 260, -200, 112)
g.flat(560)
g.ease(160, 0)
RAMP0 = g.end[0]
g.launch(360, 150)
RAMP1 = g.end[0]
g.to(RAMP1, Y_MIN)
paths.append(g)

g = Path(RAMP1 + 520, Y_MIN)
g.to(RAMP1 + 520, -120)
CHECK2 = g.end[0] + 140
g.flat(400).hill(260, 40).flat(120).hill(260, -40).flat(200)
VALLEY0 = g.end[0]
g.ease(360, -220).flat(260).ease(360, 220).flat(160)              # half-pipe valley
g.ease(260, 60).flat(240).hill(300, 34).flat(360)
GOAL = g.end[0] + 120
g.flat(700)
END = g.end[0]
g.to(END, 620)
paths.append(g)

WIDTH = int(END)

for i, (cx, cy, r) in enumerate([LOOP1, LOOP2]):
    lp = Path(cx, cy, loop=i)
    lp.pts = []
    n = int(2 * math.pi * r / 6)
    for k in range(n + 1):
        a = -math.pi / 2 + 2 * math.pi * k / n
        lp.pts.append((cx + r * math.cos(a), cy + r + r * math.sin(a)))
    paths.append(lp)
    loops.append({'cx': cx, 'cy': cy + r, 'r': r})

# floating platforms (one-way)
def platform(x, y, w):
    p = Path(x, y, one_way=True)
    p.flat(w)
    paths.append(p)
    platforms.append((x, y, w))

platform(PIT1 + 40, 140, 110)
platform(PIT1 - 6, 40, 202)
platform(WALL + 200, 330, 150)
platform(WALL + 450, 420, 150)
platform(WALL + 700, 330, 150)
platform(RAMP1 + 150, 170, 130)
platform(VALLEY0 + 420, -10, 180)

# ---------------------------------------------------------------- helpers
ground_paths = [p for p in paths if p.loop < 0 and not p.one_way]


def ground_y(x):
    best = None
    for p in ground_paths:
        pts = p.pts
        for (x0, y0), (x1, y1) in zip(pts, pts[1:]):
            if x1 > x0 and x0 <= x <= x1:
                y = y0 + (y1 - y0) * (x - x0) / (x1 - x0)
                if best is None or y > best:
                    best = y
    return best if best is not None else Y_MIN


def ring_row(x, n, gap=24, lift=26):
    for i in range(n):
        xx = x + i * gap
        obj('ring', xx, ground_y(xx) + lift)


def ring_arc(x0, y0, x1, y1, h, n):
    for i in range(n):
        t = i / (n - 1)
        obj('ring', x0 + (x1 - x0) * t, y0 + (y1 - y0) * t + h * 4 * t * (1 - t))


# ---------------------------------------------------------------- objects
obj('start', 120, 0)
ring_row(220, 5)
ring_row(460, 5, lift=60)
obj('walker', 760, ground_y(760), 680, 860)
ring_row(1000, 6)
obj('dash', 1380, ground_y(1380), 1)
lx, ly, lr = LOOP1
for k in range(10):
    a = -math.pi / 2 + 2 * math.pi * (k + 1) / 11
    obj('ring', lx + (lr - 26) * math.cos(a), ly + lr + (lr - 26) * math.sin(a))
ring_row(2350, 5)
obj('walker', 2600, ground_y(2600), 2500, 2800)
ring_arc(PIT1 - 20, 80, PIT1 + 210, 80, 80, 7)
obj('spring', PIT1 + 95, 140, 0, 11)
for i in range(3):
    hx = PIT1 + 190 + 380 * i + 190
    ring_row(hx - 36, 4, lift=90)
obj('walker', PIT1 + 190 + 380 + 100, ground_y(PIT1 + 190 + 380 + 100), PIT1 + 190 + 400, PIT1 + 190 + 700)
obj('flyer', PIT1 + 190 + 760 + 190, 200, 0, 60)
obj('spring', WALL - 40, ground_y(WALL - 40), 0, 12)
ring_row(WALL - 50, 1, lift=90)
obj('check', WALL + 90, 200)
for x, y, w in platforms[2:5]:
    ring_row(x + 30, 4, lift=0)
    objects[-4:] = [dict(o, y=y + 22) for o in objects[-4:]]
obj('flyer', WALL + 560, 300, 0, 50)
obj('spikes', WALL + 470, 200, 64)
obj('walker', WALL + 740, ground_y(WALL + 740), WALL + 660, WALL + 840)
obj('spring', WALL + 1010, ground_y(WALL + 1010), -0.785, 12)
ring_row(WALL + 900, 5)
drop_x = WALL + 860
ring_arc(drop_x + 240, 160, drop_x + 520, -60, 30, 6)
obj('dash', drop_x + 700, ground_y(drop_x + 700), 1)
lx, ly, lr = LOOP2
for k in range(12):
    a = -math.pi / 2 + 2 * math.pi * (k + 1) / 13
    obj('ring', lx + (lr - 26) * math.cos(a), ly + lr + (lr - 26) * math.sin(a))
obj('dash', RAMP0 - 60, ground_y(RAMP0 - 60), 1)
obj('ramp', RAMP0, 0, RAMP1)
ring_arc(RAMP1 + 20, 60, RAMP1 + 500, -40, 170, 11)
obj('walker', RAMP1 + 700, ground_y(RAMP1 + 700), RAMP1 + 600, RAMP1 + 880)
obj('check', CHECK2, -120)
ring_row(CHECK2 + 200, 5, lift=40)
obj('spikes', CHECK2 + 820, ground_y(CHECK2 + 820), 48)
obj('flyer', CHECK2 + 980, -10, 0, 40)
obj('dash', VALLEY0 + 460, ground_y(VALLEY0 + 460), 1)
ring_row(VALLEY0 + 380, 7)
ring_row(VALLEY0 + 440, 6, lift=-10 + 0)
objects[-6:] = [dict(o, y=12) for o in objects[-6:]]
obj('walker', VALLEY0 + 1300, ground_y(VALLEY0 + 1300), VALLEY0 + 1200, VALLEY0 + 1450)
obj('spring', VALLEY0 + 1600, ground_y(VALLEY0 + 1600), 0, 10)
ring_row(VALLEY0 + 1580, 3, lift=180)
ring_row(GOAL - 400, 8)
obj('flyer', GOAL - 250, ground_y(GOAL - 250) + 60, 0, 30)
obj('goal', GOAL, ground_y(GOAL))
# ramp/trick region marker is 'ramp'; springs are trickable automatically.

# ---------------------------------------------------------------- render terrain
W, H = WIDTH + 1, Y_MAX - Y_MIN


def to_img(x, y):
    return (x, Y_MAX - y)


SOIL = art.ramp('2a1208', '4a220c', '6e3614', '8f4c1c', 'b0682a', 'd08c3c')
GRASS = art.ramp('0d3d14', '176b1e', '2a9a2a', '4cc234', '8fe04a', 'd8f878')
ROCK = art.ramp('1e1a2a', '3a3450', '5c567a', '8a86a8')

yy, xx = np.mgrid[0:H, 0:W]


def soil_texture(mask, top):
    """Leaf-forest soil: brown checkerboard of beveled blocks, darker with depth."""
    X, Y = xx, yy
    size = 24
    cx = (X // size)
    cy = ((Y + (cx % 2) * 0) // size)
    check = ((cx + cy) % 2).astype(float)
    lx = (X % size) / size
    ly = (Y % size) / size
    bevel = np.where((lx < 0.12) | (ly < 0.12), 0.25, np.where((lx > 0.88) | (ly > 0.88), -0.25, 0))
    depth = np.clip((Y - top[None, :]) / 420.0, 0, 1)
    inten = 0.62 + check * 0.14 + bevel - depth * 0.55
    # small pebbles
    noise = np.random.RandomState(3).rand(H, W)
    inten += (noise > 0.985) * 0.18 - (noise < 0.01) * 0.15
    idx = art.quantize(np.clip(inten, 0, 1), SOIL, dither=0.22)
    return art.ramp_image(idx, SOIL)


def top_profile(mask):
    has = mask.any(axis=0)
    top = np.where(has, mask.argmax(axis=0), H)
    return top


def render_ground(img, polyline):
    pts = [to_img(x, y) for x, y in polyline] + [to_img(polyline[-1][0], Y_MIN - 10), to_img(polyline[0][0], Y_MIN - 10)]
    m = art.polygon_mask(W, H, pts)
    return m


print('rasterising ground ...')
ground_mask = np.zeros((H, W), bool)
for p in ground_paths:
    ground_mask |= render_ground(None, p.pts)

# distance below surface along the normal (approx via EDT from outside)
dist_in = ndimage.distance_transform_edt(ground_mask)
top = top_profile(ground_mask)

img = np.zeros((H, W, 4), np.uint8)

# ---- back decor: big trees behind ground (deterministic)
TRUNK = art.ramp('2a1406', '4e2a10', '784218', 'a0602a')
LEAF = art.ramp('0a2e12', '145020', '1f7a2a', '34a336', '5cc93e', 'a6e85a')
LEAFB = art.ramp('08240e', '0f3d18', '175a20', '237a2a', '3a9a34')


def blob_cluster(img, cx, cy, r, rmp, n=18, spread=1.0, seed=0):
    rs = random.Random(seed)
    x0, x1 = int(cx - r * 2.4), int(cx + r * 2.4)
    y0, y1 = int(cy - r * 2.0), int(cy + r * 1.6)
    x0, y0 = max(0, x0), max(0, y0)
    x1, y1 = min(W, x1), min(H, y1)
    if x1 <= x0 or y1 <= y0:
        return
    sub = img[y0:y1, x0:x1].copy()
    blobs = []
    for i in range(n):
        a = rs.random() * math.pi * 2
        d = rs.random() ** 0.7 * r * spread
        br = r * (0.35 + rs.random() * 0.35)
        blobs.append((cx + math.cos(a) * d * 1.3, cy + math.sin(a) * d * 0.8, br))
    blobs.sort(key=lambda b: b[1] - b[0] * 0.2)
    for bx, by, br in blobs:
        art.ellipsoid(sub, bx - x0, by - y0, br, br * 0.9, rmp, outline=rmp[0], gloss=False)
    img[y0:y1, x0:x1] = sub


def tree(img, x, gy, h, seed):
    ix, iy = to_img(x, gy)
    rs = random.Random(seed)
    w = 14 + rs.randint(0, 6)
    y0 = int(iy - h)
    sub_x0 = int(ix - w)
    trunk = art.canvas(w * 2 + 2, int(h) + 30)
    art.poly(trunk, [(3, 0), (w * 2 - 3, 0), (w * 2 + 1, h + 29), (0, h + 29)], TRUNK, outline=TRUNK[0], grad=(1, 0))
    # bark stripes
    for k in range(6, int(h), 14):
        trunk[k:k + 2, 4:w * 2 - 3][trunk[k:k + 2, 4:w * 2 - 3, 3] > 0] = TRUNK[0]
    th, tw = trunk.shape[:2]
    ys, xs = slice(max(0, y0), min(H, y0 + th)), slice(max(0, sub_x0), min(W, sub_x0 + tw))
    region = img[ys, xs]
    t = trunk[ys.start - y0:ys.stop - y0, xs.start - sub_x0:xs.stop - sub_x0]
    a = t[..., 3] > 0
    region[a] = t[a]
    blob_cluster(img, ix, y0 + 6, 46 + rs.randint(0, 16), LEAF, n=22, seed=seed)


print('decor trees ...')
tx = 260
while tx < WIDTH - 200:
    gy = ground_y(tx)
    if gy > Y_MIN + 40:
        tree(img, tx, gy - 30, 170 + random.randint(0, 90), int(tx))
    tx += random.randint(420, 700)

# ---- ground fill
soil = soil_texture(ground_mask, top)
img[ground_mask] = soil[ground_mask]

# grass band: pixels close to the surface (distance-based so slopes stay even)
band = ground_mask & (dist_in <= 11 + (np.sin(xx * 0.9) > 0.3) * 2)
gd = dist_in
g_int = np.clip(1.0 - gd / 13.0, 0, 1)
g_int = np.where(gd <= 2.2, 1.0, g_int * 0.8 + 0.1)
gidx = art.quantize(g_int, GRASS, dither=0.3)
gimg = art.ramp_image(gidx, GRASS)
img[band] = gimg[band]
# dark underline of grass
under = ground_mask & (dist_in > 11 + (np.sin(xx * 0.9) > 0.3) * 2) & (dist_in <= 13.5 + (np.sin(xx * 0.9) > 0.3) * 2)
img[under] = GRASS[0]

# grass tufts above the surface
print('tufts ...')
rs = np.random.RandomState(11)
outside = ~ground_mask
near = outside & (ndimage.distance_transform_edt(outside) <= 1.5)
tuft = np.zeros_like(ground_mask)
cols = np.nonzero(top < H)[0]
for x in cols[::3]:
    t = top[x]
    hgt = rs.randint(0, 5)
    if hgt > 1 and t - hgt > 0:
        tuft[t - hgt:t, x] = True
        if x + 1 < W:
            tuft[t - max(1, hgt - 2):t, x + 1] = True
tuft &= outside
img[tuft] = GRASS[4]
edge_top = outside & ndimage.binary_dilation(tuft, iterations=1) & ~tuft
img[edge_top & (img[..., 3] == 0)] = GRASS[1]

# ---- loops
print('loops ...')
for L in loops:
    cx, cy = to_img(L['cx'], L['cy'])
    r = L['r']
    x0, x1 = int(cx - r - 40), int(cx + r + 40)
    y0, y1 = int(cy - r - 40), int(cy + r + 40)
    sy, sx = slice(y0, y1), slice(x0, x1)
    d = np.hypot(xx[sy, sx] + 0.5 - cx, yy[sy, sx] + 0.5 - cy)
    ring = (d >= r) & (d < r + 30)
    sub = img[sy, sx]
    ang = np.arctan2(yy[sy, sx] - cy, xx[sy, sx] - cx)
    chk = ((np.floor(ang / (math.pi / 16)) + np.floor((d - r) / 15)) % 2)
    inten = 0.7 + chk * 0.14 - (d - r) / 30 * 0.35 - 0.1 * np.sin(ang)
    sidx = art.quantize(np.clip(inten, 0, 1), SOIL)
    simg = art.ramp_image(sidx, SOIL)
    sub[ring] = simg[ring]
    rim = (d >= r) & (d < r + 8)
    gi = np.clip(1 - (d - r) / 9, 0, 1) * 0.8 + 0.15
    gidx2 = art.quantize(gi, GRASS)
    gimg2 = art.ramp_image(gidx2, GRASS)
    sub[rim] = gimg2[rim]
    sub[(d >= r + 30) & (d < r + 32)] = SOIL[0]
    sub[(d >= r + 8) & (d < r + 9.5)] = GRASS[0]

# ---- floating platforms
print('platforms ...')
for x, y, w in platforms:
    ix, iy = to_img(x, y)
    pw, ph = int(w) + 8, 60
    pc = art.canvas(pw, ph)
    pts = [(4, 0), (pw - 4, 0), (pw - 14, 22), (pw * 0.65, 44), (pw * 0.35, 50), (14, 24)]
    art.poly(pc, pts, SOIL, outline=SOIL[0], grad=(0.2, 1))
    for k in range(2, 12):
        t = min(1, k / 11)
        col = GRASS[5] if k < 4 else GRASS[3] if k < 8 else GRASS[1]
        pc[k - 2 + 0:k - 1, 4:pw - 4] = col
    pc[10:12, 4:pw - 4] = GRASS[0]
    for k in range(4, pw - 4, 3):
        hgt = random.randint(0, 3)
        pc[max(0, 2 - hgt):2, k] = GRASS[4]
    ox, oy = int(ix - 4), int(iy - 2)
    reg = img[oy:oy + ph, ox:ox + pw]
    a = pc[:reg.shape[0], :reg.shape[1], 3] > 0
    reg[a] = pc[:reg.shape[0], :reg.shape[1]][a]

# ---- front decor: flowers and bushes sitting on the grass
FLOW = art.ramp('7a1a08', 'c83c10', 'f07818', 'ffc030', 'fff08a')
PINK = art.ramp('5a0c3a', 'a8246a', 'e04c9a', 'ff8cc8', 'ffd0ec')
BUSH = art.ramp('08300f', '12521b', '1f7a26', '37a332', '6cd444')
print('flowers ...')
fx = 60
while fx < WIDTH - 60:
    gy = ground_y(fx)
    if gy > Y_MIN + 50:
        ix, iy = to_img(fx, gy)
        iy = int(iy) + 3
        kind = random.random()
        if kind < 0.45:  # sunflower
            hgt = random.randint(16, 30)
            s = img[iy - hgt - 12:iy + 2, int(ix) - 10:int(ix) + 10]
            if s.shape[0] > 0 and s.shape[1] == 20:
                c = art.canvas(20, hgt + 14)
                art.line(c, (10, 10), (10, hgt + 12), 2, GRASS[1])
                art.ellipsoid(c, 6, hgt * 0.7, 3, 1.6, GRASS, gloss=False)
                rmp = FLOW if random.random() < 0.6 else PINK
                for k in range(8):
                    a = k * math.pi / 4
                    art.ellipsoid(c, 10 + 4.5 * math.cos(a), 8 + 4.5 * math.sin(a), 2.6, 2.6, rmp, gloss=False)
                art.ellipsoid(c, 10, 8, 3, 3, art.ramp('3a1a06', '6a3410', '9a5a1c'), gloss=False)
                art.outline(c, (20, 30, 12, 255))
                a = c[..., 3] > 0
                s[a[:s.shape[0]]] = c[:s.shape[0]][a[:s.shape[0]]]
        elif kind < 0.75:
            bx0, by0 = int(ix) - 26, iy - 30
            s = img[by0:by0 + 34, bx0:bx0 + 52]
            if s.shape == (34, 52, 4):
                c = art.canvas(52, 34)
                for k in range(5):
                    art.ellipsoid(c, 10 + k * 8 + random.random() * 3, 20 - (k % 2) * 5, 9, 8, BUSH, outline=BUSH[0], gloss=False)
                a = c[..., 3] > 0
                c[27:, :][c[27:, :, 3] > 0] = BUSH[0]
                s[a] = c[a]
    fx += random.randint(70, 190)

# ---------------------------------------------------------------- export
print('export chunks ...')
tdir = os.path.join(OUT, 'Terrain')
for f in os.listdir(tdir):
    if f.endswith('.png'):
        os.remove(os.path.join(tdir, f))
chunks = []
for cy in range(0, H, CHUNK):
    for cx in range(0, W, CHUNK):
        sub = img[cy:cy + CHUNK, cx:cx + CHUNK]
        if sub[..., 3].max() == 0:
            continue
        name = f't_{cx // CHUNK}_{cy // CHUNK}'
        Image.fromarray(np.ascontiguousarray(sub), 'RGBA').save(os.path.join(tdir, name + '.png'), optimize=True)
        chunks.append({'file': name, 'x': cx, 'y': Y_MAX - cy - sub.shape[0], 'w': sub.shape[1], 'h': sub.shape[0]})

level = {
    'width': WIDTH, 'yMin': Y_MIN, 'yMax': Y_MAX,
    'chunks': chunks,
    'paths': [{'pts': [round(v, 2) for p in pa.pts for v in p], 'oneWay': pa.one_way, 'loop': pa.loop} for pa in paths],
    'loops': loops,
    'objects': objects,
}
with open(os.path.join(OUT, 'level1.json'), 'w') as f:
    json.dump(level, f)
Image.fromarray(img[::4, ::4]).save(os.path.join(os.path.dirname(__file__), 'preview_level.png'))
print('chunks', len(chunks), 'objects', len(objects), 'width', WIDTH)
