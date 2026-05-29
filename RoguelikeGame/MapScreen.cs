using SadConsole;
using SadConsole.Input;
using SadRogue.Primitives;

namespace RoguelikeGame;

// Et lite popup-vindu som viser en mini-oversikt over labyrinten.
// Hvert rom blir én "celle" som viser dørene og om vi har vært der.
// Lukkes med M eller Esc.
internal class MapScreen : ScreenSurface
{
    private readonly Dungeon _dungeon;

    // Hvert rom tegnes som en 3x3-blokk i mini-kartet:
    //   ###     # = vegg
    //   #.#     . = gulv
    //   ###     + = dør (hvis det er en dør på den vegen)
    // Pluss en X i midten som viser hvor spilleren er nå.
    private const int CellSize = 3;

    // Disse beregnes basert på Dungeon-størrelsen og sentreres på skjermen.
    private readonly int _originX;
    private readonly int _originY;

    private static readonly Color WallColor = new(150, 120, 90);
    private static readonly Color FloorColor = new(70, 70, 70);
    private static readonly Color UnvisitedColor = new(25, 25, 35);
    private static readonly Color DoorColor = new(220, 170, 70);
    private static readonly Color PlayerColor = Color.Yellow;

    public MapScreen(Dungeon dungeon) : base(80, 25)
    {
        _dungeon = dungeon;

        int mapWidthInCells = dungeon.GridWidth * CellSize;
        int mapHeightInCells = dungeon.GridHeight * CellSize;
        _originX = (Width - mapWidthInCells) / 2;
        _originY = (Height - mapHeightInCells) / 2;

        Draw();
    }

    private void Draw()
    {
        Surface.Clear();

        // Tittel øverst og hint nederst
        const string title = "KART";
        Surface.Print((Width - title.Length) / 2, 1, title, Color.White);
        const string hint = "M eller Esc for å lukke";
        Surface.Print((Width - hint.Length) / 2, Height - 2, hint, new Color(120, 120, 120));

        // Tegn hvert rom som en 3x3-celle.
        for (int gx = 0; gx < _dungeon.GridWidth; gx++)
        {
            for (int gy = 0; gy < _dungeon.GridHeight; gy++)
            {
                var room = _dungeon.GetRoom(gx, gy)!;
                DrawRoomCell(gx, gy, room);
            }
        }
    }

    private void DrawRoomCell(int gx, int gy, Room room)
    {
        int baseX = _originX + gx * CellSize;
        int baseY = _originY + gy * CellSize;

        // Ikke-besøkte rom vises som en mørk firkant (fog of war).
        if (!room.IsVisited)
        {
            for (int dx = 0; dx < CellSize; dx++)
                for (int dy = 0; dy < CellSize; dy++)
                    Surface.SetGlyph(baseX + dx, baseY + dy, ' ', UnvisitedColor, UnvisitedColor);
            return;
        }

        // Tegn romvegger
        for (int dx = 0; dx < CellSize; dx++)
            for (int dy = 0; dy < CellSize; dy++)
            {
                bool isEdge = dx == 0 || dy == 0 || dx == CellSize - 1 || dy == CellSize - 1;
                if (isEdge)
                    Surface.SetGlyph(baseX + dx, baseY + dy, '#', WallColor, Color.Black);
                else
                    Surface.SetGlyph(baseX + dx, baseY + dy, '.', FloorColor, Color.Black);
            }

        // Dører oppå vegene
        int midX = baseX + CellSize / 2;
        int midY = baseY + CellSize / 2;
        if (room.HasDoorNorth) Surface.SetGlyph(midX, baseY, '+', DoorColor, Color.Black);
        if (room.HasDoorSouth) Surface.SetGlyph(midX, baseY + CellSize - 1, '+', DoorColor, Color.Black);
        if (room.HasDoorWest) Surface.SetGlyph(baseX, midY, '+', DoorColor, Color.Black);
        if (room.HasDoorEast) Surface.SetGlyph(baseX + CellSize - 1, midY, '+', DoorColor, Color.Black);

        // Marker spillerens nåværende rom med X
        if (gx == _dungeon.CurrentGridX && gy == _dungeon.CurrentGridY)
            Surface.SetGlyph(midX, midY, 'X', PlayerColor, Color.Black);
    }

    public override bool ProcessKeyboard(Keyboard keyboard)
    {
        if (keyboard.IsKeyPressed(Keys.M) || keyboard.IsKeyPressed(Keys.Escape))
        {
            // Fjern oss selv fra foreldreskjermen og gi fokus tilbake.
            if (Parent != null)
            {
                var parent = Parent;
                Parent.Children.Remove(this);
                parent.IsFocused = true;
            }
            return true;
        }
        return true; // svelg alle andre tastetrykk så de ikke flytter spilleren
    }
}