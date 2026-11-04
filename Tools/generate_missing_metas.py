#!/usr/bin/env python3
"""
Generate missing Unity .meta files with deterministic MD5 GUIDs based on relative asset paths.
Supports:
- Folders (DefaultImporter)
- C# scripts (MonoImporter)
- Textures/Sprites (.png, .jpg) (TextureImporter as Sprite 2D)
- Generic Assets (DefaultImporter)
"""

import os
import hashlib

ASSETS_DIR = "Assets"

FOLDER_META_TEMPLATE = """fileFormatVersion: 2
guid: {guid}
folderAsset: yes
DefaultImporter:
  externalObjects: {{}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""

CS_META_TEMPLATE = """fileFormatVersion: 2
guid: {guid}
MonoImporter:
  externalObjects: {{}}
  serializedVersion: 2
  defaultReferences: []
  executionOrder: 0
  icon: {{instanceID: 0}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""

SPRITE_META_TEMPLATE = """fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
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
  isReadable: 1
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMasterTextureLimit: 0
  doesThumbnails: 1
  isAsync: 0
  ignoreQuality: 0
  assetStoreQuality: 0
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 1
    mipBias: 0
    wrapU: 0
    wrapV: 0
    wrapW: 0
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 1
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {{x: 0.5, y: 0.5}}
  spritePixelsToUnits: 100
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  spriteGenerateFallbackPhysicsShape: 1
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationDetail: -1
  textureType: 8
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  cookieLightType: 0
  platformSettings:
  - serializedVersion: 3
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 1
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  - serializedVersion: 3
    buildTarget: Android
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 1
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""

def compute_guid(rel_path):
    # Normalized unix path for deterministic hash
    norm = rel_path.replace("\\", "/").lower()
    return hashlib.md5(norm.encode("utf-8")).hexdigest()

def generate_missing_metas():
    created = 0
    for root, dirs, files in os.walk(ASSETS_DIR):
        for d in dirs:
            dir_path = os.path.join(root, d)
            meta_path = dir_path + ".meta"
            if not os.path.exists(meta_path):
                guid = compute_guid(dir_path)
                with open(meta_path, "w", encoding="utf-8") as fp:
                    fp.write(FOLDER_META_TEMPLATE.format(guid=guid))
                created += 1
                print(f"[Created Dir Meta] {meta_path} (guid: {guid})")

        for f in files:
            if f.endswith(".meta") or f.startswith("."):
                continue
            file_path = os.path.join(root, f)
            meta_path = file_path + ".meta"
            if not os.path.exists(meta_path):
                guid = compute_guid(file_path)
                if f.endswith(".cs"):
                    content = CS_META_TEMPLATE.format(guid=guid)
                elif f.lower().endswith((".png", ".jpg", ".jpeg", ".tga", ".psd")):
                    content = SPRITE_META_TEMPLATE.format(guid=guid)
                else:
                    content = FOLDER_META_TEMPLATE.format(guid=guid)
                with open(meta_path, "w", encoding="utf-8") as fp:
                    fp.write(content)
                created += 1
                print(f"[Created File Meta] {meta_path} (guid: {guid})")

    print(f"\nDone. Successfully generated {created} missing .meta files.")

if __name__ == "__main__":
    generate_missing_metas()
