using SadConsole;
using SadConsole.Input;
using SadRogue.Primitives;

namespace RoguelikeGame;

// Skjermen som vises FØRST. Velg helt med piltaster, bekreft med Enter.
internal class CharacterSelectScreen : ScreenSurface
{
    private int _selected;

    public CharacterSelectScreen() : base(80, 25)
    {
        Draw();
    }

    private void Draw()
    {
        Surface.Clear();

        const string title = "CHOOSE YOUR HERO";
        Surface.Print((Width - title.Length) / 2, 2, title, Color.White);

        const string hint = "Up/Down to choose   -   Enter to start";
        Surface.Print((Width - hint.Length) / 2, Height - 2, hint, new Color(130, 130, 130));

        int startY = 5;
        for (int i = 0; i < Character.All.Count; i++)
        {
            var c = Character.All[i];
            int y = startY + i * 2;
            bool isSelected = i == _selected;

            Surface.Print(4, y, isSelected ? ">" : " ", Color.Yellow);
            Surface.Print(6, y, c.Name[..1], c.Color);
            Surface.Print(8, y, c.Name, isSelected ? Color.White : new Color(120, 120, 120));
            Surface.Print(24, y, $"HP {c.MaxHp}", isSelected ? new Color(210, 120, 120) : new Color(90, 90, 90));

            // Beskrivelsen vises bare for den valgte helten.
            if (isSelected)
                Surface.Print(8, y + 1, c.Description, new Color(170, 170, 170));
        }
    }

    public override bool ProcessKeyboard(Keyboard keyboard)
    {
        if (keyboard.IsKeyPressed(Keys.Up))
        {
            _selected = (_selected - 1 + Character.All.Count) % Character.All.Count;
            Draw();
        }
        else if (keyboard.IsKeyPressed(Keys.Down))
        {
            _selected = (_selected + 1) % Character.All.Count;
            Draw();
        }
        else if (keyboard.IsKeyPressed(Keys.Enter))
        {
            // Bytt til selve spillet med den valgte helten.
            var game = new RootScreen(Character.All[_selected]);
            Game.Instance.Screen = game;
            game.IsFocused = true;
        }
        return true;
    }
}