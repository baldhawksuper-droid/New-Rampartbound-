import os
from PIL import Image, ImageDraw, ImageFilter

def create_citadel_icon():
    # 256x256 cute 3D stone castle miniature with bright blue conical roofs and gold trim
    size = 256
    im = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(im)

    # Soft drop shadow
    draw.ellipse([30, 210, 226, 246], fill=(0, 0, 0, 90))

    # Base stone wall & foundation
    draw.rounded_rectangle([42, 110, 92, 215], radius=6, fill=(225, 220, 210, 255), outline=(95, 75, 55, 255), width=4)
    draw.rounded_rectangle([164, 110, 214, 215], radius=6, fill=(215, 210, 200, 255), outline=(95, 75, 55, 255), width=4)
    draw.rounded_rectangle([82, 125, 174, 215], radius=4, fill=(235, 230, 220, 255), outline=(95, 75, 55, 255), width=4)
    draw.rounded_rectangle([102, 75, 154, 150], radius=5, fill=(240, 235, 225, 255), outline=(95, 75, 55, 255), width=4)

    # Crenellations
    for cx in [86, 110, 134, 158]:
        draw.rectangle([cx, 118, cx+14, 128], fill=(235, 230, 220, 255), outline=(95, 75, 55, 255), width=3)

    # Arched wooden door
    draw.chord([110, 160, 146, 215], start=180, end=0, fill=(130, 75, 40, 255), outline=(75, 40, 20, 255), width=3)
    draw.line([128, 165, 128, 214], fill=(75, 40, 20, 255), width=2)
    draw.ellipse([116, 185, 120, 189], fill=(240, 200, 80, 255))
    draw.ellipse([136, 185, 140, 189], fill=(240, 200, 80, 255))

    # Conical roofs: Bright vibrant fantasy blue with gold trim
    draw.polygon([(67, 35), (32, 115), (102, 115)], fill=(30, 135, 235, 255), outline=(15, 65, 130, 255))
    draw.polygon([(189, 35), (154, 115), (224, 115)], fill=(25, 115, 215, 255), outline=(15, 65, 130, 255))
    draw.polygon([(128, 10), (92, 80), (164, 80)], fill=(45, 155, 250, 255), outline=(15, 65, 130, 255))

    # Golden finials & flags on roof peaks
    for peak_x, peak_y in [(67, 35), (189, 35), (128, 10)]:
        draw.ellipse([peak_x-4, peak_y-5, peak_x+4, peak_y+3], fill=(255, 215, 50, 255), outline=(140, 95, 20, 255), width=2)
        draw.polygon([(peak_x+2, peak_y-4), (peak_x+18, peak_y), (peak_x+2, peak_y+4)], fill=(235, 45, 55, 255), outline=(140, 20, 25, 255))

    # Windows
    draw.chord([58, 140, 76, 165], start=180, end=0, fill=(35, 45, 65, 255), outline=(95, 75, 55, 255), width=2)
    draw.chord([180, 140, 198, 165], start=180, end=0, fill=(35, 45, 65, 255), outline=(95, 75, 55, 255), width=2)
    draw.chord([120, 95, 136, 118], start=180, end=0, fill=(35, 45, 65, 255), outline=(95, 75, 55, 255), width=2)

    return im

def create_forge_icon():
    # 256x256 cute 3D golden smelting crucible / cauldron with fiery glow and blacksmith hammer/tongs
    size = 256
    im = Image.new("RGBA", (size, size), (0, 0, 0, 0))

    # Soft drop shadow
    draw = ImageDraw.Draw(im)
    draw.ellipse([34, 215, 222, 248], fill=(0, 0, 0, 95))

    # Molten furnace glow in background
    glow = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    gdraw = ImageDraw.Draw(glow)
    gdraw.ellipse([70, 70, 186, 170], fill=(255, 160, 20, 140))
    glow = glow.filter(ImageFilter.GaussianBlur(14))
    im = Image.alpha_composite(im, glow)
    draw = ImageDraw.Draw(im)

    # Blacksmith hammer crossed behind
    draw.line([40, 40, 180, 190], fill=(140, 80, 40, 255), width=10)
    draw.rounded_rectangle([30, 25, 75, 55], radius=4, fill=(180, 190, 205, 255), outline=(60, 70, 85, 255), width=3)

    # Tongs crossed
    draw.line([216, 40, 76, 190], fill=(130, 140, 155, 255), width=9)

    # Golden Cauldron Body
    draw.ellipse([80, 195, 176, 222], fill=(210, 150, 30, 255), outline=(100, 65, 15, 255), width=4)
    draw.chord([52, 90, 204, 210], start=0, end=180, fill=(245, 185, 40, 255), outline=(110, 75, 20, 255), width=5)
    draw.arc([34, 110, 64, 155], start=90, end=270, fill=(230, 165, 30, 255), width=7)
    draw.arc([192, 110, 222, 155], start=270, end=90, fill=(230, 165, 30, 255), width=7)

    # Rim of cauldron
    draw.ellipse([50, 85, 206, 125], fill=(255, 205, 55, 255), outline=(120, 80, 20, 255), width=5)
    draw.ellipse([64, 94, 192, 118], fill=(255, 70, 20, 255))
    draw.ellipse([80, 98, 176, 114], fill=(255, 235, 90, 255))

    # Metallic highlights
    draw.arc([68, 115, 188, 195], start=45, end=135, fill=(255, 245, 180, 230), width=6)
    draw.arc([90, 200, 166, 218], start=30, end=150, fill=(255, 245, 180, 200), width=4)

    return im

def create_heroes_icon():
    # 256x256 cute 3D knight winged helm with ruby gem in forehead
    size = 256
    im = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(im)

    # Drop shadow
    draw.ellipse([40, 215, 216, 248], fill=(0, 0, 0, 95))

    # Golden Winged Crest
    draw.polygon([(110, 90), (45, 30), (40, 75), (20, 50), (25, 110), (100, 130)], fill=(245, 185, 40, 255), outline=(120, 80, 15, 255))
    draw.polygon([(146, 90), (211, 30), (216, 75), (236, 50), (231, 110), (156, 130)], fill=(230, 170, 35, 255), outline=(120, 80, 15, 255))

    draw.line([(55, 55), (100, 105)], fill=(255, 225, 90, 255), width=3)
    draw.line([(201, 55), (156, 105)], fill=(255, 225, 90, 255), width=3)

    # Silver Helmet Shell
    draw.chord([60, 48, 196, 190], start=180, end=0, fill=(225, 232, 240, 255), outline=(65, 75, 90, 255), width=5)
    draw.polygon([(60, 115), (70, 215), (128, 225), (186, 215), (196, 115)], fill=(200, 210, 225, 255), outline=(65, 75, 90, 255))

    # Golden Crown Brow Band
    draw.rounded_rectangle([56, 104, 200, 134], radius=6, fill=(250, 195, 45, 255), outline=(125, 85, 15, 255), width=4)

    # Ruby diamond gem in center of brow band
    draw.polygon([(128, 92), (144, 118), (128, 144), (112, 118)], fill=(235, 30, 60, 255), outline=(140, 10, 25, 255), width=3)
    draw.polygon([(128, 96), (138, 118), (128, 138), (118, 118)], fill=(255, 90, 120, 255))
    draw.ellipse([124, 108, 128, 114], fill=(255, 255, 255, 240))

    # T-slit Dark Visor Opening
    draw.rounded_rectangle([92, 150, 164, 166], radius=3, fill=(25, 30, 42, 255))
    draw.rounded_rectangle([122, 150, 134, 205], radius=3, fill=(25, 30, 42, 255))

    # Reflections
    draw.arc([75, 58, 180, 140], start=210, end=270, fill=(255, 255, 255, 220), width=6)
    draw.arc([68, 125, 188, 215], start=40, end=90, fill=(255, 255, 255, 180), width=4)

    return im

def create_bag_icon():
    # 256x256 cute chunky 3D adventurer traveler backpack with caramel tan leather & gold diamond buckle
    size = 256
    im = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(im)

    # Drop shadow
    draw.ellipse([36, 215, 220, 248], fill=(0, 0, 0, 95))

    # Shoulder straps
    draw.arc([25, 60, 85, 210], start=90, end=270, fill=(120, 65, 30, 255), width=10)
    draw.arc([171, 60, 231, 210], start=270, end=90, fill=(120, 65, 30, 255), width=10)

    # Main backpack body
    draw.rounded_rectangle([52, 70, 204, 215], radius=32, fill=(195, 125, 65, 255), outline=(90, 45, 15, 255), width=5)

    # Side pockets
    draw.rounded_rectangle([34, 125, 62, 195], radius=8, fill=(175, 110, 55, 255), outline=(90, 45, 15, 255), width=4)
    draw.rounded_rectangle([194, 125, 222, 195], radius=8, fill=(175, 110, 55, 255), outline=(90, 45, 15, 255), width=4)

    # Top leather flap
    draw.chord([52, 45, 204, 145], start=180, end=0, fill=(215, 145, 75, 255), outline=(95, 50, 18, 255), width=5)
    draw.rounded_rectangle([52, 90, 204, 138], radius=14, fill=(215, 145, 75, 255), outline=(95, 50, 18, 255), width=5)
    draw.arc([56, 92, 200, 134], start=0, end=180, fill=(245, 195, 45, 255), width=5)

    # Vertical strap
    draw.rounded_rectangle([118, 65, 138, 175], radius=4, fill=(145, 80, 35, 255), outline=(85, 40, 15, 255), width=3)

    # Golden diamond buckle
    draw.polygon([(128, 115), (146, 133), (128, 151), (110, 133)], fill=(255, 210, 50, 255), outline=(135, 90, 15, 255), width=3)
    draw.polygon([(128, 123), (138, 133), (128, 143), (118, 133)], fill=(120, 65, 25, 255))

    # Bedroll on top
    draw.rounded_rectangle([60, 32, 196, 68], radius=12, fill=(105, 135, 95, 255), outline=(50, 75, 45, 255), width=4)
    draw.rectangle([85, 32, 95, 68], fill=(120, 65, 30, 255))
    draw.rectangle([161, 32, 171, 68], fill=(120, 65, 30, 255))

    # Highlight
    draw.arc([65, 75, 190, 135], start=190, end=260, fill=(255, 215, 165, 200), width=6)

    return im

def main():
    icons = {
        "UI_Icon_Nav_Citadel": create_citadel_icon(),
        "UI_Icon_Nav_Forge": create_forge_icon(),
        "UI_Icon_Nav_Heroes": create_heroes_icon(),
        "UI_Icon_Nav_Bag": create_bag_icon()
    }

    base_dirs = [
        "D:/Hermes Project/Rampartbound/Assets/Art/Sprites/UI",
        "D:/Hermes Project/Rampartbound/Assets/Resources/Sprites/UI"
    ]

    for name, img in icons.items():
        for bdir in base_dirs:
            os.makedirs(bdir, exist_ok=True)
            png_path = os.path.join(bdir, f"{name}.png")
            img.save(png_path)
            print(f"Saved {png_path}")

if __name__ == "__main__":
    main()
