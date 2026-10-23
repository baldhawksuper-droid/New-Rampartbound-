import asyncio
import os
import subprocess
import time
import json
import sys
from pathlib import Path
from telegram import Bot

BOT_TOKEN = "8998182093:AAHgjcnIbGNlLJ5JDc1xVD87lWQvAC9mRvs"
CHAT_ID = 5597248348
REPO = "baldhawksuper-droid/Rampartbound-Unity"
OUTPUT_DIR = Path(r"D:\PROJECT GAMES\Rampartbound\Builds\Android")
STATUS_FILE = Path(r"D:\PROJECT GAMES\NewProjectGames\build_status.json")

def get_run_for_commit(commit_sha):
    cmd = ["gh", "run", "list", "-R", REPO, "--limit", "10", "--json", "databaseId,status,conclusion,headSha,createdAt"]
    result = subprocess.run(cmd, capture_output=True, text=True)
    if result.returncode == 0:
        try:
            runs = json.loads(result.stdout)
            for r in runs:
                if r.get("headSha", "").startswith(commit_sha):
                    return str(r.get("databaseId"))
        except Exception as e:
            print("[Monitor] Error parsing run list for commit:", e)
    return None

def get_latest_run_id():
    cmd = ["gh", "run", "list", "-R", REPO, "--limit", "1", "--json", "databaseId,status,conclusion,headSha"]
    result = subprocess.run(cmd, capture_output=True, text=True)
    if result.returncode == 0:
        try:
            runs = json.loads(result.stdout)
            if runs and len(runs) > 0:
                return str(runs[0].get("databaseId"))
        except Exception as e:
            print("[Monitor] Error parsing latest run:", e)
    return None

def check_run_status(run_id):
    cmd = ["gh", "run", "view", run_id, "-R", REPO, "--json", "status,conclusion,databaseId"]
    result = subprocess.run(cmd, capture_output=True, text=True)
    if result.returncode != 0:
        return None, None
    try:
        data = json.loads(result.stdout)
        return data.get("status"), data.get("conclusion")
    except Exception:
        return None, None

def get_failed_log_summary(run_id):
    cmd = ["gh", "run", "view", run_id, "-R", REPO, "--log-failed"]
    result = subprocess.run(cmd, capture_output=True, text=True, errors="replace")
    output = result.stdout + "\n" + result.stderr
    error_lines = []
    for line in output.splitlines():
        if any(err_kw in line for err_kw in ["error CS", "Exception:", "ArgumentException", "NullReferenceException", "Build failed", "Failed with"]):
            if "ValidationExceptions.json" in line:
                continue
            error_lines.append(line.strip())
            if len(error_lines) >= 12:
                break
    return "\n".join(error_lines) if error_lines else "Lihat log GitHub Actions untuk detail lengkap."

def download_artifact(run_id):
    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
    for f in OUTPUT_DIR.rglob("*.apk"):
        try:
            f.unlink()
        except Exception:
            pass
    cmd = ["gh", "run", "download", run_id, "-R", REPO, "-n", "Rampartbound-Android-apk", "-D", str(OUTPUT_DIR)]
    result = subprocess.run(cmd, capture_output=True, text=True)
    print("[Monitor] Download result:", result.stdout, result.stderr)
    return result.returncode == 0

async def notify_failure_to_telegram(run_id, error_summary):
    bot = Bot(token=BOT_TOKEN)
    msg = (
        f"🚨 <b>LAPORAN KEGAGALAN BUILD ANDROID</b>\n\n"
        f"<b>Repository:</b> <code>{REPO}</code>\n"
        f"<b>Run ID:</b> <code>{run_id}</code>\n\n"
        f"⚠️ <b>Cuplikan Log Kompilasi Error:</b>\n"
        f"<code>{error_summary[:1800]}</code>\n\n"
        f"🛠️ <i>Langkah perbaikan otomatis sedang diselesaikan oleh BaldHawk & tim Senior Developer.</i>"
    )
    try:
        await bot.send_message(chat_id=CHAT_ID, text=msg, parse_mode="HTML")
        print("[Monitor] Telegram failure notification sent.")
    except Exception as e:
        print("[Monitor] Failed to send Telegram alert:", e)

async def send_apk_to_telegram(apk_path: Path):
    bot = Bot(token=BOT_TOKEN)
    size_mb = apk_path.stat().st_size / (1024 * 1024)
    short_caption = (
        f"⚔️ <b>Rampartbound: Merge Defense — v1.2.14</b>\n"
        f"✨ <i>Monster Hero Engagement, Gate Balance & Crash Hotfix</i>\n"
        f"📦 <b>File:</b> <code>{apk_path.name}</code> ({size_mb:.2f} MB)"
    )
    details_msg = (
        "🔥 <b>Perbaikan & Penyempurnaan Resmi v1.2.14:</b>\n\n"
        "1. ⚔️ <b>Monster Menyerang & Menghadang Hero Terlebih Dahulu</b>:\n"
        "   • Monster tidak lagi menembus tubuh hero! Saat mendekati hero defender di grid, monster kini otomatis berhenti dan menyerang hero di hadapannya.\n"
        "   • Hero frontline (Cedric & Rose Tanker) kini bertindak sebagai garda benteng sesungguhnya dengan animasi lunge, hit recoil, dan kalkulasi HP.\n\n"
        "2. 🛡️ <b>Penyeimbangan Proporsi Gerbang & Menara Balista/Meriam</b>:\n"
        "   • Ukuran menara balista panah & meriam di kiri-kanan disesuaikan proporsional (skala 0.7x) agar gerbang benteng tampil seimbang, megah, dan kokoh.\n\n"
        "3. 🛠️ <b>Eliminasi Error InvalidOperationException</b>:\n"
        "   • Menuntaskan bug 'Collection was modified' pada loop damage monster dan proyektil splash dengan sistem snapshotting daftar aman.\n\n"
        "Silakan pasang versi terbaru ini untuk pengujian langsung di Android!"
    )
    print(f"[Monitor] Mengunggah {apk_path.name} ke Telegram...")
    with open(apk_path, "rb") as f:
        message = await bot.send_document(
            chat_id=CHAT_ID,
            document=f,
            filename=apk_path.name,
            caption=short_caption,
            parse_mode="HTML",
            read_timeout=360,
            write_timeout=360,
            connect_timeout=90,
        )
    print("[Monitor] APK berhasil dikirim ke Telegram! Message ID:", message.message_id)
    try:
        await bot.send_message(
            chat_id=CHAT_ID,
            text=details_msg,
            parse_mode="HTML",
        )
    except Exception as e:
        print("[Monitor] Gagal mengirim pesan detail:", e)

async def monitor_run(target_run_id=None, target_sha=None):
    if not target_run_id and target_sha:
        print(f"[Monitor] Menunggu GitHub Actions memproses commit {target_sha}...")
        for _ in range(25):
            target_run_id = get_run_for_commit(target_sha)
            if target_run_id:
                break
            await asyncio.sleep(4)

    if not target_run_id:
        print("[Monitor] Mencari Run ID terbaru...")
        for _ in range(15):
            target_run_id = get_latest_run_id()
            if target_run_id:
                break
            await asyncio.sleep(4)

    if not target_run_id:
        print("[Monitor] Tidak dapat menemukan Run ID untuk dimonitor.")
        return

    print(f"[Monitor] Memulai pemantauan aktif run {target_run_id} pada {REPO}...")
    start_time = time.time()
    while True:
        status, conclusion = check_run_status(target_run_id)
        elapsed_min = (time.time() - start_time) / 60.0
        print(f"[Monitor] [{elapsed_min:.1f}m] Run {target_run_id} -> status: {status}, conclusion: {conclusion}")

        # Update status file
        status_data = {
            "run_id": target_run_id,
            "status": status,
            "conclusion": conclusion,
            "elapsed_minutes": round(elapsed_min, 1),
            "updated_at": time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime())
        }
        try:
            with open(STATUS_FILE, "w", encoding="utf-8") as sf:
                json.dump(status_data, sf, indent=2)
        except Exception:
            pass

        if status == "completed":
            if conclusion == "success":
                print("[Monitor] Build berhasil 100%! Mengunduh artifact APK...")
                if download_artifact(target_run_id):
                    apk_files = list(OUTPUT_DIR.rglob("*.apk"))
                    if apk_files:
                        apk_files.sort(key=lambda p: p.stat().st_mtime, reverse=True)
                        target_apk = apk_files[0]
                        renamed_apk = target_apk.parent / "Rampartbound_v1.2.14_release.apk"
                        if target_apk != renamed_apk:
                            if renamed_apk.exists():
                                try:
                                    renamed_apk.unlink()
                                except Exception:
                                    pass
                            target_apk.rename(renamed_apk)
                            target_apk = renamed_apk
                        await send_apk_to_telegram(target_apk)
                    else:
                        print("[Monitor] Tidak ditemukan file APK pada artifact yang diunduh.")
                else:
                    print("[Monitor] Gagal mengunduh artifact APK.")
            else:
                print(f"[Monitor] Build gagal dengan conclusion: {conclusion}")
                error_summary = get_failed_log_summary(target_run_id)
                await notify_failure_to_telegram(target_run_id, error_summary)
            break

        await asyncio.sleep(25)

if __name__ == "__main__":
    run_id_arg = None
    sha_arg = None
    args = sys.argv[1:]
    if "--sha" in args:
        idx = args.index("--sha")
        if idx + 1 < len(args):
            sha_arg = args[idx + 1]
    elif len(args) > 0 and not args[0].startswith("--"):
        run_id_arg = args[0]

    asyncio.run(monitor_run(target_run_id=run_id_arg, target_sha=sha_arg))
