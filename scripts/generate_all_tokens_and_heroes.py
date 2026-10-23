#!/usr/bin/env python3
"""
Rampartbound Asset Division - Token Medallions & Full-Body Chibi Hero Generator.
Produces:
1. Distinct 12 Token Medallions (T1, T2, T3 for Cedric, Rose, Rabbit, Wind):
   - T1: Classic Polished Bronze/Silver Medallion with Archetype Emblem & 1 Star (★).
   - T2: Radiant Ornate Golden Medallion with Filigree Studs & 2 Stars (★★).
   - T3: Mythic Ascended Platinum/Diamond Medallion with Crested Wings & 3 Stars (★★★).
2. Distinct 12 Standing Chibi Hero Characters (T1, T2, T3 for Cedric, Rose, Rabbit, Wind):
   - Standing full-body chibi anime fantasy characters (NO circular medallion coin!).
   - Distinct weapons, armor, stances, capes, and progressive tier effects.
"""

import os
import math
import shutil
from PIL import Image, ImageDraw, ImageFilter

GRID_DIR = r"D:\PROJECT GAMES\Rampartbound\Assets\Art\Sprites\Grid"
RESOURCES_GRID_DIR = r"D:\PROJECT GAMES\Rampartbound\Assets\Resources\Sprites\Grid"
HERO_DIR = r"D:\PROJECT GAMES\Rampartbound\Assets\Art\Sprites\Heroes"

os.makedirs(GRID_DIR, exist_ok=True)
os.makedirs(RESOURCES_GRID_DIR, exist_ok=True)
os.makedirs(HERO_DIR, exist_ok=True)

# ==============================================================================
# PART 1: TOKEN MEDALLIONS GENERATION (GRID / PUZZLE MODE)
# ==============================================================================

S = 512
cx, cy = S // 2, S // 2

def draw_star(draw, x, y, radius, fill_color, outline_color=(255, 255, 255, 255)):
    points = []
    for i in range(10):
        ang = math.radians(i * 36 - 90)
        r = radius if i % 2 == 0 else radius * 0.42
        points.append((x + r * math.cos(ang), y + r * math.sin(ang)))
    draw.polygon(points, fill=fill_color, outline=outline_color)

def draw_stars_badge(draw, count, tier, star_y, spacing=48, r=22):
    start_x = cx - ((count - 1) * spacing) // 2
    if tier == 1:
        s_col = (255, 215, 60, 255)
    elif tier == 2:
        s_col = (255, 235, 90, 255)
    else:
        s_col = (210, 245, 255, 255) # Mythic Diamond cyan-white

    # Background badge pill for stars
    pill_w = count * spacing + 28
    pill_h = 44
    draw.rounded_rectangle([cx - pill_w // 2, star_y - pill_h // 2, cx + pill_w // 2, star_y + pill_h // 2],
                           radius=22, fill=(20, 20, 30, 230), outline=s_col, width=4)

    for i in range(count):
        sx = start_x + (i * spacing)
        draw_star(draw, sx, star_y, r, s_col, outline_color=(255, 255, 255, 255))

def draw_cedric_emblem(draw, tier):
    # Sword blade
    w = 26 + (tier * 6)
    blade = [(cx - w, cy - 135), (cx + w, cy - 135), (cx + (w - 6), cy + 70), (cx, cy + 125 + tier * 10), (cx - (w - 6), cy + 70)]
    draw.polygon(blade, fill=(245, 248, 255, 255), outline=(170, 185, 210, 255))
    draw.line([(cx, cy - 125), (cx, cy + 70)], fill=(150, 175, 205, 255), width=8 + tier * 2)

    # Crossguard
    gw = 90 + tier * 25
    guard = [(cx - gw, cy - 75), (cx + gw, cy - 75), (cx + gw - 15, cy - 45), (cx - gw + 15, cy - 45)]
    g_col = (255, 215, 50, 255) if tier >= 2 else (190, 170, 140, 255)
    draw.polygon(guard, fill=g_col, outline=(130, 90, 20, 255))

    # Grip & Pommel
    draw.rectangle([cx - 14, cy - 135, cx + 14, cy - 75], fill=(90, 20, 20, 255))
    draw.ellipse([cx - 24, cy - 165, cx + 24, cy - 125], fill=g_col, outline=(130, 90, 20, 255))
    draw.ellipse([cx - 12, cy - 153, cx + 12, cy - 137], fill=(220, 30, 30, 255))

    if tier >= 2:
        # Dual swords crossed for T2 & T3
        cross_blade1 = [(cx - 120, cy + 40), (cx + 120, cy - 120)]
        cross_blade2 = [(cx + 120, cy + 40), (cx - 120, cy - 120)]
        draw.line(cross_blade1, fill=(255, 235, 120, 220), width=10)
        draw.line(cross_blade2, fill=(255, 235, 120, 220), width=10)

    if tier == 3:
        # Blazing Fire Tempest Arc
        for angle in range(-120, 121, 20):
            rad = math.radians(angle - 90)
            fx = cx + int(140 * math.cos(rad))
            fy = cy + int(140 * math.sin(rad))
            draw_star(draw, fx, fy, 14, (255, 100, 30, 230), (255, 240, 100, 255))

def draw_rose_emblem(draw, tier):
    # Warhammer & Heraldic Shield
    sw = 80 + tier * 20
    shield = [(cx - sw, cy - 90), (cx + sw, cy - 90), (cx + sw - 12, cy + 30), (cx, cy + 115 + tier * 10), (cx - sw + 12, cy + 30)]
    s_col = (195, 55, 115, 255)
    rim_col = (255, 215, 230, 255) if tier >= 2 else (210, 180, 190, 255)
    draw.polygon(shield, fill=s_col, outline=rim_col)

    # Warhammer handle
    draw.line([(cx - 85 - tier * 10, cy + 90), (cx + 85 + tier * 10, cy - 70)], fill=(130, 95, 65, 255), width=20 + tier * 4)
    # Hammer head
    head = [(cx + 50, cy - 105), (cx + 115, cy - 60), (cx + 90, cy - 30), (cx + 25, cy - 75)]
    draw.polygon(head, fill=(245, 230, 240, 255), outline=(150, 110, 130, 255))
    draw.polygon([(cx + 115, cy - 60), (cx + 140, cy - 40), (cx + 90, cy - 30)], fill=(255, 215, 70, 255))

    # Rose emblem center
    draw.ellipse([cx - 28, cy - 35, cx + 28, cy + 20], fill=(255, 115, 185, 255), outline=(255, 245, 250, 255))

    if tier >= 2:
        # Spikes / Rose thorns
        for offset in [-40, 40]:
            draw.polygon([(cx + offset, cy + 50), (cx + offset * 1.5, cy + 80), (cx + offset * 0.7, cy + 60)],
                         fill=(255, 220, 80, 255))

    if tier == 3:
        # Seismic Ground Aura / Thorn Aegis
        draw.arc([cx - 150, cy - 150, cx + 150, cy + 150], start=30, end=150, fill=(255, 190, 220, 240), width=16)
        draw.arc([cx - 150, cy - 150, cx + 150, cy + 150], start=210, end=330, fill=(255, 190, 220, 240), width=16)

def draw_rabbit_emblem(draw, tier):
    # Arcane Staff & Crystal Mana Orb
    draw.line([(cx - 45 - tier * 10, cy + 125), (cx + 25, cy - 85)], fill=(130, 100, 75, 255), width=18 + tier * 4)
    staff_top_col = (255, 225, 90, 255) if tier >= 2 else (190, 180, 150, 255)
    draw.arc([cx - 45, cy - 145, cx + 85, cy - 15], start=120, end=340, fill=staff_top_col, width=22)

    # Crystal Mana Orb
    orb_r = 30 + tier * 8
    draw.ellipse([cx + 15 - orb_r, cy - 75 - orb_r, cx + 15 + orb_r, cy - 75 + orb_r],
                 fill=(70, 225, 255, 255), outline=(255, 255, 255, 255))

    if tier >= 2:
        # Magic sparkles orbiting orb
        for a in [0, 90, 180, 270]:
            rad = math.radians(a + tier * 20)
            mx = cx + 15 + int(60 * math.cos(rad))
            my = cy - 75 + int(60 * math.sin(rad))
            draw_star(draw, mx, my, 12, (255, 255, 255, 255), (100, 230, 255, 255))

    if tier == 3:
        # Arcane Meteor Rings
        draw.arc([cx - 140, cy - 140, cx + 140, cy + 140], start=0, end=360, fill=(160, 120, 255, 220), width=14)
        for a in [45, 135, 225, 315]:
            rad = math.radians(a)
            sx = cx + int(140 * math.cos(rad))
            sy = cy + int(140 * math.sin(rad))
            draw_star(draw, sx, sy, 14, (255, 215, 60, 255))

def draw_wind_emblem(draw, tier):
    # Emerald Recurve Bow & Gale Arrowhead
    bow_pts = []
    r_bow = 180 + tier * 20
    for deg in range(-80, 81, 4):
        rad = math.radians(deg)
        x = cx - r_bow * 0.40 * math.cos(rad) + 40
        y = cy + r_bow * 0.70 * math.sin(rad)
        bow_pts.append((x, y))

    bow_col = (70, 240, 130, 255) if tier >= 2 else (80, 190, 100, 255)
    draw.line(bow_pts, fill=bow_col, width=28 + tier * 6)
    draw.line(bow_pts, fill=(230, 255, 190, 255), width=12)
    # Bow string
    draw.line([bow_pts[0], (cx - 60, cy), bow_pts[-1]], fill=(255, 255, 255, 240), width=8)
    # Arrow
    draw.line([(cx - 70, cy), (cx + 120 + tier * 20, cy)], fill=(255, 245, 180, 255), width=16)
    # Arrowhead
    hx = cx + 160 + tier * 20
    head = [(hx, cy), (hx - 70, cy - 42), (hx - 55, cy), (hx - 70, cy + 42)]
    draw.polygon(head, fill=(130, 255, 180, 255), outline=(255, 255, 240, 255))

    if tier >= 2:
        # Feathers & gale lines
        draw.line([(cx - 40, cy - 35), (cx - 75, cy)], fill=(120, 230, 150, 255), width=14)
        draw.line([(cx - 40, cy + 35), (cx - 75, cy)], fill=(120, 230, 150, 255), width=14)

    if tier == 3:
        # Zephyr Tornado Vortex
        draw.arc([cx - 150, cy - 150, cx + 150, cy + 150], start=45, end=225, fill=(180, 255, 150, 230), width=16)
        draw.arc([cx - 130, cy - 130, cx + 130, cy + 130], start=225, end=405, fill=(220, 255, 200, 230), width=12)

TOKEN_HEROES = [
    {
        'key': 'Cedric',
        'full_key': 'Cedric',
        'bg': (165, 25, 30, 255),
        'inner': (105, 15, 20, 255),
        'draw_fn': draw_cedric_emblem
    },
    {
        'key': 'Rose',
        'full_key': 'RoseTanker',
        'bg': (210, 60, 125, 255),
        'inner': (140, 30, 80, 255),
        'draw_fn': draw_rose_emblem
    },
    {
        'key': 'Rabbit',
        'full_key': 'RabbitMage',
        'bg': (30, 75, 185, 255),
        'inner': (15, 45, 125, 255),
        'draw_fn': draw_rabbit_emblem
    },
    {
        'key': 'Wind',
        'full_key': 'WindRanger',
        'bg': (25, 145, 70, 255),
        'inner': (15, 95, 45, 255),
        'draw_fn': draw_wind_emblem
    }
]

def generate_tokens():
    print(">>> Generating 12 Token Medallions (T1, T2, T3) with striking visual differences...")
    for h in TOKEN_HEROES:
        for tier in [1, 2, 3]:
            im = Image.new('RGBA', (S, S), (0, 0, 0, 0))
            d = ImageDraw.Draw(im)

            # Drop Shadow
            d.ellipse([14, 22, S - 14, S - 6], fill=(10, 10, 15, 180))

            # Rim & Outer Border Evolution:
            if tier == 1:
                # Tier 1: Clean Polished Bronze/Silver Steel
                rim_col = (195, 175, 145, 255)
                rim_dark = (50, 40, 30, 255)
                d.ellipse([24, 24, S - 24, S - 24], fill=rim_col, outline=rim_dark, width=10)
                d.ellipse([54, 54, S - 54, S - 54], fill=h['bg'], outline=h['inner'], width=10)
                d.ellipse([84, 84, S - 84, S - 84], fill=h['inner'])
            elif tier == 2:
                # Tier 2: Radiant Polished Gold with 4 Filigree Wings
                rim_col = (255, 215, 60, 255)
                rim_dark = (90, 60, 10, 255)
                # Outer wings/studs
                wing_d = 20
                d.ellipse([wing_d, cy - 40, wing_d + 50, cy + 40], fill=rim_col, outline=rim_dark, width=6)
                d.ellipse([S - wing_d - 50, cy - 40, S - wing_d, cy + 40], fill=rim_col, outline=rim_dark, width=6)
                d.ellipse([cx - 40, wing_d, cx + 40, wing_d + 50], fill=rim_col, outline=rim_dark, width=6)

                # Outer Golden Rim
                d.ellipse([20, 20, S - 20, S - 20], fill=rim_col, outline=rim_dark, width=12)
                d.ellipse([46, 46, S - 46, S - 46], fill=(255, 240, 130, 255), outline=rim_dark, width=6)
                d.ellipse([58, 58, S - 58, S - 58], fill=h['bg'], outline=h['inner'], width=10)
                d.ellipse([88, 88, S - 88, S - 88], fill=h['inner'])
            else:
                # Tier 3: Mythic Platinum / Prismatic Royal Diamond with Blazing Wings & Crests
                rim_col = (225, 245, 255, 255) # Platinum
                gold_accent = (255, 220, 70, 255)
                rim_dark = (30, 40, 60, 255)
                # Mythic Crown Spikes & Dragon Wings on outer rim
                for deg in [30, 70, 110, 150, 210, 250, 290, 330]:
                    rad = math.radians(deg)
                    px = cx + int(245 * math.cos(rad))
                    py = cy + int(245 * math.sin(rad))
                    draw_star(d, px, py, 26, gold_accent, outline_color=(255, 255, 255, 255))

                d.ellipse([16, 16, S - 16, S - 16], fill=rim_col, outline=rim_dark, width=14)
                d.ellipse([40, 40, S - 40, S - 40], fill=gold_accent, outline=rim_dark, width=8)
                d.ellipse([52, 52, S - 52, S - 52], fill=h['bg'], outline=h['inner'], width=10)
                d.ellipse([84, 84, S - 84, S - 84], fill=h['inner'])

                # Radiant Corona Arc
                d.arc([60, 60, S - 60, S - 60], start=0, end=360, fill=(255, 255, 255, 140), width=6)

            # Draw Hero Archetype Emblem
            h['draw_fn'](d, tier)

            # Stars / Rank Badge at Bottom
            star_y = cy + int((S // 2 - 30) * 0.70)
            draw_stars_badge(d, tier, tier, star_y, spacing=48, r=22)

            # Glossy Highlight Arc top-left
            d.arc([55, 55, S - 55, S - 55], start=205, end=335, fill=(255, 255, 255, 190), width=12)

            # Save as 256x256 (PPU 100)
            im_thumb = im.resize((256, 256), Image.Resampling.LANCZOS)

            suffix = "" if tier == 1 else f"_T{tier}"
            fn = f"Token_Medallion_{h['key']}{suffix}.png"
            p1 = os.path.join(GRID_DIR, fn)
            p2 = os.path.join(RESOURCES_GRID_DIR, fn)
            im_thumb.save(p1, 'PNG')
            im_thumb.save(p2, 'PNG')
            print(f"  Saved Token: {fn}")

# ==============================================================================
# PART 2: STANDING CHIBI HERO SPRITES (COMBAT & CHARACTER SHOWCASE)
# ==============================================================================

def draw_chibi_cedric(draw, tier):
    # Head & Helmet
    hx, hy = cx, cy - 50
    head_r = 85

    # Fluttering Red Cape (behind body)
    cape_w = 90 + tier * 25
    cape_pts = [(cx - 70, cy - 20), (cx + 70, cy - 20), (cx + cape_w, cy + 180), (cx - cape_w, cy + 180)]
    draw.polygon(cape_pts, fill=(185, 25, 30, 255), outline=(90, 10, 15, 255))
    if tier >= 2:
        # Cape Gold Trim
        draw.line([(cx - cape_w, cy + 180), (cx + cape_w, cy + 180)], fill=(255, 215, 60, 255), width=10)

    # Body Armor (Steel Plate)
    body = [(cx - 50, cy - 20), (cx + 50, cy - 20), (cx + 40, cy + 100), (cx - 40, cy + 100)]
    armor_col = (210, 220, 235, 255) if tier <= 2 else (235, 240, 255, 255)
    draw.polygon(body, fill=armor_col, outline=(40, 50, 70, 255))
    draw.rectangle([cx - 15, cy - 10, cx + 15, cy + 80], fill=(190, 25, 30, 255)) # Red tabard

    # Belt & Buckle
    draw.rectangle([cx - 42, cy + 70, cx + 42, cy + 90], fill=(60, 40, 25, 255))
    buckle_col = (255, 215, 50, 255) if tier >= 2 else (180, 180, 180, 255)
    draw.rectangle([cx - 16, cy + 66, cx + 16, cy + 94], fill=buckle_col, outline=(30, 20, 10, 255))

    # Legs & Boots
    draw.rectangle([cx - 38, cy + 100, cx - 10, cy + 180], fill=(160, 175, 195, 255), outline=(40, 50, 70, 255))
    draw.rectangle([cx + 10, cy + 100, cx + 38, cy + 180], fill=(160, 175, 195, 255), outline=(40, 50, 70, 255))
    draw.ellipse([cx - 45, cy + 160, cx - 5, cy + 195], fill=(50, 35, 25, 255))
    draw.ellipse([cx + 5, cy + 160, cx + 45, cy + 195], fill=(50, 35, 25, 255))

    # Big Chibi Head / Face
    draw.ellipse([hx - head_r, hy - head_r, hx + head_r, hy + head_r], fill=(255, 225, 205, 255)) # Skin tone

    # Red Hood / Knight Helm
    hood_pts = [
        (hx - head_r - 10, hy + 20), (hx - head_r - 5, hy - 60), (hx - 40, hy - head_r - 25),
        (hx + 40, hy - head_r - 25), (hx + head_r + 5, hy - 60), (hx + head_r + 10, hy + 20),
        (hx + 65, hy + 50), (hx - 65, hy + 50)
    ]
    draw.polygon(hood_pts, fill=(205, 30, 35, 255), outline=(95, 15, 20, 255))

    # Cheerful Big Chibi Eyes
    eye_y = hy + 5
    # Left eye
    draw.ellipse([hx - 45, eye_y - 20, hx - 15, eye_y + 20], fill=(30, 35, 55, 255))
    draw.ellipse([hx - 40, eye_y - 15, hx - 22, eye_y + 2], fill=(255, 255, 255, 255)) # Sparkle
    # Right eye
    draw.ellipse([hx + 15, eye_y - 20, hx + 45, eye_y + 20], fill=(30, 35, 55, 255))
    draw.ellipse([hx + 20, eye_y - 15, hx + 38, eye_y + 2], fill=(255, 255, 255, 255)) # Sparkle
    # Rosy Cheeks
    draw.ellipse([hx - 60, eye_y + 18, hx - 40, eye_y + 32], fill=(255, 140, 150, 180))
    draw.ellipse([hx + 40, eye_y + 18, hx + 60, eye_y + 32], fill=(255, 140, 150, 180))
    # Smile
    draw.arc([hx - 15, eye_y + 15, hx + 15, eye_y + 35], start=0, end=180, fill=(160, 40, 40, 255), width=5)

    # Arming Sword in Right Hand
    sx, sy = cx + 85, cy + 30
    draw.line([(sx, sy + 60), (sx + 35, sy - 110 - tier * 20)], fill=(235, 245, 255, 255), width=18 + tier * 4)
    draw.line([(sx - 30, sy - 20), (sx + 30, sy - 20)], fill=(255, 215, 60, 255), width=12) # Guard
    draw.ellipse([sx - 10, sy + 50, sx + 10, sy + 70], fill=(255, 215, 60, 255)) # Pommel

    if tier >= 2:
        # Dual Swords: Left hand sword
        lx, ly = cx - 85, cy + 30
        draw.line([(lx, ly + 60), (lx - 35, ly - 100)], fill=(235, 245, 255, 255), width=18)
        draw.line([(lx - 30, ly - 20), (lx + 30, ly - 20)], fill=(255, 215, 60, 255), width=12)

    if tier == 3:
        # Winged Gold Helmet Crest & Fire Aura
        draw.polygon([(hx, hy - head_r - 65), (hx - 30, hy - head_r - 20), (hx + 30, hy - head_r - 20)],
                     fill=(255, 220, 60, 255), outline=(130, 90, 10, 255))
        # Fiery blade aura
        draw.line([(sx, sy + 60), (sx + 35, sy - 130)], fill=(255, 100, 30, 180), width=32)

def draw_chibi_rose(draw, tier):
    # Head & Ponytail
    hx, hy = cx, cy - 50
    head_r = 85

    # Pink Ponytail (drawn behind)
    pt_pts = [(cx + 45, hy - 20), (cx + 120 + tier * 15, hy - 40), (cx + 140 + tier * 20, hy + 50), (cx + 70, hy + 20)]
    draw.polygon(pt_pts, fill=(245, 115, 175, 255), outline=(150, 40, 90, 255))
    draw.ellipse([cx + 50, hy - 30, cx + 75, hy - 5], fill=(255, 220, 80, 255)) # Hair tie

    # Armor Body (Rose Gold & White Plate)
    body = [(cx - 52, cy - 20), (cx + 52, cy - 20), (cx + 45, cy + 105), (cx - 45, cy + 105)]
    plate_col = (245, 210, 220, 255)
    draw.polygon(body, fill=plate_col, outline=(100, 50, 70, 255))
    # Rose Gold Chestpiece
    draw.ellipse([cx - 30, cy - 10, cx + 30, cy + 40], fill=(225, 135, 160, 255), outline=(255, 225, 235, 255))

    # Skirt Armor & Legs
    draw.polygon([(cx - 50, cy + 95), (cx + 50, cy + 95), (cx + 65, cy + 135), (cx - 65, cy + 135)],
                 fill=(210, 75, 130, 255), outline=(100, 30, 60, 255))
    draw.rectangle([cx - 38, cy + 135, cx - 12, cy + 185], fill=plate_col, outline=(100, 50, 70, 255))
    draw.rectangle([cx + 12, cy + 135, cx + 38, cy + 185], fill=plate_col, outline=(100, 50, 70, 255))
    draw.ellipse([cx - 45, cy + 165, cx - 5, cy + 195], fill=(80, 45, 55, 255))
    draw.ellipse([cx + 5, cy + 165, cx + 45, cy + 195], fill=(80, 45, 55, 255))

    # Chibi Face
    draw.ellipse([hx - head_r, hy - head_r, hx + head_r, hy + head_r], fill=(255, 228, 212, 255))

    # Bangs / Hair (Pink)
    bangs = [
        (hx - head_r, hy - 20), (hx - 60, hy - head_r), (hx, hy - head_r - 10), (hx + 60, hy - head_r),
        (hx + head_r, hy - 20), (hx + 50, hy - 40), (hx + 10, hy - 45), (hx - 30, hy - 35), (hx - 70, hy - 15)
    ]
    draw.polygon(bangs, fill=(245, 115, 175, 255), outline=(150, 40, 90, 255))

    # Big Sparkling Anime Eyes (Magenta-Rose)
    eye_y = hy + 5
    draw.ellipse([hx - 45, eye_y - 20, hx - 15, eye_y + 20], fill=(160, 35, 95, 255))
    draw.ellipse([hx - 40, eye_y - 15, hx - 22, eye_y + 2], fill=(255, 255, 255, 255))
    draw.ellipse([hx + 15, eye_y - 20, hx + 45, eye_y + 20], fill=(160, 35, 95, 255))
    draw.ellipse([hx + 20, eye_y - 15, hx + 38, eye_y + 2], fill=(255, 255, 255, 255))
    draw.ellipse([hx - 60, eye_y + 18, hx - 40, eye_y + 32], fill=(255, 140, 170, 190))
    draw.ellipse([hx + 40, eye_y + 18, hx + 60, eye_y + 32], fill=(255, 140, 170, 190))
    draw.arc([hx - 14, eye_y + 15, hx + 14, eye_y + 33], start=0, end=180, fill=(160, 30, 80, 255), width=5)

    # Tower Shield (Left Arm)
    sh_x, sh_y = cx - 85, cy + 40
    sh_w, sh_h = 45 + tier * 10, 85 + tier * 15
    draw.polygon([(sh_x - sh_w, sh_y - sh_h), (sh_x + sh_w, sh_y - sh_h),
                  (sh_x + sh_w - 10, sh_y + sh_h), (sh_x, sh_y + sh_h + 20), (sh_x - sh_w + 10, sh_y + sh_h)],
                 fill=(220, 70, 130, 255), outline=(255, 230, 240, 255))
    # Rose emblem on shield
    draw.ellipse([sh_x - 18, sh_y - 20, sh_x + 18, sh_y + 20], fill=(255, 215, 60, 255))

    # Heavy Warhammer (Right Arm)
    wh_x, wh_y = cx + 85, cy + 30
    draw.line([(wh_x - 30, wh_y + 90), (wh_x + 40, wh_y - 90 - tier * 15)], fill=(130, 95, 65, 255), width=18 + tier * 4)
    draw.polygon([(wh_x + 10, wh_y - 110), (wh_x + 75, wh_y - 70), (wh_x + 55, wh_y - 45), (wh_x - 10, wh_y - 85)],
                 fill=(240, 225, 235, 255), outline=(130, 80, 100, 255))

    if tier == 3:
        # Thorn Aegis ground cracks
        draw.arc([cx - 140, cy + 60, cx + 140, cy + 220], start=30, end=150, fill=(255, 215, 60, 240), width=14)

def draw_chibi_rabbit(draw, tier):
    # Head & Long Rabbit Ears
    hx, hy = cx, cy - 35
    head_r = 80

    # Long Bunny Ears (White & Pink)
    # Left Ear
    draw.polygon([(hx - 55, hy - 60), (hx - 95, hy - 185), (hx - 50, hy - 195), (hx - 25, hy - 70)],
                 fill=(250, 252, 255, 255), outline=(160, 175, 205, 255))
    draw.polygon([(hx - 50, hy - 75), (hx - 85, hy - 175), (hx - 55, hy - 180), (hx - 35, hy - 80)],
                 fill=(255, 185, 205, 255))
    # Right Ear
    draw.polygon([(hx + 25, hy - 70), (hx + 50, hy - 195), (hx + 95, hy - 185), (hx + 55, hy - 60)],
                 fill=(250, 252, 255, 255), outline=(160, 175, 205, 255))
    draw.polygon([(hx + 35, hy - 80), (hx + 55, hy - 180), (hx + 85, hy - 175), (hx + 50, hy - 75)],
                 fill=(255, 185, 205, 255))

    # Wizard Robe (Blue & Gold)
    robe_w = 75 + tier * 20
    robe = [(cx - 45, cy + 10), (cx + 45, cy + 10), (cx + robe_w, cy + 180), (cx - robe_w, cy + 180)]
    draw.polygon(robe, fill=(35, 75, 180, 255), outline=(15, 35, 95, 255))
    # Golden Robe Hem
    draw.line([(cx - robe_w, cy + 180), (cx + robe_w, cy + 180)], fill=(255, 220, 60, 255), width=10)

    # Chibi Rabbit Head
    draw.ellipse([hx - head_r, hy - head_r, hx + head_r, hy + head_r], fill=(250, 252, 255, 255), outline=(170, 185, 210, 255))

    # Wizard Hat
    hat_pts = [(hx - 90, hy - 40), (hx + 90, hy - 40), (hx + 20, hy - 150 - tier * 15), (hx - 40, hy - 150 - tier * 15)]
    draw.polygon(hat_pts, fill=(45, 90, 205, 255), outline=(20, 45, 120, 255))
    # Hat Brim
    draw.ellipse([hx - 95, hy - 55, hx + 95, hy - 30], fill=(30, 65, 160, 255), outline=(255, 220, 70, 255))
    # Star on hat
    draw_star(draw, hx - 10, hy - 110, 18, (255, 225, 70, 255))

    # Cute Rabbit Face (Red-Ruby Anime Eyes)
    eye_y = hy + 10
    draw.ellipse([hx - 42, eye_y - 18, hx - 18, eye_y + 18], fill=(225, 35, 75, 255))
    draw.ellipse([hx - 38, eye_y - 14, hx - 24, eye_y + 2], fill=(255, 255, 255, 255))
    draw.ellipse([hx + 18, eye_y - 18, hx + 42, eye_y + 18], fill=(225, 35, 75, 255))
    draw.ellipse([hx + 24, eye_y - 14, hx + 38, eye_y + 2], fill=(255, 255, 255, 255))
    # Rabbit Nose & Mouth
    draw.polygon([(hx - 8, eye_y + 20), (hx + 8, eye_y + 20), (hx, eye_y + 28)], fill=(255, 140, 160, 255))
    draw.arc([hx - 14, eye_y + 24, hx, eye_y + 36], start=0, end=180, fill=(120, 60, 80, 255), width=4)
    draw.arc([hx, eye_y + 24, hx + 14, eye_y + 36], start=0, end=180, fill=(120, 60, 80, 255), width=4)

    # Magic Staff (Cyan Crystal Orb)
    st_x, st_y = cx + 80, cy + 30
    draw.line([(st_x - 15, st_y + 120), (st_x + 15, st_y - 100 - tier * 15)], fill=(140, 105, 75, 255), width=18)
    draw.arc([st_x - 30, st_y - 140 - tier * 15, st_x + 60, st_y - 50 - tier * 15], start=120, end=340, fill=(255, 225, 70, 255), width=18)
    # Mana Orb
    orb_r = 28 + tier * 6
    draw.ellipse([st_x + 15 - orb_r, st_y - 95 - tier * 15 - orb_r, st_x + 15 + orb_r, st_y - 95 - tier * 15 + orb_r],
                 fill=(70, 230, 255, 255), outline=(255, 255, 255, 255))

    if tier >= 2:
        # Magic Sparkles orbiting
        for a in [0, 120, 240]:
            rad = math.radians(a)
            sx = st_x + 15 + int(50 * math.cos(rad))
            sy = st_y - 95 - tier * 15 + int(50 * math.sin(rad))
            draw_star(draw, sx, sy, 12, (255, 255, 255, 255), (90, 230, 255, 255))

    if tier == 3:
        # Falling Cosmic Meteor Auras
        draw.arc([cx - 150, cy - 80, cx + 150, cy + 220], start=0, end=360, fill=(170, 120, 255, 180), width=12)

def draw_chibi_wind(draw, tier):
    # Head & Emerald Hood
    hx, hy = cx, cy - 50
    head_r = 82

    # Wind Cloak (Fluttering Emerald Green)
    cloak_w = 80 + tier * 25
    cloak = [(cx - 50, cy - 20), (cx + 50, cy - 20), (cx + cloak_w, cy + 180), (cx - cloak_w, cy + 180)]
    draw.polygon(cloak, fill=(35, 155, 75, 255), outline=(15, 80, 40, 255))
    if tier >= 2:
        # Golden Wind Wave Pattern on Cloak
        draw.line([(cx - cloak_w, cy + 175), (cx + cloak_w, cy + 175)], fill=(230, 255, 160, 255), width=10)

    # Leather Scout Tunic & Boots
    draw.rectangle([cx - 42, cy - 10, cx + 42, cy + 90], fill=(135, 95, 60, 255), outline=(60, 40, 20, 255))
    draw.rectangle([cx - 40, cy + 60, cx + 40, cy + 78], fill=(50, 35, 20, 255)) # Belt
    draw.rectangle([cx - 35, cy + 90, cx - 12, cy + 180], fill=(70, 130, 80, 255), outline=(20, 60, 30, 255))
    draw.rectangle([cx + 12, cy + 90, cx + 35, cy + 180], fill=(70, 130, 80, 255), outline=(20, 60, 30, 255))
    draw.ellipse([cx - 40, cy + 160, cx - 8, cy + 192], fill=(60, 40, 25, 255))
    draw.ellipse([cx + 8, cy + 160, cx + 40, cy + 192], fill=(60, 40, 25, 255))

    # Chibi Elf Face
    draw.ellipse([hx - head_r, hy - head_r, hx + head_r, hy + head_r], fill=(255, 230, 215, 255))

    # Pointed Elf Ears
    draw.polygon([(hx - 70, hy), (hx - 125, hy - 35), (hx - 60, hy - 30)], fill=(255, 220, 200, 255), outline=(180, 120, 100, 255))
    draw.polygon([(hx + 70, hy), (hx + 125, hy - 35), (hx + 60, hy - 30)], fill=(255, 220, 200, 255), outline=(180, 120, 100, 255))

    # Emerald Hood
    hood_pts = [
        (hx - head_r - 8, hy + 20), (hx - head_r, hy - 60), (hx - 30, hy - head_r - 20),
        (hx + 30, hy - head_r - 20), (hx + head_r, hy - 60), (hx + head_r + 8, hy + 20),
        (hx + 55, hy + 45), (hx - 55, hy + 45)
    ]
    draw.polygon(hood_pts, fill=(40, 170, 85, 255), outline=(15, 90, 45, 255))

    # Bright Emerald Eyes
    eye_y = hy + 5
    draw.ellipse([hx - 44, eye_y - 20, hx - 16, eye_y + 20], fill=(25, 135, 65, 255))
    draw.ellipse([hx - 40, eye_y - 15, hx - 22, eye_y + 2], fill=(255, 255, 255, 255))
    draw.ellipse([hx + 16, eye_y - 20, hx + 44, eye_y + 20], fill=(25, 135, 65, 255))
    draw.ellipse([hx + 22, eye_y - 15, hx + 38, eye_y + 2], fill=(255, 255, 255, 255))
    draw.ellipse([hx - 60, eye_y + 18, hx - 40, eye_y + 32], fill=(255, 150, 160, 180))
    draw.ellipse([hx + 40, eye_y + 18, hx + 60, eye_y + 32], fill=(255, 150, 160, 180))
    draw.arc([hx - 14, eye_y + 15, hx + 14, eye_y + 33], start=0, end=180, fill=(130, 45, 45, 255), width=5)

    # Composite Bow (Left Hand)
    bx, by = cx - 85, cy + 20
    bow_pts = []
    r_bow = 150 + tier * 20
    for deg in range(-75, 76, 5):
        rad = math.radians(deg)
        x = bx - r_bow * 0.35 * math.cos(rad) + 20
        y = by + r_bow * 0.65 * math.sin(rad)
        bow_pts.append((x, y))
    draw.line(bow_pts, fill=(80, 230, 120, 255), width=20 + tier * 4)
    draw.line([bow_pts[0], (bx + 10, by), bow_pts[-1]], fill=(255, 255, 255, 240), width=6)

    # Quiver & Wind Arrow (Right Hand)
    draw.line([(cx + 40, cy + 50), (cx + 90 + tier * 15, cy - 70)], fill=(245, 255, 190, 255), width=12)

    if tier == 3:
        # Zephyr Tornado Aura Wings
        draw.arc([cx - 150, cy - 80, cx + 150, cy + 220], start=45, end=225, fill=(140, 255, 180, 210), width=14)

HERO_CONFIGS = [
    ('Cedric', draw_chibi_cedric),
    ('Rose', draw_chibi_rose),
    ('RabbitMage', draw_chibi_rabbit),
    ('WindRanger', draw_chibi_wind)
]

def generate_heroes():
    print(">>> Generating 12 Full-Body Standing Chibi Heroes (NO circular coins!)...")
    for name, draw_fn in HERO_CONFIGS:
        for tier in [1, 2, 3]:
            im = Image.new('RGBA', (S, S), (0, 0, 0, 0))
            d = ImageDraw.Draw(im)

            # Soft Ground Shadow (Chibi character standing on ground)
            d.ellipse([cx - 110, S - 95, cx + 110, S - 35], fill=(15, 20, 30, 120))

            # Draw Chibi Character
            draw_fn(d, tier)

            # Downsample to 256x256 (PPU 100)
            im_thumb = im.resize((256, 256), Image.Resampling.LANCZOS)

            fname = f"{name}_T{tier}.png"
            out_p = os.path.join(HERO_DIR, fname)
            im_thumb.save(out_p, 'PNG')
            print(f"  Saved Full Hero: {fname}")

if __name__ == "__main__":
    generate_tokens()
    generate_heroes()
    print("ALL ASSETS SUCCESSFULLY GENERATED AND EXPORTED!")
