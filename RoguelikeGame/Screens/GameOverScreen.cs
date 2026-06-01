using SadConsole;
using SadConsole.Input;
using SadRogue.Primitives;

namespace RoguelikeGame;

internal class GameOverScreen : ScreenSurface
{
    private readonly string _heroName;

    public GameOverScreen(string heroName) : base(80, 25)
    {
        _heroName = heroName;
        Draw();
    }

    private void Draw()
    {
        Surface.Clear();

        const string title = "YOU DIED";
        Surface.Print((Width - title.Length) / 2, 10, title, new Color(220, 70, 70));

        string sub = $"The {_heroName} has fallen.";
        Surface.Print((Width - sub.Length) / 2, 12, sub, new Color(180, 180, 180));

        const string hint = "Enter: choose a new hero    Esc: quit";
        Surface.Print((Width - hint.Length) / 2, 15, hint, new Color(130, 130, 130));
    }

    public override bool ProcessKeyboard(Keyboard keyboard)
    {
        if (keyboard.IsKeyPressed(Keys.Enter))
        {
            var select = new CharacterSelectScreen();
            Game.Instance.Screen = select;
            select.IsFocused = true;
        }
        else if (keyboard.IsKeyPressed(Keys.Escape))
        {
            Environment.Exit(0);
        }
        return true;
    }
}