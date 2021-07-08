using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicTile : Griddable
{
    public enum TileColor { Green, Blue, Indigo, Yellow, Red, Purple }

    public TileColor Color { get; protected set; }
    override public TileType Type { get; protected set; } = TileType.Basic;
    override public bool Swappable { get; protected set; } = true;


    public BasicTile(PuzzleGrid Grid, int _Key, TileColor _Color, Vector2 _GridPos, bool _LockedToGrid) : base(Grid, _Key, _GridPos, _LockedToGrid)
    {
        Color = _Color;
        UpdateSprite();
    }

    protected override void UpdateSprite()
    {
        SR.sprite = GameAssets.GetSpriteByTileColor(Color);
    }

}
