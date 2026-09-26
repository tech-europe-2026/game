"""Shared pixel-art helpers: shaded ellipsoids, polygons, outlines, dithering."""
import math
import numpy as np
from PIL import Image, ImageDraw

BAYER4 = np.array([[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]], float) / 16.0
LIGHT = np.array([-0.55, -0.7, 0.55])
LIGHT = LIGHT / np.linalg.norm(LIGHT)


def hexc(h, a=255):
    h = h.lstrip('#')
    return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), a)


def ramp(*hs):
    return [hexc(h) for h in hs]


def canvas(w, h):
    return np.zeros((h, w, 4), np.uint8)


def bayer(w, h, ox=0, oy=0):
    ys = (np.arange(h) + oy) % 4
    xs = (np.arange(w) + ox) % 4
    return BAYER4[ys[:, None], xs[None, :]]


def quantize(intensity, rmp, dither=0.18, ox=0, oy=0):
    """Map intensity (0..1) array to indices into ramp with ordered dithering."""
    h, w = intensity.shape
    n = len(rmp)
    v = intensity * (n - 1) + (bayer(w, h, ox, oy) - 0.5) * dither * (n - 1)
    idx = np.clip(np.round(v), 0, n - 1).astype(int)
    return idx


def put(c, mask, rgba_arr):
    c[mask] = rgba_arr[mask]


def ramp_image(idx, rmp):
    lut = np.array(rmp, np.uint8)
    return lut[idx]


def ellipsoid(c, cx, cy, rx, ry, rmp, outline=None, light=LIGHT, flat=0.0, dither=0.18, gloss=True):
    """Draw a shaded ellipsoid. Optional outline colour drawn 1px around it first."""
    h, w = c.shape[:2]
    if outline is not None:
        yy, xx = np.mgrid[0:h, 0:w]
        u = (xx + 0.5 - cx) / (rx + 1.0)
        v = (yy + 0.5 - cy) / (ry + 1.0)
        m = u * u + v * v <= 1.0
        c[m] = outline
    yy, xx = np.mgrid[0:h, 0:w]
    u = (xx + 0.5 - cx) / rx
    v = (yy + 0.5 - cy) / ry
    d = u * u + v * v
    m = d <= 1.0
    if not m.any():
        return m
    nz = np.sqrt(np.clip(1 - d, 0, 1))
    inten = (u * light[0] + v * light[1] + nz * light[2])
    inten = np.clip(inten * (1 - flat) + flat * 0.6, 0, 1)
    inten = np.clip((inten + 0.15) / 1.05, 0, 1)
    idx = quantize(inten, rmp, dither)
    img = ramp_image(idx, rmp)
    c[m] = img[m]
    if gloss:
        gx, gy = cx - rx * 0.38, cy - ry * 0.42
        g = ((xx + 0.5 - gx) / max(rx * 0.22, 0.8)) ** 2 + ((yy + 0.5 - gy) / max(ry * 0.16, 0.6)) ** 2 <= 1
        g &= m
        hl = np.array(rmp[-1], np.uint8)
        hl2 = np.minimum(255, hl.astype(int) + 50).astype(np.uint8)
        hl2[3] = 255
        c[g] = hl2
    return m


def polygon_mask(w, h, pts):
    im = Image.new('L', (w, h), 0)
    ImageDraw.Draw(im).polygon([(float(x), float(y)) for x, y in pts], fill=255)
    return np.array(im) > 0


def poly(c, pts, rmp, outline=None, grad=(0, 1), dither=0.18):
    """Polygon filled with a linear gradient ramp along direction grad (dark at end)."""
    h, w = c.shape[:2]
    if outline is not None:
        m2 = polygon_mask(w, h, pts)
        m2 = dilate(m2)
        c[m2] = outline
    m = polygon_mask(w, h, pts)
    if not m.any():
        return m
    yy, xx = np.mgrid[0:h, 0:w]
    gx, gy = grad
    proj = xx * gx + yy * gy
    pv = proj[m]
    lo, hi = pv.min(), pv.max()
    t = (proj - lo) / max(1e-6, hi - lo)
    inten = 1 - np.clip(t, 0, 1)
    idx = quantize(inten * 0.85 + 0.1, rmp, dither)
    img = ramp_image(idx, rmp)
    c[m] = img[m]
    return m


def line(c, p0, p1, width, color):
    h, w = c.shape[:2]
    im = Image.new('L', (w, h), 0)
    d = ImageDraw.Draw(im)
    d.line([tuple(p0), tuple(p1)], fill=255, width=int(width))
    r = width / 2.0
    for p in (p0, p1):
        d.ellipse([p[0] - r + 0.5, p[1] - r + 0.5, p[0] + r - 0.5, p[1] + r - 0.5], fill=255)
    m = np.array(im) > 0
    c[m] = color
    return m


def dilate(m, n=1):
    out = m.copy()
    for _ in range(n):
        o = out.copy()
        o[1:, :] |= out[:-1, :]
        o[:-1, :] |= out[1:, :]
        o[:, 1:] |= out[:, :-1]
        o[:, :-1] |= out[:, 1:]
        out = o
    return out


def outline(c, color=(16, 12, 28, 255)):
    a = c[..., 3] > 0
    ring = dilate(a) & ~a
    c[ring] = color
    return c


def save(c, path):
    Image.fromarray(c, 'RGBA').save(path, optimize=True)


def sheet(frames, cols=None):
    fh, fw = frames[0].shape[:2]
    n = len(frames)
    cols = cols or n
    rows = (n + cols - 1) // cols
    out = canvas(fw * cols, fh * rows)
    for i, f in enumerate(frames):
        r, cc = divmod(i, cols)
        out[r * fh:(r + 1) * fh, cc * fw:(cc + 1) * fw] = f
    return out


def rot(p, a, o=(0, 0)):
    x, y = p[0] - o[0], p[1] - o[1]
    ca, sa = math.cos(a), math.sin(a)
    return (o[0] + x * ca - y * sa, o[1] + x * sa + y * ca)
