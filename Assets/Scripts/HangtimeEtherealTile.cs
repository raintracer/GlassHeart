using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Temporary invisible tile that prevents tiles from falling for a time, but may also be swapped. If swapped, if it is immediately destroyed in favor of a SwapTempTile
/// </summary>
public class HangtimeEtherealTile : Griddable
{
    override public bool Swappable { get; protected set; } = true;
    override public TileType Type { get; protected set; } = TileType.HangtimeEthereal;

    public HangtimeEtherealTile(PuzzleGrid Grid, int _Key, Vector2 _GridPos) : base(Grid, _Key, _GridPos, true)
    {
        InitializeSprite();
        mono.StartCoroutine(DestructionTimer());
        SetChaining(true);
    }

    protected override void InitializeSprite()
    {
        SR_Background.sprite = null;
        SR_Icon.sprite = null;
    }

    private IEnumerator DestructionTimer()
    {
        yield return new WaitForSeconds(0.1f);
        RequestDestruction(true);
    }

}
