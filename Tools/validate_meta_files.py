#!/usr/bin/env python3
"""
Unity .meta File Integrity Validator for Rampartbound.
Checks:
1. Every file and directory in Assets/ has a matching .meta file.
2. Every .meta file points to a valid file or directory (no orphan .meta files).
3. Every .meta file contains a valid 32-character hexadecimal GUID.
"""

import os
import re
import sys

ASSETS_DIR = "Assets"
GUID_PATTERN = re.compile(r"guid:\s*([a-fA-F0-9]{32})")

def validate_meta_files():
    if not os.path.exists(ASSETS_DIR):
        print(f"Error: {ASSETS_DIR} directory not found.")
        return 1

    missing_metas = []
    orphan_metas = []
    invalid_guids = []
    total_items = 0
    total_metas = 0

    # 1. Check all files and directories have .meta
    for root, dirs, files in os.walk(ASSETS_DIR):
        # Check directories
        for d in dirs:
            dir_path = os.path.join(root, d)
            meta_path = dir_path + ".meta"
            total_items += 1
            if not os.path.exists(meta_path):
                missing_metas.append(dir_path)

        # Check files
        for f in files:
            if f.endswith(".meta") or f.startswith("."):
                continue
            file_path = os.path.join(root, f)
            meta_path = file_path + ".meta"
            total_items += 1
            if not os.path.exists(meta_path):
                missing_metas.append(file_path)

    # 2. Check all .meta files are valid and not orphaned
    for root, dirs, files in os.walk(ASSETS_DIR):
        for f in files:
            if not f.endswith(".meta"):
                continue
            total_metas += 1
            meta_path = os.path.join(root, f)
            target_path = meta_path[:-5] # strip .meta

            if not os.path.exists(target_path):
                orphan_metas.append(meta_path)
                continue

            # Verify GUID
            try:
                with open(meta_path, "r", encoding="utf-8", errors="ignore") as fp:
                    content = fp.read()
                    match = GUID_PATTERN.search(content)
                    if not match:
                        invalid_guids.append(meta_path)
            except Exception as e:
                invalid_guids.append(f"{meta_path} (Read error: {e})")

    print("==================================================")
    print("      RAMPARTBOUND UNITY .META INTEGRITY AUDIT    ")
    print("==================================================")
    print(f"Total Assets Checked : {total_items}")
    print(f"Total .meta Checked  : {total_metas}")

    passed = True

    if missing_metas:
        passed = False
        print(f"\n[FAIL] Missing .meta files ({len(missing_metas)}):")
        for m in missing_metas[:20]:
            print(f"  - {m}")
        if len(missing_metas) > 20:
            print(f"  ... and {len(missing_metas) - 20} more")

    if orphan_metas:
        passed = False
        print(f"\n[FAIL] Orphan .meta files ({len(orphan_metas)}):")
        for o in orphan_metas[:20]:
            print(f"  - {o}")
        if len(orphan_metas) > 20:
            print(f"  ... and {len(orphan_metas) - 20} more")

    if invalid_guids:
        passed = False
        print(f"\n[FAIL] Invalid/Missing GUIDs ({len(invalid_guids)}):")
        for g in invalid_guids[:20]:
            print(f"  - {g}")

    if passed:
        print("\n[SUCCESS] All Unity .meta files are intact, matched, and valid!")
        return 0
    else:
        print("\n[FAILED] Meta integrity issues detected. Fix them before commit.")
        return 1

if __name__ == "__main__":
    sys.exit(validate_meta_files())
