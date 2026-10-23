using UnityEngine;

namespace FantasyDefense.Data
{
    public enum TalentCategory
    {
        CitadelDefense,    // Barricade HP & Regeneration
        EconomyGems,       // Starting Gems & Wave Dividends
        HeroCombat         // Hero Attack Speed & Critical Strike
    }

    [CreateAssetMenu(fileName = "NewCitadelTalent", menuName = "FantasyDefense/Citadel Talent Data", order = 6)]
    public class CitadelTalentDataSO : ScriptableObject
    {
        [Header("Talent Identity")]
        public string talentId = "citadel_aegis_regen";
        public string talentName = "Citadel Aegis Regeneration";
        public TalentCategory category = TalentCategory.CitadelDefense;
        
        [TextArea(2, 4)]
        public string description = "Benteng meregenerasi HP secara perlahan (+1% max HP per rank tiap 10 detik) di tengah pertempuran.";
        
        public Sprite talentIcon;

        [Header("Progression & Balancing")]
        [Range(1, 10)] public int maxRank = 5;
        public int baseCostCrests = 2; // Biaya dasar Citadel Crests
        public int costIncrementPerRank = 1;
        public float valuePerRank = 0.01f; // Misal 1% per rank (0.01f)

        public int GetUpgradeCost(int currentRank)
        {
            if (currentRank >= maxRank) return 0;
            return baseCostCrests + (currentRank * costIncrementPerRank);
        }

        public string GetRankDescription(int currentRank)
        {
            float currentVal = currentRank * valuePerRank;
            float nextVal = Mathf.Min(maxRank, currentRank + 1) * valuePerRank;

            switch (category)
            {
                case TalentCategory.CitadelDefense:
                    return $"Rank {currentRank}/{maxRank}: Regenerasi Benteng {currentVal * 100:F1}% Max HP / 8s (Next: {nextVal * 100:F1}%)";
                case TalentCategory.EconomyGems:
                    return $"Rank {currentRank}/{maxRank}: +{Mathf.RoundToInt(currentVal)} Starting Gems & +{Mathf.RoundToInt(currentVal * 0.67f)} Gems / Wave Clear (Next: +{Mathf.RoundToInt(nextVal)})";
                case TalentCategory.HeroCombat:
                    return $"Rank {currentRank}/{maxRank}: Bonus Tempur Kuartet Hero +{currentVal * 100:F1}% (Next: +{nextVal * 100:F1}%)";
                default:
                    return $"Rank {currentRank}/{maxRank}: Efektivitas {currentVal * 100:F0}%";
            }
        }
    }
}
