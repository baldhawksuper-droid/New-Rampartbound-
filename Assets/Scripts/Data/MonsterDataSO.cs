using UnityEngine;

namespace FantasyDefense.Data
{
    public enum MonsterType
    {
        Creep,  // Slime / Goblin biasa - Cepat, HP Rendah
        Brute,  // Orc Pelindung - Lambat, HP Tebal, Tahan Pukul
        Boss    // Golem / Monster Raksasa - Sangat Lambat, HP Masif, Gempa
    }

    [CreateAssetMenu(fileName = "NewMonsterData", menuName = "FantasyDefense/Monster Data", order = 2)]
    public class MonsterDataSO : ScriptableObject
    {
        [Header("Identity")]
        public MonsterType monsterType;
        public string monsterName;
        [TextArea(2, 3)]
        public string description;

        [Header("Movement & Health")]
        [Tooltip("Kecepatan jalan menyusuri lintasan waypoint (unit/detik)")]
        public float moveSpeed = 1.5f;

        [Tooltip("Total HP maksimal")]
        public float maxHealth = 150f;

        [Tooltip("Damage ke Benteng/Barricade jika monster berhasil menembus pertahanan")]
        public int castleDamage = 10;

        [Tooltip("Alias damage serangan monster")]
        public int damage => castleDamage;

        [Header("Economy Rewards")]
        [Tooltip("Reward gems yang didapat pemain saat monster berhasil dieliminasi")]
        public int gemsReward = 5;

        [Header("Visual & Scaling")]
        public Sprite monsterSprite;
        [Tooltip("Sprite pose melangkah kedua untuk animasi berjalan 2-frame (Sprint 44 Poin 4)")]
        public Sprite monsterWalkSprite;
        public Color themeColor = Color.white;
        public float visualScale = 1.0f;
    }
}
