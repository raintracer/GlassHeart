using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// BlockTile inherits from Griddable. 
/// It contains the methods and properties associated with a block's tile entities.
/// BlockTile sends events to the Block class to keep all associated BlockTiles synchronized.
/// </summary>
public class BlockTile : Griddable
{

    override public TileType Type { get; protected set; } = TileType.Block;
    override public bool Swappable { get; protected set; } = false;

    public Block MyBlock;


    public BlockTile(PuzzleGrid Grid, int _Key, Vector2 _GridPos, bool _LockedToGrid, Block _MyBlock) : base(Grid, _Key, _GridPos, _LockedToGrid)
    {
        MyBlock = _MyBlock;
        UpdateSprite();
    }

    protected override void UpdateSprite()
    {
        SR_Background.sprite = GameAssets.Sprite.BlockTile;
        SR_Icon.sprite = null;
    }

    override protected void OnAttach()
    {

        if (MyBlock.State == Block.BlockState.Falling) { 
            // When this tile attaches, request attachment for all of its fellow blocktiles
            MyBlock.AttachAll(this);
        }

    }

    //IMPLEMENT
    override public bool FallAllowed()
    {
        return (state == State.Set);
    }

}
