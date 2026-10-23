using System;
using UnityEngine;

namespace FantasyDefense.Combat
{
    public enum BlessingRarity
    {
        Common,
        Rare,
        Epic
    }

    public enum BlessingEffectType
    {
        HeroAttackDamage,    // +25% hero damage
        HeroAttackSpeed,     // +20% attack speed
        TacticalMoves,       // +3 extra moves
        BarricadeRepair,     // +35% barricade max HP & instant repair
        RangedAttackRange,   // +0.8 tile attack range for ranged heroes
        WarBountyGems,       // +40 starting gems & +30% gems on kill
        CriticalMight,       // +30% critical strike chance
        BarricadeThorn       // Barricade reflects 25% damage to attacking monsters
    }

    [Serializable]
    public class BattleBlessingData
    {
        public string id;
        public string titleId;
        public string titleEn;
        public string descId;
        public string descEn;
        public BlessingRarity rarity;
        public string iconEmoji;
        public BlessingEffectType effectType;
        public float effectValue;
        public Color accentColor;

        public BattleBlessingData(
            string id,
            string titleId,
            string titleEn,
            string descId,
            string descEn,
            BlessingRarity rarity,
            string iconEmoji,
            BlessingEffectType effectType,
            float effectValue,
            Color accentColor)
        {
            this.id = id;
            this.titleId = titleId;
            this.titleEn = titleEn;
            this.descId = descId;
            this.descEn = descEn;
            this.rarity = rarity;
            this.iconEmoji = iconEmoji;
            this.effectType = effectType;
            this.effectValue = effectValue;
            this.accentColor = accentColor;
        }

        public string GetLocalizedTitle()
        {
            return Localization.CitadelLocalizationManager.ActiveLanguage == Localization.AppLanguage.Indonesian
                ? titleId : titleEn;
        }

        public string GetLocalizedDesc()
        {
            return Localization.CitadelLocalizationManager.ActiveLanguage == Localization.AppLanguage.Indonesian
                ? descId : descEn;
        }

        public string GetRarityLabel()
        {
            bool isId = Localization.CitadelLocalizationManager.ActiveLanguage == Localization.AppLanguage.Indonesian;
            switch (rarity)
            {
                case BlessingRarity.Epic:
                    return isId ? "<color=#BA55D3>[EPIK]</color>" : "<color=#BA55D3>[EPIC]</color>";
                case BlessingRarity.Rare:
                    return isId ? "<color=#00FFFF>[LANGKA]</color>" : "<color=#00FFFF>[RARE]</color>";
                default:
                    return isId ? "<color=#FFD700>[BIASA]</color>" : "<color=#FFD700>[COMMON]</color>";
            }
        }
    }
}
