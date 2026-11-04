import os
import math
from PIL import Image, ImageDraw, ImageFilter

OUTPUT_DIR = r"D:\Hermes Project\Rampartbound\Assets\Art\Sprites\UI"
os.makedirs(OUTPUT_DIR, exist_ok=True)

SIZE = 1024  # 4x supersampling down to 256x256

def draw_radial_gradient(draw, bbox, color_center, color_edge):
    x0, y0, x1, y1 = bbox
    cx, cy = (x0 + x1) / 2, (y0 + y1) / 2
    rx, ry = (x1 - x0) / 2, (y1 - y0) / 2
    steps = 24
    for i in range(steps, 0, -1):
        ratio = i / float(steps)
        r = int(color_center[0] * (1 - ratio) + color_edge[0] * ratio)
        g = int(color_center[1] * (1 - ratio) + color_edge[1] * ratio)
        b = int(color_center[2] * (1 - ratio) + color_edge[2] * ratio)
        a = int(color_center[3] * (1 - ratio) + color_edge[3] * ratio)
        cur_bbox = [cx - rx * ratio, cy - ry * ratio, cx + rx * ratio, cy + ry * ratio]
        draw.ellipse(cur_bbox, fill=(r, g, b, a))

def generate_citadel_icon():
    im = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    draw = ImageDraw.Draw(im)

    # 1. Soft contact shadow at base
    shadow = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    sdraw = ImageDraw.Draw(shadow)
    sdraw.ellipse([200, 800, 824, 910], fill=(0, 0, 0, 110))
    shadow = shadow.filter(ImageFilter.GaussianBlur(24))
    im = Image.alpha_composite(im, shadow)
    draw = ImageDraw.Draw(im)

    # 2. Golden ambient backglow behind roofs
    glow = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    gdraw = ImageDraw.Draw(glow)
    gdraw.ellipse([260, 220, 764, 700], fill=(255, 215, 80, 75))
    glow = glow.filter(ImageFilter.GaussianBlur(36))
    im = Image.alpha_composite(im, glow)
    draw = ImageDraw.Draw(im)

    # 3. Main Fortress Stonework Foundations
    # Side towers foundations
    draw.rounded_rectangle([220, 480, 390, 840], radius=16, fill=(232, 228, 220, 255), outline=(78, 56, 38, 255), width=14)
    draw.rounded_rectangle([634, 480, 804, 840], radius=16, fill=(225, 220, 212, 255), outline=(78, 56, 38, 255), width=14)
    # Center tower foundation
    draw.rounded_rectangle([350, 420, 674, 840], radius=18, fill=(245, 240, 232, 255), outline=(78, 56, 38, 255), width=16)

    # Crenellations (Battlements) on towers
    for x in [230, 280, 330]:
        draw.rectangle([x, 460, x + 35, 485], fill=(240, 235, 228, 255), outline=(78, 56, 38, 255), width=10)
    for x in [644, 694, 744]:
        draw.rectangle([x, 460, x + 35, 485], fill=(235, 230, 222, 255), outline=(78, 56, 38, 255), width=10)
    for x in [370, 430, 490, 550, 610]:
        draw.rectangle([x, 400, x + 40, 430], fill=(250, 245, 238, 255), outline=(78, 56, 38, 255), width=10)

    # Masonry stone courses lines
    for y in [540, 600, 660, 720, 780]:
        draw.line([(365, y), (660, y)], fill=(195, 185, 172, 255), width=6)
        draw.line([(232, y), (375, y)], fill=(185, 175, 162, 255), width=5)
        draw.line([(645, y), (790, y)], fill=(185, 175, 162, 255), width=5)

    # Arched Castle Gateway
    draw.chord([440, 650, 584, 840], start=180, end=0, fill=(90, 52, 28, 255), outline=(55, 30, 15, 255), width=12)
    # Inner gate golden warm light
    draw.chord([460, 680, 564, 836], start=180, end=0, fill=(255, 205, 70, 255))
    draw.chord([480, 710, 544, 836], start=180, end=0, fill=(255, 240, 160, 255))
    # Portcullis grate lines
    for gx in [485, 512, 539]:
        draw.line([(gx, 690), (gx, 836)], fill=(70, 40, 20, 255), width=5)
    for gy in [730, 765, 800]:
        draw.line([(465, gy), (560, gy)], fill=(70, 40, 20, 255), width=5)

    # Windows on towers
    for wx, wy in [(290, 580), (700, 580), (512, 500)]:
        draw.chord([wx - 26, wy - 35, wx + 26, wy + 35], start=180, end=0, fill=(35, 45, 68, 255), outline=(78, 56, 38, 255), width=8)
        draw.ellipse([wx - 8, wy - 10, wx + 8, wy + 15], fill=(255, 220, 90, 220))

    # 4. Vibrant Blue Conical Roofs
    # Left cone
    left_cone = [(305, 170), (195, 465), (415, 465)]
    draw.polygon(left_cone, fill=(28, 125, 228, 255), outline=(14, 55, 115, 255))
    draw.polygon([(305, 170), (280, 465), (415, 465)], fill=(18, 95, 185, 255))
    # Specular roof highlight
    draw.line([(295, 200), (225, 450)], fill=(95, 180, 255, 200), width=10)

    # Right cone
    right_cone = [(719, 170), (609, 465), (829, 465)]
    draw.polygon(right_cone, fill=(24, 115, 218, 255), outline=(14, 55, 115, 255))
    draw.polygon([(719, 170), (694, 465), (829, 465)], fill=(16, 85, 175, 255))
    draw.line([(709, 200), (639, 450)], fill=(90, 175, 255, 200), width=10)

    # Tall Center Cone
    center_cone = [(512, 80), (370, 405), (654, 405)]
    draw.polygon(center_cone, fill=(35, 145, 245, 255), outline=(15, 65, 135, 255))
    draw.polygon([(512, 80), (490, 405), (654, 405)], fill=(22, 110, 200, 255))
    draw.line([(502, 115), (410, 390)], fill=(125, 200, 255, 230), width=14)

    # 5. Golden Finials and Red Pennants
    for px, py in [(305, 170), (719, 170), (512, 80)]:
        # Gold ball finial
        draw.ellipse([px - 18, py - 20, px + 18, py + 16], fill=(255, 215, 50, 255), outline=(145, 95, 15, 255), width=7)
        draw.ellipse([px - 8, py - 12, px + 4, py], fill=(255, 245, 170, 255))
        # Fluttering red pennant
        draw.polygon([(px + 10, py - 16), (px + 75, py), (px + 10, py + 16)], fill=(235, 45, 55, 255), outline=(135, 18, 25, 255))

    return im.resize((256, 256), Image.Resampling.LANCZOS)

def generate_forge_icon():
    im = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))

    # 1. Soft contact shadow at base
    shadow = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    sdraw = ImageDraw.Draw(shadow)
    sdraw.ellipse([240, 800, 784, 895], fill=(0, 0, 0, 110))
    shadow = shadow.filter(ImageFilter.GaussianBlur(24))
    im = Image.alpha_composite(im, shadow)

    # 2. Fiery furnace radiant heat glow
    glow = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    gdraw = ImageDraw.Draw(glow)
    gdraw.ellipse([260, 260, 764, 760], fill=(255, 140, 20, 105))
    gdraw.ellipse([340, 320, 684, 660], fill=(255, 210, 60, 140))
    glow = glow.filter(ImageFilter.GaussianBlur(32))
    im = Image.alpha_composite(im, glow)
    draw = ImageDraw.Draw(im)

    # 3. Tool / Rod protruding upward left
    draw.line([(310, 230), (512, 560)], fill=(130, 85, 45, 255), width=28)
    draw.line([(300, 220), (502, 550)], fill=(195, 145, 90, 255), width=10)
    # Hammer/Pestle head
    draw.rounded_rectangle([250, 180, 350, 260], radius=14, fill=(185, 195, 210, 255), outline=(65, 75, 90, 255), width=10)

    # 4. Golden Crucible Body & Base Pedestal
    # Flared base
    draw.ellipse([340, 770, 684, 850], fill=(215, 150, 28, 255), outline=(110, 70, 12, 255), width=14)
    draw.polygon([(380, 740), (644, 740), (670, 810), (354, 810)], fill=(235, 175, 35, 255), outline=(110, 70, 12, 255))

    # Crucible Side Handles
    draw.arc([190, 480, 330, 640], start=90, end=270, fill=(245, 185, 40, 255), width=24)
    draw.arc([190, 480, 330, 640], start=90, end=270, fill=(120, 80, 15, 255), width=6)
    draw.arc([694, 480, 834, 640], start=270, end=90, fill=(245, 185, 40, 255), width=24)
    draw.arc([694, 480, 834, 640], start=270, end=90, fill=(120, 80, 15, 255), width=6)

    # Main Chalice Bowl
    draw.chord([260, 420, 764, 790], start=0, end=180, fill=(248, 188, 42, 255), outline=(115, 72, 14, 255), width=16)
    # Shading and metallic specular curve
    draw.arc([310, 460, 714, 750], start=20, end=160, fill=(255, 235, 130, 230), width=18)
    draw.arc([350, 500, 674, 730], start=30, end=150, fill=(255, 250, 190, 255), width=8)

    # 5. Golden Lip Rim and Molten Core
    # Outer golden rim
    draw.ellipse([250, 390, 774, 480], fill=(255, 205, 55, 255), outline=(125, 80, 16, 255), width=14)
    draw.ellipse([275, 405, 749, 465], fill=(255, 240, 140, 255), width=6)

    # Molten metal inside
    draw.ellipse([285, 412, 739, 468], fill=(255, 75, 20, 255))
    draw.ellipse([325, 420, 699, 462], fill=(255, 170, 35, 255))
    draw.ellipse([390, 426, 634, 456], fill=(255, 245, 110, 255))

    # Fiery embers / molten swirl peak
    draw.polygon([(512, 330), (470, 435), (554, 435)], fill=(255, 140, 25, 255), outline=(180, 45, 10, 255))
    draw.polygon([(512, 350), (485, 430), (539, 430)], fill=(255, 240, 120, 255))

    return im.resize((256, 256), Image.Resampling.LANCZOS)

def generate_heroes_icon():
    im = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))

    # 1. Soft contact shadow at base
    shadow = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    sdraw = ImageDraw.Draw(shadow)
    sdraw.ellipse([260, 810, 764, 895], fill=(0, 0, 0, 110))
    shadow = shadow.filter(ImageFilter.GaussianBlur(24))
    im = Image.alpha_composite(im, shadow)
    draw = ImageDraw.Draw(im)

    # 2. Golden Winged Crest (Valkyrie / Paladin wings sweeping upward)
    # Left wing feathers
    l_wing = [(440, 460), (220, 230), (190, 340), (140, 290), (160, 440), (220, 490), (370, 560)]
    draw.polygon(l_wing, fill=(248, 192, 45, 255), outline=(125, 82, 18, 255))
    draw.line([(220, 230), (370, 490)], fill=(255, 240, 150, 255), width=10)
    draw.line([(140, 290), (300, 480)], fill=(255, 235, 130, 255), width=8)

    # Right wing feathers
    r_wing = [(584, 460), (804, 230), (834, 340), (884, 290), (864, 440), (804, 490), (654, 560)]
    draw.polygon(r_wing, fill=(235, 178, 38, 255), outline=(125, 82, 18, 255))
    draw.line([(804, 230), (654, 490)], fill=(255, 235, 140, 255), width=10)
    draw.line([(884, 290), (724, 480)], fill=(255, 230, 120, 255), width=8)

    # 3. Steel Knight Greathelm Body
    # Dome head
    draw.chord([310, 360, 714, 760], start=180, end=0, fill=(215, 225, 238, 255), outline=(65, 78, 96, 255), width=16)
    # Cheek guards and chin
    draw.polygon([(310, 560), (714, 560), (684, 810), (512, 850), (340, 810)], fill=(195, 208, 224, 255), outline=(65, 78, 96, 255))
    # Specular steel highlights
    draw.arc([350, 390, 674, 710], start=110, end=170, fill=(255, 255, 255, 240), width=16)
    draw.line([(365, 570), (380, 780)], fill=(255, 255, 255, 200), width=10)

    # 4. Golden Brow Circlet / Crown Band
    draw.rounded_rectangle([290, 520, 734, 590], radius=12, fill=(255, 205, 50, 255), outline=(130, 85, 18, 255), width=12)
    draw.line([(310, 535), (714, 535)], fill=(255, 245, 170, 255), width=6)

    # 5. Faceted Ruby Gemstone in Forehead Center
    # Gold gem bezel
    draw.polygon([(512, 420), (580, 520), (512, 580), (444, 520)], fill=(215, 150, 28, 255), outline=(110, 70, 12, 255))
    # Ruby core
    ruby_pts = [(512, 435), (568, 520), (512, 565), (456, 520)]
    draw.polygon(ruby_pts, fill=(235, 30, 45, 255), outline=(115, 12, 20, 255))
    # Gem facets & glint
    draw.polygon([(512, 435), (512, 520), (456, 520)], fill=(255, 95, 110, 255))
    draw.polygon([(512, 435), (540, 480), (512, 520)], fill=(255, 180, 190, 255))
    draw.ellipse([495, 460, 510, 475], fill=(255, 255, 255, 255))

    # 6. Visor T-Slit (Nasal & Eye Slits)
    draw.rectangle([492, 590, 532, 750], fill=(28, 36, 48, 255), outline=(50, 62, 78, 255), width=6)
    draw.rectangle([390, 615, 634, 655], fill=(28, 36, 48, 255), outline=(50, 62, 78, 255), width=6)
    # Cyan eye glow behind slit
    draw.ellipse([430, 625, 470, 645], fill=(60, 235, 255, 240))
    draw.ellipse([554, 625, 594, 645], fill=(60, 235, 255, 240))

    return im.resize((256, 256), Image.Resampling.LANCZOS)

def generate_bag_icon():
    im = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))

    # 1. Soft contact shadow at base
    shadow = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    sdraw = ImageDraw.Draw(shadow)
    sdraw.ellipse([250, 810, 774, 895], fill=(0, 0, 0, 110))
    shadow = shadow.filter(ImageFilter.GaussianBlur(24))
    im = Image.alpha_composite(im, shadow)
    draw = ImageDraw.Draw(im)

    # 2. Rolled Teal/Emerald Traveler Blanket on Top
    draw.rounded_rectangle([250, 240, 774, 380], radius=40, fill=(38, 145, 135, 255), outline=(18, 70, 65, 255), width=14)
    # Blanket fold creases and highlight
    draw.line([(280, 310), (744, 310)], fill=(75, 205, 190, 255), width=10)
    draw.line([(280, 335), (744, 335)], fill=(25, 95, 88, 255), width=8)

    # Blanket leather straps & buckles
    for sx in [360, 664]:
        draw.rounded_rectangle([sx - 24, 230, sx + 24, 390], radius=10, fill=(165, 105, 50, 255), outline=(85, 48, 20, 255), width=8)
        draw.rectangle([sx - 18, 290, sx + 18, 330], fill=(255, 215, 60, 255), outline=(130, 85, 18, 255), width=6)

    # 3. Main Leather Rucksack Body
    # Rich chestnut leather
    draw.rounded_rectangle([270, 350, 754, 825], radius=38, fill=(158, 96, 48, 255), outline=(78, 42, 16, 255), width=16)
    # Specular leather highlight
    draw.arc([300, 380, 724, 800], start=100, end=170, fill=(210, 145, 95, 220), width=16)

    # 4. Flap and Straps
    draw.polygon([(270, 350), (754, 350), (720, 560), (512, 630), (304, 560)], fill=(182, 116, 62, 255), outline=(78, 42, 16, 255))
    # Stitched seam line along flap
    draw.polygon([(290, 365), (734, 365), (705, 545), (512, 610), (319, 545)], fill=None, outline=(235, 185, 135, 200), width=4)

    # Side leather pouches
    draw.rounded_rectangle([200, 480, 285, 710], radius=20, fill=(145, 85, 40, 255), outline=(78, 42, 16, 255), width=10)
    draw.rounded_rectangle([739, 480, 824, 710], radius=20, fill=(140, 80, 36, 255), outline=(78, 42, 16, 255), width=10)

    # Vertical gold-trimmed leather straps
    for sx in [370, 654]:
        draw.rounded_rectangle([sx - 20, 370, sx + 20, 780], radius=10, fill=(235, 175, 45, 255), outline=(115, 72, 14, 255), width=8)
        draw.line([(sx, 380), (sx, 770)], fill=(255, 235, 140, 255), width=5)

    # 5. Golden Center Clasp / Diamond Buckle
    draw.polygon([(512, 530), (580, 600), (512, 670), (444, 600)], fill=(255, 215, 55, 255), outline=(125, 80, 15, 255), width=10)
    draw.polygon([(512, 550), (560, 600), (512, 650), (464, 600)], fill=(215, 155, 30, 255), outline=(105, 65, 12, 255))
    draw.ellipse([495, 585, 529, 615], fill=(255, 250, 180, 255))

    return im.resize((256, 256), Image.Resampling.LANCZOS)

def main():
    icons = {
        "UI_Icon_Nav_Citadel.png": generate_citadel_icon(),
        "UI_Icon_Nav_Map.png": generate_citadel_icon(),       # Synchronized parity
        "UI_Icon_Nav_Forge.png": generate_forge_icon(),
        "UI_Icon_Nav_Heroes.png": generate_heroes_icon(),
        "UI_Icon_Nav_Hero.png": generate_heroes_icon(),       # Synchronized parity
        "UI_Icon_Nav_Bag.png": generate_bag_icon(),
        "UI_Icon_Nav_Store.png": generate_bag_icon(),         # Synchronized parity
    }

    for filename, img in icons.items():
        target_path = os.path.join(OUTPUT_DIR, filename)
        img.save(target_path, "PNG")
        print(f"Generated {filename}: {img.size}")

if __name__ == "__main__":
    main()
