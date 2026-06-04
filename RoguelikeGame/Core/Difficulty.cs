namespace RoguelikeGame;

internal enum Difficulty { Easy, Normal, Hard }

internal readonly struct DiffSettings
{
    public int DensityDiv { get; init; }   // høyere = færre monstre per rom
    public int Cap { get; init; }          // maks monstre i ett rom
    public double StatMul { get; init; }   // ganges på monster-HP og -skade
    public int RewardBonus { get; init; }  // ekstra oppgraderinger per ryddet rom
    public string Name { get; init; }
    public string Desc { get; init; }
}

internal static class DifficultySettings
{
    public static DiffSettings Get(Difficulty d) => d switch
    {
        Difficulty.Easy => new DiffSettings
        {
            DensityDiv = 16, Cap = 7, StatMul = 0.80, RewardBonus = 0,
            Name = "Easy", Desc = "Fewer, weaker foes. Standard rewards."
        },
        Difficulty.Hard => new DiffSettings
        {
            DensityDiv = 9, Cap = 12, StatMul = 1.35, RewardBonus = 1,
            Name = "Hard", Desc = "Packed, deadly rooms. Double rewards."
        },
        _ => new DiffSettings
        {
            DensityDiv = 12, Cap = 10, StatMul = 1.0, RewardBonus = 0,
            Name = "Normal", Desc = "A balanced challenge."
        },
    };
}
