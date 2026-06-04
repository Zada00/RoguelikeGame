using SadConsole;
using SadConsole.Input;
using SadRogue.Primitives;

namespace RoguelikeGame;

internal class DifficultySelectScreen : ScreenSurface
{
    private readonly Character _character;
    private int _selected = 1;   // Normal som standard
    private static readonly Difficulty[] Options = { Difficulty.Easy, Difficulty.Normal, Difficulty.Hard };

    public DifficultySelectScreen(Character character) : base(104, 34)
    {
        _character = character;
        Draw();
    }

    private void Draw()
    {
        Surface.Clear();

        string title = "CHOOSE DIFFICULTY";
        Surface.Print((Width - title.Length) / 2, 6, title, Color.White);

        string hero = $"Hero: {_character.Name}";
        Surface.Print((Width - hero.Length) / 2, 8, hero, _character.Color);

        for (int i = 0; i < Options.Length; i++)
        {
            var d = DifficultySettings.Get(Options[i]);
            int y = 13 + i * 3;
            bool sel = i == _selected;
            int x = (Width - 44) / 2;

            Surface.Print(x, y, (sel ? "> " : "  ") + d.Name,
                          sel ? Color.White : new Color(120, 120, 120));
            Surface.Print(x + 2, y + 1, d.Desc, new Color(140, 140, 140));
        }

        string hint = "Up/Down to choose   -   Enter to start   -   Esc to go back";
        Surface.Print((Width - hint.Length) / 2, Height - 3, hint, new Color(130, 130, 130));
        Surface.IsDirty = true;
    }

    public override bool ProcessKeyboard(Keyboard keyboard)
    {
        if (keyboard.IsKeyPressed(Keys.Up))
        {
            _selected = (_selected - 1 + Options.Length) % Options.Length;
            Draw();
        }
        else if (keyboard.IsKeyPressed(Keys.Down))
        {
            _selected = (_selected + 1) % Options.Length;
            Draw();
        }
        else if (keyboard.IsKeyPressed(Keys.Escape))
        {
            var back = new CharacterSelectScreen();
            Game.Instance.Screen = back;
            back.IsFocused = true;
        }
        else if (keyboard.IsKeyPressed(Keys.Enter))
        {
            var game = new RootScreen(_character, Options[_selected]);
            Game.Instance.Screen = game;
            game.IsFocused = true;
        }
        return true;
    }
}
