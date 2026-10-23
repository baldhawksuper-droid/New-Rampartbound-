using System;
using System.Collections.Generic;
using UnityEngine;
using FantasyDefense.Audio;
using FantasyDefense.Data;
using FantasyDefense.Save;

namespace FantasyDefense.Metagame
{
    /// <summary>
    /// Master Orchestrator untuk Sistem Aliansi Benteng (Guild Citadel) & Asynchronous Titan Raid.
    /// Mengelola anggota guild, donasi harian, ekspedisi patroli hero, serta pertarungan raid kolosal mingguan.
    /// </summary>
    public class GuildManager : MonoBehaviour
    {
        public static GuildManager Instance { get; private set; }

        [Header("Guild Profile Data")]
        [SerializeField] private GuildProfile currentGuild;
        [SerializeField] private bool isInGuild = true;

        public GuildProfile CurrentGuild => currentGuild;
        public bool IsInGuild => isInGuild;

        public static event Action OnGuildUpdated;
        public static event Action<GuildExpedition> OnExpeditionDispatched;
        public static event Action<GuildExpedition> OnExpeditionClaimed;
        public static event Action<long, long> OnTitanRaidDamageDealt; // (damageDealt, remainingTitanHp)
        public static event Action<int> OnTitanMilestoneClaimed;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            InitializeDefaultGuild();
        }

        private void InitializeDefaultGuild()
        {
            if (currentGuild == null)
            {
                currentGuild = new GuildProfile();
            }

            // Inisialisasi anggota jika kosong
            if (currentGuild.members == null || currentGuild.members.Count == 0)
            {
                currentGuild.members = new List<GuildMember>
                {
                    new GuildMember("leader_01", "Sir Cedric", GuildMemberRole.Leader, 35, 14200, 1500),
                    new GuildMember("officer_01", "Lady Rose", GuildMemberRole.Officer, 30, 12500, 1200),
                    new GuildMember("member_01", "Rabbit Mage", GuildMemberRole.Member, 28, 10800, 800),
                    new GuildMember("player_self", "Commander (You)", GuildMemberRole.Member, 25, 9600, 500)
                };
            }

            // Inisialisasi ekspedisi jika kosong
            if (currentGuild.expeditions == null || currentGuild.expeditions.Count == 0)
            {
                currentGuild.expeditions = new List<GuildExpedition>
                {
                    new GuildExpedition("exp_01", "Patroli Hutan Abyssal", "Amankan pinggiran benteng dari serbuan goblin pengintai.", 300f, 350, 25, 15),
                    new GuildExpedition("exp_02", "Eksplorasi Tambang Kristal", "Kumpulkan kristal mana dan serpihan tempa relik kuno.", 600f, 750, 50, 30),
                    new GuildExpedition("exp_03", "Reruntuhan Benteng Kuno", "Telusuri reruntuhan kerajaan lama untuk mencari cetak biru legendaris.", 1200f, 1600, 100, 60)
                };
            }

            if (currentGuild.titanRaid == null)
            {
                currentGuild.titanRaid = new GuildTitanRaid();
            }
        }

        /// <summary>
        /// Donasi Gold ke kas Guild untuk menambah EXP Guild dan mendapatkan Guild Tokens.
        /// </summary>
        public bool DonateToGuild(int goldAmount, out string resultMessage)
        {
            if (!isInGuild || currentGuild == null)
            {
                resultMessage = "Kamu belum bergabung ke dalam Guild!";
                return false;
            }

            if (goldAmount <= 0)
            {
                resultMessage = "Jumlah donasi harus lebih dari 0!";
                return false;
            }

            // Cek saldo gold pemain
            if (CitadelLobbyManager.Instance != null && !CitadelLobbyManager.Instance.SpendGold(goldAmount))
            {
                resultMessage = $"Gold tidak mencukupi untuk berdonasi ({goldAmount} Gold)!";
                return false;
            }

            int expGained = Mathf.RoundToInt(goldAmount * 0.2f); // 500 Gold -> 100 EXP
            int tokensGained = Mathf.RoundToInt(goldAmount * 0.1f); // 500 Gold -> 50 Tokens

            bool leveledUp = currentGuild.AddExp(expGained, out bool up);
            currentGuild.AddTokens(tokensGained);

            // Perbarui donasi player
            var self = currentGuild.members.Find(m => m.memberId == "player_self");
            if (self != null) self.weeklyDonation += goldAmount;

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound(SoundType.MergeSuccess);
            }

            resultMessage = $"Donasi Berhasil! +{expGained} Guild EXP, +{tokensGained} Guild Tokens." + (up ? " Guild Naik Level!" : "");
            Debug.Log($"[GuildManager] {resultMessage}");

            SaveManager.Instance?.SaveProfile(SaveManager.Instance.CurrentData, syncToCloud: true);
            OnGuildUpdated?.Invoke();
            return true;
        }

        /// <summary>
        /// Mengirim hero untuk melakukan ekspedisi patroli otomatis.
        /// </summary>
        public bool DispatchExpedition(string expeditionId, UnitType heroType, out string resultMessage)
        {
            if (currentGuild == null)
            {
                resultMessage = "Data guild tidak valid!";
                return false;
            }

            var exp = currentGuild.expeditions.Find(e => e.expeditionId == expeditionId);
            if (exp == null)
            {
                resultMessage = "Ekspedisi tidak ditemukan!";
                return false;
            }

            if (exp.isDispatched && !exp.IsFinished())
            {
                resultMessage = $"Ekspedisi sedang berjalan! Sisa waktu: {Mathf.CeilToInt(exp.GetRemainingSeconds())}s.";
                return false;
            }

            exp.Dispatch(heroType);
            resultMessage = $"Hero diberangkatkan ke misi '{exp.title}'! Durasi: {exp.durationSeconds / 60f:F0} menit.";
            Debug.Log($"[GuildManager] {resultMessage}");

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound(SoundType.ButtonClick);
            }

            SaveManager.Instance?.SaveProfile(SaveManager.Instance.CurrentData);
            OnExpeditionDispatched?.Invoke(exp);
            OnGuildUpdated?.Invoke();
            return true;
        }

        /// <summary>
        /// Mengklaim hadiah ekspedisi patroli yang telah rampung.
        /// </summary>
        public bool ClaimExpeditionReward(string expeditionId, out string resultMessage)
        {
            if (currentGuild == null)
            {
                resultMessage = "Data guild tidak valid!";
                return false;
            }

            var exp = currentGuild.expeditions.Find(e => e.expeditionId == expeditionId);
            if (exp == null)
            {
                resultMessage = "Ekspedisi tidak ditemukan!";
                return false;
            }

            if (!exp.isDispatched)
            {
                resultMessage = "Ekspedisi belum diberangkatkan!";
                return false;
            }

            if (!exp.IsFinished())
            {
                resultMessage = $"Ekspedisi masih berlangsung! Tunggu {Mathf.CeilToInt(exp.GetRemainingSeconds())} detik lagi.";
                return false;
            }

            if (exp.isClaimed)
            {
                resultMessage = "Hadiah ekspedisi sudah diklaim!";
                return false;
            }

            exp.isClaimed = true;
            exp.isDispatched = false;

            // Tambahkan hadiah ke profil pemain
            if (CitadelLobbyManager.Instance != null)
            {
                CitadelLobbyManager.Instance.AddGold(exp.rewardGold);
                CitadelLobbyManager.Instance.AddForgeMaterials(exp.rewardForgeDust, 0);
            }
            currentGuild.AddTokens(exp.rewardTokens);

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound(SoundType.MergeSuccess);
            }

            resultMessage = $"Klaim Hadiah Berhasil: +{exp.rewardGold} Gold, +{exp.rewardTokens} Tokens, +{exp.rewardForgeDust} Forge Dust!";
            Debug.Log($"[GuildManager] {resultMessage}");

            SaveManager.Instance?.SaveProfile(SaveManager.Instance.CurrentData, syncToCloud: true);
            OnExpeditionClaimed?.Invoke(exp);
            OnGuildUpdated?.Invoke();
            return true;
        }

        /// <summary>
        /// Menyerang Boss Titan Purba (Asynchronous Co-op Titan Raid).
        /// Kerusakan dihitung secara dinamis dari akumulasi Team Combat Power (CP) pemain.
        /// </summary>
        public bool AttackTitanRaid(out long damageDealt, out string resultMessage)
        {
            damageDealt = 0;
            if (currentGuild == null || currentGuild.titanRaid == null)
            {
                resultMessage = "Data Titan Raid tidak tersedia!";
                return false;
            }

            var titan = currentGuild.titanRaid;
            if (titan.titanCurrentHp <= 0)
            {
                resultMessage = "Titan Colossus telah tumbang minggu ini! Klaim peti harta karun kemenanganmu!";
                return false;
            }

            // Hitung kekuatan tempur berdasarkan Team CP
            int teamCp = 8000;
            if (CitadelLobbyManager.Instance != null && CitadelLobbyManager.Instance.AccountData != null)
            {
                teamCp = Mathf.Max(3000, CitadelLobbyManager.Instance.AccountData.CalculateTotalTeamCP());
            }

            // Damage formula: CP * (20 ~ 28) + variasi taktis
            float multiplier = UnityEngine.Random.Range(20f, 28f);
            damageDealt = (long)(teamCp * multiplier);

            titan.ApplyDamage(damageDealt, isPersonal: true);

            // Berikan imbalan partisipasi serangan Titan
            currentGuild.AddTokens(35);

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound(SoundType.MergeSuccess);
            }

            resultMessage = $"Serangan Berhasil! Kerusakan: {damageDealt:N0} DMG ke Titan! (+35 Guild Tokens)";
            Debug.Log($"[GuildManager] Titan Raid: {resultMessage}. Sisa HP: {titan.titanCurrentHp:N0} / {titan.titanMaxHp:N0}");

            SaveManager.Instance?.SaveProfile(SaveManager.Instance.CurrentData, syncToCloud: true);
            OnTitanRaidDamageDealt?.Invoke(damageDealt, titan.titanCurrentHp);
            OnGuildUpdated?.Invoke();
            return true;
        }

        /// <summary>
        /// Mengklaim peti hadiah pencapaian ambang batas kerusakan Titan Raid (20%, 40%, 60%, 80%, 100%).
        /// </summary>
        public bool ClaimTitanChest(int milestonePercent, out string resultMessage)
        {
            if (currentGuild == null || currentGuild.titanRaid == null)
            {
                resultMessage = "Data Titan Raid tidak valid!";
                return false;
            }

            var titan = currentGuild.titanRaid;
            if (!titan.IsMilestoneClaimable(milestonePercent))
            {
                resultMessage = $"Ambang kerusakan {milestonePercent}% belum tercapai atau sudah diklaim!";
                return false;
            }

            titan.ClaimMilestone(milestonePercent);

            // Hadiah skala bertahap
            int gold = milestonePercent * 100;
            int gems = milestonePercent * 4;
            int tokens = milestonePercent * 3;

            if (CitadelLobbyManager.Instance != null)
            {
                CitadelLobbyManager.Instance.AddGold(gold);
                CitadelLobbyManager.Instance.AddGems(gems);
            }
            currentGuild.AddTokens(tokens);

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound(SoundType.MergeSuccess);
            }

            resultMessage = $"Peti Hadiah {milestonePercent}% Diklaim! +{gold} Gold, +{gems} Gems, +{tokens} Tokens!";
            Debug.Log($"[GuildManager] {resultMessage}");

            SaveManager.Instance?.SaveProfile(SaveManager.Instance.CurrentData, syncToCloud: true);
            OnTitanMilestoneClaimed?.Invoke(milestonePercent);
            OnGuildUpdated?.Invoke();
            return true;
        }
    }
}
