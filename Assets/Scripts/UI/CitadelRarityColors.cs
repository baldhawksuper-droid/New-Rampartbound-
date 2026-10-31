using UnityEngine;

namespace FantasyDefense.UI
{
    /// <summary>
    /// Satu sumber kebenaran untuk kode warna kelangkaan resmi di Citadel.
    /// Gunakan kelas ini di semua tampilan UI untuk menjamin konsistensi warna.
    /// </summary>
    public static class CitadelRarityColors
    {
        // Common  = #9E9E9E (Abu-abu Batu Citadel)
        public static readonly Color Common = new Color(0.62f, 0.62f, 0.62f, 1f);

        // Rare    = #4FC3F7 (Biru Kristal Langit)
        public static readonly Color Rare = new Color(0.31f, 0.76f, 0.97f, 1f);

        // Epic    = #BA68C8 (Ungu Ametis Kerajaan)
        public static readonly Color Epic = new Color(0.73f, 0.41f, 0.78f, 1f);

        // Legendary = #FFD54F (Emas Mahkota Sang Raja)
        public static readonly Color Legendary = new Color(1f, 0.84f, 0.31f, 1f);

        public static Color GetColor(string rarityLabel)
        {
            if (rarityLabel.Contains("Legendary")) return Legendary;
            if (rarityLabel.Contains("Epic")) return Epic;
            if (rarityLabel.Contains("Rare")) return Rare;
            return Common;
        }
    }
}
