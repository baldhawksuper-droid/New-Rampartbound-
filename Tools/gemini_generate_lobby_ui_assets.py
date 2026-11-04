import urllib.request
import json
import os
import base64
from PIL import Image
import io
from collections import deque

URL = "http://localhost:20128/v1/images/generations"
API_KEY = os.environ.get("HERMES_CUSTOM_LOCALHOST_20128_API_KEY", "")

SCRATCH_DIR = "C:/Users/ShezaEhzad/AppData/Local/hermes/cache/scratch/gemini_ui_assets"
OUT_DIR = "D:/Hermes Project/Rampartbound/Assets/Art/Sprites/UI"
os.makedirs(SCRATCH_DIR, exist_ok=True)
os.makedirs(OUT_DIR, exist_ok=True)

PROMPTS = {
    "crossed_swords": (
        "High-fidelity 3D tactile mobile game UI icon of twin crossed silver paladin broadswords: "
        "polished steel blades with razor sharp edges crossed in an X, ornate golden cruciform hilts "
        "with small red rubies set into the pommels, bright specular metallic reflections, "
        "clean isolated icon centered on a pure black background, no text, no frame, Blizzard / Supercell mobile game UI style."
    ),
    "gold_diamond_accent": (
        "High-fidelity 3D tactile mobile game UI decorative element of an ornate golden diamond finial ornament: "
        "faceted polished gold rhombus stud with sharp beveled facets and gleaming golden highlights, "
        "chunky tactile mobile RPG UI ornament, isolated centered on a pure black background, no text, no frame."
    ),
    "active_tab_podium": (
        "High-fidelity 3D tactile mobile game UI button frame: an elongated horizontal capsule pill pedestal "
        "made of glowing golden brass filigree with smooth beveled borders, soft amber inner glow, "
        "and tiny golden rivets at the ends, clean isolated UI frame on pure black background, Blizzard / Supercell style."
    ),
    "cyan_stat_pill": (
        "High-fidelity 3D tactile mobile game UI element: a sleek horizontal capsule stat pill bar, "
        "deep enchanted midnight cyan-teal stone base with a fine polished metallic cyan-gold beveled rim, "
        "clean isolated horizontal pill shape on pure black background, mobile RPG HUD asset, no text."
    )
}

def generate_raw(name, prompt):
    print(f"Generating {name} via Gemini...")
    payload = {
        "model": "ag/gemini-3.1-flash",
        "prompt": prompt,
        "size": "1024x1024",
        "n": 1
    }
    req = urllib.request.Request(
        URL,
        data=json.dumps(payload).encode("utf-8"),
        headers={
            "Authorization": f"Bearer {API_KEY}",
            "Content-Type": "application/json"
        }
    )
    with urllib.request.urlopen(req, timeout=120) as resp:
        result = json.loads(resp.read().decode("utf-8"))
        b64_data = result["data"][0]["b64_json"]
        img_bytes = base64.b64decode(b64_data)
        img = Image.open(io.BytesIO(img_bytes))
        raw_path = os.path.join(SCRATCH_DIR, f"raw_{name}.png")
        img.save(raw_path)
        print(f"Saved raw {name} to {raw_path}")
        return raw_path

def process_to_rgba(raw_path, out_path, target_max_dim=256, canvas_size=(256, 256)):
    im = Image.open(raw_path).convert('RGBA')
    w, h = im.size
    pixels = im.load()

    visited = set()
    queue = deque([(0, 0), (w-1, 0), (0, h-1), (w-1, h-1)])
    for pt in queue:
        visited.add(pt)
    bg_pixels = set()

    while queue:
        x, y = queue.popleft()
        r, g, b, _ = pixels[x, y]
        lum = max(r, g, b)
        if lum < 38:
            bg_pixels.add((x, y))
            for dx, dy in [(-1, 0), (1, 0), (0, -1), (0, 1)]:
                nx, ny = x + dx, y + dy
                if 0 <= nx < w and 0 <= ny < h and (nx, ny) not in visited:
                    visited.add((nx, ny))
                    nr, ng, nb, _ = pixels[nx, ny]
                    if max(nr, ng, nb) < 38:
                        queue.append((nx, ny))

    alpha_img = Image.new('L', (w, h), 255)
    alpha_pixels = alpha_img.load()

    for (x, y) in bg_pixels:
        r, g, b, _ = pixels[x, y]
        lum = max(r, g, b)
        if lum <= 8:
            alpha_pixels[x, y] = 0
        else:
            alpha_pixels[x, y] = int((lum - 8) / 30.0 * 255)

    im.putalpha(alpha_img)
    bbox = im.getbbox()
    cropped = im.crop(bbox) if bbox else im

    cw, ch = cropped.size
    scale = target_max_dim / max(cw, ch)
    nw, nh = max(1, int(cw * scale)), max(1, int(ch * scale))
    resized = cropped.resize((nw, nh), Image.Resampling.LANCZOS)

    canvas_w, canvas_h = canvas_size
    final_canvas = Image.new('RGBA', (canvas_w, canvas_h), (0, 0, 0, 0))
    px = (canvas_w - nw) // 2
    py = (canvas_h - nh) // 2
    final_canvas.paste(resized, (px, py), resized)
    final_canvas.save(out_path, format='PNG')
    print(f"Processed and saved -> {out_path} ({nw}x{nh} in {canvas_size})")

if __name__ == "__main__":
    targets = {
        "crossed_swords": ("UI_Icon_Crossed_Swords.png", 220, (256, 256)),
        "gold_diamond_accent": ("UI_Decor_Gold_Diamond.png", 180, (256, 256)),
        "active_tab_podium": ("UI_Frame_Active_Tab_Podium.png", 240, (256, 128)),
        "cyan_stat_pill": ("UI_Frame_Hero_Stat_Pill.png", 240, (256, 96))
    }
    for key, (out_name, max_dim, c_size) in targets.items():
        raw_p = generate_raw(key, PROMPTS[key])
        final_p = os.path.join(OUT_DIR, out_name)
        process_to_rgba(raw_p, final_p, max_dim, c_size)
