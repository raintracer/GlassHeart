using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIAction
{
    public enum ActionType { Swap, ScrollBoost, FireSpell, AirSpell, EarthSpell, WaterSpell};
    public ActionType Action;
    public bool ColumnMatters;
    public bool RowMatters;
    public Vector2Int TargetCoordinate;

    public AIAction(ActionType _Action, Vector2Int _TargetCoordinate, bool _ColumnMatters = true, bool _RowMatters = true)
    {
        Action = _Action;
        TargetCoordinate = _TargetCoordinate;
        ColumnMatters = _ColumnMatters;
        RowMatters = _RowMatters;
    }

}
