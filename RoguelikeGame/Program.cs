using RoguelikeGame;
using SadConsole;
using SadConsole.Configuration;
using SadRogue.Primitives;

// Tittel på vinduet
Settings.WindowTitle = "Min Roguelike";

// SadConsole bygger opp konfigurasjonen sin med en "Builder".
// Her sier vi hvor stort vinduet skal være, målt i ruter (ikke piksler).
Builder gameStartup = new Builder()
    .SetScreenSize(80, 25)
    .SetStartingScreen<RootScreen>()        // klassen som blir brukt for å starte spillet istedenfor UseDefaultConsole
    .IsStartingScreenFocused(true)  
    .ConfigureFonts(true);

// Start spillet
Game.Create(gameStartup);
Game.Instance.Run();
Game.Instance.Dispose();


// Denne metoden kjøres én gang, rett etter at SadConsole er ferdig med å starte opp.
static void Startup(object? sender, GameHost host)
{
    var screen = Game.Instance.StartingConsole;

    // Print tekst på rad 2, kolonne 2
    screen.Print(2, 2, "Hei fra SadConsole!");

    // Tegn et gult '@' midt på skjermen
    screen.SetGlyph(40, 12, '@', Color.Yellow);
}