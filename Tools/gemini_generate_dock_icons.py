import urllib.request
import json
import os
import base64
from PIL import Image
import io

URL = "http://localhost:20128/v1/images/generations"
API_KEY = os.environ.get("HERMES_CUSTOM_LOCALHOST_20128_API_KEY", "")

OUTPUT_DIR = "C:/Users/ShezaEhzad/AppData/Local/hermes/cache/scratch/gemini_icons"
os.makedirs(OUTPUT_DIR, exist_ok=True)

PROMPTS = {
    "Citadel": (
        "High-fidelity 3D tactile mobile game UI icon of a royal fantasy Citadel castle: "
        "three cylindrical stone towers with clean cream masonry, vibrant royal cobalt blue conical tiled roofs, "
        "golden spire finials, an ornate arched castle gate with warm glowing golden light spilling out, "
        "and fluttering small red heraldic pennants. Cute semi-chibi 3D game asset render, "
        "rich volumetric studio lighting, ambient occlusion, glossy specular highlights, "
        "clean isolated icon centered on a pure black background, no text, no frame, Blizzard / Supercell mobile RPG style."
    ),
    "Forge": (
        "High-fidelity 3D tactile mobile game UI icon of an enchanted blacksmith forge crucible: "
        "an ornate chunky golden chalice cauldron with two curved winged handles and a sturdy base pedestal, "
        "filled with bubbling molten liquid gold and glowing orange-yellow fiery embers, "
        "with a polished heavy steel hammer and tongs resting across the top rim. "
        "Dramatic warm fire lighting, rich metallic gold reflections, "
        "clean isolated icon centered on a pure black background, no text, no frame, Supercell / Blizzard mobile RPG style."
    ),
    "Heroes": (
        "High-fidelity 3D tactile mobile game UI icon of a legendary paladin warrior helmet: "
        "a polished steel close-helm with magnificent sweeping golden valkyrie wings extending outward, "
        "a glowing faceted red ruby gemstone embedded into the gold forehead band, "
        "and a sleek glowing cyan visor eye slit. Rich metallic reflections, golden sheen, "
        "clean isolated icon centered on a pure black background, no text, no frame, Supercell / Blizzard mobile RPG style."
    ),
    "Bag": (
        "High-fidelity 3D tactile mobile game UI icon of an adventurer's travel backpack: "
        "chunky chestnut brown stitched leather rucksack with heavy golden brass buckle and straps, "
        "side pouches, and a neat rolled teal-emerald traveler bedroll blanket strapped securely across the top. "
        "Tactile semi-3D toy-like proportions, rich leather texture, bright specular highlights, "
        "clean isolated icon centered on a pure black background, no text, no frame, Supercell / Blizzard mobile RPG style."
    )
}

def generate_icon(name, prompt):
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
        out_path = os.path.join(OUTPUT_DIR, f"raw_{name.lower()}.png")
        img.save(out_path)
        print(f"Saved raw {name} to {out_path} ({img.size})")
        return out_path

if __name__ == "__main__":
    for name, p in PROMPTS.items():
        generate_icon(name, p)
