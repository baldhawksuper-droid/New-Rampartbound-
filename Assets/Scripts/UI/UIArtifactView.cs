using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using FantasyDefense.Audio;
using FantasyDefense.Core;
using FantasyDefense.Data;
using FantasyDefense.Metagame;

namespace FantasyDefense.UI
{
    /// <summary>
    /// Antarmuka pengguna (UI View) untuk melihat dan mengonfigurasi Artefak Benteng Citadel
    /// serta memantau status aktif Sinergi Set Artefak 2-Piece & 4-Piece.
    /// </summary>
    public class UIArtifactView : MonoBehaviour
    {
        [Header("Root Panel")]
        [SerializeField] private GameObject viewPanel;

        [Header("Equipped Slots")]
        [SerializeField] private Text helmSlotText;
        [SerializeField] private Text sigilSlotText;
        [SerializeField] private Text ringSlotText;
        [SerializeField] private Text tomeSlotText;

        [Header("Set Synergy Indicators")]
        [SerializeField] private Text guardiansVowStatusText;
        [SerializeField] private Text berserkersFuryStatusText;
        [SerializeField] private Text sorcerersSurgeStatusText;

        [Header("Owned Artifacts List")]
        [SerializeField] private Transform inventoryContainer;
        [SerializeField] private GameObject artifactItemPrefab;

        [Header("Close Button")]
        [SerializeField] private Button closeButton;

        private void Awake()
        {
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(CloseView);
            }
        }

        private void OnEnable()
        {
            ArtifactManager.OnArtifactsChanged += RefreshView;
        }

        private void OnDisable()
        {
            ArtifactManager.OnArtifactsChanged -= RefreshView;
        }

        private void Start()
        {
            RefreshView();
        }

        public void OpenView()
        {
            transform.SetAsLastSibling();
            if (viewPanel != null)
            {
                viewPanel.transform.SetAsLastSibling();
                viewPanel.SetActive(true);
            }
            else gameObject.SetActive(true);

            CanvasGroup cg = GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.interactable = true;
                cg.blocksRaycasts = true;
            }

            RefreshView();
        }

        public void CloseView()
        {
            if (viewPanel != null) viewPanel.SetActive(false);
            else gameObject.SetActive(false);

            CanvasGroup cg = GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.blocksRaycasts = false;
            }

            if (GameFlowManager.Instance != null && GameFlowManager.Instance.CurrentActivePanel == GamePanelType.Artifacts)
            {
                GameFlowManager.Instance.CloseCurrentPanel();
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound(SoundType.ButtonClick);
            }
        }

        public void RefreshView()
        {
            if (ArtifactManager.Instance == null) return;

            // 1. Perbarui 4 Slot Artefak yang Sedang Terpasang
            UpdateSlotDisplay(ArtifactPieceSlot.Helm, helmSlotText);
            UpdateSlotDisplay(ArtifactPieceSlot.Sigil, sigilSlotText);
            UpdateSlotDisplay(ArtifactPieceSlot.Ring, ringSlotText);
            UpdateSlotDisplay(ArtifactPieceSlot.Tome, tomeSlotText);

            // 2. Perbarui Status Sinergi Set
            UpdateSetSynergyDisplay(
                ArtifactSetType.GuardiansVow,
                guardiansVowStatusText,
                "Guardian's Vow",
                "2-Pc: -15% Barricade DMG",
                "4-Pc: Aegis Barrier (Invulnerable 4s)"
            );

            UpdateSetSynergyDisplay(
                ArtifactSetType.BerserkersFury,
                berserkersFuryStatusText,
                "Berserker's Fury",
                "2-Pc: +8% Hero Crit",
                "4-Pc: +50% Crit DMG (Low Castle HP)"
            );

            UpdateSetSynergyDisplay(
                ArtifactSetType.SorcerersSurge,
                sorcerersSurgeStatusText,
                "Sorcerer's Surge",
                "2-Pc: +10% Attack Speed",
                "4-Pc: -25% Skill Cooldown"
            );

            // 3. Perbarui Daftar Inventaris Artefak yang Dimiliki
            PopulateInventoryList();
        }

        private void UpdateSlotDisplay(ArtifactPieceSlot slot, Text slotText)
        {
            if (slotText == null) return;

            var art = ArtifactManager.Instance.GetEquippedArtifact(slot);
            if (art != null)
            {
                slotText.text = $"[{slot}] {art.artifactName}\nSet: {art.setType} | CP +{art.combatPower}";
                slotText.color = art.GetSetThemeColor();
            }
            else
            {
                slotText.text = $"[{slot}] (Kosong / Belum Terpasang)";
                slotText.color = new Color(0.7f, 0.7f, 0.7f);
            }
        }

        private void UpdateSetSynergyDisplay(ArtifactSetType setType, Text statusText, string setName, string bonus2, string bonus4)
        {
            if (statusText == null) return;

            int count = ArtifactManager.Instance.GetEquippedCountForSet(setType);
            string active2 = count >= 2 ? "<color=#00FF66>✔ [AKTIF]</color>" : "<color=#888888>○ (Terkunci)</color>";
            string active4 = count >= 4 ? "<color=#FFCC00>★ [AKTIF]</color>" : "<color=#888888>○ (Terkunci)</color>";

            statusText.text = $"<b>{setName} ({count}/4)</b>\n" +
                              $"• {bonus2} {active2}\n" +
                              $"• {bonus4} {active4}";
        }

        private void PopulateInventoryList()
        {
            if (inventoryContainer == null) return;

            // Bersihkan item lama
            foreach (Transform child in inventoryContainer)
            {
                Destroy(child.gameObject);
            }

            var allArts = ArtifactManager.Instance.GetAllArtifacts();
            var acc = CitadelLobbyManager.Instance != null ? CitadelLobbyManager.Instance.AccountData : null;

            foreach (var art in allArts)
            {
                if (art == null) continue;

                bool isOwned = acc != null && acc.HasArtifact(art.artifactId);
                if (!isOwned) continue;

                bool isEquipped = acc != null && acc.IsArtifactEquipped(art.artifactId);

                GameObject itemObj;
                if (artifactItemPrefab != null)
                {
                    itemObj = Instantiate(artifactItemPrefab, inventoryContainer);
                }
                else
                {
                    itemObj = new GameObject($"ArtifactItem_{art.artifactId}");
                    itemObj.transform.SetParent(inventoryContainer, false);
                    var txt = itemObj.AddComponent<Text>();
                    txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    txt.fontSize = 14;
                }

                var textComp = itemObj.GetComponentInChildren<Text>();
                if (textComp != null)
                {
                    string status = isEquipped ? "[TERPASANG]" : "[SIAP]";
                    textComp.text = $"{art.artifactName} ({art.pieceSlot}) - {status}\n{art.description}";
                }

                var btn = itemObj.GetComponentInChildren<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    if (isEquipped)
                    {
                        btn.onClick.AddListener(() => ArtifactManager.Instance.UnequipArtifact(art.pieceSlot));
                    }
                    else
                    {
                        btn.onClick.AddListener(() => ArtifactManager.Instance.EquipArtifact(art));
                    }
                }
            }
        }
    }
}
