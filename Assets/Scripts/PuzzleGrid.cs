using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// The main player object. Handles inputs and all activity within the player grid-space.
/// This includes all spawning of objects.
/// </summary>
public class PuzzleGrid : MonoBehaviour
{

    // This collection holds all Griddable objects on the Grid
    private Dictionary<int, Griddable> Tiles = new Dictionary<int, Griddable>();
    public Vector2Int GridSize = new Vector2Int(6, 15);
    private int NextTileID = 1;
    private int NextBlockID = 1;

    // These collections hold integer keys that correspond with the Dictionary of Griddables
    public int[,] TileGrid;
    private List<int> UnlockedTiles = new List<int>();

    // This collection holds the block entities
    private Dictionary<int, Block> Blocks = new Dictionary<int, Block>();
    private List<Vector2Int> BlockQueue = new List<Vector2Int>();

    // This collection holds tiles are clearing
    private HashSet<int> ClearingTiles = new HashSet<int>();

    // Clear set logic
    private List<ClearSet> ClearSets = new List<ClearSet>();
    public int ChainLevel { get; private set; } = 0;

    // This collection holds asynchronous update requests to process
    public List<GridRequest> GridRequests = new List<GridRequest>();

    // Input-related declarations
    private ControlMap Inputs;
    private Vector2Int CursorPosition = new Vector2Int(2, 4);
    private GameObject CursorObject;
    private Vector2 Movement = Vector2.zero;
    private Vector2 LastMovement = Vector2.zero;
    private int FastScrollCounter = 0;
    private bool CusorSwitchFlag = false;
    private bool ScrollBoostInput = false;
    private bool ScrollBoostLock = false;

    // Constants
    private const int CEILING_ROW = 13;
    private const int FLOOR_ROW = 2;
    private const float SCROLL_BOOST_FACTOR = 20f;
    private const int FAST_SCROLL_FRAMES = 10;
    private const int DANGER_ROW = 11;
    private const int BLOCK_SPAWN_ROW = 14;
    private const float SCROLL_SPEED_BASE = 0.1f;

    // Player properties
    private float Health = 2f;
    private float StopTime = 0f;

    // Rendering fields

    /// <summary> Describes the relative position of the grid-space to world-space. </summary>
    public Vector2 GridWorldPosition { get; private set; }

    /// <summary>
    /// Holds a float between 0 and 1 that is responsible for smoothing the movement of the grid up and down.
    /// Once it reaches 1 or higher, all grid references are shifted by one position, and this resets.
    /// </summary>
    public float GridScrollOffset { get; private set; }

    // Tile screen handlers
    GameObject TileScreenObject;

    #region Unity Events

    void Awake()
    {

        // Initialize Grid
        GridWorldPosition = transform.position + new Vector3(1, -1, 0);
        CreateInitialTileGrid();

        // Initialize cursor object
        CursorObject = Instantiate(Resources.Load<GameObject>("PuzzleCursorPrefab"), transform);
        UpdateCursorPosition();

        // Initialize controls
        Inputs = new ControlMap();
        Inputs.Enable();
        Inputs.Player.MoveCursor.performed += ctx => Movement = ctx.ReadValue<Vector2>();
        Inputs.Player.SwitchAtCursor.started += ctx => CusorSwitchFlag = true;
        Inputs.Player.ScrollBoost.performed += ctx => ScrollBoostInput = true;
        Inputs.Player.ScrollBoost.canceled += ctx => ScrollBoostInput = false;

        // Instantiate tile screen (acts as a sprite mask for the tiles)
        TileScreenObject = Instantiate(Resources.Load<GameObject>("TileScreen"), transform);

        // Start Music
        GameAssets.Sound.StoneRock.Play();
    }

    void FixedUpdate()
    {

        // Check the block queue for a valid spawn
        ProcessBlockQueue();

        // Move Cursor
        ProcessCursorMovement();

        // Process Grid Requests
        ProcessGridRequests();

        // Swap at cursor
        if (CusorSwitchFlag)
        {
            CusorSwitchFlag = false;
            SwitchAtCursor();
        }

        // Run Free Tile Physics
        UnlockedTilesFreefall();

        // Scroll Grid - Lock scroll if there are clearing or falling tiles that are not blocks
        ProcessScrolling();

        // Check for tiles to clear
        ProcessClearing();

        // Reset all locked tile's chain level if they are not clearing, are not over a chaining tile, and are not over a swapping tile
        ProcessChainingTiles();

        // Check to clear the chain level
        CheckForEndOfChain();

        // Reposition Tile Screen
        TileScreenObject.transform.position = GridWorldPosition + Vector2.up * (FLOOR_ROW + GridScrollOffset - 1);

        // Determine Columns that should bounce
        for (int i = 0; i < GridSize.x; i++)
        {
            for (int j = 0; j < GridSize.y; j++)
            {
                Griddable _Tile = GetTileByGridCoordinate(new Vector2Int(i, j));
                if (_Tile != null)
                {
                    if (IsColumnInDanger(i))
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

    private void UnlockedTilesFreefall()
    {
        if (UnlockedTiles.Count != 0)
        {
            // Sort FreeTiles by grid y position in ascending order
            List<int> _UnlockedTileTemp = new List<int>(UnlockedTiles);
            _UnlockedTileTemp.Sort(CompareFreeTileHeightAscending);
            for (int i = _UnlockedTileTemp.Count - 1; i >= 0; i--)
            {
                int TileKey = _UnlockedTileTemp[i];
                Griddable _Tile = GetTileByID(TileKey);
                if (!_Tile.LockedToGrid) _Tile.FreeFall();
            }
        }
    }

    private void ProcessScrolling(){
        
        if (ClearingTiles.Count == 0)
        {

            // Check if there is a non-block falling
            bool NonblockFalling = false;
            foreach (int _TileID in UnlockedTiles)
            {
                Griddable _Tile = GetTileByID(_TileID);
                if (_Tile.Type != Griddable.TileType.Block)
                {
                    NonblockFalling = true;
                    break;
                }
            }

            if (!NonblockFalling)
            {

                if (RowContainsLockedTiles(CEILING_ROW))
                {

                    // If scrolling is otherwise legal but the ceiling row is reached, take damage.
                    TakeDamage(Time.fixedDeltaTime);

                }
                else
                {

                    // If the scroll button is pressed while scrolling is legal, lock in the boost scroll speed until another row of tiles is created.
                    if (ScrollBoostInput) ScrollBoostLock = true;
                    float ScrollAmount = SCROLL_SPEED_BASE * Time.fixedDeltaTime;
                    if (ScrollBoostLock) ScrollAmount *= SCROLL_BOOST_FACTOR;
                    Scroll(ScrollAmount);

                }
            }

        }

    }

    /// <summary>
    /// Check all locked tiles for conditions to reset their chaining status. The following conditions will prevent resetting:
    /// 1. The tile is not empty
    /// 2. The tile is a basic tile
    /// 3. The tile is not clearing
    /// 4. The tile below is not empty
    /// 5. The tile beneath the tile is not chaining
    /// 6. The tile beneath the tile is not swapping
    /// </summary>
    public void ProcessChainingTiles()
    {

        for (int i = 0; i < GridSize.x; i++)
        {

            for (int j = 1; j < GridSize.y; j++)
            {

                // Check Condition 1
                if (TileGrid[i, j] == 0) continue;

                // Get tile class
                Griddable _Tile = GetTileByGridCoordinate(new Vector2Int(i, j));

                // Check Condition 2
                if (_Tile.Type != Griddable.TileType.Basic) continue;

                // Check Condition 3
                if (_Tile.IsClearing()) continue;

                // Check Condition 4
                if (TileGrid[i, j - 1] == 0) continue;

                // Get Tile Below
                Griddable _TileBelow = GetTileByGridCoordinate(new Vector2Int(i, j - 1));

                // Check Condition 5
                if (_TileBelow.GetChaining()) continue;

                // Check Condition 6
                if (_TileBelow.IsSwapping()) continue;

                // If no exceptions are found, set tile chaining to false
                _Tile.SetChaining(false);

            }

        }

    }

    private void SetChainLevel(int _Level)
    {
        ChainLevel = _Level;
        transform.Find("ChainLevelText").GetComponent<TextMeshPro>().text = _Level.ToString();
    }

    private void TakeDamage(float _DamageAmount)
    {
        Health -= _DamageAmount;
        if(Health < 0)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Checks if the specified grid column has a locked tile in the danger row.
    /// </summary>
    private bool IsColumnInDanger(int _Column)
    {
        return (TileGrid[_Column, DANGER_ROW] != 0);
    }

    /// <summary>
    /// Processes and clears all GridRequests queued since last update.
    /// </summary>
    private void ProcessGridRequests()
    {

        for (int i = 0; i < GridRequests.Count; i++)
        {


            // Created on a coordinate that a basic tile clears
            if (GridRequests[i].Type == GridRequestType.BlockClear)
            {

                Vector2Int TileCoordinate = GridRequests[i].Coordinate;
                Vector2Int[] _CheckOffsets = { Vector2Int.left, Vector2Int.right, Vector2Int.up, Vector2Int.down };

                foreach (Vector2Int _CheckOffset in _CheckOffsets)
                {
                    
                    Vector2Int _CheckCoordinate = TileCoordinate + _CheckOffset;
                    if (_CheckCoordinate.x < 0 || _CheckCoordinate.x >= GridSize.x || _CheckCoordinate.y < 0 || _CheckCoordinate.y >= GridSize.y) continue;
                    
                    int TileID = TileGrid[_CheckCoordinate.x, _CheckCoordinate.y];
                    if (TileID == 0) continue;

                    Griddable _Tile = GetTileByID(TileID);
                    if (_Tile.Type == Griddable.TileType.Block)
                    {
                        
                        BlockTile _BlockTile = _Tile as BlockTile;
                        Block _Block = _BlockTile.MyBlock;
                        if (_Block.State == Block.BlockState.Set)
                        {

                            _Block.Clear();

                        }
                        
                    }

                }
                
            }

            if (GridRequests[i].Type == GridRequestType.ReplaceWithTile)
            {

                // Place new tile in its position
                Vector2Int _TileCoordinate = GridRequests[i].Coordinate;
                int TileID = CreateNewBasicTile(GridRequests[i].TileColor, _TileCoordinate, true);
                Griddable _Tile = GetTileByID(TileID);
                AttachTileToGrid(_Tile, _TileCoordinate);

                // Update tile
                GridRequests.Add(new GridRequest { Type = GridRequestType.Update, Coordinate = _TileCoordinate, Chaining = GridRequests[i].Chaining });

            }

            if (GridRequests[i].Type == GridRequestType.Destroy)
            {
                
                Vector2Int _TileCoordinate = GridRequests[i].Coordinate;
                int TileID = GetTileKeyAtGridCoordinate(_TileCoordinate);
                Griddable.TileType _TileType = GetTileByID(TileID).Type;
                GridRequests.Add(new GridRequest { Type = GridRequestType.Update, Coordinate = _TileCoordinate, Chaining = GridRequests[i].Chaining });
                UnattachTileFromGrid(_TileCoordinate);
                DestroyUnlockedTile(GetTileByID(TileID));

                //Replace basic tiles with a hangtime ethereal tile
                if (_TileType == Griddable.TileType.Basic) AttachTileToGrid(GetTileByID(CreateNewHangtimeEtherialTile(_TileCoordinate)), _TileCoordinate);

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
                            _Tile.SetChaining(GridRequests[i].Chaining);
                            UnattachTileFromGrid(TileCoordinate);
                            if (TileCoordinate.y < GridSize.y - 1) GridRequests.Add(new GridRequest { Type = GridRequestType.Update, Coordinate = TileCoordinate + Vector2Int.up, Chaining = GridRequests[i].Chaining });
                        }
                    }
                }

                // Tell above tile to update/fall if there is no tile, and there is a tile above
                if (TileCoordinate.y < GridSize.y - 1)
                {
                    // If the updated Tile is empty, check for an up-neighbor and unattach. Add that tile to the new requests hash
                    if (!CoordinateContainsLockedTile(TileCoordinate) && CoordinateContainsLockedTile(TileCoordinate + Vector2Int.up))
                    {
                        GridRequests.Add(new GridRequest { Type = GridRequestType.Update, Coordinate = TileCoordinate + Vector2Int.up, Chaining = GridRequests[i].Chaining });
                    }
                }
            }
        }

        GridRequests.Clear();

    }

    /// <summary>
    /// Handles all cursor movement, including fast scrolling after the same direction has been held
    /// for more frames than specified by FAST_SCROLL_FRAMES.
    /// </summary>
    private void ProcessCursorMovement()
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

    /// <summary>
    /// Check for tiles that are eligible to clear. Handles clear commands to tiles,
    /// and checks for tech (combos and chains).
    /// </summary>
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
            bool Chained = false;

            for (int i = 0; i < ListCount; i++)
            {
                Vector2Int _TileCoordinate = ClearedCoordinatesList[i];
                Griddable _Tile = GetTileByGridCoordinate(_TileCoordinate);
                if (_Tile.GetChaining()) Chained = true;
                _Tile.Clear(i, ListCount);
                GridRequests.Add(new GridRequest { Type = GridRequestType.BlockClear, Coordinate = _TileCoordinate });
                ClearingTiles.Add(_Tile.KeyID);
            }

            // Check for Combo
            if (ClearedCoordinatesList.Count > 3)
            {
                GameAssets.Sound.Combo1.Play();
                GameObject CounterObject = Instantiate(Resources.Load<GameObject>("TechCounterObject"));
                TechCounter Counter = CounterObject.GetComponent<TechCounter>();
                Griddable FirstTile = GetTileByGridCoordinate(ClearedCoordinatesList[0]);
                Counter.StartEffect(TechCounter.TechType.Combo, ClearedCoordinatesHash.Count, (Vector2)FirstTile.GetWorldPosition() + new Vector2(0.5f, 0.75f));
            }

            // Check for chain
            if (Chained)
            {
                GameAssets.Sound.Combo1.Play();
                SetChainLevel(ChainLevel + 1);
                GameObject CounterObject = Instantiate(Resources.Load<GameObject>("TechCounterObject"));
                TechCounter Counter = CounterObject.GetComponent<TechCounter>();
                Griddable FirstTile = GetTileByGridCoordinate(ClearedCoordinatesList[0]);

                // Offset the chain tech counter if there was also a combo
                Vector2 ComboOffset = Vector2.zero;
                if (ClearedCoordinatesList.Count > 3) ComboOffset = new Vector2(0f, 1f);

                Counter.StartEffect(TechCounter.TechType.Chain, ChainLevel + 1, (Vector2)FirstTile.GetWorldPosition() + ComboOffset + new Vector2(0.5f, 0.75f));
            }

                // Create Clear Set (Is this necessary?)
                // ClearSets.Add(new ClearSet(ClearedCoordinatesList));

                ClearedCoordinatesHash.Clear();

        }
    }

    /// <summary>
    /// Determines when the current chain has ended. Resets chain level to 0 if so.
    /// </summary>
    private void CheckForEndOfChain()
    {

        if (ChainLevel == 0) return;

        bool ContinueChain = false;
        
        // Check for grid tiles
        foreach(Griddable _Tile in Tiles.Values)
        {
            if (_Tile.GetChaining())
            {
                ContinueChain = true;
                break;
            }
        }

        // Check falling tiles
        if (!ContinueChain)
        {
            foreach (int _TildID in UnlockedTiles)
            {
                Griddable _Tile = GetTileByID(_TildID);
                if (_Tile.GetChaining())
                {
                    ContinueChain = true;
                    break;
                }
            }
        }

        if (!ContinueChain) SetChainLevel(0);
    }

    /// <summary>
    /// List comparer method. Clear order goes from top to bottom, left to right of the grid-space.
    /// </summary>
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

        // REPLACE ANY HANGTIME ETHEREAL TILES
        if (TileA != null && TileA.Type == Griddable.TileType.HangtimeEthereal)
        {
            Vector2Int _TileCoordinate = TileA.GridCoordinate;
            UnattachTileFromGrid(_TileCoordinate);
            DestroyUnlockedTile(TileA);
            TileA = null;
        }

        if (TileB != null && TileB.Type == Griddable.TileType.HangtimeEthereal)
        {
            Vector2Int _TileCoordinate = TileB.GridCoordinate;
            UnattachTileFromGrid(_TileCoordinate);
            DestroyUnlockedTile(TileB);
            TileB = null;
        }

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
    Griddable GetTileByGridCoordinate(Vector2Int GridCoordinate)
    {
        int TileKey = TileGrid[GridCoordinate.x, GridCoordinate.y];
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
            for (int j = 0; j < 3; j++)
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

    int CreateNewHangtimeEtherialTile(Vector2 _GridPosition)
    {
        int TileID = NextTileID;
        Tiles.Add(NextTileID++, new HangtimeEtherealTile(this, TileID, _GridPosition));
        return TileID;
    }

    void AttachTileToGrid(Griddable _Tile, Vector2Int _GridCoordinate)
    {
        
        // Remove Tile key from Unlocked Tile List
        UnlockedTiles.Remove(_Tile.KeyID);

        // Make sure the TileGrid position is valid
        if (_GridCoordinate.x < 0 || _GridCoordinate.x >= GridSize.x || _GridCoordinate.y < 0 || _GridCoordinate.y >= GridSize.y)
        {
            Debug.LogError("Attempted to attach Tile out-of-bounds: " + _GridCoordinate);
        }

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

        
        int TileID = GetTileKeyAtGridCoordinate(_GridCoordinate);
        UpdateTileAtGridCoordinate(_GridCoordinate.x, _GridCoordinate.y);

        // Make sure the grid position is not empty (Value 0)
        if (TileID == 0) Debug.LogError("Attempted to unattach a non-existent Tile.");

        // Add Tile key to Unlocked Tile List
        UnlockedTiles.Add(TileID);

        // Remove Tile from Grid
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

        // Check for out of bounds request
        if (_GridCoordinate.x < 0 || _GridCoordinate.x >= GridSize.x || _GridCoordinate.y < 0 || _GridCoordinate.y >= GridSize.y)
        {
            Debug.LogError("Tile Key requested for invalid coordinate: " + _GridCoordinate);
        }

        return TileGrid[_GridCoordinate.x, _GridCoordinate.y];
    }

    public bool RequestAttachment(Griddable _Tile, Vector2Int _GridCoordinate)
    {

        if (_Tile.LockedToGrid)
        {
            Debug.LogWarning("An already-locked tile requested attachment:" + _Tile.GridCoordinate );
        }

        if (GetTileKeyAtGridCoordinate(_GridCoordinate) != 0)
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

    private bool RowContainsUnlockedTiles(int RowIndex)
    {
        for (int i = 0; i < GridSize.x; i++)
        {
            if (TileGrid[i, RowIndex] != 0) return true;
        }
        return false;
    }

    private bool CoordinateContainsLockedTile(Vector2Int _GridCoordinate)
    {
        return (GetTileKeyAtGridCoordinate(_GridCoordinate) != 0);
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
            if (_Chain) GridRequests.Add(new GridRequest { Type = GridRequestType.Destroy, Coordinate = _Tile.GridCoordinate, Chaining = true });
            else GridRequests.Add(new GridRequest { Type = GridRequestType.Destroy, Coordinate = _Tile.GridCoordinate, Chaining = false });
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

    #region Block Methods

    private void QueueBlock(Vector2Int _BlockSize)
    {
        BlockQueue.Add(_BlockSize);
    }

    private void ProcessBlockQueue()
    {
        
        // Return if the block queue is empty
        if (BlockQueue.Count == 0) return;

        // Determine if the spawn area is clear for a block
        bool SpawnRowIsClear = true;
        for(int i = 0; i < GridSize.x; i++)
        {
            if (CoordinateContainsFreeTile(new Vector2Int(i, BLOCK_SPAWN_ROW)) || CoordinateContainsLockedTile(new Vector2Int(i, BLOCK_SPAWN_ROW))) {
                SpawnRowIsClear = false;
                break;
            }
        }

        // If clear, spawn the next block and clear the list entry
        if (SpawnRowIsClear)
        {
            SpawnBlock(new Vector2(0f, BLOCK_SPAWN_ROW), BlockQueue[0]);
            BlockQueue.RemoveAt(0);
        }


    }

    private int SpawnBlock(Vector2 _GridPosition, Vector2Int _BlockSize)
    {
        Block _Block = new Block(this, NextBlockID, _BlockSize, _GridPosition);
        Blocks.Add(NextBlockID, _Block);

        // Create BlockTiles
        for (int i = 0; i <  _BlockSize.x; i++)
        {
            for (int j = 0; j < _BlockSize.y; j++)
            {

                BlockTile.BlockSection _BlockSection;
                if(i == 0)
                {
                    _BlockSection = BlockTile.BlockSection.SingleLeft;
                } 
                else if (i == _BlockSize.x - 1)
                {
                    _BlockSection = BlockTile.BlockSection.SingleRight;
                }
                else
                {
                    _BlockSection = BlockTile.BlockSection.SingleCenter;
                }

                int _KeyID = CreateNewBlockTile(_Block, _GridPosition + new Vector2(i, j), false, _BlockSection);
                BlockTile _BlockTile = (BlockTile) GetTileByID(_KeyID);
                if (_BlockTile == null) Debug.LogError("Recently created block not found or cast correctly.");
                _Block.AddBlockTile(_BlockTile);
            }
        }

        return NextBlockID++;
    }

    int CreateNewBlockTile(Block _Block, Vector2 _GridPosition, bool _LockedToGrid, BlockTile.BlockSection _BlockSection)
    {
        Tiles.Add(NextTileID, new BlockTile(this, NextTileID, _GridPosition, _LockedToGrid, _Block, _BlockSection));
        if (!_LockedToGrid) UnlockedTiles.Add(NextTileID);
        return NextTileID++;
    }

    public void RemoveBlockByID(int _BlockID)
    {
        Blocks.Remove(_BlockID);
    }

    public void RequestTileReplacement(BasicTile.TileColor _TileColor, Vector2Int _Coordinate, bool _Chaining)
    {
        GridRequests.Add(
                new GridRequest { 
                    Type = GridRequestType.ReplaceWithTile, 
                    Coordinate = _Coordinate, 
                    Chaining = _Chaining, 
                    TileColor = _TileColor 
                } 
            );
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
