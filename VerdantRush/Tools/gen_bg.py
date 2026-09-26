"""Seamless parallax background layers for a lush leaf-forest zone (512px wide tiles)."""
import math, os, random
import numpy as np
import art

OUT = os.path.join(os.path.dirname(__file__), '..', 'Assets', 'Resources', 'Sprites')
TW = 512
rs = random.Random(5)


def wrap_blob(c, x, y, rx, ry, rmp, outline=None, gloss=False, dither=0.18):
    for ox in (-TW, 0, TW):
        if -rx - 2 < x + ox < TW + rx + 2:
            art.ellipsoid(c, x + ox, y, rx, ry, rmp, outline=outline, gloss=gloss, dither=dither)


# ---- sky 512x240: gradient + puffy clouds
H = 240
sky = art.canvas(TW, H)
SKY = art.ramp('1848c0', '2a64d8', '3c82ec', '5aa2f6', '7ec0fa', 'a8dcff', 'd4f0ff')
yy = np.arange(H)[:, None] * np.ones((1, TW))
t = np.clip(yy / (H * 0.95), 0, 1) ** 1.3
idx = art.quantize(t, SKY, dither=0.5)
sky[:] = art.ramp_image(idx, SKY)
CLOUD = art.ramp('8ab4e0', 'bcd8f4', 'e4f2ff', 'ffffff')
for k in range(7):
    cx = rs.uniform(0, TW)
    cy = rs.uniform(30, 150)
    n = rs.randint(5, 9)
    w = rs.uniform(30, 60)
    for i in range(n):
        bx = cx + (i - n / 2) * w / n * 2
        br = rs.uniform(8, 16) * (1 - abs(i - n / 2) / n)
        wrap_blob(sky, bx, cy - br * 0.4, br + 3, br, CLOUD)
    for ox in (-TW, 0, TW):
        x0, x1 = int(max(0, cx - w + ox)), int(min(TW, cx + w + ox))
        if x1 > x0:
            sky[int(cy):int(cy + 3), x0:x1] = CLOUD[1]
art.save(sky, os.path.join(OUT, 'bg_sky.png'))

# ---- far: misty blue-green mountains with rounded forest bumps (512x140)
H = 140
far = art.canvas(TW, H)
FAR = art.ramp('3a7a9a', '4e94ae', '66acc0', '84c4d0', 'a8dcdc')
xs = np.arange(TW)
ridge = 70 + 18 * np.sin(xs * 2 * math.pi / TW * 2 + 1) + 10 * np.sin(xs * 2 * math.pi / TW * 5) + 5 * np.sin(xs * 2 * math.pi / TW * 11)
yy, xx = np.mgrid[0:H, 0:TW]
m = yy >= ridge[None, :]
inten = 0.75 - (yy - ridge[None, :]) / H * 0.8 + 0.12 * np.sin(xx * 0.05 + yy * 0.08)
fimg = art.ramp_image(art.quantize(np.clip(inten, 0, 1), FAR, 0.4), FAR)
far[m] = fimg[m]
FARG = art.ramp('2c6e62', '3a8a6a', '52a476', '74c084')
for k in range(46):
    x = k * TW / 46 + rs.uniform(-4, 4)
    y = 105 + 6 * math.sin(k * 0.9)
    wrap_blob(far, x, y, rs.uniform(9, 14), rs.uniform(8, 12), FARG, outline=FARG[0])
far[118:, :] = art.ramp_image(art.quantize(np.full((H - 118, TW), 0.3), FARG), FARG)
art.save(far, os.path.join(OUT, 'bg_far.png'))

# ---- mid: tall leaf-forest trees with dense canopies and a waterfall (512x220)
H = 220
mid = art.canvas(TW, H)
TRUNK = art.ramp('3a1c0a', '5e3014', '86481e', 'a8642c')
CAN = art.ramp('0c3a1a', '165a24', '237a2c', '36983a', '56b646', '8ad45a')
WATER = art.ramp('3a8ad0', '6ab4ec', 'a8dcff', 'ffffff')
# waterfall
wx0, wx1 = 300, 336
for y in range(40, H):
    for x in range(wx0, wx1):
        v = (math.sin(x * 1.3 + y * 0.35) + math.sin(x * 0.4 - y * 0.2)) * 0.25 + 0.55
        mid[y, x] = WATER[min(3, int(v * 4))]
for i in range(10):
    wrap_blob(mid, rs.uniform(wx0, wx1), H - 14 + rs.uniform(-4, 4), 8, 5, WATER)
trees = [(k * TW / 5 + rs.uniform(-20, 20), rs.uniform(60, 90)) for k in range(5)]
for tx, top in trees:
    w = rs.uniform(8, 12)
    for ox in (-TW, 0, TW):
        art.poly(mid, [(tx + ox - w, top), (tx + ox + w, top), (tx + ox + w * 1.5, H), (tx + ox - w * 1.5, H)], TRUNK, outline=TRUNK[0], grad=(1, 0))
    for i in range(16):
        a = rs.uniform(0, 2 * math.pi)
        d = rs.uniform(0, 40)
        wrap_blob(mid, tx + math.cos(a) * d * 1.4, top + math.sin(a) * d * 0.7, rs.uniform(14, 24), rs.uniform(12, 20), CAN, outline=CAN[0])
# undergrowth band
for k in range(40):
    wrap_blob(mid, k * TW / 40, H - 20 + rs.uniform(-8, 6), rs.uniform(12, 18), rs.uniform(10, 14), CAN[:5], outline=CAN[0])
mid[H - 12:, :] = CAN[0]
art.save(mid, os.path.join(OUT, 'bg_mid.png'))

# ---- near: dark foreground canopy hedge with pink blossoms (512x120)
H = 120
near = art.canvas(TW, H)
NEAR = art.ramp('06200c', '0c3614', '145020', '1e6c28', '2e8a30')
PINK = art.ramp('7a1a4a', 'c83c80', 'f07ab0', 'ffc0e0')
for k in range(30):
    wrap_blob(near, k * TW / 30 + rs.uniform(-6, 6), 60 + rs.uniform(-18, 10), rs.uniform(16, 26), rs.uniform(14, 22), NEAR, outline=NEAR[0])
for k in range(24):
    wrap_blob(near, rs.uniform(0, TW), rs.uniform(40, 90), 3, 3, PINK)
near[90:, :] = NEAR[0]
art.save(near, os.path.join(OUT, 'bg_near.png'))
print('bg done')
