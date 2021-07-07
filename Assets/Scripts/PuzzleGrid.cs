using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PuzzleGrid : MonoBehaviour
{

    private Dictionary<int, PuzzleTile> Tiles;
    public Vector2 GridWorldPosition { get; private set; }
    public float GridScrollOffset { get; private set; }
    
    private int[,] TileGrid;
    Vector2Int GridSize = new Vector2Int(6, 15);
    private int NextTileID = 1;
    private const int CEILING_ROW = 12;

    ControlMap Inputs;

    // CURSOR
    private Vector2Int CursorPosition = new Vector2Int(2, 4);
    GameObject CursorObject;

    void Awake()
    {
        GridWorldPosition = transform.position + new Vector3(1, 0, 0);
        Tiles = new Dictionary<int, PuzzleTile>();
        CreateInitialTileGrid();
        
        CursorObject = Instantiate(Resources.Load<GameObject>("PuzzleCursorPrefab"), transform);
        UpdateCursorPosition();

        // INITIALIZE CONTROLS
        Inputs = new ControlMap();
        Inputs.Enable();
        Inputs.Player.MoveCursor.performed += ctx => MoveCursor(ctx.ReadValue<Vector2>());
        Inputs.Player.SwitchAtCursor.started += ctx => SwitchAtCursor();

    }

    void FixedUpdate()
    {
        if (!RowContainsTiles(CEILING_ROW)) Scroll(0.01f);
    }

    void MoveCursor(Vector2 _Movement)
    {
        if (_Movement.SqrMagnitude() > 0) {
            Vector2Int OldCursorPosition = CursorPosition;
            CursorPosition += new Vector2Int((int)_Movement.x, (int)_Movement.y);
            CursorPosition.Clamp(new Vector2Int(0, 1), new Vector2Int(GridSize.x - 2, CEILING_ROW));
            UpdateCursorPosition();
            //if (CursorPosition != OldCursorPosition) GameAssets.Sound.CursorClick.Play();
        }
    }

    void SwitchAtCursor()
    {

        int CursorX = CursorPosition.x;
        int CursorY = CursorPosition.y;
        int TempValue;
        TempValue = TileGrid[CursorX, CursorY];
        TileGrid[CursorX, CursorY] = TileGrid[CursorX + 1, CursorY];
        TileGrid[CursorX + 1, CursorY] = TempValue;

        UpdateTileAtGridPosition(CursorX, CursorY);
        UpdateTileAtGridPosition(CursorX + 1, CursorY);
    }

    PuzzleTile GetTileByGridPosition(Vector2Int GridPosition)
    {
        int TileKey = TileGrid[GridPosition.x, GridPosition.y];
        if (TileKey == 0) return null;
        if (!Tiles.TryGetValue(TileGrid[GridPosition.x, GridPosition.y], out PuzzleTile TileTemp)) Debug.LogError("Grid Tile not Found: " + TileGrid[GridPosition.x, GridPosition.y]);
        return TileTemp;
    }

    void CreateInitialTileGrid()
    {

        // INITIALIZE LOCKED TILE ARRAY
        TileGrid = new int[GridSize.x, GridSize.y];
        for (int i = 0; i < GridSize.x; i++)
        {
            for (int j = 0; j < GridSize.y; j++)
            {
                TileGrid[i, j] = 0;
            }
        }

        // INITIALIZE UNLOCKED TILES
        Tiles= new Dictionary<int, PuzzleTile>();

        // CREATE STARTING LOCKED TILES
        for (int i = 0; i < GridSize.x; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                TileGrid[i, j] = CreateNewTile(GameAssets.GetRandomTileColor(), new Vector2(i,j), true);
            }
        }

    }

    int CreateNewTile(PuzzleTile.TileColor _Color, Vector2 _GridPosition, bool _LockedToGrid)
    {
        int TileID = NextTileID;
        Tiles.Add(NextTileID++, new PuzzleTile(this, TileID, _Color, _GridPosition, _LockedToGrid));
        return TileID;
    }

    void AttachTileToGrid(PuzzleTile _Tile, Vector2Int _GridPosition)
    {
        // W R I T E   T H I S
    }

    void UnattachTileFromGrid(Vector2Int _GridPosition)
    {
        // W R I T E   T H I S
    }

    void ShiftGridUp ()
    {
        // SHIFT LOCKED TILES UP
        for (int j = GridSize.y - 1; j >= 0; j--)
        {
            for (int i = 0; i < GridSize.x; i++)
            {
                if (j > 0) TileGrid[i, j] = TileGrid[i, j - 1];
                else TileGrid[i, j] = CreateNewTile(GameAssets.GetRandomTileColor(), new Vector2(i, j), true);
            }
        }
        CursorPosition.y += 1;
    }

    void ShiftGridDown()
    {
        // SHIFT LOCKED TILES DOWN
        for (int j = 0; j < GridSize.y; j++)
        {
            for (int i = 0; i < GridSize.x; i++)
            {
                if (j == (GridSize.y - 1)) TileGrid[i, j] = 0;
                else TileGrid[i, j] = TileGrid[i, j + 1];
            }
        }
        CursorPosition.y -= 1;
    }

    void Scroll(float _ScrollAmount)
    {

        // SHIFT UNLOCKED TILE POSITIONS
        foreach (PuzzleTile Tile in Tiles.Values)
        {
            if (!Tile.LockedToGrid) Tile.ShiftPosition(_ScrollAmount);
        }

        // SHIFT GRID IF NECESSARY
        GridScrollOffset += _ScrollAmount;
        while (GridScrollOffset >= 1.0)
        {
            ShiftGridUp();
            GridScrollOffset--;
        }
        while (GridScrollOffset < 0)
        {
            ShiftGridDown();
            GridScrollOffset++;
        }

        UpdateGridTiles();

        UpdateCursorPosition();
    }

    void UpdateCursorPosition()
    {
        CursorObject.transform.position = CursorPosition + new Vector2(0, GridScrollOffset) + GridWorldPosition;
    }

    void UpdateGridTiles()
    {
        for (int j = 0; j < GridSize.y; j++)
        {
            for (int i = 0; i < GridSize.x; i++)
            {
                UpdateTileAtGridPosition(i, j);
            }
        }
    }

    void UpdateTileAtGridPosition(int x, int y)
    {
        int TileKey = TileGrid[x, y];
        if (TileKey != 0)
        {
            if (!Tiles.TryGetValue(TileKey, out PuzzleTile TileTemp)) Debug.LogError("Grid Tile not Found: " + TileGrid[x, y]);
            TileTemp.SetGridPosition(new Vector2(x, y + GridScrollOffset));
        }
    }

    bool RowContainsTiles(int RowIndex)
    {
        for (int i = 0; i < GridSize.x; i++)
        {
            if (TileGrid[i, RowIndex] != 0) return true;
        }
        return false;
    }

}
