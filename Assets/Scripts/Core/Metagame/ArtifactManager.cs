using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FantasyDefense.Data;
using FantasyDefense.Combat;

namespace FantasyDefense.Metagame
{
    /// <summary>
    /// Orkestrator Sistem Artefak & Sinergi Set Benteng (Citadel Artifacts & Relic Sets).
    /// Mengelola 4 slot artefak benteng (Helm, Sigil, Ring, Tome),
    /// menghitung aktivasi set bonus 2-Piece dan 4-Piece:
    /// 1. Guardian's Vow: -15% Barricade Damage (2-pc), Aegis Barrier 4s Invulnerable saat benteng <=30% (4-pc).
    /// 2. Berserker's Fury: +8% Hero Crit Chance (2-pc), +50% Crit Damage saat benteng <50% (4-pc).
    /// 3. Sorcerer's Surge: +10% Hero Attack Speed (2-pc), -25% Hero Skill Cooldown (4-pc).
    /// </summary>
    public class ArtifactManager : MonoBehaviour
    {
        public static ArtifactManager Instance { get; private set; }

        [Header("Artifact Database / Registry")]
        [SerializeField] private List<ArtifactDataSO> allArtifacts = new List<ArtifactDataSO>();

        [Header("Aegis Barrier Runtime (Guardian's Vow 4-Piece)")]
        [SerializeField] private float aegisBarrierDuration = 4.0f;
        [SerializeField] private float aegisBarrierCooldown = 60.0f;
        private float lastAegisBarrierTriggerTime = -999f;
        private bool isAegisBarrierActive = false;

        // Slot Artefak Terpasang (Maksimal 4 Slot: Helm, Sigil, Ring, Tome)
        private readonly Dictionary<ArtifactPieceSlot, ArtifactDataSO> equippedSlots = new Dictionary<ArtifactPieceSlot, ArtifactDataSO>();

        public bool IsAegisBarrierActive => isAegisBarrierActive;
        public float AegisCooldownRemaining => Mathf.Max(0f, (lastAegisBarrierTriggerTime + aegisBarrierCooldown) - Time.time);

        public static event Action OnArtifactsChanged;
        public static event Action<bool> OnAegisBarrierStateChanged; // (isActive)

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            LoadArtifactCatalog();
        }

        private void Start()
        {
            RestoreEquippedArtifactsFromProfile();
        }

        private void LoadArtifactCatalog()
        {
            if (allArtifacts.Count == 0)
            {
                var loaded = Resources.LoadAll<ArtifactDataSO>("ScriptableObjects/Artifacts");
                if (loaded != null && loaded.Length > 0)
                {
                    allArtifacts.AddRange(loaded);
                }
            }
        }

        public void RegisterArtifact(ArtifactDataSO artifact)
        {
            if (artifact != null && !allArtifacts.Contains(artifact))
            {
                allArtifacts.Add(artifact);
            }
        }

        public ArtifactDataSO GetArtifactById(string id)
        {
            return allArtifacts.Find(a => a != null && a.artifactId == id);
        }

        public List<ArtifactDataSO> GetAllArtifacts()
        {
            return allArtifacts;
        }

        /// <summary>
        /// Memuat artefak yang sedang terpasang dari profil data akun lokal/cloud.
        /// </summary>
        public void RestoreEquippedArtifactsFromProfile()
        {
            equippedSlots.Clear();

            if (CitadelLobbyManager.Instance == null || CitadelLobbyManager.Instance.AccountData == null)
                return;

            var acc = CitadelLobbyManager.Instance.AccountData;
            if (acc.equippedArtifactIds != null)
            {
                foreach (var id in acc.equippedArtifactIds)
                {
                    var art = GetArtifactById(id);
                    if (art != null)
                    {
                        equippedSlots[art.pieceSlot] = art;
                    }
                }
            }

            // Berikan set awal default jika akun belum memiliki artefak sama sekali
            if (acc.ownedArtifactIds == null || acc.ownedArtifactIds.Count == 0)
            {
                InitializeStarterArtifacts(acc);
            }

            OnArtifactsChanged?.Invoke();
        }

        private void InitializeStarterArtifacts(CitadelAccountData acc)
        {
            // Tambahkan starter pieces
            string[] starterIds = new string[]
            {
                "guardians_helm", "guardians_sigil",
                "berserker_crest", "sorcerer_crown"
            };

            foreach (var id in starterIds)
            {
                acc.AddArtifact(id);
            }

            // Pasang starter pieces secara default
            acc.EquipArtifact("guardians_helm");
            acc.EquipArtifact("guardians_sigil");

            var helm = GetArtifactById("guardians_helm");
            var sigil = GetArtifactById("guardians_sigil");
            if (helm != null) equippedSlots[helm.pieceSlot] = helm;
            if (sigil != null) equippedSlots[sigil.pieceSlot] = sigil;

            if (FantasyDefense.Save.SaveManager.Instance != null)
            {
                FantasyDefense.Save.SaveManager.Instance.SaveProfile(acc, syncToCloud: false);
            }
        }

        #region Equip & Unequip Operations

        public bool EquipArtifact(ArtifactDataSO artifact)
        {
            if (artifact == null) return false;

            equippedSlots[artifact.pieceSlot] = artifact;

            if (CitadelLobbyManager.Instance != null && CitadelLobbyManager.Instance.AccountData != null)
            {
                var acc = CitadelLobbyManager.Instance.AccountData;
                acc.AddArtifact(artifact.artifactId);
                acc.EquipArtifact(artifact.artifactId);

                if (FantasyDefense.Save.SaveManager.Instance != null)
                {
                    FantasyDefense.Save.SaveManager.Instance.SaveProfile(acc, syncToCloud: true);
                }
            }

            Debug.Log($"[ArtifactManager] Artefak dipasang: {artifact.artifactName} ({artifact.pieceSlot} - Set: {artifact.setType})");
            OnArtifactsChanged?.Invoke();
            return true;
        }

        public bool UnequipArtifact(ArtifactPieceSlot slot)
        {
            if (equippedSlots.TryGetValue(slot, out var artifact))
            {
                equippedSlots.Remove(slot);

                if (CitadelLobbyManager.Instance != null && CitadelLobbyManager.Instance.AccountData != null)
                {
                    var acc = CitadelLobbyManager.Instance.AccountData;
                    acc.UnequipArtifact(artifact.artifactId);

                    if (FantasyDefense.Save.SaveManager.Instance != null)
                    {
                        FantasyDefense.Save.SaveManager.Instance.SaveProfile(acc, syncToCloud: true);
                    }
                }

                Debug.Log($"[ArtifactManager] Artefak dilepas dari slot {slot}: {artifact.artifactName}");
                OnArtifactsChanged?.Invoke();
                return true;
            }
            return false;
        }

        public ArtifactDataSO GetEquippedArtifact(ArtifactPieceSlot slot)
        {
            equippedSlots.TryGetValue(slot, out var artifact);
            return artifact;
        }

        public int GetEquippedCountForSet(ArtifactSetType setType)
        {
            int count = 0;
            foreach (var kvp in equippedSlots)
            {
                if (kvp.Value != null && kvp.Value.setType == setType)
                {
                    count++;
                }
            }
            return count;
        }

        public bool IsSetBonusActive(ArtifactSetType setType, int pieceThreshold)
        {
            return GetEquippedCountForSet(setType) >= pieceThreshold;
        }

        #endregion

        #region Combat Set Synergies & Combat Hooks

        /// <summary>
        /// Guardian's Vow (2-Piece): Mengurangi seluruh damage yang diterima benteng sebesar 15%.
        /// </summary>
        public float GetBarricadeDamageReductionMultiplier()
        {
            if (IsSetBonusActive(ArtifactSetType.GuardiansVow, 2))
            {
                return 0.85f; // Pengurangan damage 15%
            }
            return 1.0f;
        }

        /// <summary>
        /// Guardian's Vow (4-Piece): Saat HP benteng turun di bawah 30%,
        /// memicu Aegis Barrier yang kebal terhadap semua serangan musuh selama 4 detik (cooldown 60s).
        /// </summary>
        public bool TryTriggerAegisBarrier()
        {
            if (!IsSetBonusActive(ArtifactSetType.GuardiansVow, 4))
                return false;

            if (isAegisBarrierActive)
                return false;

            if (Time.time < lastAegisBarrierTriggerTime + aegisBarrierCooldown)
                return false;

            StartCoroutine(AegisBarrierRoutine());
            return true;
        }

        private IEnumerator AegisBarrierRoutine()
        {
            isAegisBarrierActive = true;
            lastAegisBarrierTriggerTime = Time.time;
            OnAegisBarrierStateChanged?.Invoke(true);

            Debug.Log("[ArtifactManager] AEGIS BARRIER AKTIF! Benteng kebal selama 4 detik!");

            if (SkillVfxManager.Instance != null && CastleBarricade.Instance != null)
            {
                SkillVfxManager.Instance.SpawnFloatingText(CastleBarricade.Instance.transform.position + Vector3.up * 1.0f, "★ AEGIS BARRIER AKTIF ★", new Color(1f, 0.9f, 0.2f), 2.0f);
            }

            yield return new WaitForSeconds(aegisBarrierDuration);

            isAegisBarrierActive = false;
            OnAegisBarrierStateChanged?.Invoke(false);
            Debug.Log("[ArtifactManager] Aegis Barrier telah berakhir.");
        }

        /// <summary>
        /// Berserker's Fury (2-Piece): Menambah Critical Chance seluruh hero sebesar +8% (0.08f).
        /// </summary>
        public float GetHeroCritChanceBonus()
        {
            if (IsSetBonusActive(ArtifactSetType.BerserkersFury, 2))
            {
                return 0.08f;
            }
            return 0.0f;
        }

        /// <summary>
        /// Berserker's Fury (4-Piece): Critical Damage hero melonjak +50% saat HP benteng di bawah 50%.
        /// </summary>
        public float GetHeroCritDamageBonus()
        {
            if (IsSetBonusActive(ArtifactSetType.BerserkersFury, 4))
            {
                return 0.50f; // +50% Critical Damage
            }
            return 0.0f;
        }

        /// <summary>
        /// Sorcerer's Surge (2-Piece): Meningkatkan Attack Speed hero sebesar +10% (0.10f).
        /// </summary>
        public float GetHeroAttackSpeedBonus()
        {
            if (IsSetBonusActive(ArtifactSetType.SorcerersSurge, 2))
            {
                return 0.10f;
            }
            return 0.0f;
        }

        /// <summary>
        /// Sorcerer's Surge (4-Piece): Interval cooldown / aktivasi skill pamungkas terpotong 25%.
        /// </summary>
        public float GetSkillCooldownMultiplier()
        {
            if (IsSetBonusActive(ArtifactSetType.SorcerersSurge, 4))
            {
                return 0.75f; // Pengurangan jeda skill 25%
            }
            return 1.0f;
        }

        #endregion
    }
}
