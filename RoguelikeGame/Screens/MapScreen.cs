using SadConsole;
using SadConsole.Input;
using SadRogue.Primitives;

namespace RoguelikeGame;

internal class MapScreen : ScreenSurface
{
    private readonly Dungeon _dungeon;
    private const int CellSize = 3;
    private readonly int _originX;
    private readonly int _originY;

    private static readonly Color WallColor = new(150, 120, 90);
    private static readonly Color FloorColor = new(70, 70, 70);
    private static readonly Color UnvisitedColor = new(25, 25, 35);
    private static readonly Color DoorColor = new(220, 170, 70);
    private static readonly Color PlayerColor = Color.Yellow;
    private static readonly Color StairColor = new(120, 220, 120);

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

        string title = $"MAP  -  Depth {_dungeon.Depth}";
        Surface.Print((Width - title.Length) / 2, 1, title, Color.White);
        const string hint = "M or Esc to close      > = stairs down";
        Surface.Print((Width - hint.Length) / 2, Height - 2, hint, new Color(120, 120, 120));

        for (int gx = 0; gx < _dungeon.GridWidth; gx++)
            for (int gy = 0; gy < _dungeon.GridHeight; gy++)
            {
                var room = _dungeon.GetRoom(gx, gy)!;
                DrawRoomCell(gx, gy, room);
            }
    }

    private void DrawRoomCell(int gx, int gy, Room room)
    {
        int baseX = _originX + gx * CellSize;
        int baseY = _originY + gy * CellSize;

        if (!room.IsVisited)
        {
            for (int dx = 0; dx < CellSize; dx++)
                for (int dy = 0; dy < CellSize; dy++)
                    Surface.SetGlyph(baseX + dx, baseY + dy, ' ', UnvisitedColor, UnvisitedColor);
            return;
        }

        for (int dx = 0; dx < CellSize; dx++)
            for (int dy = 0; dy < CellSize; dy++)
            {
                bool isEdge = dx == 0 || dy == 0 || dx == CellSize - 1 || dy == CellSize - 1;
                if (isEdge)
                    Surface.SetGlyph(baseX + dx, baseY + dy, '#', WallColor, Color.Black);
                else
                    Surface.SetGlyph(baseX + dx, baseY + dy, '.', FloorColor, Color.Black);
            }

        int midX = baseX + CellSize / 2;
        int midY = baseY + CellSize / 2;
        if (room.HasDoorNorth) Surface.SetGlyph(midX, baseY, '+', DoorColor, Color.Black);
        if (room.HasDoorSouth) Surface.SetGlyph(midX, baseY + CellSize - 1, '+', DoorColor, Color.Black);
        if (room.HasDoorWest)  Surface.SetGlyph(baseX, midY, '+', DoorColor, Color.Black);
        if (room.HasDoorEast)  Surface.SetGlyph(baseX + CellSize - 1, midY, '+', DoorColor, Color.Black);

        // marker rom med trapp
        if (room.Stairs.HasValue)
            Surface.SetGlyph(midX, midY, '>', StairColor, Color.Black);

        // spillerens nåværende rom (tegnes oppå, så X vinner)
        if (gx == _dungeon.CurrentGridX && gy == _dungeon.CurrentGridY)
            Surface.SetGlyph(midX, midY, 'X', PlayerColor, Color.Black);
    }

    public override bool ProcessKeyboard(Keyboard keyboard)
    {
        if (keyboard.IsKeyPressed(Keys.M) || keyboard.IsKeyPressed(Keys.Escape))
        {
            if (Parent != null)
            {
                var parent = Parent;
                Parent.Children.Remove(this);
                parent.IsFocused = true;
            }
            return true;
        }
        return true;
    }
}
