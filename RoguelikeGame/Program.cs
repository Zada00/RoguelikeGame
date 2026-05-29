using SadConsole;
using SadConsole.Configuration;
using RoguelikeGame;

Settings.WindowTitle = "Min Roguelike";

Builder gameStartup = new Builder()
    .SetScreenSize(80, 25)
    .SetStartingScreen<CharacterSelectScreen>()
    .IsStartingScreenFocused(true)
    .ConfigureFonts(true);

Game.Create(gameStartup);
Game.Instance.Run();
Game.Instance.Dispose();