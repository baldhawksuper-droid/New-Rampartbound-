using System;
using System.Collections.Generic;
using UnityEngine;
using FantasyDefense.Audio;
using FantasyDefense.Data;
using FantasyDefense.Save;

namespace FantasyDefense.Metagame
{
    /// <summary>
    /// Master Controller untuk Citadel Relic Forge & Gear Crafting.
    /// Mengelola peleburan gear duplikat (Salvage) menjadi Forge Dust & Refine Stones,
    /// penempaan tingkat bonus gear (+1 s.d +5), serta promosi kelangkaan (Common -> Rare -> Epic -> Legendary).
    /// </summary>
    public class RelicForgeManager : MonoBehaviour
    {
        public static RelicForgeManager Instance { get; private set; }

        public static event Action<EquipmentItem, int, int> OnEquipmentSalvaged; // (item, dustEarned, stonesEarned)
        public static event Action<EquipmentItem> OnEquipmentRefined;            // (item)
        public static event Action<EquipmentItem> OnEquipmentRarityUpgraded;     // (item)
        public static event Action<string> OnForgeActionFailed;                 // (errorMessage)

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        #region Salvage Mechanics

        /// <summary>
        /// Melebur 1 item perlengkapan yang tidak terpakai menjadi material tempa.
        /// </summary>
        public bool TrySalvageEquipment(EquipmentItem item, out int dustEarned, out int stonesEarned)
        {
            dustEarned = 0;
            stonesEarned = 0;

            if (item == null || CitadelLobbyManager.Instance == null)
            {
                OnForgeActionFailed?.Invoke("Item tidak valid atau sistem Lobby belum siap.");
                return false;
            }

            var inventory = CitadelLobbyManager.Instance.AccountData.equipmentInventory;
            if (!inventory.Contains(item))
            {
                OnForgeActionFailed?.Invoke("Item tidak ditemukan di tas inventaris! (Mungkin sedang terpasang pada pahlawan).");
                return false;
            }

            item.GetSalvageYield(out dustEarned, out stonesEarned);

            // Hapus dari inventaris & tambahkan material tempa
            inventory.Remove(item);
            CitadelLobbyManager.Instance.AddForgeMaterials(dustEarned, stonesEarned);

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound(SoundType.MergeSuccess);
            }

            OnEquipmentSalvaged?.Invoke(item, dustEarned, stonesEarned);
            Debug.Log($"[RelicForgeManager] Sukses melebur {item.itemName}! Memperoleh +{dustEarned} Forge Dust & +{stonesEarned} Refine Stones.");

            return true;
        }

        /// <summary>
        /// Melebur sekumpulan item sekaligus (Batch Salvage).
        /// </summary>
        public void BatchSalvage(List<EquipmentItem> itemsToSalvage)
        {
            if (itemsToSalvage == null || itemsToSalvage.Count == 0) return;

            int totalDust = 0;
            int totalStones = 0;
            var inventory = CitadelLobbyManager.Instance.AccountData.equipmentInventory;

            foreach (var item in itemsToSalvage)
            {
                if (item != null && inventory.Contains(item))
                {
                    item.GetSalvageYield(out int d, out int s);
                    totalDust += d;
                    totalStones += s;
                    inventory.Remove(item);
                    OnEquipmentSalvaged?.Invoke(item, d, s);
                }
            }

            if (totalDust > 0 || totalStones > 0)
            {
                CitadelLobbyManager.Instance.AddForgeMaterials(totalDust, totalStones);
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySound(SoundType.MergeSuccess);
                }
            }
        }

        #endregion

        #region Refine Mechanics (+0 -> +5)

        /// <summary>
        /// Menempa item perlengkapan (+0 -> +5) untuk meningkatkan seluruh statistik sebesar +10% per tingkat.
        /// </summary>
        public bool TryRefineEquipment(EquipmentItem item, UnitType? heroOwner = null)
        {
            if (item == null || CitadelLobbyManager.Instance == null)
            {
                OnForgeActionFailed?.Invoke("Item tidak valid!");
                return false;
            }

            if (item.refineLevel >= 5)
            {
                OnForgeActionFailed?.Invoke($"{item.itemName} telah mencapai level tempa maksimal (+5)!");
                return false;
            }

            if (!item.GetRefineCost(out int reqGold, out int reqDust, out int reqStones))
            {
                OnForgeActionFailed?.Invoke("Gagal menghitung biaya tempa.");
                return false;
            }

            var lobby = CitadelLobbyManager.Instance;
            if (lobby.GoldCoins < reqGold)
            {
                OnForgeActionFailed?.Invoke($"Gold tidak mencukupi! Butuh {reqGold} Gold (Kamu punya: {lobby.GoldCoins}).");
                return false;
            }

            if (lobby.ForgeDust < reqDust || lobby.RefineStones < reqStones)
            {
                OnForgeActionFailed?.Invoke($"Bahan tempa kurang! Butuh {reqDust} Dust & {reqStones} Refine Stones.");
                return false;
            }

            // Potong sumber daya
            lobby.AccountData.goldCoins -= reqGold;
            lobby.SpendForgeMaterials(reqDust, reqStones);

            // Tingkatkan level tempa
            item.refineLevel++;

            // Jika item terpasang pada pahlawan, perbarui status CP pahlawan
            if (heroOwner.HasValue)
            {
                var profile = lobby.GetHeroProfile(heroOwner.Value);
                if (profile != null)
                {
                    profile.CalculateCombatPower();
                    lobby.BroadcastAllStates();
                }
            }

            SaveManager.Instance?.SaveProfile(lobby.AccountData);

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound(SoundType.MergeSuccess);
            }

            OnEquipmentRefined?.Invoke(item);
            Debug.Log($"[RelicForgeManager] Selamat! {item.itemName} berhasil ditempa ke +{item.refineLevel}!");

            return true;
        }

        #endregion

        #region Rarity Promotion Mechanics

        /// <summary>
        /// Meningkatkan kualitas kelangkaan perlengkapan (Common -> Rare -> Epic -> Legendary).
        /// </summary>
        public bool TryUpgradeEquipmentRarity(EquipmentItem item, UnitType? heroOwner = null)
        {
            if (item == null || CitadelLobbyManager.Instance == null)
            {
                OnForgeActionFailed?.Invoke("Item tidak valid!");
                return false;
            }

            if (item.rarity == EquipmentRarity.Legendary)
            {
                OnForgeActionFailed?.Invoke($"{item.itemName} sudah memiliki tingkat kelangkaan tertinggi (Legendary)!");
                return false;
            }

            if (!item.GetRarityUpgradeCost(out int reqGold, out int reqDust, out int reqStones))
            {
                OnForgeActionFailed?.Invoke("Item tidak dapat ditingkatkan kelangkaannya.");
                return false;
            }

            var lobby = CitadelLobbyManager.Instance;
            if (lobby.GoldCoins < reqGold)
            {
                OnForgeActionFailed?.Invoke($"Gold tidak mencukupi! Butuh {reqGold} Gold.");
                return false;
            }

            if (lobby.ForgeDust < reqDust || lobby.RefineStones < reqStones)
            {
                OnForgeActionFailed?.Invoke($"Bahan tempa kurang! Butuh {reqDust} Dust & {reqStones} Refine Stones.");
                return false;
            }

            // Potong sumber daya
            lobby.AccountData.goldCoins -= reqGold;
            lobby.SpendForgeMaterials(reqDust, reqStones);

            // Naikkan kelangkaan & tingkatkan base stat +30%
            EquipmentRarity nextRarity = (EquipmentRarity)((int)item.rarity + 1);
            item.rarity = nextRarity;
            item.bonusAttack *= 1.30f;
            item.bonusHealth *= 1.30f;
            item.bonusDefense *= 1.25f;
            item.bonusCritChance += 0.02f;

            // Jika terpasang pada pahlawan, hitung ulang CP
            if (heroOwner.HasValue)
            {
                var profile = lobby.GetHeroProfile(heroOwner.Value);
                if (profile != null)
                {
                    profile.CalculateCombatPower();
                    lobby.BroadcastAllStates();
                }
            }

            SaveManager.Instance?.SaveProfile(lobby.AccountData);

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound(SoundType.MergeSuccess);
            }

            OnEquipmentRarityUpgraded?.Invoke(item);
            Debug.Log($"[RelicForgeManager] Luar biasa! {item.itemName} bertransformasi menjadi {item.rarity}!");

            return true;
        }

        #endregion
    }
}
