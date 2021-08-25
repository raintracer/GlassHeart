using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Class handler for groups of tile blocks. In current implementation, blocks may be any size horizontally and only one-tile think vertically.
/// </summary>
public class Block
{

    private PuzzleGrid ParentGrid;
    private int BlockID;
    private Vector2Int BlockSize;
    private List<BlockTile> BlockTiles = new List<BlockTile>();
    public int[,] BlockGrid;

    // Adjusts the value of a block randomly to differentiate them
    private float BlockShade;
    private const float SHADE_RANGE = 0.3f;

    public enum BlockState { Set, Falling, Clearing, Dying };
    public BlockState State = BlockState.Falling;
    

    public Block(PuzzleGrid _ParentGrid, int _BlockID, Vector2Int _BlockSize, Vector2 _GridPosition)
    {
        
        ParentGrid = _ParentGrid;
        BlockID = _BlockID;
        BlockSize = _BlockSize;

        // Initialize a BlockGrid. PuzzleGrid will immediately assign these for Block with BlockTiles via AddBlockTile
        BlockGrid = new int[BlockSize.x, BlockSize.y];

        // Randomize Block Shade
        BlockShade = (Random.Range(0f, 2f) - 1f) * SHADE_RANGE;

    }

    public void AddBlockTile(BlockTile _BlockTileToAdd, Vector2Int _BlockGridPosition)
    {

        BlockTiles.Add(_BlockTileToAdd);

        // Add the BlockTile to the BlockGrid
        BlockGrid[_BlockGridPosition.x, _BlockGridPosition.y] = _BlockTileToAdd.KeyID;

        // Apply the BlockShade to the BlockTile
        _BlockTileToAdd.SetShade(BlockShade);

    }

    public void AttachAll(BlockTile Caller)
    {
        
        State = BlockState.Set;

        for (int i = 0; i < BlockTiles.Count; i++)
        {

            BlockTile _BlockTile = BlockTiles[i];
            if (_BlockTile.KeyID == Caller.KeyID) continue; // Ignore the tile that already attached.

            // Attach other tiles on their respective x coordinate but with the same y coordinate as the caller
            Vector2Int AttachPoint = new Vector2Int((int)(_BlockTile.GridPosition.x + 0.5f), Caller.GridCoordinate.y);
            _BlockTile.ParentGrid.RequestAttachment(_BlockTile, AttachPoint);

        }
    }

    public void Clear()
    {

        State = BlockState.Clearing;

        for (int i = 0; i < BlockTiles.Count; i++)
        {

            // Clear each block
            BlockTile _BlockTile = BlockTiles[i];
            _BlockTile.Clear(i, BlockTiles.Count, false);

            // Generate a blockclear request at each block
            ParentGrid.GridRequests.Add(new GridRequest { Type = GridRequestType.BlockClear, Coordinate = _BlockTile.GridCoordinate});

        }

    }
    /// <summary>
    /// Checks if all tile entities may fall. If so, commands all tile entities to fall together.
    /// </summary>
    public bool CheckForFallCondition()
    {

        // If block is already falling, return true
        if (State == BlockState.Falling) return true;

        // Block must be set to change to falling
        if (State != BlockState.Set) return false;

        // If not, check for fall conditions

        bool FallAllowed = true; // FallAllowed is true unless proven otherwise

        for (int i = 0; i < BlockSize.x; i++)
        {
            BlockTile _BlockTile = BlockTiles[i];

            // All tiles must be set to fall
            if (!_BlockTile.IsSet())
            {
                Debug.LogWarning("Block checked fall conditions on a non-set block tile.");
                FallAllowed = false;
                break;
            }

            // All tiles must have an empty grid-position under them.
            Vector2Int _GridCoordinate = _BlockTile.GridCoordinate;
            Vector2Int _CheckCoordinate = _GridCoordinate + Vector2Int.down;

            // Ensure that the check coordinate is in bounds of the grid
            if(_CheckCoordinate.x < 0 || _CheckCoordinate.x >= ParentGrid.GridSize.y)
            {
                Debug.LogWarning("Block checked fall conditions on a tile that is out of bounds of the puzzle grid (Coordinate: ( " + _CheckCoordinate + " )");
                FallAllowed = false;
                break;
            }

            // The check coordinate must not have a tile present
            int _CheckID = ParentGrid.TileGrid[_CheckCoordinate.x, _CheckCoordinate.y];
            if(_CheckID != 0)
            {
                FallAllowed = false;
                break;
            }


        }

        if (FallAllowed)
        {
            State = BlockState.Falling;

            for (int i = 0; i < BlockSize.x; i++)
            {
                BlockTile _BlockTile = BlockTiles[i];
                ParentGrid.GridRequests.Add( new GridRequest { Type = GridRequestType.Update, Coordinate = _BlockTile.GridCoordinate, Chaining = false } );
            } 
        }


        return FallAllowed;

    }

    public void RequestDestruction(BlockTile _BlockTile)
    {
        BlockTiles.Remove(_BlockTile);
        if (BlockTiles.Count == 0)
        {
            ParentGrid.RemoveBlockByID(BlockID);
        }
    }

    #region Block Shapes

    static public bool [,] RectangularBlock(Vector2Int BlockSize)
    {

        bool[,] BlockArray = new bool[BlockSize.x, BlockSize.y];

        for (int i = 0; i < BlockSize.x; i++)
            for (int j = 0; j < BlockSize.y; j++)
                BlockArray[i, j] = true;

        return BlockArray;
    }

    static public bool[,] FullBlock = RectangularBlock(new Vector2Int(6, 1));
    static public bool[,] HalfBlock = RectangularBlock(new Vector2Int(3, 1));
    static public bool[,] SmallSquareBlock = RectangularBlock(new Vector2Int(2, 2));
    static public bool[,] SpikeBlock = RectangularBlock(new Vector2Int(1, 3));
    static public bool[,] PebbleBlock = RectangularBlock(new Vector2Int(1, 1));

    #endregion

}
