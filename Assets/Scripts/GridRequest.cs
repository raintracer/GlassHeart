using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GridRequestType { Update, Clear, BlockClear, Destroy };

public struct GridRequest
{
    public GridRequestType Type;
    public Vector2Int Coordinate;
    public int ChainLevel;

    public void ShiftReference(int Positions)
    {
        Coordinate += new Vector2Int(0, Positions);
    }

}
