using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class BasicTile : Griddable
{
    public enum TileColor { Green, Blue, Indigo, Yellow, Red, Purple }

    [SyncVar] public TileColor Color;
    override public TileType Type { get; protected set; } = TileType.Basic;
    override public bool Swappable { get; protected set; } = true;


    public void InitializeDerived(PuzzleGrid _ParentGrid, int _KeyID, Vector2 _GridPosition, bool _LockedToGrid, TileColor _Color)
    {
        Color = _Color;
        base.Initialize(_ParentGrid, _KeyID, _GridPosition, _LockedToGrid);
    }

    [Server]
    public override void UpdateSpriteServer()
    {
        SR_Background.sprite = GameAssets.GetBackgroundSpriteByTileColor(Color);
        SR_Icon.sprite = GameAssets.GetIconSpriteByTileColor(Color);
        UpdateSpriteClient();
    }

    [ClientRpc]
    public override void UpdateSpriteClient()
    {
        SR_Background = gameObject.transform.Find("TileBackground").GetComponent<SpriteRenderer>();
        SR_Icon = gameObject.transform.Find("TileIcon").GetComponent<SpriteRenderer>();

        SR_Background.sprite = GameAssets.GetBackgroundSpriteByTileColor(Color);
        SR_Icon.sprite = GameAssets.GetIconSpriteByTileColor(Color);
    }

}
