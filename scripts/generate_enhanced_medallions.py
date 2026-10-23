import os
import math
from PIL import Image, ImageDraw, ImageFilter

DEST_DIR = r"D:\PROJECT GAMES\Rampartbound\Assets\Art\Sprites\Grid"
os.makedirs(DEST_DIR, exist_ok=True)

# Helper supersampled drawing (draw at 512x512, downsample to 128x128 for razor-sharp AA)
S = 512
cx, cy = S // 2, S // 2
r = S // 2 - 24

def draw_cedric_emblem(draw):
    # Knight Greatsword pointing downward
    blade = [(cx - 24, cy - 140), (cx + 24, cy - 140), (cx + 18, cy + 80), (cx, cy + 130), (cx - 18, cy + 80)]
    draw.polygon(blade, fill=(240, 245, 255, 255), outline=(180, 195, 215, 255))
    draw.line([(cx, cy - 130), (cx, cy + 70)], fill=(160, 180, 205, 255), width=8)
    guard = [(cx - 90, cy - 85), (cx + 90, cy - 85), (cx + 80, cy - 55), (cx - 80, cy - 55)]
    draw.polygon(guard, fill=(255, 215, 50, 255), outline=(180, 130, 20, 255))
    draw.rectangle([cx - 12, cy - 140, cx + 12, cy - 85], fill=(80, 20, 20, 255))
    draw.ellipse([cx - 22, cy - 170, cx + 22, cy - 130], fill=(255, 215, 50, 255), outline=(180, 130, 20, 255))
    draw.ellipse([cx - 10, cy - 158, cx + 10, cy - 142], fill=(220, 30, 30, 255))

def draw_rose_emblem(draw):
    # Warhammer & Shield
    shield = [(cx - 80, cy - 100), (cx + 80, cy - 100), (cx + 70, cy + 25), (cx, cy + 110), (cx - 70, cy + 25)]
    draw.polygon(shield, fill=(180, 50, 110, 255), outline=(255, 200, 220, 255))
    draw.line([(cx - 70, cy + 85), (cx + 70, cy - 55)], fill=(120, 90, 60, 255), width=18)
    head = [(cx + 40, cy - 90), (cx + 95, cy - 50), (cx + 75, cy - 25), (cx + 20, cy - 65)]
    draw.polygon(head, fill=(245, 225, 235, 255), outline=(160, 120, 140, 255))
    draw.polygon([(cx + 95, cy - 50), (cx + 120, cy - 30), (cx + 75, cy - 25)], fill=(255, 215, 70, 255))
    draw.ellipse([cx - 25, cy - 40, cx + 25, cy + 10], fill=(255, 105, 180, 255), outline=(255, 240, 250, 255))

def draw_rabbit_emblem(draw):
    # Arcane Staff & Crystal Mana Orb
    draw.line([(cx - 40, cy + 120), (cx + 20, cy - 80)], fill=(140, 110, 80, 255), width=16)
    draw.arc([cx - 40, cy - 140, cx + 80, cy - 20], start=120, end=340, fill=(255, 225, 100, 255), width=20)
    draw.ellipse([cx - 5, cy - 100, cx + 45, cy - 50], fill=(70, 220, 255, 255), outline=(255, 255, 255, 255))
    pts = [(cx + 20, cy - 145), (cx + 28, cy - 115), (cx + 58, cy - 115), (cx + 34, cy - 95),
           (cx + 42, cy - 65), (cx + 20, cy - 85), (cx - 2, cy - 65), (cx + 6, cy - 95),
           (cx - 18, cy - 115), (cx + 12, cy - 115)]
    draw.polygon(pts, fill=(220, 245, 255, 255))

def draw_wind_emblem(draw):
    # Emerald Recurve Bow & Gale Arrowhead
    bow_pts = []
    for deg in range(-80, 81, 4):
        rad = math.radians(deg)
        x = cx - r * 0.40 * math.cos(rad) + 40
        y = cy + r * 0.70 * math.sin(rad)
        bow_pts.append((x, y))
    draw.line(bow_pts, fill=(80, 235, 120, 255), width=28)
    draw.line(bow_pts, fill=(230, 255, 180, 255), width=12)
    draw.line([bow_pts[0], (cx - 60, cy), bow_pts[-1]], fill=(255, 255, 255, 230), width=8)
    draw.line([(cx - 70, cy), (cx + 120, cy)], fill=(255, 245, 190, 255), width=14)
    head = [(cx + 160, cy), (cx + 90, cy - 42), (cx + 105, cy), (cx + 90, cy + 42)]
    draw.polygon(head, fill=(120, 255, 170, 255), outline=(255, 255, 240, 255))
    draw.line([(cx - 40, cy - 28), (cx - 70, cy)], fill=(100, 220, 140, 255), width=12)
    draw.line([(cx - 40, cy + 28), (cx - 70, cy)], fill=(100, 220, 140, 255), width=12)

def draw_stars(draw, count, tier_color):
    star_r = 20
    spacing = 46
    start_x = cx - ((count - 1) * spacing) // 2
    y = cy + r * 0.65
    for i in range(count):
        sx = start_x + (i * spacing)
        pts = []
        for p in range(10):
            ang = math.radians(p * 36 - 90)
            cur_r = star_r if p % 2 == 0 else star_r * 0.45
            pts.append((sx + cur_r * math.cos(ang), y + cur_r * math.sin(ang)))
        draw.polygon(pts, fill=tier_color, outline=(255, 255, 255, 255))

HEROES = [
    {
        'key': 'Cedric',
        'bg': (160, 25, 25, 255),
        'rim': (240, 195, 60, 255),
        'inner': (110, 15, 15, 255),
        'draw_fn': draw_cedric_emblem
    },
    {
        'key': 'Rose',
        'bg': (205, 65, 125, 255),
        'rim': (250, 205, 215, 255),
        'inner': (145, 35, 85, 255),
        'draw_fn': draw_rose_emblem
    },
    {
        'key': 'Rabbit',
        'bg': (25, 75, 180, 255),
        'rim': (130, 215, 255, 255),
        'inner': (15, 45, 130, 255),
        'draw_fn': draw_rabbit_emblem
    },
    {
        'key': 'Wind',
        'bg': (25, 145, 65, 255),
        'rim': (180, 245, 110, 255),
        'inner': (15, 95, 40, 255),
        'draw_fn': draw_wind_emblem
    }
]

TIER_CONFIGS = [
    (1, (240, 180, 80, 255), ''),           # T1: base filename
    (2, (255, 220, 60, 255), '_T2'),        # T2: 2 stars, radiant
    (3, (255, 245, 180, 255), '_T3')        # T3: 3 stars, mythic
]

for h in HEROES:
    for tier, star_color, suffix in TIER_CONFIGS:
        im = Image.new('RGBA', (S, S), (0, 0, 0, 0))
        d = ImageDraw.Draw(im)
        
        # Drop shadow
        d.ellipse([16, 24, S - 16, S - 8], fill=(10, 10, 15, 170))
        # Outer Metallic Beveled Rim
        d.ellipse([20, 20, S - 20, S - 20], fill=h['rim'], outline=(35, 25, 10, 255), width=8)
        # Inner Background
        d.ellipse([50, 50, S - 50, S - 50], fill=h['bg'], outline=h['inner'], width=10)
        d.ellipse([80, 80, S - 80, S - 80], fill=h['inner'])
        
        # Hero specific emblem
        h['draw_fn'](d)
        
        # Tier stars indicator
        draw_stars(d, tier, star_color)
        
        # Shine arc at top
        d.arc([55, 55, S - 55, S - 55], start=200, end=340, fill=(255, 255, 255, 180), width=10)
        
        # Downsample to 128x128 for razor-sharp AA
        im_thumb = im.resize((128, 128), Image.Resampling.LANCZOS)
        
        fname = f"Token_Medallion_{h['key']}{suffix}.png"
        out_p = os.path.join(DEST_DIR, fname)
        im_thumb.save(out_p, 'PNG')
        print(f"Saved: {fname}")

print("All enhanced hero medallions successfully generated!")
