using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Block
{

    private int BlockID;
    private Vector2Int BlockSize;
    private List<BlockTile> BlockTiles = new List<BlockTile>();

    public enum BlockState { Set, Falling, Clearing, Dying };
    public BlockState State = BlockState.Falling;

    public Block(int _BlockID, Vector2Int _BlockSize, Vector2 _GridPosition)
    {
        BlockID = _BlockID;
        BlockSize = _BlockSize;
    }

    public void AddBlockTile(BlockTile _BlockTileToAdd)
    {
        BlockTiles.Add(_BlockTileToAdd);
    }

    public void AttachAll(BlockTile Caller)
    {

        State = BlockState.Set;

        foreach (BlockTile _BlockTile in BlockTiles)
        {

            if (_BlockTile.KeyID == Caller.KeyID) continue; // Ignore the tile that already attached.

            // Attach other tiles on their respective x coordinate but with the same y coordinate as the caller
            Vector2Int AttachPoint = new Vector2Int((int)(_BlockTile.GridPosition.x + 0.5f), Caller.GridCoordinate.y);

            Debug.Log("Block sent a tile a request command to attach to: " + AttachPoint);
            _BlockTile.ParentGrid.RequestAttachment(_BlockTile, AttachPoint);

        }
    }

    

}
