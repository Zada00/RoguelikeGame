using SadConsole;
using SadRogue.Primitives;

namespace RoguelikeGame;

internal class GameMap
{
    public int Width { get; }
    public int Height { get; }

    // Et todimensjonalt array: _tiles[x, y] gir oss ruta på den posisjonen.
    private readonly Tile[,] _tiles;

    // Liste over alle rommene vi har laget.
    private readonly List<Rectangle> _rooms = new();

    // Hvor spilleren skal starte (sentrum av første rom).
    public Point PlayerStart { get; private set; }


    // Forhåndsdefinerte farger så koden under blir ryddigere.
    private static readonly Color FloorColor = new(70, 70, 70);
    private static readonly Color WallColor = new(150, 120, 90);

    public GameMap(int width, int height)
    {
        Width = width;
        Height = height;
        _tiles = new Tile[width, height];
        Generate();
    }

    // Lager et veldig enkelt kart: gulv overalt, vegger rundt kanten.
    // Senere bytter vi denne ut med ekte prosedyre-generering.
    private void Generate()
    {
        var rng = new Random();
        const int maxRooms = 12;
        const int minSize = 5;
        const int maxSize = 10;
        const int maxAttempts = 50;

        // Start med å fylle ALT med vegg. Så graver vi ut rom og ganger.
        for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
                _tiles[x, y] = Wall();

        for (int attempt = 0; attempt < maxAttempts && _rooms.Count < maxRooms; attempt++)
        {
            int w = rng.Next(minSize, maxSize + 1);
            int h = rng.Next(minSize, maxSize + 1);
            int x = rng.Next(1, Width - w - 1);
            int y = rng.Next(1, Height - h - 1);

            var newRoom = new Rectangle(x, y, w, h);

            // Sjekk om det nye rommet overlapper et eksisterende rom.
            // Vi utvider med 1 i alle retninger så det alltid blir minst én vegg
            // mellom to rom.
            var buffer = new Rectangle(newRoom.X - 1, newRoom.Y - 1,
                                       newRoom.Width + 2, newRoom.Height + 2);
            bool overlaps = false;
            foreach (var other in _rooms)
            {
                if (buffer.Intersects(other))
                {
                    overlaps = true;
                    break;
                }
            }
            if (overlaps) continue;

            CarveRoom(newRoom);

            if (_rooms.Count == 0)
            {
                // Første rom: spilleren starter her.
                PlayerStart = newRoom.Center;
            }
            else
            {
                // Koble til forrige rom med L-formet korridor.
                ConnectRooms(_rooms[^1].Center, newRoom.Center, rng);
            }

            _rooms.Add(newRoom);
        }
    }

    // Setter alle ruter inni rektangelet til gulv.
    private void CarveRoom(Rectangle room)
    {
        for (int x = room.X; x < room.X + room.Width; x++)
            for (int y = room.Y; y < room.Y + room.Height; y++)
                _tiles[x, y] = Floor();
    }

    // Graver en L-formet gang mellom to punkter. Vi velger tilfeldig
    // om vi går vannrett først eller loddrett først.
    private void ConnectRooms(Point a, Point b, Random rng)
    {
        if (rng.Next(2) == 0)
        {
            CarveHorizontal(a.X, b.X, a.Y);
            CarveVertical(a.Y, b.Y, b.X);
        }
        else
        {
            CarveVertical(a.Y, b.Y, a.X);
            CarveHorizontal(a.X, b.X, b.Y);
        }
    }

    private void CarveHorizontal(int x1, int x2, int y)
    {
        int start = Math.Min(x1, x2);
        int end = Math.Max(x1, x2);
        for (int x = start; x <= end; x++)
            _tiles[x, y] = Floor();
    }

    private void CarveVertical(int y1, int y2, int x)
    {
        int start = Math.Min(y1, y2);
        int end = Math.Max(y1, y2);
        for (int y = start; y <= end; y++)
            _tiles[x, y] = Floor();
    }

    // "Fabrikk-metoder" som lager en ferdig rute.
    private static Tile Floor() => new()
    {
        Glyph = '.',
        Foreground = FloorColor,
        Background = Color.Black,
        IsWalkable = true
    };

    private static Tile Wall() => new()
    {
        Glyph = '#',
        Foreground = WallColor,
        Background = Color.Black,
        IsWalkable = false
    };

    // Brukes av RootScreen for å sjekke om en flytting er lovlig.
    public bool IsWalkable(int x, int y)
    {
        if (x < 0 || y < 0 || x >= Width || y >= Height) return false;
        return _tiles[x, y].IsWalkable;
    }

    // Tegner hele kartet på en SadConsole-flate.
    public void Render(ICellSurface surface)
    {
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                Tile t = _tiles[x, y];
                surface.SetGlyph(x, y, t.Glyph, t.Foreground, t.Background);
            }
        }
    }
}