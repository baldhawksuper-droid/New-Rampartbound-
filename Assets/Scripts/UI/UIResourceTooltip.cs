using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using FantasyDefense.Audio;

namespace FantasyDefense.UI
{
    public enum ResourceCommodityType
    {
        Gold,
        Gems,
        Energy,
        HeroShards,
        ScrapIron
    }

    /// <summary>
    /// Mini Tooltip Informasi Sumber Daya:
    /// Menampilkan jendela modal pop-up ringkas saat pemain menyentuh saldo Gold, Gems, Shard, atau Scrap di header,
    /// menjelaskan kegunaan dan cara mendapatkan komoditas tersebut.
    /// </summary>
    public class UIResourceTooltip : MonoBehaviour, IPointerClickHandler
    {
        [Header("Commodity Configuration")]
        [SerializeField] private ResourceCommodityType commodityType = ResourceCommodityType.Gold;
        [SerializeField] private Sprite commodityIcon;

        private static GameObject activeTooltipInstance;

        public void OnPointerClick(PointerEventData eventData)
        {
            ShowTooltip();
        }

        public void ShowTooltip()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound(SoundType.ButtonClick);
            }

            // Tutup tooltip lama jika ada
            CloseCurrentTooltip();

            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            string title = "";
            string desc = "";
            string source = "";
            Color headerColor = Color.white;

            switch (commodityType)
            {
                case ResourceCommodityType.Gold:
                    title = "Koin Emas Kerajaan (Gold)";
                    headerColor = new Color(1f, 0.85f, 0.2f);
                    desc = "Mata uang utama Citadel untuk menaikkan level pahlawan (Level Up) dan memodifikasi perlengkapan di Relic Forge.";
                    source = "Diperoleh dari: Kemenangan Stage, Ekspedisi Guild, Misi Harian, dan Bonus Gelombang.";
                    break;

                case ResourceCommodityType.Gems:
                    title = "Permata Abyssal (Gems)";
                    headerColor = new Color(0.35f, 0.95f, 1f);
                    desc = "Permata langka berkualitas tinggi untuk memanggil pahlawan di Summon Hall dan membeli paket eksklusif di Toko Citadel.";
                    source = "Diperoleh dari: Bintang 3-Star Stage, Hadiah Season Pass, Misi Harian, dan Pembelian Toko.";
                    break;

                case ResourceCommodityType.Energy:
                    title = "Stamina Benteng (Energy)";
                    headerColor = new Color(0.4f, 1f, 0.5f);
                    desc = "Energi stamina komandan yang dibutuhkan untuk meluncurkan pertahanan di Stage Peta Dunia (5 Energi/Battle).";
                    source = "Pulih otomatis: 1 Stamina setiap 5 menit (Maksimal 100/100) atau dari klaim Misi Harian.";
                    break;

                case ResourceCommodityType.HeroShards:
                    title = "Pecahan Pahlawan (Hero Shards)";
                    headerColor = new Color(0.85f, 0.45f, 1f);
                    desc = "Kristal fragmen esensi jiwa pahlawan yang digunakan untuk Promosi Bintang (Star Promotion 1★ - 5★) guna melipatgandakan Combat Power.";
                    source = "Diperoleh dari: Duplikat Gacha Summon Hall, Tantangan Event, dan Misi Pencapaian.";
                    break;

                case ResourceCommodityType.ScrapIron:
                    title = "Gerigi Besi (Scrap Iron)";
                    headerColor = new Color(0.8f, 0.85f, 0.9f);
                    desc = "Bahan baku logam besi tempa untuk menempa (Crafting) dan meng-upgrade gear pahlawan di Relic Forge.";
                    source = "Diperoleh dari: Salvage gear tak terpakai dan hadiah Ekspedisi Raid Guild.";
                    break;
            }

            // Bangun kartu tooltip dinamis di canvas
            activeTooltipInstance = new GameObject("ResourceTooltipModal", typeof(RectTransform), typeof(Image));
            activeTooltipInstance.transform.SetParent(canvas.transform, false);

            var bg = activeTooltipInstance.GetComponent<Image>();
            bg.color = new Color(0.04f, 0.07f, 0.12f, 0.96f);

            var rt = activeTooltipInstance.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.1f, 0.45f);
            rt.anchorMax = new Vector2(0.9f, 0.65f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            // Backdrop click blocker
            var blocker = new GameObject("TooltipBlocker", typeof(RectTransform), typeof(Image), typeof(Button));
            blocker.transform.SetParent(canvas.transform, false);
            blocker.transform.SetSiblingIndex(activeTooltipInstance.transform.GetSiblingIndex());
            var bRt = blocker.GetComponent<RectTransform>();
            bRt.anchorMin = Vector2.zero;
            bRt.anchorMax = Vector2.one;
            bRt.offsetMin = Vector2.zero;
            bRt.offsetMax = Vector2.zero;
            blocker.GetComponent<Image>().color = new Color(0, 0, 0, 0.45f);
            blocker.GetComponent<Button>().onClick.AddListener(CloseCurrentTooltip);

            // Container teks
            var txtObj = new GameObject("TooltipContent", typeof(RectTransform), typeof(Text));
            txtObj.transform.SetParent(activeTooltipInstance.transform, false);
            var tRt = txtObj.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0.05f, 0.05f);
            tRt.anchorMax = new Vector2(0.95f, 0.95f);
            tRt.offsetMin = Vector2.zero;
            tRt.offsetMax = Vector2.zero;

            var txt = txtObj.GetComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.alignment = TextAnchor.MiddleCenter;
            txt.supportRichText = true;
            txt.fontSize = 22;
            txt.text = $"<size=26><b><color=#{ColorUtility.ToHtmlStringRGB(headerColor)}>{title}</color></b></size>\n\n{desc}\n\n<i><color=#A0C0E0>{source}</color></i>";

            // Animasi squash-and-stretch
            var cg = activeTooltipInstance.AddComponent<CanvasGroup>();
            var anim = activeTooltipInstance.AddComponent<UIWindowTransition>();
            anim.OpenWindow();
        }

        public static void CloseCurrentTooltip()
        {
            if (activeTooltipInstance != null)
            {
                var parent = activeTooltipInstance.transform.parent;
                if (parent != null)
                {
                    var blocker = parent.Find("TooltipBlocker");
                    if (blocker != null) Destroy(blocker.gameObject);
                }
                Destroy(activeTooltipInstance);
                activeTooltipInstance = null;
            }
        }
    }
}
