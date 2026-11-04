namespace FantasyDefense.Combat
{
    /// <summary>
    /// Tema visual latar belakang medan pertempuran (Battlefield Environment Theme).
    /// Menyesuaikan suasana grafis sesuai progres babak stage:
    /// - Forest: Padang rumput & gerbang luar benteng (Chapter 1, Stage 1-4).
    /// - Mountain: Punggung bukit karang & batuan granit dingin (Chapter 1, Stage 5-8).
    /// - Volcanic: Dataran basal obsidian membara & urat magma kuno (Chapter 1, Stage 9-10 & Boss).
    /// </summary>
    public enum BattlefieldEnvironmentTheme
    {
        Forest = 0,
        Mountain = 1,
        Volcanic = 2,
        Beach = 3
    }
}
