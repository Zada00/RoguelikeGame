"""
Tileset v2 - 32x32 fliser, ny stil: omriss, skyggelegging, tydelige ansikter.
Layout (8 kolonner):
  0 solid  1 floor  2 wall  3 door  4 void  5 rubble  6 pillar  7 (spare)
  8 Warrior 9 Guardian 10 Rogue 11 Mage 12 Scout 13 Necromancer 14 Priest 15 (spare)
"""
from PIL import Image, ImageDraw

TILE = 32
COLS = 8
ROWS = 5
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

# ---- tema-varianter: gulv (16,17,20) og vegg (18,19,21) ----
def floorvar(i, base, grid=True):
    rect(i, 0, 0, 31, 31, base)
    if grid:
        rect(i, 0, 0, 31, 1, darken(base, 10))
        rect(i, 0, 0, 1, 31, darken(base, 10))
        rect(i, 0, 30, 31, 31, darken(base, 18))
        rect(i, 30, 0, 31, 31, darken(base, 18))


def wallvar(i, base):
    rect(i, 0, 0, 31, 31, base)
    rect(i, 0, 0, 31, 2, lighten(base, 26))
    rect(i, 0, 29, 31, 31, darken(base, 30))
    m = darken(base, 44)
    rect(i, 0, 15, 31, 16, m)
    rect(i, 15, 2, 16, 14, m)
    rect(i, 7, 17, 8, 29, m)
    rect(i, 23, 17, 24, 29, m)


# 16 moss-gulv
floorvar(16, (46, 50, 50, 255))
for mx, my in [(8, 10), (20, 22), (14, 6), (24, 14)]:
    ellipse(16, mx, my, mx + 4, my + 3, (54, 86, 50, 255))
    ellipse(16, mx + 1, my + 1, mx + 2, my + 2, (72, 110, 60, 255))

# 17 krypt-gulv
floorvar(17, (56, 56, 64, 255))
for cx in (10, 22):
    rect(17, cx, 4, cx, 27, darken((56, 56, 64, 255), 22))
for sx, sy in [(7, 9), (19, 18), (25, 8)]:
    put(17, sx, sy, (132, 128, 118, 255))

# 18 moss-vegg
wallvar(18, (90, 104, 92, 255))
for mx in (6, 18, 27):
    rect(18, mx, 2, mx, 7, (58, 92, 54, 255))
    put(18, mx, 8, (72, 110, 62, 255))

# 19 krypt-vegg
wallvar(19, (98, 100, 116, 255))
ox19, oy19 = org(19)
d.line([ox19 + 5, oy19 + 4, ox19 + 11, oy19 + 13], fill=darken((98, 100, 116, 255), 40), width=1)
d.line([ox19 + 24, oy19 + 18, ox19 + 19, oy19 + 27], fill=darken((98, 100, 116, 255), 40), width=1)

# 20 hule-gulv
floorvar(20, (60, 50, 40, 255), grid=False)
for dx2, dy2 in [(7, 8), (16, 20), (22, 11), (11, 24), (26, 25), (5, 17)]:
    ellipse(20, dx2, dy2, dx2 + 2, dy2 + 1, darken((60, 50, 40, 255), 14))
    put(20, dx2, dy2, lighten((60, 50, 40, 255), 14))

# 21 hule-vegg
rect(21, 0, 0, 31, 31, (98, 82, 64, 255))
rect(21, 0, 0, 31, 2, lighten((98, 82, 64, 255), 22))
rect(21, 0, 29, 31, 31, darken((98, 82, 64, 255), 28))
for bx, by in [(6, 8), (18, 6), (12, 18), (24, 20), (9, 24), (22, 12)]:
    ellipse(21, bx, by, bx + 4, by + 3, darken((98, 82, 64, 255), 18))

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

# ================================================================= monsters
# 24 Rat - liten, lav, hale
def rat(i):
    body = (120, 110, 104, 255)
    ellipse(i, 8, 16, 24, 27, body)              # kropp
    ellipse(i, 18, 12, 28, 22, body)             # hode
    rect(i, 18, 10, 20, 13, darken(body, 10))    # ører
    rect(i, 24, 10, 26, 13, darken(body, 10))
    put(i, 26, 16, (30, 26, 30, 255))            # øye
    put(i, 28, 18, (220, 150, 160, 255))         # snute
    # hale
    ox, oy = org(i)
    d.line([ox + 8, oy + 22, ox + 2, oy + 26], fill=(180, 140, 140, 255), width=2)
    outline_tile(i)


# 25 Goblin - liten grønn humanoid
def goblin(i):
    skin = (96, 150, 78, 255)
    ellipse(i, 5, 28, 26, 31, (0, 0, 0, 70))     # skygge
    rect(i, 11, 24, 14, 29, darken(skin, 24))    # bein
    rect(i, 17, 24, 20, 29, darken(skin, 24))
    shaded(i, 9, 15, 22, 25, (120, 90, 60, 255), 12, 18)  # tunika
    shaded(i, 9, 6, 22, 16, skin, 12, 18)        # hode
    rect(i, 5, 8, 9, 12, skin); rect(i, 22, 8, 26, 12, skin)  # store ører
    rect(i, 12, 10, 13, 12, (210, 40, 40, 255))  # røde øyne
    rect(i, 18, 10, 19, 12, (210, 40, 40, 255))
    rect(i, 13, 14, 18, 14, (40, 30, 30, 255))   # munn
    rect(i, 24, 14, 26, 22, (150, 150, 158, 255))  # liten klubbe/dolk
    outline_tile(i)


# 26 Skeleton - hvite bein, skalle
def skeleton(i):
    bone = (224, 222, 210, 255)
    ellipse(i, 5, 28, 26, 31, (0, 0, 0, 70))
    rect(i, 12, 24, 14, 30, bone); rect(i, 17, 24, 19, 30, bone)  # bein
    rect(i, 10, 16, 21, 24, bone)                # ribbein-blokk
    rect(i, 12, 18, 19, 18, darken(bone, 30))
    rect(i, 12, 21, 19, 21, darken(bone, 30))
    rect(i, 5, 17, 8, 23, bone); rect(i, 23, 17, 26, 23, bone)    # armer
    ellipse(i, 9, 4, 22, 16, bone)               # skalle
    rect(i, 12, 9, 14, 12, (20, 20, 26, 255))    # øyehuler
    rect(i, 17, 9, 19, 12, (20, 20, 26, 255))
    rect(i, 13, 13, 18, 14, (60, 60, 66, 255))   # tenner
    outline_tile(i)


# 27 Slime - grønn klatt
def slime(i):
    body = (90, 200, 120, 255)
    ellipse(i, 5, 28, 26, 31, (0, 0, 0, 70))
    ellipse(i, 6, 12, 26, 30, body)
    ellipse(i, 6, 8, 26, 26, body)               # toppen buler opp
    ellipse(i, 9, 12, 15, 18, lighten(body, 40)) # høylys
    rect(i, 12, 18, 14, 21, (30, 60, 40, 255))   # øyne
    rect(i, 18, 18, 20, 21, (30, 60, 40, 255))
    rect(i, 14, 24, 18, 25, (40, 80, 55, 255))   # munn
    outline_tile(i)


rat(24)
goblin(25)
skeleton(26)
slime(27)

# 7 stairs down - trinn som synker ned i mørket
def stairs_down(i):
    base = (44, 44, 54, 255)
    rect(i, 0, 0, 31, 31, base)
    rect(i, 0, 0, 31, 1, darken(base, 10))
    rect(i, 0, 0, 1, 31, darken(base, 10))
    rect(i, 4, 4, 27, 28, (14, 14, 18, 255))   # mørk sjakt
    for n in range(6):
        y = 6 + n * 3
        x0 = 6 + n
        shade = 100 - n * 13
        rect(i, x0, y, 25, y + 1, (shade, shade, shade + 6, 255))
        edge = min(255, shade + 34)
        rect(i, x0, y, 25, y, (edge, edge, min(255, edge + 6), 255))

stairs_down(7)

# 15 bullet - liten lys orb (fargelegges av forgrunnsfargen i spillet)
def bullet(i):
    ellipse(i, 10, 10, 21, 21, (255, 255, 255, 90))   # glød
    ellipse(i, 12, 12, 19, 19, (255, 255, 255, 255))  # kjerne
    ellipse(i, 13, 13, 16, 16, (255, 255, 255, 255))

bullet(15)

# 28 Cultist - hettekledd skytter som jager
def cultist(i):
    robe = (130, 55, 75, 255)
    ellipse(i, 5, 28, 26, 31, (0, 0, 0, 70))
    shaded(i, 7, 14, 24, 29, robe, 14, 20)
    rect(i, 9, 28, 22, 29, darken(robe, 22))
    shaded(i, 9, 5, 22, 17, darken(robe, 10), 12, 18)
    ellipse(i, 11, 8, 20, 18, (24, 16, 24, 255))
    put(i, 13, 12, (255, 80, 80, 255))
    put(i, 18, 12, (255, 80, 80, 255))
    ellipse(i, 12, 20, 19, 26, (255, 140, 120, 255))
    ellipse(i, 14, 21, 17, 24, (255, 220, 190, 255))
    outline_tile(i)

# 29 Seer - svevende oye som forutser bevegelsen din
def seer(i):
    body = (95, 72, 135, 255)
    ellipse(i, 7, 29, 24, 31, (0, 0, 0, 70))
    ellipse(i, 6, 7, 25, 26, body)
    ellipse(i, 9, 9, 22, 22, (236, 236, 246, 255))
    ellipse(i, 12, 11, 19, 20, (120, 90, 205, 255))
    ellipse(i, 14, 13, 18, 18, (18, 18, 28, 255))
    put(i, 16, 14, (255, 255, 255, 255))
    rect(i, 9, 9, 22, 10, darken(body, 12))
    outline_tile(i)

cultist(28)
seer(29)

# ---- flerfelts-objekter ----
# 30 water - vanndam (fyller hele flisa, flere ved siden av hverandre = dam)
def water(i):
    rect(i, 0, 0, 31, 31, (38, 66, 116, 255))
    rect(i, 0, 0, 31, 2, (30, 54, 96, 255))
    rect(i, 4, 8, 13, 9, (92, 138, 198, 255))
    rect(i, 17, 13, 26, 14, (92, 138, 198, 255))
    rect(i, 8, 21, 20, 22, (78, 120, 180, 255))
    rect(i, 20, 25, 27, 26, (92, 138, 198, 255))

# 31 crate - trekasse
def crate(i):
    rect(i, 4, 4, 27, 27, (120, 84, 48, 255))
    rect(i, 4, 4, 27, 6, (150, 110, 66, 255))
    rect(i, 4, 4, 6, 27, (150, 110, 66, 255))
    rect(i, 4, 4, 27, 4, darken((120, 84, 48, 255), 30))
    rect(i, 4, 27, 27, 27, darken((120, 84, 48, 255), 30))
    rect(i, 4, 15, 27, 16, (92, 64, 36, 255))     # midtplanke
    d.line([org(i)[0] + 5, org(i)[1] + 5, org(i)[0] + 26, org(i)[1] + 26], fill=(92, 64, 36, 255), width=2)
    outline_tile(i)

# 32 statue top / 33 statue bottom - 2 fliser hoy stenstatue
def statue_top(i):
    stone = (150, 150, 160, 255)
    ellipse(i, 11, 8, 20, 18, stone)               # hode
    rect(i, 9, 18, 22, 31, stone)                  # skuldre/overkropp
    rect(i, 13, 12, 15, 14, (60, 60, 70, 255))     # oyne
    rect(i, 17, 12, 19, 14, (60, 60, 70, 255))
    rect(i, 9, 18, 22, 19, lighten(stone, 25))
    outline_tile(i)

def statue_bottom(i):
    stone = (150, 150, 160, 255)
    rect(i, 9, 0, 22, 20, stone)                   # kropp
    rect(i, 6, 20, 25, 27, darken(stone, 18))      # sokkel
    rect(i, 4, 27, 27, 31, darken(stone, 30))
    rect(i, 6, 20, 25, 21, lighten(stone, 20))
    outline_tile(i)

# 34 crack - sprekk / avgrunn i gulvet
def crack(i):
    rect(i, 0, 0, 31, 31, (40, 40, 50, 255))       # mork stein rundt
    pts = [(16, 2), (12, 9), (19, 15), (11, 22), (17, 30)]
    for n in range(len(pts) - 1):
        d.line([org(i)[0] + pts[n][0], org(i)[1] + pts[n][1],
                org(i)[0] + pts[n + 1][0], org(i)[1] + pts[n + 1][1]],
               fill=(8, 8, 12, 255), width=5)
    for n in range(len(pts) - 1):
        d.line([org(i)[0] + pts[n][0], org(i)[1] + pts[n][1],
                org(i)[0] + pts[n + 1][0], org(i)[1] + pts[n + 1][1]],
               fill=(70, 70, 84, 255), width=1)

water(30)
crate(31)
statue_top(32)
statue_bottom(33)
crack(34)

sheet.save("/home/claude/tiles.png")
print("tiles.png:", sheet.size)

sheet.resize((sheet.width * 5, sheet.height * 5), Image.NEAREST).save("/home/claude/tiles_preview.png")

strip = Image.new("RGBA", (7 * TILE, TILE), (28, 28, 36, 255))
for n, hi in enumerate(range(8, 15)):
    ox, oy = org(hi)
    strip.alpha_composite(sheet.crop((ox, oy, ox + TILE, oy + TILE)), (n * TILE, 0))
strip.resize((strip.width * 6, strip.height * 6), Image.NEAREST).save("/home/claude/heroes_preview.png")

mstrip = Image.new("RGBA", (4 * TILE, TILE), (28, 28, 36, 255))
for n, mi in enumerate(range(24, 28)):
    ox, oy = org(mi)
    mstrip.alpha_composite(sheet.crop((ox, oy, ox + TILE, oy + TILE)), (n * TILE, 0))
mstrip.resize((mstrip.width * 6, mstrip.height * 6), Image.NEAREST).save("/home/claude/monsters_preview.png")

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

# tema-forhåndsvisning: fire små rom side om side
themes = [(1, 2), (16, 18), (17, 19), (20, 21)]
tw, th = 8, 6
tp = Image.new("RGBA", (4 * tw * TILE, th * TILE), (12, 12, 18, 255))
for ti, (fl, wl) in enumerate(themes):
    for x in range(tw):
        for y in range(th):
            edge = x in (0, tw - 1) or y in (0, th - 1)
            g = wl if edge else fl
            gx, gy = org(g)
            tp.alpha_composite(sheet.crop((gx, gy, gx + TILE, gy + TILE)), ((ti * tw + x) * TILE, y * TILE))
tp.resize((tp.width * 3, tp.height * 3), Image.NEAREST).save("/home/claude/themes_preview.png")
print("previews done")
