using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GridRequestType { Update, Clear, BlockClear, Destroy, ReplaceWithTile };

public struct GridRequest
{
    public GridRequestType Type;
    public Vector2Int Coordinate;
    public int ChainLevel;
    public BasicTile.TileColor TileColor;

    public void ShiftReference(int Positions)
    {
        Coordinate += new Vector2Int(0, Positions);
    }

}
