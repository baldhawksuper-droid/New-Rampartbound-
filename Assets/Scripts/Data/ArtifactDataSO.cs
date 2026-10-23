using UnityEngine;

namespace FantasyDefense.Data
{
    public enum ArtifactSetType
    {
        GuardiansVow,   // Set Pelindung Benteng (Damage Reduction & Aegis Barrier)
        BerserkersFury, // Set Amarah Cedric (Crit Chance & Low-HP Crit Burst)
        SorcerersSurge  // Set Kelinci Arcane (Attack Speed & Skill Cooldown Reduction)
    }

    public enum ArtifactPieceSlot
    {
        Helm,  // Mahkota / Helm Pelindung
        Sigil, // Segel / Amulet Benteng
        Ring,  // Cincin Purba
        Tome   // Kitab / Artefak Keramat
    }

    [CreateAssetMenu(fileName = "NewArtifactData", menuName = "FantasyDefense/Artifact Data", order = 7)]
    public class ArtifactDataSO : ScriptableObject
    {
        [Header("Artifact Identity")]
        public string artifactId = "guardians_helm";
        public string artifactName = "Guardian's Helm";
        public ArtifactSetType setType = ArtifactSetType.GuardiansVow;
        public ArtifactPieceSlot pieceSlot = ArtifactPieceSlot.Helm;
        public Sprite artifactIcon;

        [TextArea(2, 4)]
        public string description = "Helm baja pelindung suci penjaga benteng Citadel.";

        [Header("Individual Stat Contributions")]
        public float bonusHealth = 40f;
        public float bonusAttack = 8f;
        public float bonusCritChance = 0.02f;
        public int combatPower = 150;

        /// <summary>
        /// Warna tema set artefak untuk visual UI.
        /// </summary>
        public Color GetSetThemeColor()
        {
            switch (setType)
            {
                case ArtifactSetType.GuardiansVow:
                    return new Color(0.2f, 0.75f, 1f); // Pelindung Suci Cyan / Biru
                case ArtifactSetType.BerserkersFury:
                    return new Color(1f, 0.35f, 0.2f); // Amarah Merah / Oranye
                case ArtifactSetType.SorcerersSurge:
                    return new Color(0.75f, 0.35f, 1f); // Arcane Ungu
                default:
                    return Color.white;
            }
        }
    }
}
