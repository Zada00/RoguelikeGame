using SadConsole;
using SadRogue.Primitives;

namespace RoguelikeGame;

internal class MapScreen : ScreenSurface
{
    private readonly Dungeon _dungeon;
    private readonly int _heroTile;
    private readonly ScreenSurface _grid;

    private const int CellSize = 5;

    private static readonly Color Backdrop = new(12, 12, 18);
    private static readonly Color DoorTint = new(230, 180, 80);
    private static readonly Color ClearedDot = new(110, 230, 130);
    private static readonly Color Unseen = new(24, 24, 32);

    public MapScreen(Dungeon dungeon, int heroTile) : base(104, 34)
    {
        _dungeon = dungeon;
        _heroTile = heroTile;

        Surface.DefaultBackground = Backdrop;
        Surface.Clear();

        string title = "DUNGEON MAP";
        Surface.Print((Width - title.Length) / 2, 2, title, Color.White);
        string depth = $"- Depth {_dungeon.Depth} -";
        Surface.Print((Width - depth.Length) / 2, 3, depth, new Color(150, 200, 150));

        string legend = "your hero = you      > = stairs down      green dot = cleared      shaded = unexplored";
        Surface.Print((Width - legend.Length) / 2, Height - 3, legend, new Color(150, 150, 150));
        Surface.Print((Width - 17) / 2, Height - 2, "M or Esc to close", new Color(110, 110, 110));

        int gw = dungeon.GridWidth * CellSize;
        int gh = dungeon.GridHeight * CellSize;
        _grid = new ScreenSurface(gw, gh) { UsePixelPositioning = true };
        _grid.Font = GameFonts.Tiles;
        _grid.FontSize = GameFonts.Tiles.GetFontSize(IFont.Sizes.Half);
        int tile = _grid.FontSize.X;
        _grid.Position = new Point((104 * 8 - gw * tile) / 2, 96);
        _grid.Surface.DefaultBackground = Backdrop;
        _grid.Surface.Clear();
        Children.Add(_grid);

        for (int gx = 0; gx < _dungeon.GridWidth; gx++)
            for (int gy = 0; gy < _dungeon.GridHeight; gy++)
                DrawRoom(gx, gy, _dungeon.GetRoom(gx, gy)!);
    }

    private static (int floor, int wall) ThemeGlyphs(RoomTheme t) => t switch
    {
        RoomTheme.Moss => (Glyph.MossFloor, Glyph.MossWall),
        RoomTheme.Crypt => (Glyph.CryptFloor, Glyph.CryptWall),
        RoomTheme.Cave => (Glyph.CaveFloor, Glyph.CaveWall),
        _ => (Glyph.Floor, Glyph.Wall),
    };

    private void DrawRoom(int gx, int gy, Room room)
    {
        var s = _grid.Surface;
        int bx = gx * CellSize;
        int by = gy * CellSize;

        if (!room.IsVisited)
        {
            for (int dx = 0; dx < CellSize; dx++)
                for (int dy = 0; dy < CellSize; dy++)
                    s.SetGlyph(bx + dx, by + dy, Glyph.Solid, Unseen, Unseen);
            return;
        }

        bool isCurrent = gx == _dungeon.CurrentGridX && gy == _dungeon.CurrentGridY;
        bool cleared = room.HadMonsters && room.Monsters.Count == 0;
        var (floorGlyph, wallGlyph) = ThemeGlyphs(room.Theme);

        Color cellBg = cleared ? new Color(20, 70, 35) : Color.Black;

        for (int dx = 0; dx < CellSize; dx++)
            for (int dy = 0; dy < CellSize; dy++)
            {
                bool edge = dx == 0 || dy == 0 || dx == CellSize - 1 || dy == CellSize - 1;
                s.SetGlyph(bx + dx, by + dy, edge ? wallGlyph : floorGlyph, Color.White, cellBg);
            }

        int mx = bx + CellSize / 2;
        int my = by + CellSize / 2;

        // kun nord/vest -> én dør per forbindelse
        if (room.HasDoorNorth) s.SetGlyph(mx, by, Glyph.Door, DoorTint, Color.Black);
        if (room.HasDoorWest) s.SetGlyph(bx, my, Glyph.Door, DoorTint, Color.Black);

        // markører: deg (karakter-sprite) > trapp
        if (isCurrent)
        {
            s.SetGlyph(mx, my, _heroTile, Color.White, Color.Black);
            if (room.Stairs.HasValue)
                s.SetGlyph(bx + CellSize - 2, by + 1, Glyph.StairsDown, Color.White, Color.Black);
        }
        else if (room.Stairs.HasValue)
        {
            s.SetGlyph(mx, my, Glyph.StairsDown, Color.White, Color.Black);
        }
    }

    public override bool ProcessKeyboard(SadConsole.Input.Keyboard keyboard)
    {
        if (keyboard.IsKeyPressed(SadConsole.Input.Keys.M) || keyboard.IsKeyPressed(SadConsole.Input.Keys.Escape))
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