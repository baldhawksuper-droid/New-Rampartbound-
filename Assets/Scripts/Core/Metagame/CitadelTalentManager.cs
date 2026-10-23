using System;
using System.Collections.Generic;
using UnityEngine;
using FantasyDefense.Data;
using FantasyDefense.Save;

namespace FantasyDefense.Metagame
{
    /// <summary>
    /// Mengelola Pohon Talenta & Berkah Benteng (Citadel Talents & Blessings Tree).
    /// Pemain menggunakan Citadel Crests untuk membuka buff permanen:
    /// 1. Citadel Aegis: Regenerasi HP benteng saat bertarung.
    /// 2. Economic Prosperity: Bonus awal gems & dividen gems tiap wave selesai.
    /// 3. Elemental Synergy: Peningkatan Attack Speed permanen untuk kuartet hero.
    /// 4. Lethal Precision: Peningkatan Critical Chance & Critical Damage.
    /// </summary>
    public class CitadelTalentManager : MonoBehaviour
    {
        public static CitadelTalentManager Instance { get; private set; }

        public const string TALENT_AEGIS_ID = "citadel_aegis_regen";
        public const string TALENT_ECONOMY_ID = "economic_prosperity";
        public const string TALENT_SYNERGY_ID = "elemental_synergy";
        public const string TALENT_PRECISION_ID = "lethal_precision";

        [Header("Configured Talents")]
        [SerializeField] private List<CitadelTalentDataSO> talentList = new List<CitadelTalentDataSO>();

        public List<CitadelTalentDataSO> Talents => talentList;

        public int AvailableCrests
        {
            get
            {
                if (CitadelLobbyManager.Instance != null)
                {
                    return CitadelLobbyManager.Instance.CitadelCrests;
                }
                return 0;
            }
        }

        public static event Action<string, int> OnTalentUpgraded; // (talentId, newRank)
        public static event Action<int> OnCitadelCrestsChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            EnsureFallbackTalents();
        }

        private void OnEnable()
        {
            CitadelLobbyManager.OnCitadelCrestsChanged += HandleCitadelCrestsChanged;
        }

        private void OnDisable()
        {
            CitadelLobbyManager.OnCitadelCrestsChanged -= HandleCitadelCrestsChanged;
        }

        private void HandleCitadelCrestsChanged(int crests)
        {
            OnCitadelCrestsChanged?.Invoke(crests);
        }

        private void EnsureFallbackTalents()
        {
            if (talentList == null || talentList.Count == 0)
            {
                talentList = new List<CitadelTalentDataSO>();

                // 1. Aegis Regen
                var aegis = ScriptableObject.CreateInstance<CitadelTalentDataSO>();
                aegis.talentId = TALENT_AEGIS_ID;
                aegis.talentName = "Citadel Aegis Regeneration";
                aegis.category = TalentCategory.CitadelDefense;
                aegis.description = "Benteng meregenerasi HP secara perlahan (+1.0% HP tiap 8 detik) saat bertarung.";
                aegis.maxRank = 5;
                aegis.baseCostCrests = 2;
                aegis.costIncrementPerRank = 1;
                aegis.valuePerRank = 0.01f;
                talentList.Add(aegis);

                // 2. Economy Prosperity
                var econ = ScriptableObject.CreateInstance<CitadelTalentDataSO>();
                econ.talentId = TALENT_ECONOMY_ID;
                econ.talentName = "Economic Prosperity";
                econ.category = TalentCategory.EconomyGems;
                econ.description = "Bonus modal summon hero (+15 gems awal) dan dividen gems (+10 gems tiap wave selesai).";
                econ.maxRank = 5;
                econ.baseCostCrests = 2;
                econ.costIncrementPerRank = 1;
                econ.valuePerRank = 15f;
                talentList.Add(econ);

                // 3. Elemental Synergy
                var synergy = ScriptableObject.CreateInstance<CitadelTalentDataSO>();
                synergy.talentId = TALENT_SYNERGY_ID;
                synergy.talentName = "Elemental Synergy";
                synergy.category = TalentCategory.HeroCombat;
                synergy.description = "Meningkatkan kecepatan serangan (Attack Speed +5% per rank) untuk seluruh pahlawan.";
                synergy.maxRank = 5;
                synergy.baseCostCrests = 3;
                synergy.costIncrementPerRank = 1;
                synergy.valuePerRank = 0.05f;
                talentList.Add(synergy);

                // 4. Lethal Precision
                var crit = ScriptableObject.CreateInstance<CitadelTalentDataSO>();
                crit.talentId = TALENT_PRECISION_ID;
                crit.talentName = "Lethal Precision";
                crit.category = TalentCategory.HeroCombat;
                crit.description = "Meningkatkan peluang serangan kritikal (+3% Crit Chance per rank) untuk seluruh pahlawan.";
                crit.maxRank = 5;
                crit.baseCostCrests = 3;
                crit.costIncrementPerRank = 1;
                crit.valuePerRank = 0.03f;
                talentList.Add(crit);
            }
        }

        public int GetTalentRank(string talentId)
        {
            if (CitadelLobbyManager.Instance != null && CitadelLobbyManager.Instance.AccountData != null)
            {
                return CitadelLobbyManager.Instance.AccountData.GetTalentRank(talentId);
            }
            return 0;
        }

        public bool CanUpgradeTalent(CitadelTalentDataSO talent)
        {
            if (talent == null) return false;
            int currentRank = GetTalentRank(talent.talentId);
            if (currentRank >= talent.maxRank) return false;

            int cost = talent.GetUpgradeCost(currentRank);
            return AvailableCrests >= cost;
        }

        public bool TryUpgradeTalent(CitadelTalentDataSO talent, out string message)
        {
            if (talent == null)
            {
                message = "Talenta tidak valid!";
                return false;
            }

            int currentRank = GetTalentRank(talent.talentId);
            if (currentRank >= talent.maxRank)
            {
                message = $"{talent.talentName} telah mencapai Rank Maksimal ({talent.maxRank}/{talent.maxRank})!";
                return false;
            }

            int cost = talent.GetUpgradeCost(currentRank);
            if (AvailableCrests < cost)
            {
                message = $"Citadel Crests tidak mencukupi! Butuh {cost} Crests (Kamu punya: {AvailableCrests}).";
                return false;
            }

            if (CitadelLobbyManager.Instance != null)
            {
                if (CitadelLobbyManager.Instance.SpendCitadelCrests(cost))
                {
                    int newRank = currentRank + 1;
                    CitadelLobbyManager.Instance.AccountData.SetTalentRank(talent.talentId, newRank);
                    SaveManager.Instance?.SaveProfile(CitadelLobbyManager.Instance.AccountData);

                    message = $"Sukses! {talent.talentName} ditingkatkan ke Rank {newRank}!";
                    Debug.Log($"[CitadelTalentManager] {message}");

                    OnTalentUpgraded?.Invoke(talent.talentId, newRank);
                    return true;
                }
            }

            message = "Gagal memproses peningkatan talenta!";
            return false;
        }

        public void AddCitadelCrests(int amount)
        {
            if (CitadelLobbyManager.Instance != null)
            {
                CitadelLobbyManager.Instance.AddCitadelCrests(amount);
            }
        }

        #region Value Calculations for Combat & Economy

        /// <summary>
        /// Mengembalikan persentase pemulihan HP benteng per siklus (8 detik) dari talenta Aegis.
        /// </summary>
        public float GetBarricadeRegenPercent()
        {
            int rank = GetTalentRank(TALENT_AEGIS_ID);
            return rank * 0.01f; // 1% per rank (Max 5% per 8s)
        }

        /// <summary>
        /// Mengembalikan bonus modal awal gems di arena dari talenta Economic Prosperity.
        /// </summary>
        public int GetStartingGemsBonus()
        {
            int rank = GetTalentRank(TALENT_ECONOMY_ID);
            return rank * 15; // +15 gems per rank
        }

        /// <summary>
        /// Mengembalikan dividen gems ekstra yang diterima setiap menyelesaikan 1 gelombang musuh.
        /// </summary>
        public int GetWaveDividendBonus()
        {
            int rank = GetTalentRank(TALENT_ECONOMY_ID);
            return rank * 10; // +10 gems per rank
        }

        /// <summary>
        /// Mengembalikan buff attack speed permanen untuk seluruh pahlawan dari talenta Elemental Synergy.
        /// </summary>
        public float GetHeroAttackSpeedBonus()
        {
            int rank = GetTalentRank(TALENT_SYNERGY_ID);
            return rank * 0.05f; // +5% Attack Speed per rank
        }

        /// <summary>
        /// Mengembalikan bonus peluang kritikal permanen dari talenta Lethal Precision.
        /// </summary>
        public float GetHeroCritChanceBonus()
        {
            int rank = GetTalentRank(TALENT_PRECISION_ID);
            return rank * 0.03f; // +3% Crit Chance per rank
        }

        #endregion
    }
}
