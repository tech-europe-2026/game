"""Character, enemies, objects, effects and HUD font sprite sheets."""
import math, os, random
import numpy as np
from PIL import Image, ImageDraw, ImageFont
import art

OUT = os.path.join(os.path.dirname(__file__), '..', 'Assets', 'Resources', 'Sprites')
os.makedirs(OUT, exist_ok=True)
OL = (22, 10, 30, 255)

RED = art.ramp('5a0a18', '9a1428', 'd42a3a', 'f25a4a', 'ff9a7a')
REDD = art.ramp('3a0612', '6a0e1e', '9a1a2c', 'c42e3a')
TAN = art.ramp('8a4a26', 'c47e4a', 'eab07a', 'ffe0b8')
WHITE = art.ramp('7a8098', 'b8c0d8', 'e8eefa', 'ffffff')
SHOE = art.ramp('4a3408', '9a7010', 'e8b818', 'fff070')
GOG = art.ramp('0a3a3a', '107a6a', '28c0a0', '9af8e0')
METAL = art.ramp('20243a', '464e6e', '7a86a8', 'c0cae0')
GOLD = art.ramp('6a3a00', 'b07000', 'f0b800', 'fff060', 'ffffff')
BLUE = art.ramp('0a1a5a', '1a3aa8', '3a74e8', '8ab8ff')
YEL = art.ramp('6a4a00', 'b08a00', 'f0d020', 'fff890')

FW = FH = 56


def kit(pose, t=0.0):
    """Draw Kit, a crimson hedgehog with teal goggles, facing right. Feet at y=50."""
    c = art.canvas(FW, FH)
    foot_y = 50
    lean = {'idle': 0, 'walk': 3, 'run': 6, 'fast': 8, 'skid': -5, 'spring': 0, 'hurt': -4, 'trick': 0, 'look': 0, 'crouch': 0}[pose]
    hx, hy = 29 + lean, 17
    bx, by = 26 + lean * 0.5, 32
    if pose == 'crouch':
        hy, by = 25, 38
    if pose == 'spring':
        hy, by = 13, 28
    hipx, hipy = bx - 1, by + 6

    def leg(ph, back):
        col = REDD if back else RED
        shoe = art.ramp(*['%02x%02x%02x' % tuple(int(v * (0.75 if back else 1)) for v in s[:3]) for s in SHOE])
        if pose in ('idle', 'look'):
            fx, fy = hipx + (4 if back else -2), foot_y
        elif pose == 'crouch':
            fx, fy = hipx + (6 if back else -3), foot_y
        elif pose == 'spring':
            fx, fy = hipx + (2 if back else -1), foot_y + 2
        elif pose == 'skid':
            fx, fy = hipx + (10 if not back else 2), foot_y - (0 if not back else 2)
        elif pose == 'hurt':
            fx, fy = hipx + (-8 if back else 6), foot_y - 6
        elif pose == 'trick':
            fx, fy = hipx + (-9 if back else 9), foot_y - 4
        else:
            stride = {'walk': 8, 'run': 11, 'fast': 12}[pose]
            fx = hipx + stride * math.sin(ph)
            fy = foot_y - max(0, math.cos(ph)) * (6 if pose == 'walk' else 9)
        kx = (hipx + fx) / 2 + (3 if pose not in ('hurt', 'trick') else 0)
        ky = (hipy + fy) / 2
        art.line(c, (hipx, hipy), (kx, ky), 4, OL)
        art.line(c, (kx, ky), (fx, fy - 2), 4, OL)
        art.line(c, (hipx, hipy), (kx, ky), 2, col[2])
        art.line(c, (kx, ky), (fx, fy - 2), 2, col[2])
        art.ellipsoid(c, fx + 2.5, fy - 1.5, 5.5, 3.2, shoe, outline=OL)
        c[int(fy):int(fy) + 1, int(fx - 2):int(fx + 7)][c[int(fy):int(fy) + 1, int(fx - 2):int(fx + 7), 3] > 0] = art.hexc('f4f4f4')

    def arm(ph, back):
        sx, sy = bx + 1, by - 4
        if pose in ('idle', 'look', 'crouch'):
            hx2, hy2 = sx + (3 if back else -1), sy + 11
        elif pose == 'spring':
            hx2, hy2 = sx + (5 if back else -3), sy - 16
        elif pose == 'skid':
            hx2, hy2 = sx - 10, sy - 2 - (3 if back else 0)
        elif pose == 'hurt':
            hx2, hy2 = sx + (8 if back else -9), sy - 10
        elif pose == 'trick':
            hx2, hy2 = sx + (12 if back else -12), sy - 9
        elif pose == 'fast':
            hx2, hy2 = sx - 9 - (2 if back else 0), sy + 5
        else:
            sw = 7 if pose == 'walk' else 9
            hx2, hy2 = sx - sw * math.sin(ph), sy + 8 - abs(math.cos(ph)) * 2
        col = REDD if back else RED
        art.line(c, (sx, sy), (hx2, hy2), 4, OL)
        art.line(c, (sx, sy), (hx2, hy2), 2, col[2])
        art.ellipsoid(c, hx2, hy2, 3.2, 3.2, WHITE, outline=OL)

    fast = pose == 'fast'
    # back limbs
    if fast:
        for k in range(6):
            a = t + k * math.pi / 3
            fx, fy = hipx + 9 * math.sin(a), foot_y - 6 - 5 * math.cos(a)
            art.ellipsoid(c, fx + 2, fy, 5, 3, art.ramp('9a7010', 'e8b818', 'fff070'), gloss=False)
        blur = (c[..., 3] > 0)
        c[blur & (np.random.RandomState(1).rand(FH, FW) > 0.55)] = art.hexc('f0c030')
    else:
        leg(t + math.pi, True)
    arm(t, True)
    # quills (behind head/body)
    q = []
    base = [(-14, -9), (-17, -1), (-15, 7), (-9, 13)] if pose != 'spring' else [(-10, -14), (-15, -6), (-15, 3), (-10, 10)]
    if pose in ('run', 'fast', 'walk'):
        base = [(x - 3, y + 2) for x, y in base]
    for i, (qx, qy) in enumerate(base):
        tip = (hx + qx - 4, hy + qy + (0 if i < 3 else 2))
        root1 = (hx - 1, hy - 10 + i * 5)
        root2 = (hx + 2, hy + 0 + i * 5)
        art.poly(c, [root1, tip, root2], RED, outline=OL, grad=(1, 1))
    # torso
    art.ellipsoid(c, bx, by, 7, 8, RED, outline=OL)
    art.ellipsoid(c, bx + 3, by + 1, 3.6, 5.5, TAN, gloss=False)
    # head
    art.ellipsoid(c, hx, hy, 10, 9.5, RED, outline=OL)
    # ear
    art.poly(c, [(hx - 5, hy - 7), (hx - 3, hy - 15), (hx + 2, hy - 8)], RED, outline=OL)
    # goggles band + lens
    band_y = hy - 5
    art.line(c, (hx - 8, band_y + 1), (hx + 7, band_y - 1), 3, OL)
    art.line(c, (hx - 8, band_y + 1), (hx + 7, band_y - 1), 1, GOG[1])
    art.ellipsoid(c, hx + 4, band_y - 2, 3.4, 3, GOG, outline=OL)
    # muzzle
    art.ellipsoid(c, hx + 6, hy + 4, 5, 4, TAN, outline=OL, gloss=False)
    c[int(hy + 1):int(hy + 3), int(hx + 10):int(hx + 12)] = OL
    # eye
    ex, ey = hx + 4, hy - 0.5
    if pose == 'hurt':
        art.line(c, (ex - 2, ey - 2), (ex + 2, ey + 2), 1, OL)
        art.line(c, (ex - 2, ey + 2), (ex + 2, ey - 2), 1, OL)
    else:
        art.ellipsoid(c, ex, ey, 2.6, 3.4, WHITE, outline=OL, gloss=False)
        py = ey - (1 if pose == 'look' else 0)
        c[int(py - 1):int(py + 2), int(ex + 0.5):int(ex + 2.5)] = art.hexc('1a8a4a')
        c[int(py - 1), int(ex + 1)] = art.hexc('ffffff')
    c[int(hy + 7), int(hx + 5):int(hx + 9)] = OL  # smile
    # front limbs
    if not fast:
        leg(t, False)
    else:
        for k in range(6):
            a = t + k * math.pi / 3 + 0.5
            fx, fy = hipx + 10 * math.sin(a), foot_y - 5 - 5 * math.cos(a)
            art.ellipsoid(c, fx + 3, fy, 5, 3, SHOE, outline=OL, gloss=False)
    arm(t + math.pi, False)
    art.outline(c, OL)
    return c


def ball(t, squash=0.0):
    c = art.canvas(FW, FH)
    cx, cy, r = 28, 36, 13
    ry = r * (1 - squash)
    cy = 50 - ry - 1
    art.ellipsoid(c, cx, cy, r, ry, RED, outline=OL)
    yy, xx = np.mgrid[0:FH, 0:FW]
    ang = np.arctan2(yy - cy, xx - cx)
    d = np.hypot((xx - cx) / r, (yy - cy) / ry)
    stripes = (np.sin(ang * 3 + t) > 0.55) & (d < 0.95) & (d > 0.35)
    c[stripes & (c[..., 3] > 0)] = REDD[1]
    # goggle flash rotating
    gx, gy = cx + math.cos(t) * r * 0.55, cy + math.sin(t) * ry * 0.55
    art.ellipsoid(c, gx, gy, 2.4, 2.4, GOG, gloss=False)
    art.outline(c, OL)
    return c


def dash_ball(t):
    c = ball(t, squash=0.18)
    # dust puffs behind
    for k in range(3):
        art.ellipsoid(c, 8 + k * 5, 48 - k * 2, 3 + k, 2.5 + k * 0.6, WHITE, gloss=False)
    return c


print('kit ...')
frames = []
frames += [kit('idle', 0), kit('idle', 0)]              # 0,1
frames[1][14:17, 33:36][frames[1][14:17, 33:36, 3] > 0] = RED[1]  # blink
frames += [kit('walk', i * math.pi / 4) for i in range(8)]  # 2-9
frames += [kit('run', i * math.pi / 3) for i in range(6)]   # 10-15
frames += [kit('fast', i * math.pi / 2) for i in range(4)]  # 16-19
frames += [ball(i * math.pi / 4) for i in range(8)]         # 20-27
frames += [dash_ball(i * math.pi / 2) for i in range(4)]    # 28-31
frames += [kit('skid'), kit('spring'), kit('hurt'), kit('trick'), kit('look'), kit('crouch')]  # 32-37
art.save(art.sheet(frames, 8), os.path.join(OUT, 'kit.png'))

# life icon
icon = kit('idle')[4:28, 16:44].copy()
art.save(icon, os.path.join(OUT, 'kit_icon.png'))

# ---------------------------------------------------------------- enemies (32x32)
print('enemies ...')
SHELL = art.ramp('4a0808', '9a1010', 'e02020', 'ff6a4a', 'ffc0a0')


def beetle(ph):
    c = art.canvas(40, 32)
    for wx in (10, 30):
        art.ellipsoid(c, wx, 26, 4.5, 4.5, METAL, outline=OL)
        a = ph + wx
        c[int(26 + 2 * math.sin(a)), int(wx + 2 * math.cos(a))] = OL
    art.ellipsoid(c, 20, 20, 13, 5, METAL, outline=OL, gloss=False)
    art.ellipsoid(c, 18, 15, 13, 10, SHELL, outline=OL)
    for sx, sy, sr in [(12, 12, 2.6), (19, 9, 2.2), (22, 17, 2.6), (13, 19, 2)]:
        art.ellipsoid(c, sx, sy, sr, sr, art.ramp('100814', '2a1a30'), gloss=False)
    c[5:24, 18:19][c[5:24, 18:19, 3] > 0] = OL
    art.ellipsoid(c, 32, 17, 5.5, 5, METAL, outline=OL)
    art.ellipsoid(c, 34, 16, 2.4, 2.4, art.ramp('7a3a00', 'ffb000', 'fff4a0'), gloss=False)
    art.line(c, (31, 12), (34 + math.sin(ph) * 2, 4), 1, OL)
    c[3:5, int(33 + math.sin(ph) * 2):int(36 + math.sin(ph) * 2)] = art.hexc('ff4040')
    art.outline(c, OL)
    return c


def wasp(ph):
    c = art.canvas(40, 32)
    wing = art.ramp('6a8aa8', 'a8d0f0', 'e0f4ff')
    wy = 6 + 3 * math.sin(ph)
    art.ellipsoid(c, 16, wy, 7, 3.5, wing, outline=OL, gloss=False)
    art.ellipsoid(c, 22, wy + 1, 6, 3, wing, outline=OL, gloss=False)
    art.ellipsoid(c, 14, 17, 10, 6.5, YEL, outline=OL)
    for k in (9, 14, 19):
        c[11:24, k:k + 2][c[11:24, k:k + 2, 3] > 0] = art.hexc('1a1420')
    art.poly(c, [(4, 17), (0, 24), (7, 21)], METAL, outline=OL)
    art.ellipsoid(c, 27, 14, 6.5, 6, METAL, outline=OL)
    art.ellipsoid(c, 29, 13, 2.8, 2.8, art.ramp('600000', 'ff2020', 'ffa0a0'), gloss=False)
    art.outline(c, OL)
    return c


art.save(art.sheet([beetle(0), beetle(1.6), beetle(3.2), beetle(4.8), wasp(0), wasp(2), wasp(4), wasp(1)], 4), os.path.join(OUT, 'enemies.png'))

# ---------------------------------------------------------------- rings (16x16)
print('objects ...')
rings = []
for wfac in (1.0, 0.72, 0.4, 0.16, 0.4, 0.72):
    c = art.canvas(16, 16)
    rx = max(1.2, 7 * wfac)
    art.ellipsoid(c, 8, 8, rx, 7, GOLD, outline=OL, flat=0.2)
    if rx > 3:
        art.ellipsoid(c, 8, 8, rx * 0.52, 3.8, art.ramp('000000'), gloss=False)
        yy, xx = np.mgrid[0:16, 0:16]
        hole = ((xx + 0.5 - 8) / (rx * 0.52)) ** 2 + ((yy + 0.5 - 8) / 3.8) ** 2 <= 1
        c[hole] = 0
        ring_in = art.dilate(hole) & ~hole
        c[ring_in] = OL
    rings.append(c)
spark = []
for k in range(4):
    c = art.canvas(16, 16)
    s = [2, 5, 7, 4][k]
    art.line(c, (8 - s, 8), (8 + s, 8), 1, art.hexc('ffffff'))
    art.line(c, (8, 8 - s), (8, 8 + s), 1, art.hexc('ffffff'))
    if k in (1, 2):
        art.line(c, (8 - s * 0.5, 8 - s * 0.5), (8 + s * 0.5, 8 + s * 0.5), 1, art.hexc('fff060'))
        art.line(c, (8 - s * 0.5, 8 + s * 0.5), (8 + s * 0.5, 8 - s * 0.5), 1, art.hexc('fff060'))
    spark.append(c)
art.save(art.sheet(rings + spark, 10), os.path.join(OUT, 'rings.png'))

# springs (32x32): yellow, compressed/extended
def spring(ext, rmp):
    c = art.canvas(32, 32)
    top = 14 if ext else 22
    for yy_ in range(top + 4, 29, 3):
        art.line(c, (8, yy_), (24, yy_ + 1), 2, METAL[2])
    art.poly(c, [(4, 28), (28, 28), (28, 31), (4, 31)], METAL, outline=OL)
    art.ellipsoid(c, 16, top + 2, 13, 4, rmp, outline=OL)
    art.outline(c, OL)
    return c


art.save(art.sheet([spring(False, YEL), spring(True, YEL), spring(False, RED), spring(True, RED)], 4), os.path.join(OUT, 'springs.png'))

# dash panel (40x12), 2 frames
dash = []
for k in range(2):
    c = art.canvas(40, 12)
    art.poly(c, [(0, 3), (40, 3), (38, 11), (2, 11)], METAL, outline=OL, grad=(0, 1))
    for i in range(3):
        x0 = 6 + i * 11
        col = art.hexc('ffe030') if (i + k) % 2 == 0 else art.hexc('ff8a10')
        pts = [(x0, 4), (x0 + 5, 4), (x0 + 10, 7), (x0 + 5, 10), (x0, 10), (x0 + 5, 7)]
        m = art.polygon_mask(40, 12, pts)
        c[m] = col
    art.outline(c, OL)
    dash.append(c)
art.save(art.sheet(dash, 2), os.path.join(OUT, 'dash.png'))

# spikes (16x16)
c = art.canvas(16, 16)
for sx in (0, 8):
    art.poly(c, [(sx, 15), (sx + 4, 1), (sx + 8, 15)], METAL, outline=OL, grad=(1, 0))
art.poly(c, [(0, 13), (16, 13), (16, 16), (0, 16)], METAL, grad=(0, 1))
art.save(c, os.path.join(OUT, 'spikes.png'))

# checkpoint (16x48): 2 frames (off=blue, on=red)
cps = []
for on in (False, True):
    c = art.canvas(16, 48)
    art.poly(c, [(6, 10), (10, 10), (10, 46), (6, 46)], METAL, outline=OL, grad=(1, 0))
    art.poly(c, [(3, 44), (13, 44), (13, 48), (3, 48)], METAL, outline=OL)
    art.ellipsoid(c, 8, 7, 5.5, 5.5, RED if on else BLUE, outline=OL)
    art.outline(c, OL)
    cps.append(c)
art.save(art.sheet(cps, 2), os.path.join(OUT, 'check.png'))

# goal signpost (48x56): face (enemy mark), turning, edge, back (Kit head)
def sign(kind, w):
    c = art.canvas(48, 56)
    art.poly(c, [(22, 30), (26, 30), (26, 54), (22, 54)], METAL, outline=OL, grad=(1, 0))
    hw = max(1.5, 17 * w)
    art.poly(c, [(24 - hw, 2), (24 + hw, 2), (24 + hw, 32), (24 - hw, 32)], art.ramp('8a90a8', 'd0d6e8', 'ffffff'), outline=OL, grad=(1, 0))
    if w > 0.5:
        if kind == 'face':
            art.ellipsoid(c, 24, 17, 10 * w, 10, SHELL, outline=OL)
            c[14:17, int(20):int(28)] = OL
        else:
            head = kit('idle')[5:30, 18:44]
            hh, hw2 = head.shape[:2]
            sx = int(24 - hw2 / 2)
            reg = c[5:5 + hh, sx:sx + hw2]
            a = head[..., 3] > 0
            reg[a] = head[a]
    art.outline(c, OL)
    return c


art.save(art.sheet([sign('face', 1), sign('face', 0.55), sign('face', 0.1), sign('back', 0.55), sign('back', 1)], 5), os.path.join(OUT, 'goal.png'))

# explosion (32x32) x6 frames, dust (16x16) x4
boom = []
for k in range(6):
    c = art.canvas(32, 32)
    rs = random.Random(k)
    fire = art.ramp('7a1a00', 'e04000', 'ff9a10', 'fff070', 'ffffff')
    smoke = art.ramp('3a3448', '6a6480', 'a8a4c0', 'e0e0f0')
    r = 5 + k * 2.2
    for i in range(6):
        a = i * math.pi / 3 + rs.random()
        d = r * 0.55
        rr = max(1, (r * 0.55) * (1 - k / 7))
        art.ellipsoid(c, 16 + math.cos(a) * d, 16 + math.sin(a) * d, rr, rr, fire if k < 3 else smoke, outline=OL if k < 4 else None, gloss=False)
    boom.append(c)
art.save(art.sheet(boom, 6), os.path.join(OUT, 'boom.png'))
dust = []
for k in range(4):
    c = art.canvas(16, 16)
    rr = 2 + k * 1.4
    art.ellipsoid(c, 8, 10 - k, rr, rr * 0.8, art.ramp('b8a890', 'e8dcc8', 'ffffff'), gloss=False)
    if k == 3:
        c[np.random.RandomState(2).rand(16, 16) > 0.5] = 0
    dust.append(c)
art.save(art.sheet(dust, 4), os.path.join(OUT, 'dust.png'))

# ---------------------------------------------------------------- font
print('font ...')
CHARS = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ:.!'-/x "
FONT = '/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf'


GLYPHS = {
    '0': ["01110", "10011", "10101", "10101", "11001", "10001", "01110"],
    '1': ["00100", "01100", "00100", "00100", "00100", "00100", "01110"],
    '2': ["01110", "10001", "00001", "00110", "01000", "10000", "11111"],
    '3': ["11110", "00001", "00001", "01110", "00001", "00001", "11110"],
    '4': ["00110", "01010", "10010", "11111", "00010", "00010", "00010"],
    '5': ["11111", "10000", "11110", "00001", "00001", "10001", "01110"],
    '6': ["00110", "01000", "10000", "11110", "10001", "10001", "01110"],
    '7': ["11111", "00001", "00010", "00100", "01000", "01000", "01000"],
    '8': ["01110", "10001", "10001", "01110", "10001", "10001", "01110"],
    '9': ["01110", "10001", "10001", "01111", "00001", "00010", "01100"],
    'A': ["01110", "10001", "10001", "11111", "10001", "10001", "10001"],
    'B': ["11110", "10001", "10001", "11110", "10001", "10001", "11110"],
    'C': ["01110", "10001", "10000", "10000", "10000", "10001", "01110"],
    'D': ["11110", "10001", "10001", "10001", "10001", "10001", "11110"],
    'E': ["11111", "10000", "10000", "11110", "10000", "10000", "11111"],
    'F': ["11111", "10000", "10000", "11110", "10000", "10000", "10000"],
    'G': ["01110", "10001", "10000", "10111", "10001", "10001", "01111"],
    'H': ["10001", "10001", "10001", "11111", "10001", "10001", "10001"],
    'I': ["01110", "00100", "00100", "00100", "00100", "00100", "01110"],
    'J': ["00111", "00010", "00010", "00010", "00010", "10010", "01100"],
    'K': ["10001", "10010", "10100", "11000", "10100", "10010", "10001"],
    'L': ["10000", "10000", "10000", "10000", "10000", "10000", "11111"],
    'M': ["10001", "11011", "10101", "10101", "10001", "10001", "10001"],
    'N': ["10001", "11001", "10101", "10011", "10001", "10001", "10001"],
    'O': ["01110", "10001", "10001", "10001", "10001", "10001", "01110"],
    'P': ["11110", "10001", "10001", "11110", "10000", "10000", "10000"],
    'Q': ["01110", "10001", "10001", "10001", "10101", "10010", "01101"],
    'R': ["11110", "10001", "10001", "11110", "10100", "10010", "10001"],
    'S': ["01111", "10000", "10000", "01110", "00001", "00001", "11110"],
    'T': ["11111", "00100", "00100", "00100", "00100", "00100", "00100"],
    'U': ["10001", "10001", "10001", "10001", "10001", "10001", "01110"],
    'V': ["10001", "10001", "10001", "10001", "10001", "01010", "00100"],
    'W': ["10001", "10001", "10001", "10101", "10101", "10101", "01010"],
    'X': ["10001", "10001", "01010", "00100", "01010", "10001", "10001"],
    'Y': ["10001", "10001", "01010", "00100", "00100", "00100", "00100"],
    'Z': ["11111", "00001", "00010", "00100", "01000", "10000", "11111"],
    ':': ["00000", "01100", "01100", "00000", "01100", "01100", "00000"],
    '.': ["00000", "00000", "00000", "00000", "00000", "01100", "01100"],
    '!': ["00100", "00100", "00100", "00100", "00100", "00000", "00100"],
    "'": ["00100", "00100", "01000", "00000", "00000", "00000", "00000"],
    '-': ["00000", "00000", "00000", "11111", "00000", "00000", "00000"],
    '/': ["00001", "00010", "00010", "00100", "01000", "01000", "10000"],
    'x': ["00000", "00000", "10001", "01010", "00100", "01010", "10001"],
    ' ': ["00000"] * 7,
}


def font_sheet(scale, top, bottom, name):
    """5x7 pixel glyphs (optionally doubled) with gradient fill, outline and drop shadow."""
    gw, gh = 5 * scale, 7 * scale
    cw, ch = gw + 4, gh + 4
    cells = []
    for chh in CHARS:
        m = np.zeros((ch, cw), bool)
        g = np.array([[c == '1' for c in row] for row in GLYPHS[chh]])
        g = np.kron(g, np.ones((scale, scale), bool))
        m[1:1 + gh, 1:1 + gw] = g
        c = art.canvas(cw, ch)
        t = np.clip((np.arange(ch)[:, None] - 1) / max(1, gh - 1), 0, 1) * np.ones((1, cw))
        cols = np.array([art.hexc(top), art.hexc(bottom)], float)
        grad = (cols[0] * (1 - t[..., None]) + cols[1] * t[..., None]).astype(np.uint8)
        c[m] = grad[m]
        o = art.dilate(m, 1) & ~m
        c[o] = OL
        sh = np.zeros_like(m)
        sh[1:, 1:] = (m | o)[:-1, :-1]
        c[sh & (c[..., 3] == 0)] = (10, 6, 20, 255)
        cells.append(c)
    art.save(art.sheet(cells, len(CHARS)), os.path.join(OUT, name + '.png'))


font_sheet(1, 'ffffff', 'b8d0ff', 'font_small')
font_sheet(1, 'fff890', 'ffa010', 'font_gold')
font_sheet(2, 'ffffff', 'ffd040', 'font_big')
with open(os.path.join(OUT, 'font_chars.txt'), 'w') as fh:
    fh.write(CHARS)

# title logo
print('logo ...')
LW, LH = 360, 110
im = Image.new('L', (LW, LH), 0)
d = ImageDraw.Draw(im)
d.fontmode = '1'
fb = ImageFont.truetype('/usr/share/fonts/truetype/liberation/LiberationSans-BoldItalic.ttf', 46)
d.text((18, 4), 'VERDANT', font=fb, fill=255)
d.text((70, 50), 'RUSH', font=ImageFont.truetype('/usr/share/fonts/truetype/liberation/LiberationSans-BoldItalic.ttf', 52), fill=255)
m = np.array(im) > 0
logo = art.canvas(LW, LH)
# banner behind
art.poly(logo, [(20, 60), (340, 44), (330, 100), (10, 106)], BLUE, outline=OL, grad=(0, 1))
yy = np.arange(LH)[:, None] * np.ones((1, LW))
t = ((yy % 50) / 50.0)
ga = np.array(art.hexc('fffbd0'), float)
gb = np.array(art.hexc('ff9a10'), float)
gcol = (ga * (1 - t[..., None]) + gb * t[..., None]).astype(np.uint8)
thick = art.dilate(m, 3)
logo[thick] = OL
logo[art.dilate(m, 1) & ~m] = art.hexc('c02010')
logo[m] = gcol[m]
art.save(logo, os.path.join(OUT, 'logo.png'))
print('done')
