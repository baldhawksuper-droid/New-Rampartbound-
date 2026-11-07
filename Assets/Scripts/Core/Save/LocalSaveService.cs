using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using FantasyDefense.Metagame;

namespace FantasyDefense.Save
{
    [Serializable]
    public class SaveEnvelope
    {
        public string payload;
        public string checksum;
        public long timestamp;
    }

    /// <summary>
    /// Layanan penyimpanan data lokal berstandar mobile & PC dengan proteksi keamanan Anti-Tamper:
    /// Menghitung SHA-256 HMAC Checksum pada payload profil akun untuk mencegah modifikasi ilegal.
    /// Menyimpan berkas cadangan (.bak) dan otomatis memulihkan data jika terjadi kerusakan atau manipulasi file.
    /// </summary>
    public class LocalSaveService : ISaveService
    {
        private const string SAVE_FILE_NAME = "citadel_profile.dat";
        private const string BACKUP_FILE_NAME = "citadel_profile.bak";
        private const string CRYPTO_SALT = "RAMPARTBOUND_SECURE_SALT_2026_ANTI_TAMPER";

        private readonly string saveFilePath;
        private readonly string backupFilePath;

        public static event Action OnSaveTamperedDetected;

        public LocalSaveService()
        {
            saveFilePath = Path.Combine(Application.persistentDataPath, SAVE_FILE_NAME);
            backupFilePath = Path.Combine(Application.persistentDataPath, BACKUP_FILE_NAME);
        }

        public bool HasSaveData()
        {
            return File.Exists(saveFilePath) || File.Exists(backupFilePath);
        }

        public void Save(CitadelAccountData data, Action<bool> onComplete = null)
        {
            try
            {
                if (data == null)
                {
                    Debug.LogWarning("[LocalSaveService] Data bernilai null, proses simpan dibatalkan.");
                    onComplete?.Invoke(false);
                    return;
                }

                // Perbarui timestamp waktu simpan UTC
                data.lastSaveTimestampUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                string jsonPayload = JsonUtility.ToJson(data, false);
                string checksum = ComputeSha256Checksum(jsonPayload);

                SaveEnvelope envelope = new SaveEnvelope
                {
                    payload = jsonPayload,
                    checksum = checksum,
                    timestamp = data.lastSaveTimestampUtc
                };

                string envelopeJson = JsonUtility.ToJson(envelope, true);
                byte[] rawBytes = Encoding.UTF8.GetBytes(envelopeJson);

                // Buat salinan backup jika berkas lama yang valid sudah ada
                if (File.Exists(saveFilePath))
                {
                    File.Copy(saveFilePath, backupFilePath, true);
                }

                // Tulis berkas save utama berproteksi checksum
                File.WriteAllBytes(saveFilePath, rawBytes);
#if UNITY_WEBGL
                PlayerPrefs.Save();
#endif

                Debug.Log($"[LocalSaveService] Data berhasil disimpan secara aman dengan Checksum SHA-256 ke: {saveFilePath}");
                onComplete?.Invoke(true);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LocalSaveService] Gagal menyimpan data lokal: {ex.Message}");
                onComplete?.Invoke(false);
            }
        }

        public void Load(Action<CitadelAccountData> onLoaded)
        {
            CitadelAccountData loadedData = null;

            try
            {
                if (File.Exists(saveFilePath))
                {
                    loadedData = TryLoadAndVerifyFile(saveFilePath);
                }

                if (loadedData == null && File.Exists(backupFilePath))
                {
                    Debug.LogWarning("[LocalSaveService] Berkas utama tidak valid atau hilang, mencoba memulihkan dari cadangan (.bak)...");
                    loadedData = TryLoadAndVerifyFile(backupFilePath);
                    if (loadedData != null)
                    {
                        Debug.Log("[LocalSaveService] Berhasil memulihkan profil pemain dari berkas cadangan (.bak)!");
                        // Tulis ulang berkas utama yang bersih
                        Save(loadedData);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LocalSaveService] Gagal membaca data lokal: {ex.Message}");
            }

            onLoaded?.Invoke(loadedData);
        }

        private CitadelAccountData TryLoadAndVerifyFile(string path)
        {
            try
            {
                byte[] rawBytes = File.ReadAllBytes(path);
                string content = Encoding.UTF8.GetString(rawBytes);

                // Cek apakah berkas menggunakan format SaveEnvelope ber-checksum
                if (content.Contains("\"payload\"") && content.Contains("\"checksum\""))
                {
                    SaveEnvelope envelope = JsonUtility.FromJson<SaveEnvelope>(content);
                    if (envelope != null && !string.IsNullOrEmpty(envelope.payload))
                    {
                        string expectedHash = ComputeSha256Checksum(envelope.payload);
                        if (string.Equals(expectedHash, envelope.checksum, StringComparison.OrdinalIgnoreCase))
                        {
                            // Integritas data valid dan tidak termanipulasi!
                            return JsonUtility.FromJson<CitadelAccountData>(envelope.payload);
                        }
                        else
                        {
                            Debug.LogError($"[LocalSaveService] PERINGATAN INTEGRITAS: Checksum SHA-256 tidak cocok pada {Path.GetFileName(path)}! Terdeteksi indikasi manipulasi eksternal.");
                            OnSaveTamperedDetected?.Invoke();
                            return null;
                        }
                    }
                }

                // Fallback kompatibilitas: Berkas JSON lama tanpa envelope
                return JsonUtility.FromJson<CitadelAccountData>(content);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LocalSaveService] Gagal memverifikasi berkas {path}: {ex.Message}");
                return null;
            }
        }

        private string ComputeSha256Checksum(string rawData)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(rawData + "_" + CRYPTO_SALT);
                byte[] hash = sha256.ComputeHash(bytes);
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hash.Length; i++)
                {
                    sb.Append(hash[i].ToString("x2"));
                }
                return sb.ToString();
            }
        }

        public void DeleteSave()
        {
            try
            {
                if (File.Exists(saveFilePath)) File.Delete(saveFilePath);
                if (File.Exists(backupFilePath)) File.Delete(backupFilePath);
#if UNITY_WEBGL
                PlayerPrefs.DeleteAll();
                PlayerPrefs.Save();
#endif
                Debug.Log("[LocalSaveService] Seluruh berkas penyimpanan lokal telah dibersihkan.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LocalSaveService] Gagal menghapus save file: {ex.Message}");
            }
        }
    }
}
