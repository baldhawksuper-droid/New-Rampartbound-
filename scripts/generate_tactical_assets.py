import os
import uuid
from PIL import Image, ImageDraw

OUTPUT_DIR = r"D:\PROJECT GAMES\Rampartbound\Assets\Art\Sprites\UI"
os.makedirs(OUTPUT_DIR, exist_ok=True)

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
  spritePivot: {x: 0.5, y: 0.5}
  spriteBorder: {x: 20, y: 15, z: 20, w: 15}
  spriteGenerateFallbackPhysicsShape: 0
  alphaIsTransparency: 1
  spriteMeshType: 1
"""

def generate_tactical_banner():
    width, height = 480, 96
    img = Image.new("RGBA", (width, height), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    # 1. Base Banner Ribbon Shape with dovetail cuts on left/right
    # Background gradient / layered polygons
    left_inset = 24
    right_inset = width - 24

    # Shadow
    draw.polygon([
        (left_inset, 8), (right_inset, 8),
        (width - 6, height // 2 + 2), (right_inset, height - 4),
        (left_inset, height - 4), (6, height // 2 + 2)
    ], fill=(10, 15, 30, 200))

    # Outer Gold Frame
    draw.polygon([
        (left_inset + 2, 4), (right_inset - 2, 4),
        (width - 8, height // 2), (right_inset - 2, height - 8),
        (left_inset + 2, height - 8), (8, height // 2)
    ], fill=(225, 180, 50, 255))

    # Inner Dark Royal Bastion Body
    draw.polygon([
        (left_inset + 6, 8), (right_inset - 6, 8),
        (width - 16, height // 2), (right_inset - 6, height - 12),
        (left_inset + 6, height - 12), (16, height // 2)
    ], fill=(25, 35, 65, 245))

    # Highlighting stripes
    draw.rectangle([left_inset + 12, 12, right_inset - 12, 16], fill=(255, 220, 100, 180))
    draw.rectangle([left_inset + 12, height - 16, right_inset - 12, height - 14], fill=(180, 140, 30, 160))

    # Left & Right decorative gold rivets
    for x in [left_inset + 14, right_inset - 14]:
        draw.ellipse([x - 4, height // 2 - 4, x + 4, height // 2 + 4], fill=(255, 240, 150, 255), outline=(150, 100, 20, 255))

    png_path = os.path.join(OUTPUT_DIR, "Banner_Tactical_Phase.png")
    img.save(png_path, "PNG")
    print(f"Generated {png_path}")

    meta_path = png_path + ".meta"
    if not os.path.exists(meta_path):
        guid = uuid.uuid4().hex
        with open(meta_path, "w", encoding="utf-8") as f:
            f.write(META_TEMPLATE.replace("{guid}", guid))
        print(f"Generated meta: {meta_path} (GUID: {guid})")

def generate_tactical_move_icon():
    size = 64
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    # Glowing aura
    draw.ellipse([4, 4, size - 4, size - 4], fill=(255, 215, 0, 40))
    draw.ellipse([8, 8, size - 8, size - 8], fill=(30, 45, 80, 230), outline=(240, 190, 40, 255), width=3)

    # Stylized lightning bolt / tactical move symbol
    points = [
        (34, 12),
        (20, 32),
        (30, 32),
        (24, 52),
        (44, 28),
        (32, 28)
    ]
    draw.polygon(points, fill=(255, 235, 80, 255))
    # Bolt inner highlight
    draw.polygon([(34, 16), (24, 30), (32, 30), (28, 46), (40, 28), (32, 28)], fill=(255, 255, 200, 255))

    png_path = os.path.join(OUTPUT_DIR, "Icon_Tactical_Move.png")
    img.save(png_path, "PNG")
    print(f"Generated {png_path}")

    meta_path = png_path + ".meta"
    if not os.path.exists(meta_path):
        guid = uuid.uuid4().hex
        with open(meta_path, "w", encoding="utf-8") as f:
            f.write(META_TEMPLATE.replace("{guid}", guid))
        print(f"Generated meta: {meta_path} (GUID: {guid})")

if __name__ == "__main__":
    generate_tactical_banner()
    generate_tactical_move_icon()
