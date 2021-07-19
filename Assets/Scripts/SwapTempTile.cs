using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Temporary spriteless tile that is created as a placeholder when swapping a Griddable with an empty grid space.
public class SwapTempTile : Griddable
{
    override public bool Swappable { get; protected set; } = true;
    override public TileType Type { get; protected set; } = TileType.SwapTemp;

    public SwapTempTile(PuzzleGrid Grid, int _Key, Vector2 _GridPos) : base(Grid, _Key, _GridPos, true)
    {
        UpdateSprite();
    }

    protected override void UpdateSprite()
    {
        SR_Background.sprite = null;
        SR_Icon.sprite = null;
    }

    protected override void OnSwapComplete()
    {
        RequestDestruction(false);
    }

}
