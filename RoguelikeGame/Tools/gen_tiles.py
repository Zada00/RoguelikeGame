"""
Tileset v2 - 32x32 fliser, ny stil: omriss, skyggelegging, tydelige ansikter.
Layout (8 kolonner):
  0 solid  1 floor  2 wall  3 door  4 void  5 rubble  6 pillar  7 (spare)
  8 Warrior 9 Guardian 10 Rogue 11 Mage 12 Scout 13 Necromancer 14 Priest 15 (spare)
"""
from PIL import Image, ImageDraw

TILE = 32
COLS = 8
ROWS = 2
OUTLINE = (22, 18, 30, 255)

sheet = Image.new("RGBA", (COLS * TILE, ROWS * TILE), (0, 0, 0, 0))
d = ImageDraw.Draw(sheet)
px = sheet.load()


def org(i):
    return (i % COLS) * TILE, (i // COLS) * TILE


def clamp(c):
    return tuple(max(0, min(255, v)) for v in c)


def lighten(c, a):
    return clamp((c[0] + a, c[1] + a, c[2] + a)) + (255,)


def darken(c, a):
    return lighten(c, -a)


def rect(i, x0, y0, x1, y1, color):
    ox, oy = org(i)
    d.rectangle([ox + x0, oy + y0, ox + x1, oy + y1], fill=color)


def put(i, x, y, color):
    ox, oy = org(i)
    if 0 <= x < TILE and 0 <= y < TILE:
        px[ox + x, oy + y] = color


def shaded(i, x0, y0, x1, y1, base, light=14, dark=22):
    rect(i, x0, y0, x1, y1, base)
    rect(i, x0, y0, x1, y0, lighten(base, light))
    rect(i, x0, y0, x0, y1, lighten(base, light // 2))
    rect(i, x0, y1, x1, y1, darken(base, dark))
    rect(i, x1, y0, x1, y1, darken(base, dark // 2))


def ellipse(i, x0, y0, x1, y1, color):
    ox, oy = org(i)
    d.ellipse([ox + x0, oy + y0, ox + x1, oy + y1], fill=color)


def arc(i, x0, y0, x1, y1, a0, a1, color, w=2):
    ox, oy = org(i)
    d.arc([ox + x0, oy + y0, ox + x1, oy + y1], a0, a1, fill=color, width=w)


def outline_tile(i):
    ox, oy = org(i)
    targets = []
    for x in range(TILE):
        for y in range(TILE):
            if px[ox + x, oy + y][3] != 0:
                continue
            touch = False
            for nx in (-1, 0, 1):
                for ny in (-1, 0, 1):
                    if nx == 0 and ny == 0:
                        continue
                    sx, sy = x + nx, y + ny
                    if 0 <= sx < TILE and 0 <= sy < TILE:
                        n = px[ox + sx, oy + sy]
                        if n[3] != 0 and n != OUTLINE:
                            touch = True
            if touch:
                targets.append((x, y))
    for x, y in targets:
        px[ox + x, oy + y] = OUTLINE


# ============================================================ environment
rect(0, 0, 0, 31, 31, (255, 255, 255, 255))

fb = (44, 44, 54, 255)
rect(1, 0, 0, 31, 31, fb)
rect(1, 0, 0, 31, 1, darken(fb, 10))
rect(1, 0, 0, 1, 31, darken(fb, 10))
rect(1, 0, 30, 31, 31, darken(fb, 18))
rect(1, 30, 0, 31, 31, darken(fb, 18))
for sx, sy in [(8, 10), (22, 7), (13, 22), (25, 19), (6, 25), (18, 14)]:
    put(1, sx, sy, lighten(fb, 16))
    put(1, sx + 1, sy + 1, darken(fb, 8))

wb = (104, 104, 114, 255)
rect(2, 0, 0, 31, 31, wb)
rect(2, 0, 0, 31, 2, lighten(wb, 26))
rect(2, 0, 29, 31, 31, darken(wb, 30))
mortar = darken(wb, 44)
rect(2, 0, 15, 31, 16, mortar)
rect(2, 15, 2, 16, 14, mortar)
rect(2, 7, 17, 8, 29, mortar)
rect(2, 23, 17, 24, 29, mortar)

rect(3, 0, 0, 31, 31, (32, 30, 36, 255))
shaded(3, 4, 2, 27, 30, (124, 80, 42, 255), 18, 26)
for vx in (10, 16, 22):
    rect(3, vx, 3, vx, 29, darken((124, 80, 42, 255), 30))
rect(3, 4, 8, 27, 9, (150, 150, 160, 255))
rect(3, 4, 22, 27, 23, (150, 150, 160, 255))
ellipse(3, 20, 14, 24, 18, (215, 185, 80, 255))

rect(4, 0, 0, 31, 31, (12, 12, 18, 255))

rect(5, 0, 0, 31, 31, fb)
rect(5, 0, 0, 31, 1, darken(fb, 10))
rect(5, 0, 0, 1, 31, darken(fb, 10))
for rx, ry in [(10, 18), (12, 20), (20, 14), (22, 16), (16, 22), (8, 12), (24, 23)]:
    ellipse(5, rx, ry, rx + 3, ry + 2, (96, 90, 80, 255))
    put(5, rx + 1, ry, (130, 124, 112, 255))

rect(6, 0, 0, 31, 31, fb)
shaded(6, 10, 2, 21, 31, (122, 122, 132, 255), 22, 30)
rect(6, 8, 2, 23, 5, lighten((122, 122, 132, 255), 18))
rect(6, 8, 27, 23, 30, darken((122, 122, 132, 255), 10))

# ================================================================= heroes
SKIN = (232, 188, 146, 255)
SKIN_PALE = (206, 208, 220, 255)


def hero(i, cfg):
    skin = cfg.get("skin", SKIN)
    tunic = cfg["tunic"]
    boots = cfg.get("boots", (70, 52, 40, 255))

    ellipse(i, 8, 28, 23, 31, (0, 0, 0, 70))

    shaded(i, 11, 25, 14, 30, boots, 10, 16)
    shaded(i, 17, 25, 20, 30, boots, 10, 16)

    shaded(i, 8, 16, 23, 26, tunic, 16, 24)
    if cfg.get("belt"):
        rect(i, 8, 23, 23, 24, cfg["belt"])

    shaded(i, 5, 17, 8, 24, tunic, 12, 20)
    shaded(i, 23, 17, 26, 24, tunic, 12, 20)
    rect(i, 5, 23, 7, 25, skin)
    rect(i, 24, 23, 26, 25, skin)

    shaded(i, 9, 5, 22, 16, skin, 12, 16)
    rect(i, 8, 10, 8, 13, skin)
    rect(i, 23, 10, 23, 13, skin)

    # hodeplagg tegnes FØR ansiktet, så ansiktet alltid er synlig
    head = cfg["head"]
    hc = cfg.get("hair", (60, 44, 30, 255))
    if head == "hair":
        rect(i, 9, 4, 22, 7, hc)
        rect(i, 9, 5, 9, 9, hc); rect(i, 22, 5, 22, 9, hc)
    elif head == "helmet":
        mc = cfg.get("metal", (158, 158, 168, 255))
        shaded(i, 8, 3, 23, 8, mc, 24, 20)
        rect(i, 15, 8, 16, 14, mc)
        if cfg.get("plume"):
            rect(i, 14, 0, 17, 3, cfg["plume"])
    elif head == "hood":
        hoodc = cfg["hoodc"]
        shaded(i, 7, 3, 24, 9, hoodc, 12, 18)      # hette over pannen
        rect(i, 7, 9, 9, 16, hoodc)                # sider rammer ansiktet
        rect(i, 22, 9, 24, 16, hoodc)
        rect(i, 10, 8, 21, 8, darken(hoodc, 16))   # liten skygge over øynene
    elif head == "wizardhat":
        hatc = cfg["hatc"]
        rect(i, 5, 6, 26, 8, hatc)
        rect(i, 9, 4, 22, 6, hatc)
        rect(i, 12, 1, 19, 4, hatc)
        rect(i, 14, 0, 17, 1, hatc)
        put(i, 15, 0, lighten(hatc, 30))
    elif head == "halo":
        rect(i, 8, 4, 23, 8, cfg["hoodc"])
        rect(i, 8, 4, 23, 4, lighten(cfg["hoodc"], 18))
        arc(i, 9, 0, 22, 6, 180, 360, (248, 218, 110, 255), 2)

    # ansikt - tegnes oppå hodeplagget så det alltid synes
    eye = cfg.get("eye", (40, 36, 48, 255))
    rect(i, 12, 10, 13, 11, eye)
    rect(i, 18, 10, 19, 11, eye)
    if cfg.get("glow"):
        put(i, 12, 9, cfg["glow"]); put(i, 19, 9, cfg["glow"])
        put(i, 13, 9, cfg["glow"]); put(i, 18, 9, cfg["glow"])
    rect(i, 15, 12, 16, 13, darken(skin, 26))
    if cfg.get("beard") == "short":
        rect(i, 11, 14, 20, 16, cfg["hair"])
        rect(i, 13, 14, 18, 14, skin)
    elif cfg.get("beard") == "long":
        rect(i, 11, 13, 20, 20, cfg["hair"])
        rect(i, 13, 13, 18, 13, skin)
    else:
        rect(i, 14, 14, 17, 14, darken(skin, 30))

    w = cfg.get("weapon")
    steel = (206, 208, 218, 255)
    if w == "sword":
        shaded(i, 26, 4, 28, 20, steel, 30, 24)
        rect(i, 24, 20, 30, 21, (210, 180, 80, 255))
        rect(i, 26, 21, 28, 25, (120, 84, 50, 255))
    elif w == "shieldsword":
        shaded(i, 27, 6, 29, 20, steel, 30, 24)
        rect(i, 25, 20, 31, 21, (210, 180, 80, 255))
        ac = cfg.get("accent", (70, 95, 150, 255))
        ellipse(i, 1, 15, 9, 27, ac)
        ellipse(i, 2, 16, 8, 26, darken(ac, 18))
        ellipse(i, 4, 19, 6, 23, (220, 200, 120, 255))
    elif w == "daggers":
        shaded(i, 3, 21, 4, 26, steel, 20, 20)
        shaded(i, 27, 21, 28, 26, steel, 20, 20)
    elif w == "staff":
        rect(i, 26, 7, 27, 30, (110, 74, 42, 255))
        ac = cfg.get("accent", (90, 210, 230, 255))
        ellipse(i, 23, 1, 30, 8, ac)
        ellipse(i, 24, 2, 28, 6, lighten(ac, 40))
        put(i, 25, 3, (255, 255, 255, 255))
    elif w == "bow":
        arc(i, 22, 4, 33, 28, 120, 240, (120, 82, 46, 255), 3)
        ox, oy = org(i)
        d.line([ox + 24, oy + 6, ox + 24, oy + 26], fill=(225, 215, 195, 255), width=1)
    elif w == "skullstaff":
        rect(i, 26, 8, 27, 30, (80, 70, 64, 255))
        ellipse(i, 23, 2, 30, 9, (225, 225, 230, 255))
        put(i, 25, 5, (30, 30, 36, 255)); put(i, 28, 5, (30, 30, 36, 255))
        ac = cfg.get("accent", (110, 220, 120, 255))
        put(i, 25, 4, ac); put(i, 28, 4, ac)
    elif w == "holystaff":
        rect(i, 26, 6, 27, 30, (190, 150, 70, 255))
        rect(i, 24, 9, 29, 10, (235, 205, 110, 255))
        rect(i, 26, 4, 27, 12, (235, 205, 110, 255))

    outline_tile(i)


hero(8, dict(tunic=(176, 54, 44, 255), boots=(96, 40, 32, 255), belt=(90, 60, 36, 255),
             head="helmet", metal=(160, 160, 170, 255), plume=(210, 70, 60, 255),
             hair=(70, 48, 32, 255), beard="short", weapon="sword"))
hero(9, dict(tunic=(96, 116, 148, 255), boots=(70, 80, 96, 255), belt=(60, 70, 90, 255),
             head="helmet", metal=(176, 182, 194, 255), weapon="shieldsword",
             accent=(72, 100, 156, 255)))
hero(10, dict(tunic=(54, 126, 76, 255), boots=(40, 60, 44, 255), belt=(40, 40, 30, 255),
              head="hood", hoodc=(44, 96, 60, 255), weapon="daggers"))
hero(11, dict(tunic=(124, 64, 176, 255), boots=(70, 40, 96, 255), belt=(210, 180, 70, 255),
              head="wizardhat", hatc=(96, 48, 138, 255), hair=(225, 225, 230, 255),
              beard="long", weapon="staff", accent=(94, 214, 234, 255)))
hero(12, dict(tunic=(120, 142, 72, 255), boots=(80, 64, 40, 255), belt=(70, 56, 34, 255),
              head="hood", hoodc=(150, 110, 66, 255), weapon="bow"))
hero(13, dict(tunic=(70, 42, 92, 255), boots=(40, 26, 52, 255), skin=SKIN_PALE,
              head="hood", hoodc=(52, 32, 74, 255), glow=(120, 235, 130, 255),
              eye=(120, 235, 130, 255), weapon="skullstaff", accent=(120, 235, 130, 255)))
hero(14, dict(tunic=(226, 216, 168, 255), boots=(150, 130, 90, 255), belt=(210, 180, 90, 255),
              head="halo", hoodc=(214, 200, 156, 255), weapon="holystaff"))

sheet.save("/home/claude/tiles.png")
print("tiles.png:", sheet.size)

sheet.resize((sheet.width * 5, sheet.height * 5), Image.NEAREST).save("/home/claude/tiles_preview.png")

strip = Image.new("RGBA", (7 * TILE, TILE), (28, 28, 36, 255))
for n, hi in enumerate(range(8, 15)):
    ox, oy = org(hi)
    strip.alpha_composite(sheet.crop((ox, oy, ox + TILE, oy + TILE)), (n * TILE, 0))
strip.resize((strip.width * 6, strip.height * 6), Image.NEAREST).save("/home/claude/heroes_preview.png")

sw, sh = 14, 9
scene = Image.new("RGBA", (sw * TILE, sh * TILE), (12, 12, 18, 255))


def blit(index, cx, cy, on_floor=False):
    ox, oy = org(index)
    cell = Image.new("RGBA", (TILE, TILE), (0, 0, 0, 0))
    if on_floor:
        fx, fy = org(1)
        cell.alpha_composite(sheet.crop((fx, fy, fx + TILE, fy + TILE)))
    cell.alpha_composite(sheet.crop((ox, oy, ox + TILE, oy + TILE)))
    scene.alpha_composite(cell, (cx * TILE, cy * TILE))


for x in range(sw):
    for y in range(sh):
        edge = x in (0, sw - 1) or y in (0, sh - 1)
        blit(2 if edge else 1, x, y)
blit(3, sw // 2, 0)
blit(3, sw - 1, 4)
blit(6, 3, 5)
blit(5, 8, 6)
for n, hi in enumerate([8, 11, 14]):
    blit(hi, 4 + n * 3, 3, on_floor=True)
scene.resize((scene.width * 4, scene.height * 4), Image.NEAREST).save("/home/claude/scene_preview.png")
print("previews done")
