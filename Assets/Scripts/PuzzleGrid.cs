using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PuzzleGrid : MonoBehaviour
{

    // This collection holds all Griddable objects on the Grid
    private Dictionary<int, Griddable> Tiles;
    private int NextTileID = 1;
    
    // These collections hold integer keys that correspond with the Dictionary of Griddables
    private int[,] TileGrid;
    private Vector2Int GridSize = new Vector2Int(6, 15);
    private List<int> UnlockedTiles;

    // This collection holds asynchronous update requests to process
    private List<GridRequest> GridRequests;

    // Input-related declarations
    private ControlMap Inputs;
    private Vector2Int CursorPosition = new Vector2Int(2, 4);
    private GameObject CursorObject;

    // Constants
    private const int CEILING_ROW = 12;

    // These properties affect the rendering of Griddables
    public Vector2 GridWorldPosition { get; private set; }
    public float GridScrollOffset { get; private set; }

    #region Unity Events

    void Awake()
    {
        GridWorldPosition = transform.position + new Vector3(1, 0, 0);
        Tiles = new Dictionary<int, Griddable>();
        UnlockedTiles = new List<int>();
        GridRequests = new List<GridRequest>();
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

        // Check Update Requests
        for (int i = 0; i < GridRequests.Count; i++)
        {
            if (GridRequests[i].Type == GridRequestType.Destroy)
            {
                Vector2Int TileCoordinate = GridRequests[i].Coordinate;
                int TileID = TileGrid[TileCoordinate.x, TileCoordinate.y];
                DeleteTileFromGrid(TileCoordinate);
                DestroyUnlockedTile(GetTileByID(TileID));
                GridRequests.Add(new GridRequest { Type = GridRequestType.Update, Coordinate = TileCoordinate});
            }
            if (GridRequests[i].Type == GridRequestType.Update)
            {

                Vector2Int TileCoordinate = GridRequests[i].Coordinate;
                if (TileCoordinate.y > 0)
                {
                    if (CoordinateContainsTile(TileCoordinate) && !CoordinateContainsTile(TileCoordinate + Vector2Int.down))
                    {
                        UnattachTileFromGrid(TileCoordinate);
                        GridRequests.Add(new GridRequest { Type = GridRequestType.Update, Coordinate = TileCoordinate + Vector2Int.up } );
                    }
                }

                if (TileCoordinate.y < GridSize.y - 1)
                {
                    // If the updated Tile is empty, check for an up-neighbor and unattach. Add that tile to the new requests hash
                    if (!CoordinateContainsTile(TileCoordinate) && CoordinateContainsTile(TileCoordinate + Vector2Int.up))
                    {
                        UnattachTileFromGrid(TileCoordinate + Vector2Int.up);
                        GridRequests.Add(new GridRequest { Type = GridRequestType.Update, Coordinate = TileCoordinate + Vector2Int.up });
                    }
                }
            }
        }
        GridRequests.Clear();

        // Run Free Tile Physics
        if (UnlockedTiles.Count != 0) { 
            for(int i = UnlockedTiles.Count - 1; i >= 0; i--)
            {
                int TileKey = UnlockedTiles[i];
                GetTileByID(TileKey).FreeFall();
            } 
        }

        // Scroll Grid
        if (!RowContainsTiles(CEILING_ROW)) Scroll(0.01f);

    }
    
    #endregion

    #region Cursor Methods
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

    void UpdateCursorPosition()
    {
        CursorObject.transform.position = CursorPosition + new Vector2(0, GridScrollOffset) + GridWorldPosition;
    }

    void SwitchAtCursor()
    {

        int CursorX = CursorPosition.x;
        int CursorY = CursorPosition.y;
        int TempValue;

        Griddable TileA = GetTileByGridPosition(CursorPosition);
        Griddable TileB = GetTileByGridPosition(CursorPosition + Vector2Int.right);

        // CONFIRM BOTH GRIDDABLES ARE SWAPPABLE OR NULL, BUT BOTH ARE NOT NULL
        if (TileA != null && !TileA.SwappingAllowed()) return;
        if (TileB != null && !TileB.SwappingAllowed()) return;
        if (TileA == null && TileB == null) return;

        // GENERATE SWAPTEMP TILES IN PLACE OF NULLS
        if (TileA == null)
        {
            AttachTileToGrid(GetTileByID(CreateNewSwapTempTile(CursorPosition)), CursorPosition);
            TileA = GetTileByGridPosition(CursorPosition);
        }
        else if (TileB == null)
        {
            AttachTileToGrid(GetTileByID(CreateNewSwapTempTile(CursorPosition + Vector2Int.right)), CursorPosition + Vector2Int.right);
            TileB = GetTileByGridPosition(CursorPosition + Vector2Int.right);
        }

        // SWAP VALUES
        TempValue = TileGrid[CursorX, CursorY];
        TileGrid[CursorX, CursorY] = TileGrid[CursorX + 1, CursorY];
        TileGrid[CursorX + 1, CursorY] = TempValue;

        // UPDATE POSITIONS
        UpdateTileAtGridCoordinate(CursorX, CursorY);
        UpdateTileAtGridCoordinate(CursorX + 1, CursorY);

        // SET STATES
        TileA.Swap(true);
        TileB.Swap(false);

    }

    #endregion

    #region Tile Methods
    Griddable GetTileByGridPosition(Vector2Int GridPosition)
    {
        int TileKey = TileGrid[GridPosition.x, GridPosition.y];
        return GetTileByID(TileKey);
    }

    Griddable GetTileByID(int TileKey)
    {
        if (TileKey == 0) return null;
        if (!Tiles.TryGetValue(TileKey, out Griddable TileTemp)) Debug.LogError("Grid Tile not Found: " + TileKey);
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

        // INITIALIZE TILES
        Tiles= new Dictionary<int, Griddable>();

        // CREATE STARTING LOCKED TILES
        for (int i = 3; i < GridSize.x; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                TileGrid[i, j] = CreateNewBasicTile(GameAssets.GetRandomTileColor(), new Vector2(i,j), true);
            }
        }

    }

    int CreateNewBasicTile(BasicTile.TileColor _Color, Vector2 _GridPosition, bool _LockedToGrid)
    {
        int TileID = NextTileID;
        Tiles.Add(NextTileID++, new BasicTile(this, TileID, _Color, _GridPosition, _LockedToGrid));
        if (!_LockedToGrid) UnlockedTiles.Add(TileID);
        return TileID;
    }

    int CreateNewSwapTempTile(Vector2 _GridPosition)
    {
        int TileID = NextTileID;
        Tiles.Add(NextTileID++, new SwapTempTile(this, TileID, _GridPosition));
        return TileID;
    }

    void AttachTileToGrid(Griddable _Tile, Vector2Int _GridCoordinate)
    {
        
        // Remove Tile key from Unlocked Tile List
        UnlockedTiles.Remove(_Tile.KeyID);

        // Make sure the TileGrid position is available (Value 0)
        if (TileGrid[_GridCoordinate.x, _GridCoordinate.y] != 0) Debug.LogError("Attempted to attach Tile to occupied grid-space.");

        // Attach Tile to Grid And Update Position
        TileGrid[_GridCoordinate.x, _GridCoordinate.y] = _Tile.KeyID;
        UpdateTileAtGridCoordinate(_GridCoordinate.x, _GridCoordinate.y);

        // Notice to Tile of Unattachment
        _Tile.Attach(_GridCoordinate);

    }

    void UnattachTileFromGrid(Vector2Int _GridCoordinate)
    {

        int TileID = TileGrid[_GridCoordinate.x, _GridCoordinate.y];

        // Make sure the grid position is not empty (Value 0)
        if (TileID == 0) Debug.LogError("Attempted to unattach a non-existent Tile.");

        // Add Tile key to Unlocked Tile List
        UnlockedTiles.Add(TileID);

        // Remove Tile to Grid
        TileGrid[_GridCoordinate.x, _GridCoordinate.y] = 0;

        // Notice to Tile of Unattachment
        GetTileByID(TileID).Unattach();

    }

    void DeleteTileFromGrid(Vector2Int _GridPosition)
    {

        int TileID = TileGrid[_GridPosition.x, _GridPosition.y];

        // Make sure the grid position is not empty (Value 0)
        if (TileID == 0) Debug.LogError("Attempted to unattach a non-existent Tile.");

        // Remove Tile to Grid
        TileGrid[_GridPosition.x, _GridPosition.y] = 0;

    }

    void UpdateGridTiles()
    {
        for (int j = 0; j < GridSize.y; j++)
        {
            for (int i = 0; i < GridSize.x; i++)
            {
                UpdateTileAtGridCoordinate(i, j);
            }
        }
    }

    void UpdateTileAtGridCoordinate(int x, int y)
    {
        int TileKey = TileGrid[x, y];
        if (TileKey != 0)
        {
            if (!Tiles.TryGetValue(TileKey, out Griddable TileTemp)) Debug.LogError("Grid Tile not Found: " + TileGrid[x, y]);
            TileTemp.SetGridPosition(new Vector2(x, y + GridScrollOffset));
            TileTemp.ChangeAttachmentCoordinate(new Vector2Int(x, y));
        }
    }

    public int GetTileKeyAtGridCoordinate(Vector2Int _GridCoordinate)
    {
        return TileGrid[_GridCoordinate.x, _GridCoordinate.y];
    }

    public void RequestAttachment(Griddable _Tile, Vector2Int _GridPosition)
    {
        if (TileGrid[_GridPosition.x, _GridPosition.y] != 0) Debug.LogError("Tile requested attachment to an occupied grid-space.");
        AttachTileToGrid(_Tile, _GridPosition);
    }

    private bool RowContainsTiles(int RowIndex)
    {
        for (int i = 0; i < GridSize.x; i++)
        {
            if (TileGrid[i, RowIndex] != 0) return true;
        }
        return false;
    }

    private bool CoordinateContainsTile(Vector2Int _GridCoordinate)
    {
        return (TileGrid[_GridCoordinate.x, _GridCoordinate.y] != 0);
    }

    public void PingUpdate(Griddable _Tile)
    {
        if (!_Tile.LockedToGrid) Debug.LogError("A Free-Falling Tile pinged for update. This should only happen for on-grid Tiles.");
        Vector2Int _TileCoordinate = _Tile.GridCoordinate;
        GridRequests.Add(new GridRequest { Type = GridRequestType.Update, Coordinate = _TileCoordinate });
    }

    public void DestroyRequest(Griddable _Tile) {
        // If Tile is unlocked, destroy immediately, otherwise add destroy request to GridRequests
        if (!_Tile.LockedToGrid) DestroyUnlockedTile(_Tile);
        else
        {
            GridRequests.Add(new GridRequest { Type = GridRequestType.Destroy, Coordinate = _Tile.GridCoordinate });
        }
    }

    public void DestroyUnlockedTile(Griddable _Tile)
    {
        int KeyID = _Tile.KeyID;
        _Tile.Destroy();
        UnlockedTiles.Remove(KeyID);
        Tiles.Remove(KeyID);
    }

    #endregion

    #region Scrolling

    void ShiftGridUp()
    {
        // SHIFT LOCKED TILES UP
        for (int j = GridSize.y - 1; j >= 0; j--)
        {
            for (int i = 0; i < GridSize.x; i++)
            {
                if (j > 0) TileGrid[i, j] = TileGrid[i, j - 1];
                else TileGrid[i, j] = CreateNewBasicTile(GameAssets.GetRandomTileColor(), new Vector2(i, j), true);
            }
        }
        CursorPosition.y += 1;
        foreach (GridRequest _GridRequest in GridRequests) _GridRequest.ShiftReference(1);
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
        foreach (GridRequest _GridRequest in GridRequests) _GridRequest.ShiftReference(-1);
    }

    void Scroll(float _ScrollAmount)
    {

        // SHIFT UNLOCKED TILE POSITIONS
        foreach (Griddable Tile in Tiles.Values)
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

    #endregion

}
