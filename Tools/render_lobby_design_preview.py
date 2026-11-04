import os
from PIL import Image, ImageDraw, ImageFont

def get_font(size, bold=True):
    font_names = ["segoeuib.ttf" if bold else "segoeui.ttf", "arialbd.ttf" if bold else "arial.ttf", "tahomabd.ttf"]
    for fn in font_names:
        try:
            return ImageFont.truetype(fn, size)
        except Exception:
            pass
    return ImageFont.load_default()

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
    header_bottom = int(h * (1.0 - 0.915)) # ~117
    draw.rectangle([0, 0, w, header_bottom], fill=(15, 20, 32, 225))
    draw.line([(0, header_bottom), (w, header_bottom)], fill=(215, 175, 60, 200), width=2)

    # Citadel Name & Level on Left
    f_title = get_font(22, bold=True)
    draw.text((28, 42), "Citadel  Lv.8", fill=(255, 240, 180, 255), font=f_title)

    # 3. Energy Pill (Royal Blue)
    en_x1, en_y1, en_x2, en_y2 = int(w*0.42), int(h*0.025), int(w*0.68), int(h*0.065)
    draw.rounded_rectangle([en_x1, en_y1, en_x2, en_y2], radius=16,
                           fill=(20, 40, 80, 240), outline=(90, 165, 245, 220), width=3)
    f_pill = get_font(17, bold=True)
    draw.text((en_x1 + 16, en_y1 + 8), "ENERGY: 30/30", fill=(140, 225, 255, 255), font=f_pill)

    # 4. Gold Pill
    g_x1, g_y1, g_x2, g_y2 = int(w*0.71), int(h*0.025), int(w*0.93), int(h*0.065)
    draw.rounded_rectangle([g_x1, g_y1, g_x2, g_y2], radius=16,
                           fill=(45, 32, 15, 240), outline=(235, 195, 60, 220), width=3)
    draw.text((g_x1 + 22, g_y1 + 8), "GOLD: 500", fill=(255, 225, 110, 255), font=f_pill)

    # 5. Team CP Banner
    cp_top = int(h * (1.0 - 0.902)) # ~135
    cp_bot = int(h * (1.0 - 0.845)) # ~213
    draw.rounded_rectangle([int(w*0.25), cp_top, int(w*0.75), cp_bot], radius=14,
                           fill=(90, 55, 25, 245), outline=(245, 210, 80, 255), width=3)
    f_cp = get_font(22, bold=True)
    cp_txt = "TEAM CP: 4,850"
    bbox = f_cp.getbbox(cp_txt)
    cp_tw = bbox[2] - bbox[0]
    draw.text(((w - cp_tw) // 2, cp_top + (cp_bot - cp_top - 24) // 2), cp_txt, fill=(255, 240, 170, 255), font=f_cp)

    # 6. Hero Courtyard Assembly: Pippin, Cedric, Rose, Wind (Silhouette)
    heroes = [
        {"name": "Pippin", "file": "Assets/Art/Sprites/Heroes/RabbitMage_T1.png", "x_range": (0.02, 0.25), "status": "Lv.12 | CP 1,600", "locked": False},
        {"name": "Cedric", "file": "Assets/Art/Sprites/Heroes/Cedric_T1.png", "x_range": (0.26, 0.49), "status": "Lv.15 | CP 1,850", "locked": False},
        {"name": "Rose", "file": "Assets/Art/Sprites/Heroes/Rose_T1.png", "x_range": (0.50, 0.73), "status": "Lv.14 | CP 1,400", "locked": False},
        {"name": "Terkunci", "file": "Assets/Art/Sprites/Heroes/WindRanger_T1.png", "x_range": (0.74, 0.97), "status": "LOCKED", "locked": True}
    ]

    courtyard_base_y = int(h * (1.0 - 0.225)) # y ~ 1066
    courtyard_top_y = int(h * (1.0 - 0.785))  # y ~ 295
    court_h = courtyard_base_y - courtyard_top_y

    # Ground shadow layer (rendered with proper alpha compositing)
    shadow_layer = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    s_draw = ImageDraw.Draw(shadow_layer)

    # Standardized horizontal baseline for all 4 slots
    pill_y1 = courtyard_base_y - 46
    pill_y2 = courtyard_base_y - 12
    ph = pill_y2 - pill_y1
    name_y = pill_y1 - 28

    stat_pill_sprite_path = "D:/Hermes Project/Rampartbound/Assets/Art/Sprites/UI/UI_Frame_Hero_Stat_Pill.png"
    pill_img = Image.open(stat_pill_sprite_path).convert("RGBA") if os.path.exists(stat_pill_sprite_path) else None

    f_hero_name = get_font(18, bold=True)
    f_stat = get_font(13, bold=True)

    # Render ground shadows first onto shadow_layer
    for hdata in heroes:
        bx1 = int(w * hdata["x_range"][0])
        bx2 = int(w * hdata["x_range"][1])
        slot_w = bx2 - bx1
        feet_y = pill_y1 - 8
        s_draw.ellipse([bx1 + 10, feet_y - 14, bx2 - 10, feet_y + 14], fill=(0, 0, 0, 115))

    canvas = Image.alpha_composite(canvas, shadow_layer)
    draw = ImageDraw.Draw(canvas)

    # Render heroes and UI overlays
    for hdata in heroes:
        bx1 = int(w * hdata["x_range"][0])
        bx2 = int(w * hdata["x_range"][1])
        slot_w = bx2 - bx1

        # Hero Sprite
        h_file = os.path.join("D:/Hermes Project/Rampartbound", hdata["file"])
        if not os.path.exists(h_file):
            h_file = h_file.replace("WindRanger_T1.png", "Wind_Ranger_T1.png")

        if os.path.exists(h_file):
            himg = Image.open(h_file).convert("RGBA")
            target_h = int(court_h * 0.74)
            ratio = target_h / float(himg.height)
            target_w = int(himg.width * ratio)
            himg_res = himg.resize((target_w, target_h), Image.Resampling.LANCZOS)

            pos_x = bx1 + (slot_w - target_w) // 2
            pos_y = (pill_y1 - 10) - target_h

            if hdata["locked"]:
                sil = Image.new("RGBA", himg_res.size, (20, 28, 45, 195))
                himg_res = Image.composite(sil, himg_res, himg_res.split()[3])

            canvas.paste(himg_res, (pos_x, pos_y), himg_res)

        # Name Plate (floating cleanly above stat pill)
        if not hdata["locked"]:
            name_txt = hdata["name"]
            bbox = f_hero_name.getbbox(name_txt)
            nw = bbox[2] - bbox[0]
            nx = bx1 + (slot_w - nw) // 2
            draw.text((nx + 1, name_y + 1), name_txt, fill=(0, 0, 0, 240), font=f_hero_name)
            draw.text((nx, name_y), name_txt, fill=(255, 245, 210, 255), font=f_hero_name)

        # Floating Cyan Stat Pill (Lv.X | CP Y,YYY)
        if pill_img:
            res_pill = pill_img.resize((slot_w, ph), Image.Resampling.LANCZOS)
            canvas.paste(res_pill, (bx1, pill_y1), res_pill)
        else:
            draw.rounded_rectangle([bx1, pill_y1, bx2, pill_y2], radius=10,
                                   fill=(18, 48, 72, 235), outline=(50, 180, 220, 230), width=2)

        # Pill text with bright green dot
        txt = hdata["status"]
        bbox = f_stat.getbbox(txt)
        tw = bbox[2] - bbox[0]
        th = bbox[3] - bbox[1]
        tx = bx1 + (slot_w - tw) // 2 + (8 if not hdata["locked"] else 0)
        ty = pill_y1 + (ph - th) // 2 - 2

        if not hdata["locked"]:
            dot_x = tx - 14
            dot_y = ty + 7
            draw.ellipse([dot_x - 4, dot_y - 4, dot_x + 4, dot_y + 4], fill=(0, 255, 170, 255))
            draw.text((tx + 1, ty + 1), txt, fill=(0, 0, 0, 240), font=f_stat)
            draw.text((tx, ty), txt, fill=(235, 250, 255, 255), font=f_stat)
        else:
            draw.text((tx + 1, ty + 1), txt, fill=(0, 0, 0, 240), font=f_stat)
            draw.text((tx, ty), txt, fill=(160, 190, 210, 255), font=f_stat)

    # 7. Battle Button (Floating Cleanly Above Compact Dock with 3D Accents)
    btn_top = int(h * (1.0 - 0.165))
    btn_bot = int(h * (1.0 - 0.096))
    btn_x1 = int(w * 0.14)
    btn_x2 = int(w * 0.86)

    # Crimson body with rich gold border
    draw.rounded_rectangle([btn_x1, btn_top, btn_x2, btn_bot], radius=24,
                           fill=(205, 25, 28, 255), outline=(255, 210, 65, 255), width=4)
    # Specular curved gloss arc
    draw.arc([btn_x1 + 10, btn_top + 4, btn_x2 - 10, btn_top + 32], start=190, end=350, fill=(255, 180, 180, 230), width=3)

    # Crossed Swords Icon on left of button
    swords_path = "D:/Hermes Project/Rampartbound/Assets/Art/Sprites/UI/UI_Icon_Crossed_Swords.png"
    if os.path.exists(swords_path):
        swords_img = Image.open(swords_path).convert("RGBA")
        swords_h = int((btn_bot - btn_top) * 0.84)
        swords_w = int(swords_img.width * (swords_h / float(swords_img.height)))
        swords_res = swords_img.resize((swords_w, swords_h), Image.Resampling.LANCZOS)
        canvas.paste(swords_res, (btn_x1 + 16, btn_top + (btn_bot - btn_top - swords_h) // 2), swords_res)

    # Gold Diamond Accents at left and right extremities
    diamond_path = "D:/Hermes Project/Rampartbound/Assets/Art/Sprites/UI/UI_Decor_Gold_Diamond.png"
    if os.path.exists(diamond_path):
        d_img = Image.open(diamond_path).convert("RGBA")
        d_h = int((btn_bot - btn_top) * 0.60)
        d_w = int(d_img.width * (d_h / float(d_img.height)))
        d_res = d_img.resize((d_w, d_h), Image.Resampling.LANCZOS)
        canvas.paste(d_res, (btn_x1 - d_w // 2, btn_top + (btn_bot - btn_top - d_h) // 2), d_res)
        canvas.paste(d_res, (btn_x2 - d_w // 2, btn_top + (btn_bot - btn_top - d_h) // 2), d_res)

    # Button Text: BATTLE in high-impact bold typography
    f_battle = get_font(34, bold=True)
    btxt = "BATTLE"
    bbox = f_battle.getbbox(btxt)
    btw = bbox[2] - bbox[0]
    bth = bbox[3] - bbox[1]
    btx = btn_x1 + (btn_x2 - btn_x1 - btw) // 2 + 20
    bty = btn_top + (btn_bot - btn_top - bth) // 2 - 4
    # Deep drop shadow
    draw.text((btx + 3, bty + 3), btxt, fill=(40, 5, 5, 255), font=f_battle)
    draw.text((btx + 1, bty + 1), btxt, fill=(120, 10, 10, 255), font=f_battle)
    # Bright white text with warm glow
    draw.text((btx, bty), btxt, fill=(255, 255, 255, 255), font=f_battle)

    # 8. Bottom Navigation Dock (Translucent Glass Gambar 2, Compact Height 0.082f)
    dock_top = int(h * (1.0 - 0.082))
    dock_overlay = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    d_draw = ImageDraw.Draw(dock_overlay)
    # Translucent dark midnight navy glass (alpha 0.78)
    d_draw.rectangle([0, dock_top, w, h], fill=(10, 20, 42, 198))

    # Continuous Golden Beveled Top Rail along dock_top
    d_draw.line([(0, dock_top - 1), (w, dock_top - 1)], fill=(255, 235, 140, 240), width=1)
    d_draw.line([(0, dock_top), (w, dock_top)], fill=(225, 185, 55, 255), width=2)
    d_draw.line([(0, dock_top + 2), (w, dock_top + 2)], fill=(120, 85, 20, 200), width=1)

    # 4 Canonical Tabs: Citadel, Forge, Heroes, Bag
    tabs = [
        {"name": "< Citadel >", "icon": "UI_Icon_Nav_Citadel.png", "active": True},
        {"name": "Forge", "icon": "UI_Icon_Nav_Forge.png", "active": False, "badge": True},
        {"name": "Heroes", "icon": "UI_Icon_Nav_Heroes.png", "active": False, "badge": True},
        {"name": "Bag", "icon": "UI_Icon_Nav_Bag.png", "active": False}
    ]

    tab_w = w // 4
    podium_path = "D:/Hermes Project/Rampartbound/Assets/Art/Sprites/UI/UI_Frame_Active_Tab_Podium.png"
    podium_img = Image.open(podium_path).convert("RGBA") if os.path.exists(podium_path) else None

    for idx, tab in enumerate(tabs):
        tx1 = idx * tab_w + 6
        tx2 = (idx + 1) * tab_w - 6
        ty1 = dock_top + 5
        ty2 = h - 5

        if tab["active"]:
            if podium_img:
                res_pod = podium_img.resize((tx2 - tx1, ty2 - ty1), Image.Resampling.LANCZOS)
                dock_overlay.paste(res_pod, (tx1, ty1), res_pod)
            else:
                d_draw.rounded_rectangle([tx1, ty1, tx2, ty2], radius=10,
                                         fill=(245, 192, 45, 50), outline=(255, 218, 90, 210), width=2)

    canvas = Image.alpha_composite(canvas, dock_overlay)
    draw = ImageDraw.Draw(canvas)

    f_tab = get_font(15, bold=True)

    for idx, tab in enumerate(tabs):
        tx1 = idx * tab_w + 6
        tx2 = (idx + 1) * tab_w - 6
        ty1 = dock_top + 5
        ty2 = h - 5

        # Tab Icon
        ic_path = os.path.join("D:/Hermes Project/Rampartbound/Assets/Art/Sprites/UI", tab["icon"])
        if os.path.exists(ic_path):
            ic_img = Image.open(ic_path).convert("RGBA")
            ic_size = int((ty2 - ty1) * 0.54)
            ic_res = ic_img.resize((ic_size, ic_size), Image.Resampling.LANCZOS)
            ic_x = tx1 + (tx2 - tx1 - ic_size) // 2
            ic_y = ty1 + 4
            canvas.paste(ic_res, (ic_x, ic_y), ic_res)

        # Tab Label
        label_text = tab["name"]
        bbox = f_tab.getbbox(label_text)
        tw = bbox[2] - bbox[0]
        lx = tx1 + (tx2 - tx1 - tw) // 2
        ly = ty2 - 20
        label_color = (255, 245, 155, 255) if tab["active"] else (210, 225, 245, 230)
        draw.text((lx + 1, ly + 1), label_text, fill=(0, 0, 0, 220), font=f_tab)
        draw.text((lx, ly), label_text, fill=label_color, font=f_tab)

        # Notification Badge (Red dot)
        if tab.get("badge", False):
            rx = tx2 - 14
            ry = ty1 + 8
            draw.ellipse([rx - 5, ry - 5, rx + 5, ry + 5], fill=(235, 30, 30, 255), outline=(255, 255, 255, 255), width=2)

    out_path = "C:/Users/ShezaEhzad/AppData/Local/hermes/cache/scratch/lobby_design_preview.jpg"
    canvas.convert("RGB").save(out_path, quality=92)
    print("Rendered and saved full design preview to:", out_path)

if __name__ == "__main__":
    render_lobby_mockup()
