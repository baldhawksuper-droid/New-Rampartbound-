import os
import uuid
from PIL import Image, ImageDraw

OUTPUT_DIR = r"D:\PROJECT GAMES\Rampartbound\Assets\Art\Sprites\Grid"
os.makedirs(OUTPUT_DIR, exist_ok=True)

MEDALLIONS = [
    {
        "filename": "Token_Medallion_Cedric.png",
        "bg_color": (160, 25, 25, 255),
        "rim_color": (230, 180, 50, 255),
        "inner_color": (120, 15, 15, 255),
        "symbol_color": (255, 240, 200, 255),
        "name": "Cedric Red Knight Medallion"
    },
    {
        "filename": "Token_Medallion_Rose.png",
        "bg_color": (205, 75, 130, 255),
        "rim_color": (245, 195, 205, 255),
        "inner_color": (155, 45, 95, 255),
        "symbol_color": (255, 250, 255, 255),
        "name": "Rose Tanker Medallion"
    },
    {
        "filename": "Token_Medallion_Rabbit.png",
        "bg_color": (30, 80, 180, 255),
        "rim_color": (140, 220, 255, 255),
        "inner_color": (15, 45, 130, 255),
        "symbol_color": (220, 250, 255, 255),
        "name": "Rabbit Mage Medallion"
    },
    {
        "filename": "Token_Medallion_Wind.png",
        "bg_color": (30, 140, 70, 255),
        "rim_color": (180, 240, 120, 255),
        "inner_color": (15, 95, 45, 255),
        "symbol_color": (230, 255, 210, 255),
        "name": "Wind Ranger Medallion"
    }
]

META_TEMPLATE = """fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {}
  serializedVersion: 12
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vramInitMode: 0
  filterMode: 1
  aniso: 1
  textureType: 8
  textureShape: 1
  singleChannelComponent: 0
  maxTextureSize: 2048
  spriteMode: 1
  spritePixelsToUnits: 100
  spritePivot: {{x: 0.5, y: 0.5}}
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  spriteGenerateFallbackPhysicsShape: 1
  alphaIsTransparency: 1
  spriteMeshType: 1
"""

def generate_medallions():
    size = (128, 128)
    for m in MEDALLIONS:
        img = Image.new("RGBA", size, (0, 0, 0, 0))
        draw = ImageDraw.Draw(img)

        # Outer drop shadow / dark rim
        draw.ellipse([4, 6, 124, 126], fill=(15, 15, 20, 160))
        # Outer Gold/Metallic Rim
        draw.ellipse([6, 6, 122, 122], fill=m["rim_color"], outline=(40, 30, 15, 255), width=2)
        # Inner beveled ring
        draw.ellipse([14, 14, 114, 114], fill=m["bg_color"], outline=m["inner_color"], width=3)
        # Center inset
        draw.ellipse([24, 24, 104, 104], fill=m["inner_color"])

        # Stylized Emblem in center
        # Diamond / star rune crest
        pts = [(64, 30), (78, 64), (64, 98), (50, 64)]
        draw.polygon(pts, fill=m["symbol_color"], outline=m["rim_color"])
        
        # Cross accent
        draw.line([(38, 64), (90, 64)], fill=m["rim_color"], width=3)
        draw.line([(64, 38), (64, 90)], fill=m["rim_color"], width=3)
        # Core gem
        draw.ellipse([58, 58, 70, 70], fill=m["rim_color"])

        # Top shine highlight
        draw.arc([16, 16, 112, 112], start=200, end=340, fill=(255, 255, 255, 180), width=3)

        out_path = os.path.join(OUTPUT_DIR, m["filename"])
        img.save(out_path, "PNG")
        print(f"Generated: {out_path}")

        meta_path = out_path + ".meta"
        if not os.path.exists(meta_path):
            g = uuid.uuid4().hex
            with open(meta_path, "w", encoding="utf-8") as f:
                f.write(META_TEMPLATE.replace("{guid}", g))
            print(f"Generated meta: {meta_path} (guid: {g})")

if __name__ == "__main__":
    generate_medallions()
