using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

// Temporary spriteless tile that is created as a placeholder when swapping a Griddable with an empty grid space.
public class SwapTempTile : Griddable
{
    override public bool Swappable { get; protected set; } = true;
    override public TileType Type { get; protected set; } = TileType.SwapTemp;

    public void InitializeDerived(PuzzleGrid _ParentGrid, int _KeyID, Vector2 _GridPosition)
    {
        bool _LockedToGrid = true;
        base.Initialize(_ParentGrid, _KeyID, _GridPosition, _LockedToGrid);
    }

    [Server]
    public override void UpdateSpriteServer()
    {
        SR_Background.sprite = null;
        SR_Icon.sprite = null;
        UpdateSpriteClient();
    }

    [ClientRpc]
    public override void UpdateSpriteClient()
    {
        SR_Background = gameObject.transform.Find("TileBackground").GetComponent<SpriteRenderer>();
        SR_Icon = gameObject.transform.Find("TileIcon").GetComponent<SpriteRenderer>();

        SR_Background.sprite = null;
        SR_Icon.sprite = null;
    }

    protected override void OnSwapComplete()
    {
        RequestDestruction(false);
    }

}
