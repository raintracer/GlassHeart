using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PuzzleGrid : MonoBehaviour
{

    // This collection holds all Griddable objects on the Grid
    private Dictionary<int, Griddable> Tiles;
    private int NextTileID = 1;
    float ScrollSpeed = 0.1f;
    
    // These collections hold integer keys that correspond with the Dictionary of Griddables
    private int[,] TileGrid;
    private Vector2Int GridSize = new Vector2Int(6, 15);
    private List<int> UnlockedTiles;

    // This collection holds tiles are clearing
    private HashSet<int> ClearingTiles = new HashSet<int>();

    // Clear set logic
    private List<ClearSet> ClearSets = new List<ClearSet>();
    private int ChainLevel = 0;

    // This collection holds asynchronous update requests to process
    private List<GridRequest> GridRequests;

    // Input-related declarations
    private ControlMap Inputs;
    private Vector2Int CursorPosition = new Vector2Int(2, 4);
    private GameObject CursorObject;
    private bool CusorSwitchFlag = false;
    private bool ScrollBoostInput = false;
    private bool ScrollBoostLock = false;
    private int FastScrollCounter = 0;
    private Vector2 Movement = Vector2.zero;
    private Vector2 LastMovement = Vector2.zero;

    // Constants
    private const int CEILING_ROW = 13;
    private const int FLOOR_ROW = 2;
    private const float SCROLL_BOOST_FACTOR = 20f;
    private const int FAST_SCROLL_FRAMES = 10;
    private const int DANGER_ROW = 11;

    // These properties affect the rendering of Griddables
    public Vector2 GridWorldPosition { get; private set; }
    public float GridScrollOffset { get; private set; }

    // Tile screen handlers
    GameObject TileScreenObject;

    #region Unity Events

    void Awake()
    {
        GridWorldPosition = transform.position + new Vector3(1, -1, 0);
        Tiles = new Dictionary<int, Griddable>();
        UnlockedTiles = new List<int>();
        GridRequests = new List<GridRequest>();
        CreateInitialTileGrid();
        
        CursorObject = Instantiate(Resources.Load<GameObject>("PuzzleCursorPrefab"), transform);
        UpdateCursorPosition();

        // INITIALIZE CONTROLS
        Inputs = new ControlMap();
        Inputs.Enable();
        Inputs.Player.MoveCursor.performed += ctx => Movement = ctx.ReadValue<Vector2>();
        Inputs.Player.SwitchAtCursor.started += ctx => CusorSwitchFlag = true;
        Inputs.Player.ScrollBoost.performed += ctx => ScrollBoostInput = true;
        Inputs.Player.ScrollBoost.canceled += ctx => ScrollBoostInput = false;

        // Instantiate screen for inactive tiles
        TileScreenObject = Instantiate(Resources.Load<GameObject>("TileScreen"), transform);

        // Start Music
        GameAssets.Sound.StoneRock.Play();
    }

    //private void OnDraw()
    //{
    //    // Draw Debug Grid
    //    Gizmos.color = Color.red;
    //    for (int i = 0; i < GridSize.x; i++)
    //    {
    //        for (int j = 0; j < GridSize.y; j++)
    //        {
    //            if (TileGrid[i,j] != 0)
    //            {
    //                Gizmos.DrawWireCube((Vector3)GridWorldPosition + new Vector3(0, GridScrollOffset, 0) + new Vector3(i, j, 0) + Vector3.up / 2 + Vector3.right / 2, Vector3.one);
    //            }
    //        }
    //    }
    //}

    void FixedUpdate()
    {

        // Move Cursor
        HandleCursorMovement();

        // Process Grid Requests
        ProcessGridRequests();

        // Swap at cursor
        if (CusorSwitchFlag)
        {
            CusorSwitchFlag = false;
            SwitchAtCursor();
        }

        // Run Free Tile Physics
        if (UnlockedTiles.Count != 0)
        {
            // Sort FreeTiles by grid y position in ascending order
            UnlockedTiles.Sort(CompareFreeTileHeightAscending);
            for (int i = UnlockedTiles.Count - 1; i >= 0; i--)
            {
                int TileKey = UnlockedTiles[i];
                GetTileByID(TileKey).FreeFall();
            }
        }

        // Scroll Grid - Lock scroll if there are clearing or falling tiles
        if (UnlockedTiles.Count == 0 && ClearingTiles.Count == 0)
        {

            // If the scroll button is pressed while scrolling is legal, lock in the boost scroll speed until another row of tiles is created.
            if (ScrollBoostInput) ScrollBoostLock = true;

            float ScrollAmount = ScrollSpeed * Time.fixedDeltaTime;
            if (ScrollBoostLock) ScrollAmount *= SCROLL_BOOST_FACTOR;
            if (!RowContainsLockedTiles(CEILING_ROW)) Scroll(ScrollAmount);

        }

        // Check for tiles to clear
        ProcessClearing();

        // Reset all locked tile's chain level if they are not clearing and are not over a swapping tile
        for (int i = 0; i < GridSize.x; i++)
        {
            for (int j = 1; j < GridSize.y; j++)
            {
                if (TileGrid[i,j] != 0) // Ignore empty tiles
                {
                    Griddable _Tile = GetTileByGridCoordinate(new Vector2Int(i, j));
                    if (!_Tile.IsClearing())
                    {
                        if (TileGrid[i, j-1] == 0 || !GetTileByGridCoordinate(new Vector2Int(i, j-1)).IsSwapping())
                        {
                            _Tile.ResetChainLevel();
                        }
                    }
                }
            }

        }

        // If Chain Level is 1, check to clear the chain level
        CheckForEndOfChain();

        // Reposition Tile Screen
        TileScreenObject.transform.position = GridWorldPosition + Vector2.up * (FLOOR_ROW + GridScrollOffset - 1);

        // Determine Columns that should bounce
        for(int i = 0; i < GridSize.x; i++)
        {
            for (int j = 0; j < GridSize.y; j++)
            {
                Griddable _Tile = GetTileByGridCoordinate(new Vector2Int(i, j));
                if (_Tile != null)
                {
                    if (ColumnInDanger(i))
                    {

                        _Tile.RequestBounceStart();
                    }
                    else
                    {
                        _Tile.RequestBounceStop();
                    }
                }
            }
        }

    }

    private bool ColumnInDanger(int _Column)
    {
        return (TileGrid[_Column, DANGER_ROW] != 0);
    }

    private void ProcessGridRequests()
    {

        for (int i = 0; i < GridRequests.Count; i++)
        {

            if (GridRequests[i].Type == GridRequestType.Destroy)
            {

                Vector2Int TileCoordinate = GridRequests[i].Coordinate;
                int TileID = TileGrid[TileCoordinate.x, TileCoordinate.y];
                GridRequests.Add(new GridRequest { Type = GridRequestType.Update, Coordinate = TileCoordinate, ChainLevel = GridRequests[i].ChainLevel });
                UnattachTileFromGrid(TileCoordinate);
                DestroyUnlockedTile(GetTileByID(TileID));
                
            }

            if (GridRequests[i].Type == GridRequestType.Update)
            {

                // Fall if there is a tile and no tile underneath 
                Vector2Int TileCoordinate = GridRequests[i].Coordinate;
                if (TileCoordinate.y > 0)
                {
                    if (CoordinateContainsLockedTile(TileCoordinate) && !CoordinateContainsLockedTile(TileCoordinate + Vector2Int.down))
                    {
                        Griddable _Tile = GetTileByGridCoordinate(TileCoordinate);
                        if (_Tile.FallAllowed())
                        {
                            _Tile.SetChain(GridRequests[i].ChainLevel);
                            UnattachTileFromGrid(TileCoordinate);
                            if (TileCoordinate.y < GridSize.y - 1) GridRequests.Add(new GridRequest { Type = GridRequestType.Update, Coordinate = TileCoordinate + Vector2Int.up, ChainLevel = GridRequests[i].ChainLevel });
                        }
                    }
                }

                // Tell above tile to update/fall if there is no tile, and there is a tile above
                if (TileCoordinate.y < GridSize.y - 1)
                {
                    // If the updated Tile is empty, check for an up-neighbor and unattach. Add that tile to the new requests hash
                    if (!CoordinateContainsLockedTile(TileCoordinate) && CoordinateContainsLockedTile(TileCoordinate + Vector2Int.up))
                    {
                        GridRequests.Add(new GridRequest { Type = GridRequestType.Update, Coordinate = TileCoordinate + Vector2Int.up, ChainLevel = GridRequests[i].ChainLevel });
                    }
                }
            }
        }

        GridRequests.Clear();

    }

    private void HandleCursorMovement()
    {

        // Handle Cursor Movement
        if (Movement != Vector2.zero)
        {

            if (Movement != LastMovement)
            {
                FastScrollCounter = 0;
            }
            else
            {
                FastScrollCounter++;
            }

            if (FastScrollCounter == 0)
            {
                MoveCursor(Movement, false);
            }
            else if (FastScrollCounter >= FAST_SCROLL_FRAMES)
            {
                MoveCursor(Movement, true);
            }

        }
        else
        {
            FastScrollCounter = 0;
        }

        LastMovement = Movement;

    }

    private void ProcessClearing() // Check for matches and set tiles to clear
    {

        // Create Nullable Color Map
        BasicTile.TileColor?[,] ColorGrid = new BasicTile.TileColor?[GridSize.x, GridSize.y];
        for (int j = CEILING_ROW; j >= FLOOR_ROW; j--)
        {
            for (int i = 0; i < GridSize.x; i++)
            {
                Griddable _Tile = GetTileByGridCoordinate(new Vector2Int(i, j));
                if (_Tile != null && _Tile.Type == Griddable.TileType.Basic && _Tile.ClearAllowed())
                {
                    BasicTile _BasicTile = _Tile as BasicTile;
                    ColorGrid[i, j] = _BasicTile.Color;
                }
                else
                {
                    ColorGrid[i, j] = null;
                }
            }
        }

        // Iterate over color map for matches to add to HashSet
        HashSet<Vector2Int> ClearedCoordinatesHash = new HashSet<Vector2Int>();
        for (int j = CEILING_ROW; j >= FLOOR_ROW; j--)
        {
            for (int i = 0; i < GridSize.x; i++)
            {
                BasicTile.TileColor? OriginColor = ColorGrid[i, j];
                if (OriginColor == null) continue;
                if (j >= FLOOR_ROW + 2)
                {
                    if (ColorGrid[i, j - 1] == OriginColor && ColorGrid[i, j - 2] == OriginColor)
                    {
                        ClearedCoordinatesHash.Add(new Vector2Int(i, j - 0));
                        ClearedCoordinatesHash.Add(new Vector2Int(i, j - 1));
                        ClearedCoordinatesHash.Add(new Vector2Int(i, j - 2));
                    }
                }
                if (i < GridSize.x - 2)
                {
                    if (ColorGrid[i + 1, j] == OriginColor && ColorGrid[i + 2, j] == OriginColor)
                    {
                        ClearedCoordinatesHash.Add(new Vector2Int(i + 0, j));
                        ClearedCoordinatesHash.Add(new Vector2Int(i + 1, j));
                        ClearedCoordinatesHash.Add(new Vector2Int(i + 2, j));
                    }
                }
            }
        }


        if (ClearedCoordinatesHash.Count > 0)
        {

            // Order Cleared Tiles In a List
            List<Vector2Int> ClearedCoordinatesList = new List<Vector2Int>(ClearedCoordinatesHash);
            ClearedCoordinatesList.Sort(CompareCoordinatesByClearOrderAscending);

            // Temporary - Remove Cleared Coordinates and check for chain
            int ListCount = ClearedCoordinatesList.Count;
            int HighestChain = 0;

            for (int i = 0; i < ListCount; i++)
            {
                Vector2Int _TileCoordinate = ClearedCoordinatesList[i];
                Griddable _Tile = GetTileByGridCoordinate(_TileCoordinate);
                if (_Tile.ChainLevel > HighestChain) HighestChain = _Tile.ChainLevel;
                _Tile.Clear(i, ListCount);
                ClearingTiles.Add(_Tile.KeyID);
            }

            // Check for Combo
            if (ClearedCoordinatesList.Count > 3)
            {
                GameAssets.Sound.Combo1.Play();
                GameObject CounterObject = Instantiate(Resources.Load<GameObject>("TechCounterObject"));
                TechCounter Counter = CounterObject.GetComponent<TechCounter>();
                Griddable FirstTile = GetTileByGridCoordinate(ClearedCoordinatesList[0]);
                Counter.StartEffect(TechCounter.TechType.Combo, ClearedCoordinatesHash.Count, (Vector2)FirstTile.GetWorldPosition() + new Vector2(0.5f, 1.75f));
            }

            // Check for chain
            if (HighestChain > 0)
            {
                GameAssets.Sound.Combo1.Play();
                ChainLevel++;
                GameObject CounterObject = Instantiate(Resources.Load<GameObject>("TechCounterObject"));
                TechCounter Counter = CounterObject.GetComponent<TechCounter>();
                Griddable FirstTile = GetTileByGridCoordinate(ClearedCoordinatesList[0]);

                // Offset the chain tech counter if there was also a combo
                Vector2 ComboOffset = Vector2.zero;
                if (ClearedCoordinatesList.Count > 3) ComboOffset = new Vector2(0f, 1f);

                Counter.StartEffect(TechCounter.TechType.Chain, ChainLevel + 1, (Vector2)FirstTile.GetWorldPosition() + ComboOffset + new Vector2(0.5f, 1.75f));
            }

                // Create Clear Set (Is this necessary?)
                // ClearSets.Add(new ClearSet(ClearedCoordinatesList));

                ClearedCoordinatesHash.Clear();

        }
    }

    private void CheckForEndOfChain()
    {
        bool ContinueChain = false;
        foreach(Griddable _Tile in Tiles.Values)
        {
            if (_Tile.ChainLevel > 1 || _Tile.IsClearing())
            {
                ContinueChain = true;
                break;
            }
        }
        if (!ContinueChain) ChainLevel = 0;
    }

    private int CompareCoordinatesByClearOrderAscending(Vector2Int CoordinateA, Vector2Int CoordinateB)
    {
        if (CoordinateA == CoordinateB) return 0;
        if (CoordinateA.y == CoordinateB.y)
        {
            return CoordinateA.x.CompareTo(CoordinateB.x);
        }
        else
        {
            return CoordinateB.y.CompareTo(CoordinateA.y);
        }
    }

    #endregion

    #region Cursor Methods
    void MoveCursor(Vector2 _Movement, bool FastScroll = false)
    {
        if (_Movement.SqrMagnitude() > 0) {
            Vector2Int OldCursorPosition = CursorPosition;
            CursorPosition += new Vector2Int((int)_Movement.x, (int)_Movement.y);
            CursorPosition.Clamp(new Vector2Int(0, FLOOR_ROW), new Vector2Int(GridSize.x - 2, CEILING_ROW));
            UpdateCursorPosition();
            if (!FastScroll && CursorPosition != OldCursorPosition) GameAssets.Sound.CursorClick.Play(); 
        }
        LastMovement = _Movement;
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

        // CONFIRM NEITHER GRID-SPACE CONTAINS A FREE TILE
        if (CoordinateContainsFreeTile(CursorPosition) || CoordinateContainsFreeTile(CursorPosition + Vector2Int.right)) return;

        // CAPTURE TILES CORRESPONDING TO THE GRID-SPACES
        Griddable TileA = GetTileByGridCoordinate(CursorPosition);
        Griddable TileB = GetTileByGridCoordinate(CursorPosition + Vector2Int.right);

        // CONFIRM BOTH GRIDDABLES ARE SWAPPABLE OR NULL, BUT BOTH ARE NOT NULL
        if (TileA != null && !TileA.SwappingAllowed()) return;
        if (TileB != null && !TileB.SwappingAllowed()) return;
        if (TileA == null && TileB == null) return;

        // GENERATE SWAPTEMP TILES IN PLACE OF NULLS
        if (TileA == null)
        {
            AttachTileToGrid(GetTileByID(CreateNewSwapTempTile(CursorPosition)), CursorPosition);
            TileA = GetTileByGridCoordinate(CursorPosition);
        }
        else if (TileB == null)
        {
            AttachTileToGrid(GetTileByID(CreateNewSwapTempTile(CursorPosition + Vector2Int.right)), CursorPosition + Vector2Int.right);
            TileB = GetTileByGridCoordinate(CursorPosition + Vector2Int.right);
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

        // Play Swap Sound
        GameAssets.Sound.Swap.Play();

    }

    #endregion

    #region Tile Methods
    Griddable GetTileByGridCoordinate(Vector2Int GridPosition)
    {
        int TileKey = TileGrid[GridPosition.x, GridPosition.y];
        return GetTileByID(TileKey);
    }

    private int CompareFreeTileHeightAscending(int TileAID, int TileBID)
    {
        float TileAY = GetTileByID(TileAID).GridPosition.y;
        float TileBY = GetTileByID(TileBID).GridPosition.y;
        return TileBY.CompareTo(TileAY);
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
        for (int i = 0; i < GridSize.x; i++)
        {
            for (int j = 0; j < i*2 + 2; j++)
            {
                AttachTileToGrid(GetTileByID(CreateNewBasicTile(GameAssets.GetRandomTileColor(), new Vector2(i,j), true)), new Vector2Int(i, j));
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
        if (TileGrid[_GridCoordinate.x, _GridCoordinate.y] != 0)
        {
            Debug.LogError("Attempted to attach Tile to occupied grid-space: " + _GridCoordinate + ". Occupying Tile Type: " + GetTileByID(TileGrid[_GridCoordinate.x, _GridCoordinate.y]).Type);
        }

        // Attach Tile to Grid And Update Position
        TileGrid[_GridCoordinate.x, _GridCoordinate.y] = _Tile.KeyID;
        UpdateTileAtGridCoordinate(_GridCoordinate.x, _GridCoordinate.y);

        // Notice to Tile of Unattachment
        _Tile.Attach(_GridCoordinate);

    }

    void UnattachTileFromGrid(Vector2Int _GridCoordinate)
    {

        int TileID = TileGrid[_GridCoordinate.x, _GridCoordinate.y];
        UpdateTileAtGridCoordinate(_GridCoordinate.x, _GridCoordinate.y);

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
            TileTemp.SetGridPosition(new Vector2(x, y));
            TileTemp.ChangeAttachmentCoordinate(new Vector2Int(x, y));
        }
    }

    public int GetTileKeyAtGridCoordinate(Vector2Int _GridCoordinate)
    {
        return TileGrid[_GridCoordinate.x, _GridCoordinate.y];
    }

    public bool RequestAttachment(Griddable _Tile, Vector2Int _GridCoordinate)
    {
        if (TileGrid[_GridCoordinate.x, _GridCoordinate.y] != 0)
        {
            Debug.LogWarning("Tile requested attachment to an occupied grid-space.");
            return false;
        }

        AttachTileToGrid(_Tile, _GridCoordinate);
        return true;
    }

    private bool RowContainsLockedTiles(int RowIndex)
    {
        for (int i = 0; i < GridSize.x; i++)
        {
            if (TileGrid[i, RowIndex] != 0) return true;
        }
        return false;
    }

    private bool CoordinateContainsLockedTile(Vector2Int _GridCoordinate)
    {
        return (TileGrid[_GridCoordinate.x, _GridCoordinate.y] != 0);
    }

    private bool CoordinateContainsFreeTile(Vector2Int _GridCoordinate)
    {
        Griddable _Tile;
        float tx, ty, tw, th;                   // Tile Position and Dimensions
        float lx = _GridCoordinate.x + 0.5f;    // Grid-space midpoint x
        float pay = _GridCoordinate.y + 1f;     // Gris-space top y
        float pby = _GridCoordinate.y;          // Grid-space bottom y
        for (int i = 0; i < UnlockedTiles.Count; i++)
        {
            _Tile = GetTileByID(UnlockedTiles[i]);
            tx = _Tile.GridPosition.x;
            ty = _Tile.GridPosition.y;
            tw = 1f;
            th = 1f;

            // If tile box overlaps vertical line in gridspace horizontal center, return true
            if (tx < lx && (tx + tw > lx) && ((ty > pby && ty < pay) || (ty + th > pby && ty + th < pay))) return true;

        }
        return false; // Return false if no free tiles meet conditions
    }

    public void PingUpdate(Griddable _Tile)
    {
        if (!_Tile.LockedToGrid) Debug.LogError("A Free-Falling Tile pinged for update. This should only happen for on-grid Tiles.");
        Vector2Int _TileCoordinate = _Tile.GridCoordinate;
        GridRequests.Add(new GridRequest { Type = GridRequestType.Update, Coordinate = _TileCoordinate });
    }

    public void DestroyRequest(Griddable _Tile, bool _Chain) {
        // If Tile is unlocked, destroy immediately, otherwise add destroy request to GridRequests
        if (!_Tile.LockedToGrid) DestroyUnlockedTile(_Tile);
        else
        {
            if (_Chain) GridRequests.Add(new GridRequest { Type = GridRequestType.Destroy, Coordinate = _Tile.GridCoordinate, ChainLevel = _Tile.ChainLevel + 1 });
            else GridRequests.Add(new GridRequest { Type = GridRequestType.Destroy, Coordinate = _Tile.GridCoordinate, ChainLevel = 0 });
        }
    }

    public void DestroyUnlockedTile(Griddable _Tile)
    {
        int KeyID = _Tile.KeyID;
        UnlockedTiles.Remove(KeyID);
        ClearingTiles.Remove(KeyID);
        Tiles.Remove(KeyID);
        _Tile.Destroy();
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
                else
                {
                    TileGrid[i, j] = 0;
                    AttachTileToGrid(GetTileByID(CreateNewBasicTile(GameAssets.GetRandomTileColor(), new Vector2(i, j), true)), new Vector2Int(i, j));
                }
            }
        }

        // SHIFT FREE TILES UP
        for (int i = 0; i < UnlockedTiles.Count; i++)
        {
            GetTileByID(UnlockedTiles[i]).ShiftPosition(1f);
        }

        if (CursorPosition.y < CEILING_ROW) CursorPosition.y += 1;
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

        // SHIFT FREE TILES UP
        for (int i = 0; i < UnlockedTiles.Count; i++)
        {
            GetTileByID(UnlockedTiles[i]).ShiftPosition(1f);
        }

        if (CursorPosition.y > FLOOR_ROW) CursorPosition.y += 1;
        foreach (GridRequest _GridRequest in GridRequests) _GridRequest.ShiftReference(-1);
    }

    void Scroll(float _ScrollAmount)
    {

        // SHIFT UNLOCKED TILE POSITIONS
        //foreach (Griddable Tile in Tiles.Values)
        //{
        //    if (!Tile.LockedToGrid) Tile.ShiftPosition(_ScrollAmount);
        //}

        // SHIFT GRID IF NECESSARY
        GridScrollOffset += _ScrollAmount;
        while (GridScrollOffset >= 1.0)
        {
            GridScrollOffset--;
            ShiftGridUp();
            ScrollBoostLock = false;
        }
        while (GridScrollOffset < 0)
        {
            GridScrollOffset++;
            ShiftGridDown();
            ScrollBoostLock = false;
        }

        UpdateGridTiles();
        UpdateCursorPosition();
    }

    #endregion

}
