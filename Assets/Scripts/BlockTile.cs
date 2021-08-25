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

    public enum BlockSection { Single, SingleLeft, SingleCenter, SingleRight }
    private BlockSection Section;

    public Block MyBlock;


    public BlockTile(PuzzleGrid Grid, int _Key, Vector2 _GridPos, bool _LockedToGrid, Block _MyBlock, BlockSection _BlockSection) : base(Grid, _Key, _GridPos, _LockedToGrid)
    {
        MyBlock = _MyBlock;
        Section = _BlockSection;
        InitializeSprite();
    }

    protected override void InitializeSprite()
    {

        Sprite BlockSprite = null;

        switch (Section)
        {
            case BlockSection.SingleLeft:
                BlockSprite = GameAssets.Sprite.BlockTileSingleLeft;
                break;
            case BlockSection.SingleCenter:
                BlockSprite = GameAssets.Sprite.BlockTileSingleCenter;
                break;
            case BlockSection.SingleRight:
                BlockSprite = GameAssets.Sprite.BlockTileSingleRight;
                break;
            case BlockSection.Single:
                BlockSprite = GameAssets.Sprite.BlockTileSingle;
                break;
            default:
                Debug.LogError("Unknown block sprite requested.");
                break;
        }

        SR_Background.sprite = null;
        SR_Icon.sprite = BlockSprite;

    }

    public void SetShade(float BlockShade)
    {
        float BaseValue = 0.6f;
        SR_Icon.color = new Color(BaseValue + BlockShade, BaseValue + BlockShade, BaseValue + BlockShade);
    }

    override protected void OnAttach()
    {

        if (MyBlock.State == Block.BlockState.Falling) { 
            // When this tile attaches, request attachment for all of its fellow blocktiles
            MyBlock.AttachAll(this);
        }

    }
    override public bool FallAllowed()
    {

        // Tile must be set to fall
        if (state != State.Set) return false;

        // Ask the block to check if all tiles can fall. If so, have it issue the fall command.
        return MyBlock.CheckForFallCondition(); 

    }

    override protected void RequestDestruction(bool _Chain)
    {
        state = State.Dying;
        MyBlock.RequestDestruction(this); 
        ParentGrid.DestroyRequest(this, _Chain);
    }

    override protected IEnumerator AnimateClear(int ClearOrder, int ClearTotal, bool HighChain) // ClearOrder is the position of this tile in a clear set (zero-indexed), ClearTotal is the total number of tiles in the clear set
    {

        SR_Icon.sprite = GameAssets.Sprite.BlockTileSingle;
        SR_Icon.material = GameAssets.Material.ClearingFlash;

        // Flash for a set time
        for (int i = 0; i < CLEAR_FLASH_FRAMES; i++)
        {
            yield return new WaitForFixedUpdate();
        }

        // Wait To Bust
        SR_Icon.material = GameAssets.Material.WaitingToBust;
        for (int i = 0; i < (ClearOrder + 1) * CLEAR_BUST_DELAY_FRAMES; i++)
        {
            yield return new WaitForFixedUpdate();
        }

        // Bust
        BasicTile.TileColor _TileColor = GameAssets.GetRandomTileColor();
        SR_Background.sprite = GameAssets.GetBackgroundSpriteByTileColor(_TileColor);
        SR_Icon.sprite = GameAssets.GetIconSpriteByTileColor(_TileColor);
        SR_Background.material = GameAssets.Material.Default;
        SR_Icon.material = GameAssets.Material.Default;
        GameAssets.Sound.DefaultBust.Play();
        ParticleController Particles = GameObject.Instantiate(Resources.Load<GameObject>("ParticleController")).GetComponent<ParticleController>();
        Particles.StartParticle("TilePop", GO.transform.position + new Vector3(0.5f, 0.5f, 0f), 0.5f);

        // Wait for others in the clear set to all bust
        for (int i = 0; i < (ClearTotal - ClearOrder) * CLEAR_BUST_DELAY_FRAMES; i++)
        {
            yield return new WaitForFixedUpdate();
        }

        // Request Destruction
        RequestDestruction(true);

        // Request replacement
        ParentGrid.RequestTileReplacement(_TileColor, GridCoordinate, GetChaining());

    }

    override protected IEnumerator AnimateLand()
    {
        yield return null;
    }


    public override void RequestBounceStart()
    {
        // Do nothing
    }

    public override void RequestBounceStop()
    {
        // Do nothing
    }

}
