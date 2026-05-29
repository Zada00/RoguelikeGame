"""
Generator for vårt eget tileset. 16x16 piksel-tiles, lagt ut i et 8-kolonners ark.
Index-oppsett:
  0 solid   1 floor   2 wall   3 door   4 void   5 rubble   6 pillar   7 (spare)
  8 Warrior 9 Guardian 10 Rogue 11 Mage 12 Scout 13 Necromancer 14 Priest 15 (spare)
"""
from PIL import Image, ImageDraw

TILE = 16
COLS = 8
ROWS = 2
sheet = Image.new("RGBA", (COLS * TILE, ROWS * TILE), (0, 0, 0, 0))
d = ImageDraw.Draw(sheet)

def origin(index):
    return (index % COLS) * TILE, (index // COLS) * TILE

def rect(idx, x0, y0, x1, y1, color):
    ox, oy = origin(idx)
    d.rectangle([ox + x0, oy + y0, ox + x1, oy + y1], fill=color)

def px(idx, x, y, color):
    ox, oy = origin(idx)
    if 0 <= x < TILE and 0 <= y < TILE:
        sheet.putpixel((ox + x, oy + y), color)

# ---------------------------------------------------------------- environment

# 0: solid white (brukes som SolidGlyphIndex for bakgrunnsfarger)
rect(0, 0, 0, 15, 15, (255, 255, 255, 255))

# 1: floor - mørk steinflis med svak rutekant og litt tekstur
rect(1, 0, 0, 15, 15, (40, 40, 48, 255))
rect(1, 0, 0, 15, 0, (30, 30, 37, 255))
rect(1, 0, 0, 0, 15, (30, 30, 37, 255))
rect(1, 0, 15, 15, 15, (24, 24, 30, 255))
rect(1, 15, 0, 15, 15, (24, 24, 30, 255))
for sx, sy in [(4, 5), (10, 3), (6, 11), (12, 9), (3, 12)]:
    px(1, sx, sy, (52, 52, 62, 255))

# 2: wall - lys grå murstein med fuger og 3D-kant
rect(2, 0, 0, 15, 15, (96, 96, 104, 255))
rect(2, 0, 0, 15, 0, (132, 132, 142, 255))   # topphøylys
rect(2, 0, 15, 15, 15, (60, 60, 66, 255))    # bunnskygge
# fuger (mørkere linjer) - to murskift med forskjøvet mønster
rect(2, 0, 7, 15, 7, (62, 62, 68, 255))
rect(2, 7, 1, 7, 6, (62, 62, 68, 255))
rect(2, 3, 8, 3, 14, (62, 62, 68, 255))
rect(2, 11, 8, 11, 14, (62, 62, 68, 255))

# 3: door - tredør med metallbånd og håndtak
rect(3, 0, 0, 15, 15, (28, 28, 32, 255))     # mørk karm
rect(3, 2, 1, 13, 14, (122, 78, 40, 255))    # treverk
for px_x in (5, 8, 11):                       # vertikale plankeskiller
    rect(3, px_x, 1, px_x, 14, (92, 58, 28, 255))
rect(3, 2, 4, 13, 4, (150, 150, 158, 255))    # øvre metallbånd
rect(3, 2, 11, 13, 11, (150, 150, 158, 255))  # nedre metallbånd
rect(3, 10, 7, 11, 8, (210, 180, 70, 255))    # håndtak

# 4: void - nær-svart (uutforsket)
rect(4, 0, 0, 15, 15, (12, 12, 16, 255))

# 5: rubble floor - gulv med litt grus
rect(5, 0, 0, 15, 15, (40, 40, 48, 255))
rect(5, 0, 0, 15, 0, (30, 30, 37, 255))
rect(5, 0, 0, 0, 15, (30, 30, 37, 255))
for rx, ry in [(5, 9), (6, 10), (9, 7), (10, 8), (8, 11), (4, 6), (11, 11)]:
    px(5, rx, ry, (78, 72, 64, 255))
    px(5, rx + 1, ry, (58, 54, 48, 255))

# 6: pillar - søyle midt i en gulvflis
rect(6, 0, 0, 15, 15, (40, 40, 48, 255))
rect(6, 5, 1, 10, 15, (120, 120, 128, 255))
rect(6, 5, 1, 5, 15, (150, 150, 158, 255))
rect(6, 10, 1, 10, 15, (80, 80, 86, 255))
rect(6, 4, 1, 11, 2, (140, 140, 148, 255))   # kapitel topp
rect(6, 4, 13, 11, 14, (140, 140, 148, 255)) # base

# ---------------------------------------------------------------- heroes

SKIN = (228, 184, 142, 255)
SKIN_PALE = (205, 205, 214, 255)

def hero(idx, body, legs, skin, head, weapon, accent=(0, 0, 0, 255)):
    # bein
    rect(idx, 6, 13, 7, 15, legs)
    rect(idx, 8, 13, 9, 15, legs)
    # kropp / kappe
    rect(idx, 5, 7, 10, 12, body)
    rect(idx, 5, 7, 5, 12, accent if accent != (0, 0, 0, 255) else body)
    # armer
    rect(idx, 4, 8, 4, 11, body)
    rect(idx, 11, 8, 11, 11, body)
    # ansikt
    rect(idx, 6, 4, 9, 7, skin)

    # hodeplagg
    if head[0] == "helmet":
        rect(idx, 5, 2, 10, 4, head[1])
        rect(idx, 5, 2, 10, 2, tuple(min(255, c + 30) for c in head[1][:3]) + (255,))
        rect(idx, 7, 4, 8, 6, head[1])   # neseguard
    elif head[0] == "hood":
        rect(idx, 5, 2, 10, 5, head[1])
        rect(idx, 6, 5, 9, 6, head[1])   # skygge over ansikt
        rect(idx, 7, 6, 8, 7, skin)
    elif head[0] == "hat":               # spiss trollmannshatt
        rect(idx, 5, 4, 10, 4, head[1])
        rect(idx, 6, 3, 9, 3, head[1])
        rect(idx, 7, 1, 8, 2, head[1])
        px(idx, 8, 0, head[1])
    elif head[0] == "halo":
        rect(idx, 5, 3, 10, 4, head[1])  # hette
        rect(idx, 6, 1, 9, 1, (245, 215, 110, 255))  # glorie
        px(idx, 5, 2, (245, 215, 110, 255))
        px(idx, 10, 2, (245, 215, 110, 255))

    # øyne
    px(idx, 7, 5, (30, 30, 35, 255))
    px(idx, 8, 5, (30, 30, 35, 255))

    # våpen
    if weapon == "sword":
        rect(idx, 12, 3, 12, 9, (200, 200, 210, 255))
        rect(idx, 11, 9, 13, 9, (120, 90, 50, 255))
    elif weapon == "shieldsword":
        rect(idx, 12, 4, 12, 9, (200, 200, 210, 255))
        rect(idx, 2, 8, 3, 12, accent)
        rect(idx, 2, 8, 3, 8, tuple(min(255, c + 40) for c in accent[:3]) + (255,))
        px(idx, 3, 10, (235, 225, 160, 255))
    elif weapon == "daggers":
        rect(idx, 3, 9, 3, 11, (200, 200, 210, 255))
        rect(idx, 12, 9, 12, 11, (200, 200, 210, 255))
    elif weapon == "staff":
        rect(idx, 12, 3, 12, 14, (110, 74, 40, 255))
        rect(idx, 11, 2, 13, 3, accent)
        px(idx, 12, 1, tuple(min(255, c + 50) for c in accent[:3]) + (255,))
    elif weapon == "bow":
        rect(idx, 12, 3, 12, 12, (120, 80, 45, 255))
        px(idx, 13, 4, (120, 80, 45, 255))
        px(idx, 13, 11, (120, 80, 45, 255))
        rect(idx, 11, 4, 11, 11, (210, 200, 180, 255))  # streng
    elif weapon == "scythe":
        rect(idx, 12, 2, 12, 14, (90, 70, 55, 255))
        rect(idx, 9, 2, 12, 2, accent)
        rect(idx, 9, 2, 9, 3, accent)

# 8 Warrior - rød rustning, sverd
hero(8, (172, 52, 42, 255), (110, 32, 26, 255), SKIN, ("helmet", (152, 152, 162, 255)), "sword")
# 9 Guardian - blågrå tank, skjold + sverd
hero(9, (92, 112, 142, 255), (56, 70, 96, 255), SKIN, ("helmet", (172, 178, 188, 255)), "shieldsword", accent=(70, 95, 150, 255))
# 10 Rogue - grønn hette, dolker
hero(10, (52, 122, 72, 255), (32, 72, 46, 255), SKIN, ("hood", (42, 92, 56, 255)), "daggers")
# 11 Mage - lilla kappe, spiss hatt, stav
hero(11, (122, 62, 172, 255), (82, 42, 122, 255), SKIN, ("hat", (92, 46, 132, 255)), "staff", accent=(90, 210, 230, 255))
# 12 Scout - kaki, hette, bue
hero(12, (122, 142, 72, 255), (82, 96, 46, 255), SKIN, ("hood", (142, 162, 92, 255)), "bow")
# 13 Necromancer - mørk lilla, hette, ljå med grønn glød
hero(13, (72, 42, 92, 255), (46, 26, 62, 255), SKIN_PALE, ("hood", (52, 32, 72, 255)), "scythe", accent=(110, 220, 120, 255))
# 14 Priest - krem/gull, glorie, stav
hero(14, (222, 212, 162, 255), (172, 162, 122, 255), SKIN, ("halo", (210, 195, 150, 255)), "staff", accent=(245, 215, 110, 255))

sheet.save("/home/claude/tiles.png")
print("tiles.png laget:", sheet.size)

# ------------------------------------------------------------- forhåndsvisning
SCALE = 8
prev = sheet.resize((sheet.width * SCALE, sheet.height * SCALE), Image.NEAREST)
prev.save("/home/claude/tiles_preview.png")

# liten scene: et rom med vegger, gulv, en dør, en søyle og noen helter
scene_w, scene_h = 18, 11
scene = Image.new("RGBA", (scene_w * TILE, scene_h * TILE), (12, 12, 16, 255))

def blit(index, cx, cy):
    ox, oy = origin(index)
    tile = sheet.crop((ox, oy, ox + TILE, oy + TILE))
    base = sheet.crop((origin(1)[0], origin(1)[1], origin(1)[0] + TILE, origin(1)[1] + TILE))
    cell = Image.new("RGBA", (TILE, TILE), (0, 0, 0, 0))
    if index >= 8:                      # helter står på gulv
        cell.alpha_composite(base)
    cell.alpha_composite(tile)
    scene.alpha_composite(cell, (cx * TILE, cy * TILE))

for x in range(scene_w):
    for y in range(scene_h):
        edge = x in (0, scene_w - 1) or y in (0, scene_h - 1)
        blit(2 if edge else 1, x, y)
blit(3, scene_w - 1, 5)         # dør
blit(3, scene_w // 2, 0)        # dør
blit(6, 4, 6)                   # søyle
blit(5, 9, 7)                   # grus
for i, hx in enumerate(range(8, 15)):
    blit(hx, 3 + i * 2, 3)

scene = scene.resize((scene.width * 4, scene.height * 4), Image.NEAREST)
scene.save("/home/claude/scene_preview.png")
print("forhåndsvisninger laget")
