using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicTile : Griddable
{
    public enum TileColor { Green, Blue, Indigo, Yellow, Red, Purple }
    [SerializeField] TileColor Color;
    override public bool Swappable { get; protected set; }


    public BasicTile(PuzzleGrid Grid, int _Key, TileColor _Color, Vector2 _GridPos, bool _LockedToGrid) : base(Grid, _Key, _GridPos, _LockedToGrid)
    {
        Color = _Color;
        Swappable = true;
        UpdateSprite();
    }

    protected override void UpdateSprite()
    {
        SR.sprite = GameAssets.GetSpriteByTileColor(Color);
    }

}
