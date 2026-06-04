using SadConsole;
using SadConsole.Input;
using SadRogue.Primitives;

namespace RoguelikeGame;

internal class CharacterSelectScreen : ScreenSurface
{
    private int _selected;
    private const int PanelX = 42;

    public CharacterSelectScreen() : base(100, 32)
    {
        Draw();
    }

    private void Draw()
    {
        Surface.Clear();

        const string title = "CHOOSE YOUR HERO";
        Surface.Print((Width - title.Length) / 2, 1, title, Color.White);
        const string hint = "Up/Down to choose   -   Enter to start";
        Surface.Print((Width - hint.Length) / 2, Height - 2, hint, new Color(130, 130, 130));

        // Venstre: liste over heltene
        for (int i = 0; i < Character.All.Count; i++)
        {
            var c = Character.All[i];
            int y = 5 + i * 2;
            bool sel = i == _selected;

            Surface.Print(3, y, sel ? ">" : " ", Color.Yellow);
            Surface.Print(5, y, c.Name[..1], c.Color);
            Surface.Print(7, y, c.Name, sel ? Color.White : new Color(120, 120, 120));
            Surface.Print(20, y, $"HP {c.MaxHp}", sel ? new Color(210, 120, 120) : new Color(90, 90, 90));
        }

        DrawDetails(Character.All[_selected]);
    }

    private void DrawDetails(Character c)
    {
        var dim = new Color(150, 150, 150);
        var label = new Color(120, 120, 120);

        Surface.Print(PanelX, 5, c.Name, c.Color);

        int y = 7;
        foreach (var line in Wrap(c.Description, 36))
            Surface.Print(PanelX, y++, line, dim);

        Surface.Print(PanelX, 12, $"ATK {c.Attack,-3} DEF {c.Defense,-3} HP {c.MaxHp}", Color.White);
        Surface.Print(PanelX, 13, $"VIS {c.Vision,-3} SPD {c.Speed,-3} MANA {c.Mana}", Color.White);

        Surface.Print(PanelX, 15, "Attacks: " + AbilityText(c), label);
    }

    private static string AbilityText(Character c)
    {
        string basic = c.Ranged ? "ranged shot" : "melee strike";
        string special = c.Special switch
        {
            Special.Blink => "Teleport",
            Special.Whirlwind => "Whirlwind",
            Special.Bash => "Shield bash",
            Special.Volley => "Arrow volley",
            Special.Nova => "Death nova",
            Special.HolyLight => "Holy light",
            _ => "none",
        };
        return $"{basic}, special: {special}";
    }

    // Enkel ordbryting så beskrivelsen ikke renner ut av panelet.
    private static IEnumerable<string> Wrap(string text, int width)
    {
        var line = "";
        foreach (var word in text.Split(' '))
        {
            if (line.Length == 0) line = word;
            else if (line.Length + 1 + word.Length <= width) line += " " + word;
            else { yield return line; line = word; }
        }
        if (line.Length > 0) yield return line;
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
            var next = new DifficultySelectScreen(Character.All[_selected]);
            Game.Instance.Screen = next;
            next.IsFocused = true;
        }
        return true;
    }
}