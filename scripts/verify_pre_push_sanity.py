#!/usr/bin/env python3
"""
Rampartbound QA Pre-Push Sanity & Anti-Regression Auditor.
Validates:
1. C# syntax sanity: bracket balance, region balance, no unescaped syntax errors.
2. Asset metadata & GUID preservative integrity: every .cs, .png, .jpg, .asset has a valid .meta with 32-char hex GUID.
3. Namespace synchronization & zero missing references.
"""

import os
import re
import sys

PROJECT_ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
ASSETS_DIR = os.path.join(PROJECT_ROOT, "Assets")

def strip_csharp_code(text):
    """
    Strips out comments, verbatim strings, interpolated strings, normal strings,
    and character literals to leave only structural C# code for syntax/brace checking.
    """
    result = []
    i = 0
    n = len(text)
    while i < n:
        # Check single-line comment
        if i + 1 < n and text[i:i+2] == '//':
            i += 2
            while i < n and text[i] != '\n':
                i += 1
            continue

        # Check block comment
        if i + 1 < n and text[i:i+2] == '/*':
            i += 2
            while i + 1 < n and text[i:i+2] != '*/':
                i += 1
            i += 2
            continue

        # Check verbatim string: @"..." or $@"..." or @$"..."
        if (i + 1 < n and text[i:i+2] == '@"') or (i + 2 < n and text[i:i+3] in ('$@"', '@$"')):
            i = text.find('"', i) + 1
            while i < n:
                if text[i] == '"':
                    if i + 1 < n and text[i+1] == '"':
                        i += 2
                    else:
                        i += 1
                        break
                else:
                    i += 1
            continue

        # Check regular string: "..." or $"..."
        if text[i] == '"' or (i + 1 < n and text[i:i+2] == '$"'):
            i = text.find('"', i) + 1
            while i < n:
                if text[i] == '\\':
                    i += 2
                elif text[i] == '"':
                    i += 1
                    break
                else:
                    i += 1
            continue

        # Check character literal: '...'
        if text[i] == "'":
            i += 1
            while i < n:
                if text[i] == '\\':
                    i += 2
                elif text[i] == "'":
                    i += 1
                    break
                else:
                    i += 1
            continue

        result.append(text[i])
        i += 1

    return "".join(result)

def audit_cs_files():
    errors = []
    cs_files = []
    for root, _, files in os.walk(ASSETS_DIR):
        for f in files:
            if f.endswith(".cs"):
                cs_files.append(os.path.join(root, f))

    stripped_cache = {}
    for path in cs_files:
        rel_path = os.path.relpath(path, PROJECT_ROOT)
        with open(path, "r", encoding="utf-8", errors="replace") as file:
            content = file.read()

        stripped = strip_csharp_code(content)
        stripped_cache[path] = stripped

        open_braces = stripped.count('{')
        close_braces = stripped.count('}')
        if open_braces != close_braces:
            errors.append(f"[Syntax Error] {rel_path}: Unbalanced curly braces! (Open: {open_braces}, Close: {close_braces})")

        open_regions = len(re.findall(r'^\s*#region\b', content, re.MULTILINE))
        close_regions = len(re.findall(r'^\s*#endregion\b', content, re.MULTILINE))
        if open_regions != close_regions:
            errors.append(f"[Syntax Error] {rel_path}: Unbalanced #region / #endregion! (Regions: {open_regions}, Endregions: {close_regions})")

        if "FantasyDefense.Security" in content:
            errors.append(f"[Namespace Error] {rel_path}: Referenced non-existent namespace 'FantasyDefense.Security'!")

    # Audit enum consistency across the codebase
    enum_def_re = re.compile(r'\benum\s+([A-Za-z0-9_]+)\s*\{([^}]+)\}')
    declared_enums = {}
    for path, c in stripped_cache.items():
        for m in enum_def_re.finditer(c):
            ename = m.group(1)
            ebody = m.group(2)
            members = set()
            for part in ebody.split(','):
                part = part.strip()
                if part:
                    mname = part.split('=')[0].strip()
                    if mname and re.match(r'^[A-Za-z_][A-Za-z0-9_]*$', mname):
                        members.add(mname)
            if ename in declared_enums:
                declared_enums[ename].update(members)
            else:
                declared_enums[ename] = members

    # Validate enum member accesses for critical project enums
    CRITICAL_ENUMS = {'SoundType', 'AudioSfxType', 'UnitType', 'UnitTier', 'MonsterType', 'BattleMode', 'BattleState'}
    for path, c in stripped_cache.items():
        rel_path = os.path.relpath(path, PROJECT_ROOT)
        for ename in CRITICAL_ENUMS:
            if ename in declared_enums:
                valid_members = declared_enums[ename]
                # Match occurrences of EnumName.MemberName
                for ref_match in re.finditer(r'\b' + ename + r'\.([A-Za-z0-9_]+)\b', c):
                    ref_member = ref_match.group(1)
                    if ref_member not in valid_members:
                        errors.append(f"[Enum Error] {rel_path}: '{ename}.{ref_member}' does not exist in enum '{ename}'! (Valid: {', '.join(sorted(valid_members))})")

    # Validate UnitDataSO member accesses across the codebase
    unit_data_so_path = os.path.join(PROJECT_ROOT, "Assets", "Scripts", "Data", "UnitDataSO.cs")
    if os.path.exists(unit_data_so_path):
        with open(unit_data_so_path, "r", encoding="utf-8") as f:
            so_code = strip_csharp_code(f.read())
        so_members = set()
        for m in re.finditer(r'public\s+(?:[\w<>\[\]?]+)\s+(\w+)\s*(?:[;=]|\{\s*get|=>|\()', so_code):
            so_members.add(m.group(1))

        for path, c in stripped_cache.items():
            rel_path = os.path.relpath(path, PROJECT_ROOT)
            for m in re.finditer(r'\bunitData\.([A-Za-z0-9_]+)\b', c):
                member_name = m.group(1)
                if member_name not in so_members:
                    errors.append(f"[Member Access Error] {rel_path}: 'unitData.{member_name}' does not exist in 'UnitDataSO'!")

    # Validate MonsterDataSO member accesses across the codebase
    monster_data_so_path = os.path.join(PROJECT_ROOT, "Assets", "Scripts", "Data", "MonsterDataSO.cs")
    if os.path.exists(monster_data_so_path):
        with open(monster_data_so_path, "r", encoding="utf-8") as f:
            m_so_code = strip_csharp_code(f.read())
        m_so_members = set()
        for m in re.finditer(r'public\s+(?:[\w<>\[\]?]+)\s+(\w+)\s*(?:[;=]|\{\s*get|=>|\()', m_so_code):
            m_so_members.add(m.group(1))

        for path, c in stripped_cache.items():
            rel_path = os.path.relpath(path, PROJECT_ROOT)
            for m in re.finditer(r'\bmonsterData\.([A-Za-z0-9_]+)\b', c):
                member_name = m.group(1)
                if member_name not in m_so_members:
                    errors.append(f"[Member Access Error] {rel_path}: 'monsterData.{member_name}' does not exist in 'MonsterDataSO'!")

    # Audit duplicate method declarations (CS0111 collision prevention)
    CONTROL_FLOW = {'if', 'while', 'for', 'foreach', 'switch', 'catch', 'using', 'lock', 'fixed', 'return', 'throw'}
    method_decl_re = re.compile(
        r'^\s*(?:(public|private|protected|internal)\s+)?(?:(?:static|virtual|override|async|new|readonly|sealed)\s+)*(void|int|float|bool|string|IEnumerator|[A-Za-z0-9_<>\[\],\?]+)\s+([A-Za-z0-9_]+)\s*\(([^)]*)\)\s*(?:\{|=>)',
        re.MULTILINE
    )

    def normalize_param_types(p_str):
        if not p_str.strip():
            return ()
        types = []
        for p in p_str.split(','):
            parts = p.strip().split()
            if parts:
                types.append(parts[0])
        return tuple(types)

    for path, c in stripped_cache.items():
        rel_path = os.path.relpath(path, PROJECT_ROOT)
        methods = {}
        for m in method_decl_re.finditer(c):
            ret_type = m.group(2)
            mname = m.group(3)
            if ret_type in CONTROL_FLOW or mname in CONTROL_FLOW:
                continue
            ptypes = normalize_param_types(m.group(4))
            key = (mname, ptypes)
            line = c[:m.start()].count('\n') + 1
            if key in methods:
                prev_line = methods[key]
                errors.append(f"[Duplicate Member Error CS0111] {rel_path}: Method '{mname}({', '.join(ptypes)})' is declared multiple times! (Line {prev_line} and Line {line})")
            else:
                methods[key] = line

    return errors, len(cs_files)

def audit_meta_integrity():
    errors = []
    total_assets = 0
    guid_pattern = re.compile(r'guid:\s*([a-fA-F0-9]{32})')

    TRACKED_EXTENSIONS = {'.cs', '.png', '.jpg', '.asset', '.prefab', '.mat', '.unity', '.controller', '.anim', '.shader'}

    for root, dirs, files in os.walk(ASSETS_DIR):
        for f in files:
            full_path = os.path.join(root, f)
            rel_path = os.path.relpath(full_path, PROJECT_ROOT)

            if f.endswith(".meta"):
                # Check target file/folder exists
                target_path = full_path[:-5]
                if not os.path.exists(target_path):
                    errors.append(f"[Orphaned Meta] {rel_path}: Meta file exists but asset is missing!")
                else:
                    # Validate GUID
                    with open(full_path, "r", encoding="utf-8", errors="replace") as mf:
                        m_content = mf.read()
                    if not guid_pattern.search(m_content):
                        errors.append(f"[Corrupted Meta] {rel_path}: Missing or invalid 32-character hexadecimal GUID!")
                    if "{{" in m_content or "}}" in m_content:
                        errors.append(f"[Corrupted Meta] {rel_path}: Invalid double braces '{{{{' or '}}}}' found in YAML meta file!")
            else:
                _, ext = os.path.splitext(f)
                if ext.lower() in TRACKED_EXTENSIONS:
                    total_assets += 1
                    meta_path = full_path + ".meta"
                    if not os.path.exists(meta_path):
                        errors.append(f"[Missing Meta] {rel_path}: Asset has no accompanying .meta file!")

    return errors, total_assets

def main():
    print("=" * 70)
    print(" [RAMPARTBOUND] QA PRE-PUSH INTEGRITY & ANTI-REGRESSION AUDITOR")
    print("=" * 70)

    cs_errors, cs_count = audit_cs_files()
    meta_errors, asset_count = audit_meta_integrity()

    all_errors = cs_errors + meta_errors

    print(f">> Audited {cs_count} C# source files.")
    print(f">> Audited {asset_count} project assets for metadata integrity.")

    if all_errors:
        print("\n❌ PRE-PUSH AUDIT FAILED! The following issues must be resolved:")
        for err in all_errors:
            print(f"  - {err}")
        print("\nPush aborted to prevent remote build or compiler regression.")
        sys.exit(1)

    print("\n✅ PRE-PUSH AUDIT 100% PASS! All scripts, namespaces, and .meta GUIDs are intact.")
    print("=" * 70)
    sys.exit(0)

if __name__ == "__main__":
    main()
