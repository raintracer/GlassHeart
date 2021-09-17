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
    public Vector2Int GridSize = new Vector2Int(6, 22);
    private int NextTileID = 1;
    private int NextBlockID = 1;

    // These collections hold integer keys that correspond with the Dictionary of Griddables
    public int[,] TileGrid;
    private List<int> UnlockedTiles = new List<int>();

    // This collection holds the block entities
    private Dictionary<int, Block> Blocks = new Dictionary<int, Block>();
    private List<bool[,]> BlockQueue = new List<bool[,]>();

    // This collection holds tiles are clearing
    private HashSet<int> ClearingTiles = new HashSet<int>();

    // Clear set logic
    private List<ClearSet> ClearSets = new List<ClearSet>();
    public int ChainLevel { get; private set; } = 0;
    const int HIGH_CHAIN_LEVEL = 4;

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
    private bool FireInputFlag = false;
    private bool WaterInputFlag = false;
    private bool AirInputFlag = false;
    private bool EarthInputFlag = false;

    // AI Fields
    [SerializeField] private bool AIControlled;
    float AIActionDelay = 0.1f;
    float AIActionDelayTimer = 0f;
    List<AIAction> AIActions = new List<AIAction>();

    // Constants
    public int Ceiling_Row = BASE_CEILING_ROW;
    public const int BASE_CEILING_ROW = 13;
    public const int FLOOR_ROW = 2;
    private const float SCROLL_BOOST_FACTOR = 20f;
    private const int FAST_SCROLL_FRAMES = 10;
    private const int DANGER_ROW = 11;
    private const int BLOCK_SPAWN_ROW_OFFSET = 1;
    private const float SCROLL_SPEED_BASE = 0.1f;

    // Player properties
    private int[] RowHealth = new int[BASE_CEILING_ROW - FLOOR_ROW + 1];
    private int MaxRowHealth = 100;
    private bool Alive = true;


    private float StopTime = 0f;

    // Opponent Fields
    [SerializeField] GameObject OpponentGridObject;
    PuzzleGrid OpponentGrid;

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

    // Spell Data
    [SerializeField] private Spell FireSpell;
    [SerializeField] private Spell WaterSpell;
    [SerializeField] private Spell EarthSpell;
    [SerializeField] private Spell AirSpell;
    private float HyperBoostTime = 0;
    private float HyperBoostIntensity = 1;
    private float IceTime = 0f;
    private float FloatTime = 0f;

    // Mana Data
    private float MaxMana = 3f;
    private float AirMana;
    private float EarthMana;
    private float FireMana;
    private float WaterMana;


    #region Unity Events

    void Awake()
    {

        // Initialize Grid
        GridWorldPosition = transform.position + new Vector3(1, -1, 0);
        CreateInitialTileGrid();

        // Initialize cursor object
        CursorObject = Instantiate(Resources.Load<GameObject>("PuzzleCursorPrefab"), transform);
        UpdateCursorPosition();

        // Initialize Opponent Grid
        if (OpponentGridObject == null)
        {
            OpponentGrid = null;
        }
        else
        {
            OpponentGrid = OpponentGridObject.GetComponent<PuzzleGrid>();
            if (OpponentGrid == null) Debug.LogError("Opponent grid object does not have the required PuzzleGrid component.");
        }

        // Initialize controls
        if (!AIControlled)
        {
            Inputs = new ControlMap();
            Inputs.Enable();
            Inputs.Player.MoveCursor.performed += ctx => Movement = ctx.ReadValue<Vector2>();
            Inputs.Player.SwitchAtCursor.started += ctx => CusorSwitchFlag = true;
            Inputs.Player.ScrollBoost.performed += ctx => ScrollBoostInput = true;
            Inputs.Player.ScrollBoost.canceled += ctx => ScrollBoostInput = false;
            Inputs.Player.CastAir.canceled += ctx => AirInputFlag = true;
            Inputs.Player.CastEarth.canceled += ctx => EarthInputFlag = true;
            Inputs.Player.CastFire.canceled += ctx => FireInputFlag = true;
            Inputs.Player.CastWater.canceled += ctx => WaterInputFlag = true;
            Inputs.Player.SpawnRandomBlock.performed += ctx => QueueRandomBlock();
        }

        // Instantiate tile screen (acts as a sprite mask for the tiles)
        TileScreenObject = Instantiate(Resources.Load<GameObject>("TileScreen"), transform);

        // Start Music
        GameAssets.Sound.StoneRock.Play();

        // Set initial mana values
        SetInitialMana();

        // Set Initial Row Healths
        for (int i = 0; i < RowHealth.Length; i++) RowHealth[i] = MaxRowHealth;

        

    }

    void FixedUpdate()
    {

        // Do nothing if defeated
        if (!Alive) return;

        // Process AI Control
        if (AIControlled)
        {
            ProcessAI();
        }

        // Check the block queue for a valid spawn
        ProcessBlockQueue();

        // Process Spell Inputs
        ProcessSpellInputs();

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

        // Only freefall if float time is not active
        if (FloatTime > 0)
        {
            FloatTime -= Time.fixedDeltaTime;
            if (FloatTime < 0) FloatTime = 0;
        }
        else
        {
            if (UnlockedTiles.Count != 0)
            {
                
                // Capture and sort FreeTiles by grid y position in ascending order in a new list
                List<int> _UnlockedTileTemp = new List<int>(UnlockedTiles);
                _UnlockedTileTemp.Sort(CompareFreeTileHeightAscending);


                for (int i = _UnlockedTileTemp.Count - 1; i >= 0; i--)
                {
                    int TileKey = _UnlockedTileTemp[i];
                    Griddable _Tile = GetTileByID(TileKey);
                    if (!_Tile.LockedToGrid) _Tile.FreeFall(); // Ensure the tile on the temporary list is still unattached.
                }
            }
        }
    }

    private void ProcessScrolling(){

        if (IceTime > 0f)
        {
            IceTime -= Time.fixedDeltaTime;
            if (IceTime < 0)
            {
                IceTime = 0;
            }
            return;
        }

        if (ClearingTiles.Count == 0 || HyperBoostTime > 0f)
        {

            // Decerement hyper boost time if it is active
            if(HyperBoostTime > 0)
            {
                HyperBoostTime -= Time.fixedDeltaTime;
                if (HyperBoostTime < 0)
                {
                    HyperBoostTime = 0;
                    HyperBoostIntensity = 1;
                }
            }

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

            if (!NonblockFalling || HyperBoostTime > 0f)
            {

                if (RowContainsLockedTiles(Ceiling_Row))
                {

                    // If scrolling is otherwise legal but the ceiling row is reached, take damage on ALL rows
                    for (int k = 0; k < RowHealth.Length; k++) TakeDamage(k, 1);

                }
                else
                {

                    // If the scroll button is pressed while scrolling is legal, lock in the boost scroll speed until another row of tiles is created.
                    if (!ScrollBoostLock)
                    {
                        if (ScrollBoostInput)
                        {
                            ScrollBoostLock = true;
                        }       
                    }

                    float ScrollAmount = 0f;
                    // Hyperboost over-rides manual scroll boost
                    if (HyperBoostTime > 0f)
                    {
                        ScrollAmount = SCROLL_SPEED_BASE * Time.fixedDeltaTime * HyperBoostIntensity;
                    }

                    else if (ScrollBoostLock)
                    {
                        ScrollAmount = SCROLL_SPEED_BASE * Time.fixedDeltaTime * SCROLL_BOOST_FACTOR;
                    }
                    else
                    {
                        ScrollAmount = SCROLL_SPEED_BASE * Time.fixedDeltaTime;
                        ScrollBoostLock = false;
                    }

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

    private void TakeDamageOnTopRow(int _DamageAmount)
    {
        TakeDamage(Ceiling_Row - FLOOR_ROW, _DamageAmount);
    }

    private void TakeDamage(int RowIndex, int _DamageAmount)
    {

        // If not alive, do nothing
        if (!Alive) return;

        // If the row health is already 0 or below, ignore:
        if (RowHealth[RowIndex] <= 0) return;


        // Otherwise, inflict damage
        int CarryoverDamage = _DamageAmount - RowHealth[RowIndex];
        RowHealth[RowIndex] -= _DamageAmount;

        // Check if the row is destroyed
        if(RowHealth[RowIndex] <= 0)
        {

            // Destroy all tiles on that row
            for (int i = 0; i < GridSize.x; i++)
            {
                new GridRequest { Coordinate = new Vector2Int(i, RowIndex + FLOOR_ROW), Type = GridRequestType.Destroy, Chaining = false };
            }

            // If this is the above the ceiling row, lower it to this level
            int New_Ceiling_Row = RowIndex + FLOOR_ROW - 1;
            if (New_Ceiling_Row <= Ceiling_Row)
            {
                Ceiling_Row = New_Ceiling_Row;
                transform.Find("Dead Frames").localScale = new Vector3(transform.Find("Dead Frames").localScale.x, (BASE_CEILING_ROW - Ceiling_Row) * 24, 0);
            }

            // Destroy all row above this, recursively
            if (RowIndex < RowHealth.Length - 1) TakeDamage(RowIndex + 1, RowHealth[RowIndex + 1]);


            // Play glass break sound, bigger sound if this is the base row, signifying a loss.
            if (RowIndex == 0) { 
                GameAssets.Sound.GlassBreakFinal.Play();
            }
            else
            {
                GameAssets.Sound.GlassBreak2.Play();
            }

            // Pass damage to next row?
            if (RowIndex > 0) TakeDamage(RowIndex - 1, CarryoverDamage);

            if (RowIndex == 0) Alive = false;

        }
        else
        {
            // Add Minor Glass Tap?
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

        // Check for Blocks to Clear
        HashSet<Vector2Int> BlockTileClearHashSet = new HashSet<Vector2Int>();


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

                            // Clear the block, and capture the Block's BlockTile keys for processing.
                            BlockTileClearHashSet.UnionWith(_Block.Clear());

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

        // After all Gridrequests are processed, process BlockTile clearing.

        if (BlockTileClearHashSet.Count > 0)
        {
            
            List<Vector2Int> BlockTileClearList = new List<Vector2Int>(BlockTileClearHashSet);
            BlockTileClearList.Sort(CompareCoordinatesByClearOrderDescending);
            int ListCount = BlockTileClearList.Count;

            for(int i = 0; i < ListCount; i++)
            {
                Vector2Int _TileCoordinate = BlockTileClearList[i];
                Griddable _Tile = GetTileByGridCoordinate(_TileCoordinate);
                _Tile.Clear(i, ListCount, false);
                ClearingTiles.Add(_Tile.KeyID);
            }

        }

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
        for (int j = Ceiling_Row; j >= FLOOR_ROW; j--)
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

        // Combo and Chain Color Hashes
        HashSet<BasicTile.TileColor> ClearColors = new HashSet<BasicTile.TileColor>();


        // Iterate over color map for matches to add to HashSet
        HashSet<Vector2Int> ClearedCoordinatesHash = new HashSet<Vector2Int>();
        for (int j = Ceiling_Row; j >= FLOOR_ROW; j--)
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
                        ClearColors.Add((BasicTile.TileColor) OriginColor);
                    }
                }
                if (i < GridSize.x - 2)
                {
                    if (ColorGrid[i + 1, j] == OriginColor && ColorGrid[i + 2, j] == OriginColor)
                    {
                        ClearedCoordinatesHash.Add(new Vector2Int(i + 0, j));
                        ClearedCoordinatesHash.Add(new Vector2Int(i + 1, j));
                        ClearedCoordinatesHash.Add(new Vector2Int(i + 2, j));
                        ClearColors.Add((BasicTile.TileColor)OriginColor);
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
            bool Combo = false;

            for (int i = 0; i < ListCount; i++)
            {
                Vector2Int _TileCoordinate = ClearedCoordinatesList[i];
                Griddable _Tile = GetTileByGridCoordinate(_TileCoordinate);
                if (_Tile.GetChaining()) Chained = true;
                _Tile.Clear(i, ListCount, ChainLevel >= HIGH_CHAIN_LEVEL);
                GridRequests.Add(new GridRequest { Type = GridRequestType.BlockClear, Coordinate = _TileCoordinate });
                ClearingTiles.Add(_Tile.KeyID);
            }

            // Check for Combo
            if (ClearedCoordinatesList.Count > 3)
            {
                Combo = true;
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

                // Add Mana for each color type
                foreach(BasicTile.TileColor _TileColor in ClearColors)
                {
                    switch (_TileColor)
                    {
                        case BasicTile.TileColor.Blue:
                        case BasicTile.TileColor.Red:
                        case BasicTile.TileColor.Yellow:
                        case BasicTile.TileColor.Purple:
                            ChangeMana(ChainLevel * ClearedCoordinatesList.Count, ConvertColorToMana(_TileColor));
                            break;
                        case BasicTile.TileColor.Indigo:
                            break;
                        case BasicTile.TileColor.Green:
                            break;
                        default:
                            Debug.LogError("Unrecognized color chained.");
                            break;

                    }
                }

            }

            // Temporary - Send a block to opponent if either a chain or combo was made
            if (Combo || Chained)
            {
                if(OpponentGrid != null)
                {
                    OpponentGrid.QueueRandomBlock();
                }
            }

            // Create Clear Set (Is this necessary?)
            // ClearSets.Add(new ClearSet(ClearedCoordinatesList));

            ClearedCoordinatesHash.Clear();

        }
    }

    Spell.Element ConvertColorToMana(BasicTile.TileColor _TileColor)
    {
        switch (_TileColor)
        {
            case BasicTile.TileColor.Blue:
                return Spell.Element.Water;
            case BasicTile.TileColor.Red:
                return Spell.Element.Fire;
            case BasicTile.TileColor.Yellow:
                return Spell.Element.Air;
            case BasicTile.TileColor.Purple:
                return Spell.Element.Earth;
            default:
                Debug.LogError("Color to mana requested on an invalid color");
                return Spell.Element.Air;
        }
    }

    void ChangeMana(float _ManaDelta, Spell.Element _SpellElement)
    {
        SetMana(Mathf.Clamp(GetManaByElement(_SpellElement) + _ManaDelta, 0f, MaxMana), _SpellElement);
    }

    void SetMana(float _ManaSet, Spell.Element _SpellElement)
    {

        Transform MeterTransform;
        Transform FillTransform;
        float MinPosition;
        float MaxPosition;

        switch (_SpellElement)
        {
            case Spell.Element.Water:
                WaterMana = _ManaSet;
                MeterTransform = CursorObject.transform.Find("Water Meter Mask");
                FillTransform = CursorObject.transform.Find("Water Cursor Fill");
                MinPosition = 2.569f;
                MaxPosition = 1.765f;
                break;
            case Spell.Element.Fire:
                FireMana = _ManaSet;
                MeterTransform = CursorObject.transform.Find("Fire Meter Mask");
                FillTransform = CursorObject.transform.Find("Fire Cursor Fill");
                MinPosition = 2.569f;
                MaxPosition = 1.765f;
                break;
            case Spell.Element.Air:
                AirMana = _ManaSet;
                MeterTransform = CursorObject.transform.Find("Air Meter Mask");
                FillTransform = CursorObject.transform.Find("Air Cursor Fill");
                MinPosition = -0.57f;
                MaxPosition = 0.229f;
                break;
            case Spell.Element.Earth:
                EarthMana = _ManaSet;
                MeterTransform = CursorObject.transform.Find("Earth Meter Mask");
                FillTransform = CursorObject.transform.Find("Earth Cursor Fill");
                MinPosition = -0.57f;
                MaxPosition = 0.229f;
                break;
            default:
                Debug.LogError("Unrecognized element mana set.");
                MeterTransform = CursorObject.transform.Find("Water Meter Mask");
                FillTransform = CursorObject.transform.Find("Water Cursor Fill");
                MinPosition = 0;
                MaxPosition = 0;
                break;

        }

        // Translate sprite mask for meter
        MeterTransform.localPosition = new Vector3(MinPosition + _ManaSet / MaxMana * (MaxPosition - MinPosition), MeterTransform.localPosition.y);
        
        // Set sprite brightness
        if (_ManaSet == MaxMana)
        {
            FillTransform.GetComponent<SpriteRenderer>().color = Color.white;
        }
        else
        {
            FillTransform.GetComponent<SpriteRenderer>().color = Color.grey;
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

    private int CompareCoordinatesByClearOrderDescending(Vector2Int CoordinateA, Vector2Int CoordinateB)
    {
        return CompareCoordinatesByClearOrderAscending(CoordinateB, CoordinateA);
    }

    #endregion

    #region AI Control Methods

    private void ProcessAI()
    {

        // Limit the AI acting and thinking speed
        AIActionDelayTimer += Time.fixedDeltaTime;
        if (AIActionDelayTimer < AIActionDelay)
        {
            Movement = Vector2.zero;
            CusorSwitchFlag = false;
            return;
        }
        AIActionDelayTimer -= AIActionDelay;

        // If there is no target action, search for one
        if (AIActions.Count == 0)
        {
            // Look for vertical one-switch matches
            AIAction SearchResult = AIAction.FindVerticalSingleSwitchMatch(this);
            if (SearchResult != null)
            {
                AIActions.Add(SearchResult);
            }
        }

        if (AIActions.Count == 0)
        {
            // Look for horizontal one-switch matches
            AIAction SearchResult = AIAction.FindHorizontalSingleSwitchMatch(this);
            if (SearchResult != null)
            {
                AIActions.Add(SearchResult);
            }
        }

        if (AIActions.Count == 0)
        {
            // Look for leveling swap
            AIAction SearchResult = AIAction.FindLevelingSwap(this);
            if (SearchResult != null)
            {
                AIActions.Add(SearchResult);
            }
        }

        if (AIActions.Count == 0)
        {
            // As a final option, boost to get more tiles to work with
            AIActions.Add(new AIAction("Default Boost", AIAction.ActionType.ScrollBoost, Vector2Int.zero, false, false));
        }

        if (AIActions.Count > 0)
        {
            
            // Refer to the next TargetAction
            AIAction TargetAction = AIActions[0];
            transform.Find("ChainLevelText").GetComponent<TextMeshPro>().text = AIActions[0].PathFinderSignature;

            // Special check for scroll boost input
            if (TargetAction.Action == AIAction.ActionType.ScrollBoost)
            {
                ScrollBoostInput = true;
                AIActions.RemoveAt(0);
                return;
            }
            else
            {
                ScrollBoostInput = false;
            }

            // If at the target coordinate, try to perform the action
            if (CursorPosition == TargetAction.TargetCoordinate)
            {
                if (AIActions[0].Action == AIAction.ActionType.Swap)
                {

                    CusorSwitchFlag = true;
                    AIActions.RemoveAt(0);

                    // On a swap, stun the AI for the length of the swap animation frames
                    AIActionDelayTimer = -Griddable.GetSwapFrames() * Time.fixedDeltaTime;

                }
            }
            // Otherwise move towards the target
            else if (CursorPosition.x < TargetAction.TargetCoordinate.x)
            {
                Movement = Vector2.right;
            }
            else if (CursorPosition.x > TargetAction.TargetCoordinate.x)
            {
                Movement = Vector2.left;
            }
            else if (CursorPosition.y < TargetAction.TargetCoordinate.y)
            {
                Movement = Vector2.up;
            }
            else if (CursorPosition.y > TargetAction.TargetCoordinate.y)
            {
                Movement = Vector2.down;
            }

        }
        
    }


    #endregion

    #region Spell Methods

    private void SetInitialMana()
    {

        SetMana(0f, Spell.Element.Air);
        SetMana(0f, Spell.Element.Earth);
        SetMana(0f, Spell.Element.Fire);
        SetMana(0f, Spell.Element.Water);

    }

    private float GetManaByElement(Spell.Element _ManaElement)
    {
        switch (_ManaElement)
        {
            case Spell.Element.Air:
                return AirMana;
            case Spell.Element.Water:
                return WaterMana;
            case Spell.Element.Fire:
                return FireMana;
            case Spell.Element.Earth:
                return EarthMana;
            default:
                Debug.LogError("Unrecognized Mana Type Requested.");
                return 0;
        }
    }

    private Spell GetSpellByElement(Spell.Element _ManaElement)
    {
        switch (_ManaElement)
        {
            case Spell.Element.Air:
                return AirSpell;
            case Spell.Element.Water:
                return WaterSpell;
            case Spell.Element.Fire:
                return FireSpell;
            case Spell.Element.Earth:
                return EarthSpell;
            default:
                Debug.LogError("Unrecognized Spell Type Requested.");
                return AirSpell;
        }
    }


    private void ProcessSpellInputs()
    {

        Spell.Element? SpellElement = null;

        if (EarthInputFlag)
        {
            EarthInputFlag = false;
            SpellElement = Spell.Element.Earth;
        }
        else if (WaterInputFlag)
        {
            WaterInputFlag = false;
            SpellElement = Spell.Element.Water;
        }
        else if (FireInputFlag)
        {
            FireInputFlag = false;
            SpellElement = Spell.Element.Fire;
        }
        else if (AirInputFlag)
        {
            AirInputFlag = false;
            SpellElement = Spell.Element.Air;
        }

        if (SpellElement == null) return;

        Spell SpellUsed = GetSpellByElement((Spell.Element)SpellElement);
        float SpellMana = GetManaByElement((Spell.Element)SpellElement);

        if (SpellMana == MaxMana)
        {
            ExecuteSpell(SpellUsed);
            SetMana(0f, (Spell.Element)SpellElement);
        }

    }

    private void ExecuteSpell(Spell _Spell)
    {
        HyperBoostTime = _Spell.BoostTime;
        HyperBoostIntensity = _Spell.BoostIntensity;
        IceTime = _Spell.StopTime;
        FloatTime = _Spell.FloatTime;

        foreach(Spell.SpellEffect _Effect in _Spell.SpellEffects)
        {
            switch (_Effect)
            {
                case Spell.SpellEffect.None:
                    break;
                case Spell.SpellEffect.IncinerateRowAtCursor:
                    IncinerateRow(CursorPosition.y);
                    break;
                default:
                    Debug.LogError("Unrecognized spell effect tried to execute: " + _Effect);
                    break;
            }
        }

    }

    private void TryToIncinerateTile(int TileKey)
    {
        if (TileKey == 0) return;
        
        Griddable _Tile = GetTileByID(TileKey);
        
        if (_Tile.IncinerateAllowed()) _Tile.Incinerate();
    }

    private void IncinerateRow(int RowIndex)
    {

        if (RowIndex < 0 || RowIndex >= GridSize.y) Debug.LogError("Invalid row sent on Incinerate Row command: Row " + RowIndex);

        for (int i = 0; i < GridSize.x; i++)
        {
            Vector2Int _Coordinate = new Vector2Int(i, RowIndex);
            int _TileKey = GetTileKeyAtGridCoordinate(_Coordinate);
            TryToIncinerateTile(_TileKey);
        }
    }

    #endregion

    #region Cursor Methods
    void MoveCursor(Vector2 _Movement, bool FastScroll = false)
    {
        if (_Movement.SqrMagnitude() > 0) {
            Vector2Int OldCursorPosition = CursorPosition;
            CursorPosition += new Vector2Int((int)_Movement.x, (int)_Movement.y);
            CursorPosition.Clamp(new Vector2Int(0, FLOOR_ROW), new Vector2Int(GridSize.x - 2, Ceiling_Row));
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

    public bool CoordinateIsSupported(Vector2Int GridCoordinate)
    {

        if(GridCoordinate.y < FLOOR_ROW)
        {
            Debug.LogWarning("Coordinates below the floor row should not be asked if they are supported");
            return true;
        } else if (GridCoordinate.y == FLOOR_ROW)
        {
            return true;
        }
        else
        {
            // Check for a set tile under the checked coordinate
            Vector2Int CheckCoordinate = GridCoordinate + Vector2Int.down;
            Griddable CheckTile = GetTileByGridCoordinate(CheckCoordinate);
            if (CheckTile == null)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }

    public Griddable GetTileByGridCoordinate(Vector2Int GridCoordinate)
    {
        if (GridCoordinate.x < 0 || GridCoordinate.x >= GridSize.x || GridCoordinate.y < 0 || GridCoordinate.y >= GridSize.y)
        {
            Debug.LogError("Out-of-bounds grid coordinate requested: " + GridCoordinate);
        }
        int TileKey = TileGrid[GridCoordinate.x, GridCoordinate.y];
        return GetTileByID(TileKey);
    }

    public Griddable GetTileByGridCoordinate(int GridCoordinateX, int GridCoordinateY)
    {
        return GetTileByGridCoordinate(new Vector2Int(GridCoordinateX, GridCoordinateY));
    }

    public int CompareFreeTileHeightAscending(int TileAID, int TileBID)
    {
        float TileAY = GetTileByID(TileAID).GridPosition.y;
        float TileBY = GetTileByID(TileBID).GridPosition.y;
        return TileBY.CompareTo(TileAY);
    }

    public int GetColumnHeightFromSwappableTiles(int ColumnIndex)
    {

        int ColumnHeight = -1;
        for (int k = 0; k <= Ceiling_Row - FLOOR_ROW; k++)
        {
            Vector2Int _Coordinate = new Vector2Int(ColumnIndex, k + FLOOR_ROW);
            Griddable _Tile = GetTileByGridCoordinate(_Coordinate);
            if (_Tile == null || !_Tile.SwappingAllowed()) break;
            ColumnHeight = k;
        }

        return ColumnHeight;

    }

    public int CompareColumnsByHighestSwappableTileAscending(int ColumnA, int ColumnB)
    {

        int HeightA = GetColumnHeightFromSwappableTiles(ColumnA);
        int HeightB = GetColumnHeightFromSwappableTiles(ColumnB);

        // Return the height comparison
        return HeightB.CompareTo(HeightA);

    }

    public Griddable GetTileByID(int TileKey)
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
            Debug.LogWarning("Tile requested attachment to an occupied grid-space at coordinate: " + _GridCoordinate);
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


    public bool CoordinateContainsAnyTile(Vector2Int _GridCoordinate)
    {
        return CoordinateContainsLockedTile(_GridCoordinate) || CoordinateContainsFreeTile(_GridCoordinate);
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
        BlockQueue.Add(Block.RectangularBlock(_BlockSize));
    }

    private void QueueRandomBlock()
    {

        switch(Random.Range(1, 6))
        {
            case 1:
                BlockQueue.Add(Block.HalfBlock);
                break;
            case 2:
                BlockQueue.Add(Block.FullBlock);
                break;
            case 3:
                BlockQueue.Add(Block.PebbleBlock);
                break;
            case 4:
                BlockQueue.Add(Block.SpikeBlock);
                break;
            case 5:
                BlockQueue.Add(Block.SmallSquareBlock);
                break;
            default:
                Debug.LogError("Unexpected value for Random Block Int.");
                break;
        }

    }

    private void ProcessBlockQueue()
    {
        
        // Return if the block queue is empty
        if (BlockQueue.Count == 0) return;

        // Determine if the spawn area is clear for a block
        bool SpawnRowIsClear = true;
        for(int i = 0; i < GridSize.x; i++)
        {
            if (CoordinateContainsFreeTile(new Vector2Int(i, BLOCK_SPAWN_ROW_OFFSET + Ceiling_Row)) || CoordinateContainsLockedTile(new Vector2Int(i, BLOCK_SPAWN_ROW_OFFSET + Ceiling_Row))) {
                SpawnRowIsClear = false;
                break;
            }
        }

        // If clear, spawn the next block and clear the list entry
        if (SpawnRowIsClear)
        {

            // Determine random horizontal position limited by the block's width
            int _BlockWidth = BlockQueue[0].GetLength(0);
            int _HorizontalOffsetInt = Random.Range(0, GridSize.x - _BlockWidth + 1);
            float _HorizontalOffsetFloat = _HorizontalOffsetInt;

            // Spawn Block and remove from queue
            SpawnBlock(new Vector2(_HorizontalOffsetFloat, BLOCK_SPAWN_ROW_OFFSET + Ceiling_Row), BlockQueue[0]);
            BlockQueue.RemoveAt(0);
        
        }


    }

    private int SpawnBlock(Vector2 _GridPosition, bool[,] _Arrangement)
    {

        Vector2Int _BlockSize = new Vector2Int(_Arrangement.GetLength(0), _Arrangement.GetLength(1));
        Block _Block = new Block(this, NextBlockID, _BlockSize, _GridPosition);
        Blocks.Add(NextBlockID, _Block);

        // Create BlockTiles
        for (int i = 0; i <  _BlockSize.x; i++)
        {
            
            for (int j = 0; j < _BlockSize.y; j++)
            {

                // Determine if there should be a block at this point in the arrangement
                if (_Arrangement[i, j])
                {

                    BlockTile.BlockSection _BlockSection = BlockTile.BlockSection.Single;
                    Vector2Int BlockGridPosition = new Vector2Int(i, j);

                    int _KeyID = CreateNewBlockTile(_Block, _GridPosition + (Vector2) BlockGridPosition, false, _BlockSection, BlockGridPosition);
                    BlockTile _BlockTile = (BlockTile) GetTileByID(_KeyID);
                    if (_BlockTile == null) Debug.LogError("Recently created block not found or cast correctly.");
                    _Block.AddBlockTile(_BlockTile, new Vector2Int(i, j));

                }

            }

        }

        return NextBlockID++;
    }

    int CreateNewBlockTile(Block _Block, Vector2 _GridPosition, bool _LockedToGrid, BlockTile.BlockSection _BlockSection, Vector2Int _BlockGridCoordinate)
    {
        Tiles.Add(NextTileID, new BlockTile(this, NextTileID, _GridPosition, _LockedToGrid, _Block, _BlockSection, _BlockGridCoordinate));
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

        // SHIFT CURSOR
        if (CursorPosition.y < Ceiling_Row) CursorPosition.y += 1;

        // SHIFT GRID REQUEST REFERENCES
        foreach (GridRequest _GridRequest in GridRequests) _GridRequest.ShiftReference(1);

        // SHIFT AIACTION TARGET COORDINATES
        if (AIControlled)
        {
            foreach(AIAction _AIAction in AIActions) _AIAction.TargetCoordinate.y++;
        }

        // Every time you shift grid up, do 5 damage to opponent
        OpponentGrid.TakeDamageOnTopRow(20);

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

        // SHIFT CURSOR
        if (CursorPosition.y > FLOOR_ROW) CursorPosition.y += 1;

        // SHIFT GRID REQUEST REFERENCES
        foreach (GridRequest _GridRequest in GridRequests) _GridRequest.ShiftReference(-1);

        // SHIFT AIACTION TARGET COORDINATES
        if (AIControlled)
        {
            foreach (AIAction _AIAction in AIActions) _AIAction.TargetCoordinate.y--;
        }

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
