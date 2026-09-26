"""Cut the ball states out of the concept sheets as round, transparent PNGs.

usage: extract_balls.py sheet1.png sheet2.png out_dir
"""
import sys
import numpy as np
from PIL import Image
from scipy import ndimage

# name: (cx, cy, rx, ry) rough guesses, refined by edge fitting
SHEET1 = {
    "idle": (106, 100, 66, 66), "spin": (309, 102, 66, 66), "bounce": (508, 102, 66, 66),
    "teleport": (710, 102, 64, 64), "grow": (910, 100, 68, 68),
    "crouch": (309, 391, 66, 55), "stun": (508, 385, 60, 60), "heal": (710, 385, 60, 60),
}
SHEET2 = {
    "dash": (110, 100, 66, 66), "climb": (513, 100, 66, 66), "camouflage": (920, 100, 70, 70),
    "reverse": (110, 370, 75, 75), "parry": (513, 370, 70, 70), "evolve": (915, 370, 76, 76),
}
OUT = 256


def fit(dark, cx, cy, rx, ry):
    th = np.linspace(0, 2 * np.pi, 180, endpoint=False)
    c, s = np.cos(th), np.sin(th)

    def score(x, y, a, b):
        def ring(k):
            px = np.clip((x + (a + k) * c).astype(int), 0, dark.shape[1] - 1)
            py = np.clip((y + (b + k) * s).astype(int), 0, dark.shape[0] - 1)
            return dark[py, px]
        return np.median(ring(-3) - ring(3))

    best = (score(cx, cy, rx, ry), cx, cy, rx, ry)
    for _ in range(3):
        _, x0, y0, a0, b0 = best
        for dx in range(-6, 7, 2):
            for dy in range(-6, 7, 2):
                for da in range(-6, 7, 2):
                    for db in ([da] if rx == ry else range(-6, 7, 2)):
                        sc = score(x0 + dx, y0 + dy, a0 + da, b0 + db)
                        if sc > best[0]:
                            best = (sc, x0 + dx, y0 + dy, a0 + da, b0 + db)
    return best[1:]


def extract(img, name, guess, out_dir):
    rgb = img.convert("RGB")
    a = np.asarray(rgb).astype(np.float32)
    dark = ndimage.gaussian_filter(255 - a.mean(-1), 1.0)
    cx, cy, rx, ry = fit(dark, *guess)
    rx -= 1.5
    ry -= 1.5
    h = int(round(OUT * ry / rx))
    crop = rgb.crop((cx - rx, cy - ry, cx + rx, cy + ry)).resize((OUT, h), Image.LANCZOS)
    yy, xx = np.mgrid[0:h, 0:OUT] + .5
    d = np.sqrt(((xx - OUT / 2) / (OUT / 2)) ** 2 + ((yy - h / 2) / (h / 2)) ** 2)
    alpha = np.clip((1 - d) * OUT / 2 * 1.2, 0, 1)
    rgba = np.dstack([np.asarray(crop), (alpha * 255).astype(np.uint8)])
    Image.fromarray(rgba, "RGBA").save(f"{out_dir}/{name}.png")
    print(name, cx, cy, rx, ry)


def main():
    for sheet, cells in ((sys.argv[1], SHEET1), (sys.argv[2], SHEET2)):
        img = Image.open(sheet)
        for name, g in cells.items():
            extract(img, name, g, sys.argv[3])


if __name__ == "__main__":
    main()
