import os
from PIL import Image, ImageDraw, ImageFont

def render_lobby_mockup():
    w, h = 768, 1376
    canvas = Image.new("RGBA", (w, h), (0, 0, 0, 255))

    # 1. Base Backdrop (Grand Courtyard + Castle)
    backdrop_path = "D:/Hermes Project/Rampartbound/Assets/Art/Sprites/UI/Citadel_Lobby_Backdrop.png"
    if os.path.exists(backdrop_path):
        backdrop = Image.open(backdrop_path).convert("RGBA").resize((w, h))
        canvas.paste(backdrop, (0, 0))

    draw = ImageDraw.Draw(canvas)

    # 2. Header Bar (Citadel Lv.8, Energy 30/30, Gold 500, Settings)
    header_top = int(h * (1.0 - 1.0)) # 0
    header_bottom = int(h * (1.0 - 0.915)) # ~117
    draw.rectangle([0, 0, w, header_bottom], fill=(15, 20, 32, 220))
    draw.line([(0, header_bottom), (w, header_bottom)], fill=(215, 175, 60, 200), width=2)

    # 3. Energy Pill (Royal Blue)
    draw.rounded_rectangle([int(w*0.37), int(h*0.025), int(w*0.62), int(h*0.075)], radius=18,
                           fill=(20, 40, 80, 240), outline=(90, 165, 245, 220), width=3)

    # 4. Gold Pill
    draw.rounded_rectangle([int(w*0.64), int(h*0.025), int(w*0.86), int(h*0.075)], radius=18,
                           fill=(40, 30, 15, 240), outline=(235, 195, 60, 220), width=3)

    # 5. Team CP Banner
    cp_top = int(h * (1.0 - 0.902)) # ~135
    cp_bot = int(h * (1.0 - 0.840)) # ~220
    draw.rounded_rectangle([int(w*0.24), cp_top, int(w*0.76), cp_bot], radius=14,
                           fill=(90, 55, 25, 245), outline=(245, 210, 80, 255), width=3)

    # 6. Artileri Benteng (Warm Royal Wood/Gold)
    art_top = int(h * (1.0 - 0.840)) # ~220
    art_bot = int(h * (1.0 - 0.785)) # ~295
    draw.rounded_rectangle([int(w*0.20), art_top, int(w*0.80), art_bot], radius=12,
                           fill=(50, 38, 25, 245), outline=(245, 200, 70, 255), width=3)

    # 7. Hero Courtyard Assembly: Pippin, Cedric, Rose, Wind (Silhouette)
    heroes = [
        {"name": "Pippin", "file": "Assets/Art/Sprites/Heroes/RabbitMage_T1.png", "box": (0.02, 0.04, 0.25, 0.88), "status": "Lv.12 | CP 1,600", "locked": False},
        {"name": "Cedric", "file": "Assets/Art/Sprites/Heroes/Cedric_T1.png", "box": (0.26, 0.01, 0.49, 0.92), "status": "Lv.15 | CP 1,850", "locked": False},
        {"name": "Rose", "file": "Assets/Art/Sprites/Heroes/Rose_T1.png", "box": (0.50, 0.04, 0.73, 0.88), "status": "Lv.14 | CP 1,400", "locked": False},
        {"name": "Terkunci", "file": "Assets/Art/Sprites/Heroes/WindRanger_T1.png", "box": (0.74, 0.04, 0.97, 0.88), "status": "🔒 BUKA GACHA", "locked": True}
    ]

    courtyard_base_y = int(h * (1.0 - 0.230)) # y ~ 1059
    courtyard_top_y = int(h * (1.0 - 0.785))  # y ~ 295
    court_h = courtyard_base_y - courtyard_top_y

    for hdata in heroes:
        bx1 = int(w * hdata["box"][0])
        bx2 = int(w * hdata["box"][2])
        # card box inside courtyard
        c_by2 = courtyard_base_y - int(court_h * hdata["box"][1])
        c_by1 = courtyard_base_y - int(court_h * hdata["box"][3])

        # Ground shadow oval
        sy1 = c_by2 - int((c_by2 - c_by1) * 0.24)
        sy2 = c_by2 - int((c_by2 - c_by1) * 0.14)
        draw.ellipse([bx1 + 10, sy1, bx2 - 10, sy2], fill=(0, 0, 0, 140))

        # Hero Sprite
        h_file = os.path.join("D:/Hermes Project/Rampartbound", hdata["file"])
        if not os.path.exists(h_file):
            h_file = h_file.replace("WindRanger_T1.png", "Wind_Ranger_T1.png")
        if os.path.exists(h_file):
            himg = Image.open(h_file).convert("RGBA")
            target_h = int((c_by2 - c_by1) * 0.78)
            ratio = target_h / float(himg.height)
            target_w = int(himg.width * ratio)
            himg_res = himg.resize((target_w, target_h), Image.Resampling.LANCZOS)

            pos_x = bx1 + (bx2 - bx1 - target_w) // 2
            pos_y = c_by2 - int((c_by2 - c_by1) * 0.95)

            if hdata["locked"]:
                # Dark silhouette effect
                sil = Image.new("RGBA", himg_res.size, (25, 35, 55, 175))
                himg_res = Image.composite(sil, himg_res, himg_res.split()[3])

            canvas.paste(himg_res, (pos_x, pos_y), himg_res)

        # Name Plate (above badge)
        ny1 = c_by2 - int((c_by2 - c_by1) * 0.24)
        ny2 = c_by2 - int((c_by2 - c_by1) * 0.16)
        draw.rectangle([bx1 + 4, ny1, bx2 - 4, ny2], fill=(10, 15, 25, 180))

        # Badge Pill (Lv / CP)
        by1 = c_by2 - int((c_by2 - c_by1) * 0.16)
        by2 = c_by2 - int((c_by2 - c_by1) * 0.01)
        draw.rounded_rectangle([bx1 + 2, by1, bx2 - 2, by2], radius=8,
                               fill=(20, 30, 48, 240), outline=(215, 180, 50, 240), width=2)

    # 8. Battle Button
    btn_top = int(h * (1.0 - 0.220))
    btn_bot = int(h * (1.0 - 0.120))
    draw.rounded_rectangle([int(w*0.08), btn_top, int(w*0.92), btn_bot], radius=16,
                           fill=(215, 35, 35, 255), outline=(245, 205, 50, 255), width=5)

    # 9. Bottom Navigation Dock (4 Tabs: Citadel, Forge, Heroes, Bag)
    dock_top = int(h * (1.0 - 0.115))
    draw.rectangle([0, dock_top, w, h], fill=(15, 20, 35, 250))
    draw.line([(0, dock_top), (w, dock_top)], fill=(80, 95, 130, 200), width=2)

    # 4 tabs
    tabs = [
        {"name": "Citadel", "icon": "UI_Icon_Nav_Citadel.png", "active": True},
        {"name": "Forge", "icon": "UI_Icon_Nav_Forge.png", "active": False, "badge": True},
        {"name": "Heroes", "icon": "UI_Icon_Nav_Heroes.png", "active": False, "badge": True},
        {"name": "Bag", "icon": "UI_Icon_Nav_Bag.png", "active": False}
    ]

    tab_w = w // 4
    for idx, tab in enumerate(tabs):
        tx1 = idx * tab_w + 6
        tx2 = (idx + 1) * tab_w - 6
        ty1 = dock_top + 8
        ty2 = h - 8

        if tab["active"]:
            # Gold capsule
            draw.rounded_rectangle([tx1, ty1, tx2, ty2], radius=14,
                                   fill=(245, 195, 55, 255), outline=(255, 240, 160, 255), width=3)
            # Shimmer sweep
            draw.rounded_rectangle([tx1+3, ty1+3, tx2-3, ty1+int((ty2-ty1)*0.45)], radius=10,
                                   fill=(255, 255, 255, 60))
        else:
            # Dark navy
            draw.rounded_rectangle([tx1, ty1, tx2, ty2], radius=12,
                                   fill=(25, 35, 55, 240), outline=(85, 105, 140, 200), width=2)

        # Tab Icon
        ic_path = os.path.join("D:/Hermes Project/Rampartbound/Assets/Art/Sprites/UI", tab["icon"])
        if os.path.exists(ic_path):
            ic_img = Image.open(ic_path).convert("RGBA")
            ic_size = int((ty2 - ty1) * 0.58)
            ic_res = ic_img.resize((ic_size, ic_size), Image.Resampling.LANCZOS)
            ic_x = tx1 + (tx2 - tx1 - ic_size) // 2
            ic_y = ty1 + 4
            canvas.paste(ic_res, (ic_x, ic_y), ic_res)

        # Notification Badge (Red dot)
        if tab.get("badge", False):
            rx = tx2 - 14
            ry = ty1 + 10
            draw.ellipse([rx - 6, ry - 6, rx + 6, ry + 6], fill=(235, 30, 30, 255), outline=(255, 255, 255, 255), width=2)

    out_path = "C:/Users/ShezaEhzad/AppData/Local/hermes/cache/scratch/lobby_design_preview.jpg"
    canvas.convert("RGB").save(out_path, quality=92)
    print("Rendered and saved full design preview to:", out_path)

if __name__ == "__main__":
    render_lobby_mockup()
