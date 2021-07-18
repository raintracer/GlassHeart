using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClearSet
{
    HashSet<Vector2Int> VectorSet;
    int FrameLifetime;

    public ClearSet(List<Vector2Int> _VectorSet, int _FrameLifetime)
    {
        VectorSet = new HashSet<Vector2Int>(_VectorSet);
        FrameLifetime = _FrameLifetime;
    }

    void FixedUpdate()
    {
        FrameLifetime--;
    }

    public void ShiftUp()
    {
        List<Vector2Int> VectorList = new List<Vector2Int>(VectorSet);
        VectorSet.Clear();
        for (int i = 0; i < VectorList.Count; i++) VectorList.Add(VectorList[i] + new Vector2Int(0, 1));
    }

}
