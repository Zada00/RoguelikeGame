using SadConsole;
using SadConsole.Configuration;
using RoguelikeGame;

Settings.WindowTitle = "Min Roguelike";

Builder gameStartup = new Builder()
    .SetScreenSize(80, 25)
    .SetStartingScreen<CharacterSelectScreen>()
    .IsStartingScreenFocused(true)
    .ConfigureFonts(true)
    .OnStart(LoadResources);   // kjør LoadResources når spillet starter

Game.Create(gameStartup);
Game.Instance.Run();
Game.Instance.Dispose();

static void LoadResources(object? sender, GameHost host)
{
    // Leser tiles.font, som igjen peker på tiles.png i Resources-mappa.
    GameFonts.Tiles = Game.Instance.LoadFont("Resources/tiles.font");
}