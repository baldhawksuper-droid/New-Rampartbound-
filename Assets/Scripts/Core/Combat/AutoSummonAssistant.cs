using System;
using UnityEngine;
using FantasyDefense.Audio;
using FantasyDefense.Core;
using FantasyDefense.Data;
using FantasyDefense.Economy;
using FantasyDefense.GridSystem;
using FantasyDefense.Summon;

namespace FantasyDefense.Combat
{
    /// <summary>
    /// Asisten Otomatis Pemanggilan Pahlawan (Citadel Smart Auto-Summon Assistant).
    /// Memanggil hero secara berkala ketika saldo Gems mencukupi dan ada petak kosong (Mode 2)
    /// atau ada kandidat evolusi katalis (Mode 1).
    /// Bekerja sinergis bersama AutoMergeAssistant untuk kenyamanan bermain pemain mobile.
    /// </summary>
    public class AutoSummonAssistant : MonoBehaviour
    {
        public static AutoSummonAssistant Instance { get; private set; }

        [Header("Configuration")]
        [Tooltip("Interval pemanggilan hero otomatis (detik)")]
        [SerializeField] private float summonInterval = 1.0f;

        [Header("Runtime State")]
        [SerializeField] private bool isAutoSummonEnabled = false;
        private float summonTimer = 0f;

        public bool IsAutoSummonEnabled => isAutoSummonEnabled;

        public static event Action<bool> OnAutoSummonToggled;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Update()
        {
            if (!isAutoSummonEnabled) return;

            // Pastikan pertempuran sedang aktif atau dalam persiapan taktis
            if (GameManager.Instance != null)
            {
                var state = GameManager.Instance.CurrentBattleState;
                if (state != BattleState.WaveInProgress && state != BattleState.Preparation)
                {
                    return;
                }
            }

            summonTimer += Time.deltaTime;
            if (summonTimer >= summonInterval)
            {
                summonTimer = 0f;
                TryExecuteAutoSummon();
            }
        }

        public void ToggleAutoSummon()
        {
            SetAutoSummon(!isAutoSummonEnabled);
        }

        public void SetAutoSummon(bool enabled)
        {
            isAutoSummonEnabled = enabled;
            summonTimer = 0f;
            Debug.Log($"[AutoSummonAssistant] Smart Auto-Summon Assistant: {(isAutoSummonEnabled ? "AKTIF (ON)" : "NONAKTIF (OFF)")}");

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound(SoundType.ButtonClick);
            }

            OnAutoSummonToggled?.Invoke(isAutoSummonEnabled);
        }

        public bool TryExecuteAutoSummon()
        {
            if (HeroSummoner.Instance == null) return false;
            if (EconomyManager.Instance == null || !EconomyManager.Instance.CanAffordSummon()) return false;

            bool isPuzzle = GameManager.Instance != null && GameManager.Instance.CurrentBattleMode == BattleMode.PuzzlePreparation;
            if (isPuzzle)
            {
                if (!HeroSummoner.Instance.HasCatalystCandidate()) return false;
            }
            else
            {
                if (GridManager.Instance != null && GridManager.Instance.IsGridFull()) return false;
            }

            return HeroSummoner.Instance.TrySummonHero();
        }
    }
}
