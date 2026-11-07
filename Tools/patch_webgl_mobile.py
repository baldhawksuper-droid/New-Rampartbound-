#!/usr/bin/env python3
"""
patch_webgl_mobile.py
Patches Unity WebGL output to support mobile & tablet viewports natively,
provides a phone/tablet frame for desktop QA, enables IndexedDB persistent saves,
unlocks Web Audio autoplay, and disables pull-to-refresh gestures.
"""

import sys
import os

HTML_TEMPLATE = """<!DOCTYPE html>
<html lang="id">
  <head>
    <meta charset="utf-8">
    <meta http-equiv="Content-Type" content="text/html; charset=utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0, user-scalable=no, viewport-fit=cover">
    <meta name="apple-mobile-web-app-capable" content="yes">
    <meta name="mobile-web-app-capable" content="yes">
    <meta name="theme-color" content="#0d111a">
    <title>Rampartbound: Merge Defense (Mobile/Tablet WebGL QA)</title>
    <link rel="shortcut icon" href="TemplateData/favicon.ico">
    <link rel="stylesheet" href="TemplateData/style.css">
  </head>
  <body>
    <div id="device-wrapper">
      <div id="unity-container" class="unity-desktop phone-mode">
        <div id="device-bezel-top">
          <div id="device-camera-notch"></div>
        </div>
        <div id="unity-fullscreen-container">
          <canvas id="unity-canvas" width="1080" height="1920" tabindex="-1"></canvas>
          <div id="unity-loading-container">
            <div id="unity-loading-bar">
              <div id="unity-logo"></div>
              <div id="loading-title">MEMUAT MEDAN PERANG RAMPARTBOUND...</div>
              <div id="unity-progress-bar-empty">
                <div id="unity-progress-bar-full"></div>
              </div>
            </div>
          </div>
        </div>
        <div id="unity-warning"> </div>
        <div id="device-bezel-bottom">
          <div id="device-home-bar"></div>
        </div>
      </div>

      <!-- QA Emulation Selector -->
      <div id="qa-toolbar">
        <button id="btn-phone" class="qa-btn active" onclick="setDeviceMode('phone')">📱 HP (9:16)</button>
        <button id="btn-tablet" class="qa-btn" onclick="setDeviceMode('tablet')">📟 Tablet (3:4)</button>
        <button id="btn-fullscreen" class="qa-btn" onclick="toggleFullscreenDirect()">⛶ Layar Penuh</button>
      </div>
    </div>

    <!-- Landscape Warning Modal (Auto-triggers on mobile/tablet landscape) -->
    <div id="landscape-warning-modal">
      <div class="landscape-box">
        <div class="rotate-device-icon">📱</div>
        <h2>PUTAR PERANGKAT ANDA</h2>
        <p>Rampartbound: Merge Defense dirancang khusus untuk layar vertikal (Portrait). Harap putar perangkat ke posisi tegak untuk pengalaman bermain optimal.</p>
      </div>
    </div>

    <script>
      let canvas = document.querySelector("#unity-canvas");
      let container = document.querySelector("#unity-container");
      let unityInstanceRef = null;

      function setDeviceMode(mode) {
        if (mode === 'phone') {
          container.className = 'unity-desktop phone-mode';
          document.querySelector('#btn-phone').classList.add('active');
          document.querySelector('#btn-tablet').classList.remove('active');
        } else if (mode === 'tablet') {
          container.className = 'unity-desktop tablet-mode';
          document.querySelector('#btn-tablet').classList.add('active');
          document.querySelector('#btn-phone').classList.remove('active');
        }
      }

      function toggleFullscreenDirect() {
        if (unityInstanceRef) {
          unityInstanceRef.SetFullscreen(1);
        } else if (container.requestFullscreen) {
          container.requestFullscreen();
        }
      }

      // Web Audio autoplay unlock handler for mobile browsers
      function resumeWebAudioContext() {
        try {
          if (unityInstanceRef && unityInstanceRef.Module) {
            const al = unityInstanceRef.Module.AL;
            if (al && al.currentCtx && al.currentCtx.audioCtx && al.currentCtx.audioCtx.state === 'suspended') {
              al.currentCtx.audioCtx.resume();
            }
          }
        } catch (e) {
          // ignore
        }
      }
      window.addEventListener('pointerdown', resumeWebAudioContext, { passive: true });
      window.addEventListener('touchstart', resumeWebAudioContext, { passive: true });
      window.addEventListener('click', resumeWebAudioContext, { passive: true });

      function unityShowBanner(msg, type) {
        var warningBanner = document.querySelector("#unity-warning");
        function updateBannerVisibility() {
          warningBanner.style.display = warningBanner.children.length ? 'block' : 'none';
        }
        var div = document.createElement('div');
        div.innerHTML = msg;
        warningBanner.appendChild(div);
        if (type == 'error') div.style = 'background: red; padding: 10px;';
        else {
          if (type == 'warning') div.style = 'background: yellow; padding: 10px;';
          setTimeout(function() {
            warningBanner.removeChild(div);
            updateBannerVisibility();
          }, 5000);
        }
        updateBannerVisibility();
      }

      var buildUrl = "Build";
      var loaderUrl = buildUrl + "/Rampartbound.loader.js";
      var config = {
        arguments: [],
        dataUrl: buildUrl + "/Rampartbound.data",
        frameworkUrl: buildUrl + "/Rampartbound.framework.js",
        codeUrl: buildUrl + "/Rampartbound.wasm",
        streamingAssetsUrl: "StreamingAssets",
        fullscreenElementID: "unity-fullscreen-container",
        companyName: "BaldHawk Studios",
        productName: "Rampartbound: Merge Defense",
        productVersion: "1.2.18",
        showBanner: unityShowBanner,
      };

      // Persistent save data in IndexedDB
      config.autoSyncPersistentDataPath = true;

      // Mobile device detection: switch to full immersion mobile viewport
      if (/iPhone|iPad|iPod|Android/i.test(navigator.userAgent) || (window.innerWidth <= 900 && window.matchMedia("(pointer: coarse)").matches)) {
        document.body.className = "unity-mobile";
        container.className = "unity-mobile";
        canvas.className = "unity-mobile";
      }

      var script = document.createElement("script");
      script.src = loaderUrl;
      script.onload = () => {
        const loadingContainer = document.querySelector("#unity-loading-container");
        const progressBarFull = document.querySelector("#unity-progress-bar-full");

        createUnityInstance(canvas, config, (progress) => {
          if (progress < 1) {
            loadingContainer.style.display = "flex";
            progressBarFull.style.width = 100 * progress + "%";
          } else {
            loadingContainer.style.display = "none";
          }
        }).then((instance) => {
          unityInstanceRef = instance;
        }).catch((message) => {
          alert(message);
        });
      };
      document.body.appendChild(script);
    </script>
  </body>
</html>
"""

CSS_TEMPLATE = """html, body {
  margin: 0;
  padding: 0;
  width: 100%;
  height: 100%;
  overflow: hidden;
  background: radial-gradient(circle at center, #1b2434 0%, #0a0d14 100%);
  display: flex;
  align-items: center;
  justify-content: center;
  touch-action: none;
  overscroll-behavior: none;
  font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif;
  user-select: none;
  -webkit-user-select: none;
}

#device-wrapper {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  width: 100%;
  height: 100%;
}

#unity-container {
  position: relative;
  background: #000;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  overflow: hidden;
  transition: all 0.25s ease;
}

#unity-container.phone-mode {
  height: min(92vh, 880px);
  width: min(calc(min(92vh, 880px) * (1080 / 1920)), 96vw);
  border-radius: 38px;
  box-shadow: 0 25px 60px rgba(0, 0, 0, 0.9), 0 0 0 10px #222b3d, 0 0 0 12px #35425c;
}

#unity-container.tablet-mode {
  height: min(92vh, 880px);
  width: min(calc(min(92vh, 880px) * (3 / 4)), 96vw);
  border-radius: 28px;
  box-shadow: 0 25px 60px rgba(0, 0, 0, 0.9), 0 0 0 10px #222b3d, 0 0 0 12px #35425c;
}

#device-bezel-top {
  width: 100%;
  height: 24px;
  background: #111520;
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 10;
  flex-shrink: 0;
}

#device-camera-notch {
  width: 50px;
  height: 5px;
  background: #2a3348;
  border-radius: 3px;
}

#device-bezel-bottom {
  width: 100%;
  height: 18px;
  background: #111520;
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 10;
  flex-shrink: 0;
}

#device-home-bar {
  width: 90px;
  height: 4px;
  background: #475569;
  border-radius: 2px;
}

#unity-fullscreen-container {
  position: relative;
  width: 100%;
  height: 100%;
  flex-grow: 1;
  overflow: hidden;
}

#unity-canvas {
  position: absolute;
  top: 0;
  left: 0;
  width: 100% !important;
  height: 100% !important;
  display: block;
  touch-action: none;
  background: #0d111a;
}

#qa-toolbar {
  margin-top: 14px;
  display: flex;
  gap: 10px;
  background: rgba(15, 23, 42, 0.85);
  padding: 6px 14px;
  border-radius: 20px;
  border: 1px solid rgba(255, 215, 0, 0.25);
  box-shadow: 0 4px 15px rgba(0,0,0,0.5);
  z-index: 20;
}

.qa-btn {
  background: #1e293b;
  color: #cbd5e1;
  border: 1px solid #334155;
  border-radius: 12px;
  padding: 5px 12px;
  font-size: 12px;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s;
}

.qa-btn:hover {
  background: #334155;
  color: #f8fafc;
}

.qa-btn.active {
  background: #d97706;
  border-color: #f59e0b;
  color: #ffffff;
  box-shadow: 0 0 10px rgba(245, 158, 11, 0.4);
}

.unity-mobile #unity-container,
#unity-container.fullscreen-direct {
  width: 100vw !important;
  height: 100vh !important;
  max-width: 100vw !important;
  max-height: 100vh !important;
  border-radius: 0 !important;
  box-shadow: none !important;
}

.unity-mobile #device-bezel-top,
.unity-mobile #device-bezel-bottom,
.unity-mobile #qa-toolbar,
#unity-container.fullscreen-direct #device-bezel-top,
#unity-container.fullscreen-direct #device-bezel-bottom {
  display: none !important;
}

#unity-loading-container {
  position: absolute;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  background: radial-gradient(circle at center, #1b2434 0%, #0a0d14 100%);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 50;
}

#unity-loading-bar {
  display: flex;
  flex-direction: column;
  align-items: center;
}

#unity-logo {
  width: 120px;
  height: 120px;
  background: url("unity-logo-dark.png") no-repeat center;
  background-size: contain;
  filter: drop-shadow(0 0 15px rgba(217, 119, 6, 0.4));
}

#loading-title {
  margin-top: 15px;
  font-size: 13px;
  font-weight: 700;
  color: #f1f5f9;
  letter-spacing: 1px;
}

#unity-progress-bar-empty {
  width: 180px;
  height: 10px;
  margin-top: 12px;
  background: #1e293b;
  border-radius: 5px;
  border: 1px solid #475569;
  overflow: hidden;
}

#unity-progress-bar-full {
  width: 0%;
  height: 100%;
  background: linear-gradient(90deg, #d97706, #fbbf24);
  border-radius: 5px;
  transition: width 0.15s ease-out;
}

#unity-warning {
  position: absolute;
  top: 10px;
  left: 50%;
  transform: translateX(-50%);
  background: #fef08a;
  color: #854d0e;
  padding: 8px 14px;
  border-radius: 6px;
  font-size: 12px;
  font-weight: 600;
  display: none;
  z-index: 99;
}

/* Landscape Warning Overlay for Mobile/Tablet Browser */
#landscape-warning-modal {
  display: none;
  position: fixed;
  top: 0;
  left: 0;
  width: 100vw;
  height: 100vh;
  background: rgba(10, 13, 20, 0.96);
  z-index: 99999;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  text-align: center;
  padding: 24px;
  box-sizing: border-box;
  backdrop-filter: blur(8px);
}

.landscape-box {
  background: #161d2b;
  border: 1px solid #f59e0b;
  border-radius: 20px;
  padding: 28px 24px;
  max-width: 380px;
  box-shadow: 0 10px 40px rgba(0, 0, 0, 0.8), 0 0 20px rgba(245, 158, 11, 0.2);
  display: flex;
  flex-direction: column;
  align-items: center;
}

.rotate-device-icon {
  font-size: 48px;
  animation: rotatePhone 2.2s infinite ease-in-out;
  margin-bottom: 12px;
}

@keyframes rotatePhone {
  0% { transform: rotate(0deg); }
  30% { transform: rotate(-90deg); }
  70% { transform: rotate(-90deg); }
  100% { transform: rotate(0deg); }
}

.landscape-box h2 {
  font-size: 18px;
  font-weight: 800;
  color: #fbbf24;
  margin: 0 0 8px 0;
  letter-spacing: 0.5px;
}

.landscape-box p {
  font-size: 13px;
  color: #cbd5e1;
  line-height: 1.5;
  margin: 0;
}

@media (orientation: landscape) and (max-height: 600px), (orientation: landscape) and (pointer: coarse) {
  #landscape-warning-modal {
    display: flex !important;
  }
}
"""

def patch_webgl(target_dir):
    html_path = os.path.join(target_dir, "index.html")
    css_path = os.path.join(target_dir, "TemplateData", "style.css")

    if not os.path.exists(target_dir):
        print(f"Error: Target directory {target_dir} does not exist.")
        return 1

    with open(html_path, "w", encoding="utf-8") as f:
        f.write(HTML_TEMPLATE.strip())
    print(f"Patched: {html_path}")

    os.makedirs(os.path.join(target_dir, "TemplateData"), exist_ok=True)
    with open(css_path, "w", encoding="utf-8") as f:
        f.write(CSS_TEMPLATE.strip())
    print(f"Patched: {css_path}")

    return 0

if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("Usage: python patch_webgl_mobile.py <path_to_webgl_build_folder>")
        sys.exit(1)
    sys.exit(patch_webgl(sys.argv[1]))
