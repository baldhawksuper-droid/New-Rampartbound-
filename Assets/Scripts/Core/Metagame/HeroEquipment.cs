using System;
using UnityEngine;

namespace FantasyDefense.Metagame
{
    public enum EquipmentSlot
    {
        Weapon,      // Menambah Physical/Magical Attack
        Armor,       // Menambah Max Health & Defense
        Relic        // Menambah Critical Strike & Efek Spesial
    }

    public enum EquipmentRarity
    {
        Common,      // Putih / Dasar
        Rare,        // Biru / Superior
        Epic,        // Ungu / Elit Kerajaan
        Legendary    // Emas / Warisan Purba
    }

    [Serializable]
    public class EquipmentItem
    {
        public string itemId;
        public string itemName;
        public string gearName => itemName; // Alias kompatibilitas
        public EquipmentSlot slot;
        public EquipmentRarity rarity;
        public int itemLevel = 1;
        public int refineLevel = 0; // Level Tempa Forge (+0 hingga +5)

        [Header("Stat Boosts (Base)")]
        public float bonusAttack = 5f;
        public float bonusHealth = 20f;
        public float bonusCritChance = 0.02f; // +2% Crit
        public float bonusDefense = 0.05f;    // +5% Defense

        public EquipmentItem() { }

        public EquipmentItem(string id, string name, EquipmentSlot equipSlot, EquipmentRarity equipRarity, int lvl, float atk, float hp, float crit = 0f, float def = 0f, int refine = 0)
        {
            itemId = id;
            itemName = name;
            slot = equipSlot;
            rarity = equipRarity;
            itemLevel = lvl;
            refineLevel = Mathf.Clamp(refine, 0, 5);
            bonusAttack = atk;
            bonusHealth = hp;
            bonusCritChance = crit;
            bonusDefense = def;
        }

        /// <summary>
        /// Pengali stat efektif berbasis Level Tempa (+10% per tingkat refine di atas +0).
        /// </summary>
        public float GetRefineMultiplier()
        {
            return 1.0f + (refineLevel * 0.10f);
        }

        public float EffectiveAttack => bonusAttack * GetRefineMultiplier();
        public float EffectiveHealth => bonusHealth * GetRefineMultiplier();
        public float EffectiveCritChance => bonusCritChance * (1f + (refineLevel * 0.05f));
        public float EffectiveDefense => bonusDefense * GetRefineMultiplier();

        /// <summary>
        /// Menghitung kontribusi Combat Power (CP) dari item equipment ini.
        /// </summary>
        public int CalculateItemCP()
        {
            float cp = (EffectiveAttack * 22f) + (EffectiveHealth * 6f) + (EffectiveCritChance * 1500f) + (EffectiveDefense * 800f) + (refineLevel * 45f);
            return Mathf.RoundToInt(cp);
        }

        /// <summary>
        /// Hasil peleburan (Salvage) item menjadi Forge Dust dan Refine Stones.
        /// </summary>
        public void GetSalvageYield(out int forgeDust, out int refineStones)
        {
            switch (rarity)
            {
                case EquipmentRarity.Legendary:
                    forgeDust = 350 + (refineLevel * 50);
                    refineStones = 8 + refineLevel;
                    break;
                case EquipmentRarity.Epic:
                    forgeDust = 120 + (refineLevel * 25);
                    refineStones = 3 + refineLevel;
                    break;
                case EquipmentRarity.Rare:
                    forgeDust = 45 + (refineLevel * 10);
                    refineStones = 1 + (refineLevel >= 3 ? 1 : 0);
                    break;
                default: // Common
                    forgeDust = 15;
                    refineStones = 0;
                    break;
            }
        }

        /// <summary>
        /// Menghitung biaya penempaan (Refine) ke level berikutnya (+0 -> +5).
        /// </summary>
        public bool GetRefineCost(out int reqGold, out int reqDust, out int reqStones)
        {
            if (refineLevel >= 5)
            {
                reqGold = reqDust = reqStones = 0;
                return false;
            }

            int baseTierMul = (int)rarity + 1;
            reqGold = 150 * (refineLevel + 1) * baseTierMul;
            reqDust = 25 * (refineLevel + 1) * baseTierMul;
            reqStones = refineLevel >= 2 ? (refineLevel) : 0;
            return true;
        }

        /// <summary>
        /// Menghitung biaya upgrade tingkat kelangkaan (Rarity Promotion: Common -> Rare -> Epic -> Legendary).
        /// </summary>
        public bool GetRarityUpgradeCost(out int reqGold, out int reqDust, out int reqStones)
        {
            if (rarity == EquipmentRarity.Legendary)
            {
                reqGold = reqDust = reqStones = 0;
                return false;
            }

            switch (rarity)
            {
                case EquipmentRarity.Common:
                    reqGold = 400;
                    reqDust = 80;
                    reqStones = 2;
                    break;
                case EquipmentRarity.Rare:
                    reqGold = 1000;
                    reqDust = 200;
                    reqStones = 6;
                    break;
                case EquipmentRarity.Epic:
                    reqGold = 2500;
                    reqDust = 500;
                    reqStones = 15;
                    break;
                default:
                    reqGold = reqDust = reqStones = 0;
                    return false;
            }
            return true;
        }

        public Color GetRarityColor()
        {
            switch (rarity)
            {
                case EquipmentRarity.Rare: return new Color(0.2f, 0.6f, 1f);     // Cyan-Biru
                case EquipmentRarity.Epic: return new Color(0.7f, 0.3f, 0.9f);    // Ungu
                case EquipmentRarity.Legendary: return new Color(1f, 0.8f, 0.1f); // Emas
                default: return Color.white;
            }
        }
    }
}
