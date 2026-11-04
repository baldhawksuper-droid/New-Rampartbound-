#!/usr/bin/env python3
"""
Downloads and configures Unity 6 (6000.0.x) reference assemblies for local C# compilation.
Used if Tools/UnityLibs/ needs to be restored or updated.
"""

import os
import sys
import urllib.request
import zipfile
import shutil

LIBS_DIR = os.path.join(os.path.dirname(os.path.abspath(__file__)), "UnityLibs")
NUGET_PKG_URL = "https://nuget.bepinex.dev/v3/package/UnityEngine.Modules/6000.6.6/UnityEngine.Modules.6000.6.6.nupkg"

def setup_libs():
    os.makedirs(LIBS_DIR, exist_ok=True)
    nupkg_path = os.path.join(LIBS_DIR, "UnityEngine.Modules.nupkg")
    
    print(f"[1/3] Downloading UnityEngine.Modules 6000.6.6...")
    urllib.request.urlretrieve(NUGET_PKG_URL, nupkg_path)
    
    print(f"[2/3] Extracting netstandard2.0/2.1 reference assemblies...")
    with zipfile.ZipFile(nupkg_path, 'r') as zf:
        for member in zf.namelist():
            if member.startswith('lib/netstandard2.0/') and member.endswith('.dll'):
                filename = os.path.basename(member)
                target = os.path.join(LIBS_DIR, filename)
                with zf.open(member) as src, open(target, 'wb') as dst:
                    shutil.copyfileobj(src, dst)
                    
    if os.path.exists(nupkg_path):
        os.remove(nupkg_path)

    print(f"[3/3] Reference libraries ready in {LIBS_DIR}!")

if __name__ == "__main__":
    setup_libs()
