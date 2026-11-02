import math
import os
from PIL import Image, ImageDraw, ImageFilter

def generate_grand_courtyard_backdrop():
    src_path = "D:/Hermes Project/Rampartbound/Assets/Art/Sprites/UI/Citadel_Lobby_Backdrop.png"
    im = Image.open(src_path).convert("RGBA")
    w, h = im.size # 768, 1376

    # Lower half starts around y = 640
    courtyard_top = int(h * 0.46) # y ~ 632
    horizon_y = courtyard_top

    # Create courtyard layer
    courtyard = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    cdraw = ImageDraw.Draw(courtyard)

    # 1. Base courtyard stone gradient: warm limestone / slate paving
    for y in range(horizon_y, h):
        t = (y - horizon_y) / (h - horizon_y) # 0.0 to 1.0
        # Color from distant stone (lighter, warmer) to foreground stone (richer, textured)
        r = int(185 + t * 25) # 185 to 210
        g = int(180 + t * 25) # 180 to 205
        b = int(175 + t * 25) # 175 to 200
        cdraw.line([(0, y), (w, y)], fill=(r, g, b, 255))

    # 2. Side green lawns / verges (manicured grass flanking the central grand plaza)
    for y in range(horizon_y, h):
        t = (y - horizon_y) / (h - horizon_y)
        # Perspective widening of center plaza
        left_grass_w = int((w * 0.16) * (1.0 - t * 0.45))  # narrower as it comes forward
        right_grass_x = w - left_grass_w
        
        # Grass color: lush vibrant fantasy green
        grass_color = (65 + int(t * 15), 135 + int(t * 20), 45 + int(t * 10), 255)
        cdraw.line([(0, y), (left_grass_w, y)], fill=grass_color)
        cdraw.line([(right_grass_x, y), (w, y)], fill=grass_color)
        
        # Clean stone curb edge between grass and stone plaza
        cdraw.line([(left_grass_w - 3, y), (left_grass_w, y)], fill=(130, 125, 120, 255))
        cdraw.line([(right_grass_x, y), (right_grass_x + 3, y)], fill=(130, 125, 120, 255))

    # 3. Horizontal stone paving courses (getting further apart as they come forward - perspective)
    y_current = horizon_y + 12
    step = 14
    while y_current < h:
        # Perspective joint line
        cdraw.line([(0, int(y_current)), (w, int(y_current))], fill=(135, 130, 125, 180), width=2)
        # Subtle highlight on top of each paving slab
        cdraw.line([(0, int(y_current - 1)), (w, int(y_current - 1))], fill=(240, 238, 230, 160), width=1)
        
        # Vertical joint lines (staggered flagstones)
        slab_w = int(45 + (y_current - horizon_y) / (h - horizon_y) * 55)
        offset = int((y_current % 3) * (slab_w // 3))
        for x in range(offset, w, slab_w):
            cdraw.line([(x, int(y_current)), (x, int(y_current + step))], fill=(135, 130, 125, 160), width=2)
            
        y_current += step
        step += 2.2 # Perspective expansion

    # 4. Central Royal Crest Medallion / Paver Accent in the plaza floor
    # Subtle radial pattern in center of the grand plaza
    center_x = w // 2
    medallion_y = int(h * 0.68)
    medallion_rx = int(w * 0.38)
    medallion_ry = int(h * 0.12)
    cdraw.ellipse([center_x - medallion_rx, medallion_y - medallion_ry,
                   center_x + medallion_rx, medallion_y + medallion_ry],
                  outline=(160, 150, 140, 140), width=3)
    cdraw.ellipse([center_x - medallion_rx + 8, medallion_y - medallion_ry + 3,
                   center_x + medallion_rx - 8, medallion_y + medallion_ry - 3],
                  outline=(245, 240, 230, 110), width=2)

    # 5. Soft castle base transition (blending the castle courtyard entrance into the plaza)
    blend_mask = Image.new("L", (w, h), 0)
    bdraw = ImageDraw.Draw(blend_mask)
    for y in range(horizon_y, horizon_y + 50):
        alpha = int(((y - horizon_y) / 50.0) * 255)
        bdraw.line([(0, y), (w, y)], fill=alpha)
    bdraw.rectangle([(0, horizon_y + 50), (w, h)], fill=255)

    # Composite courtyard over existing image using blend_mask
    result = im.copy()
    result.paste(courtyard, (0, 0), blend_mask)

    # Save to both Art and Resources
    target_art = "D:/Hermes Project/Rampartbound/Assets/Art/Sprites/UI/Citadel_Lobby_Backdrop.png"
    target_res = "D:/Hermes Project/Rampartbound/Assets/Resources/Sprites/UI/Citadel_Lobby_Backdrop.png"
    result.convert("RGB").save(target_art)
    result.convert("RGB").save(target_res)
    print(f"Grand Courtyard Plaza Backdrop successfully saved to {target_art} and {target_res}!")

if __name__ == "__main__":
    generate_grand_courtyard_backdrop()
