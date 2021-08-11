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
    public string PathFinderSignature = "";

    public AIAction(string _PathFinderSignature, ActionType _Action, Vector2Int _TargetCoordinate, bool _ColumnMatters = true, bool _RowMatters = true)
    {
        PathFinderSignature = _PathFinderSignature;
        Action = _Action;
        TargetCoordinate = _TargetCoordinate;
        ColumnMatters = _ColumnMatters;
        RowMatters = _RowMatters;
    }

    static public AIAction FindVerticalSingleSwitchMatch(PuzzleGrid Grid)
    {

        Vector2Int GridSize = Grid.GridSize;
        bool SwapFound = false;
        Vector2Int SwapCoordinate = Vector2Int.down;

        for (int i = 0; i < GridSize.x; i++)
        {

            for (int j = PuzzleGrid.FLOOR_ROW + 2; j <= PuzzleGrid.CEILING_ROW; j++)
            {

                // Look for three tiles to be present and not clearing
                BasicTile[] CheckTiles = new BasicTile[3];
                bool SearchFailed = false;

                for (int k = 0; k < 3; k++)
                {
                    CheckTiles[k] = Grid.GetTileByGridCoordinate(i, j - k) as BasicTile;
                    if (CheckTiles[k] == null || !CheckTiles[k].IsSet())
                    {
                        SearchFailed = true;
                        break;
                    }
                }

                if (SearchFailed) continue;

                // Determine if there is at least two matching colors
                bool[] ColorMatches = new bool[3];
                Vector2Int[] IndexPairs = new Vector2Int[3] { new Vector2Int(0, 1), new Vector2Int(0, 2), new Vector2Int(1, 2) };
                BasicTile.TileColor MatchedColor = BasicTile.TileColor.Blue;

                foreach (Vector2Int IndexPair in IndexPairs)
                {
                    if (CheckTiles[IndexPair.x].Color == CheckTiles[IndexPair.y].Color)
                    {
                        ColorMatches[IndexPair.x] = true;
                        ColorMatches[IndexPair.y] = true;
                        MatchedColor = CheckTiles[IndexPair.x].Color;
                        break;
                    }
                }

                // Check fails if there is no color match
                if (ColorMatches[0] == false && ColorMatches[1] == false && ColorMatches[2] == false) continue;

                // If a match is found, determine the failed match coordinate
                Vector2Int UnmatchedCoordinate = Vector2Int.zero;
                for (int k = 0; k < 3; k++)
                {
                    if (ColorMatches[k] == false) UnmatchedCoordinate = CheckTiles[k].GridCoordinate;
                }

                // Check for the correct color on the left side of the unmatched coordinate
                if (UnmatchedCoordinate.x > 0)
                {
                    BasicTile _CheckTile = Grid.GetTileByGridCoordinate(UnmatchedCoordinate + Vector2Int.left) as BasicTile;
                    if (_CheckTile != null && _CheckTile.Color == MatchedColor)
                    {
                        SwapFound = true;
                        SwapCoordinate = UnmatchedCoordinate + Vector2Int.left;
                        break;
                    }
                }

                // If the left check failed, try again on the right side
                if (UnmatchedCoordinate.x < GridSize.x - 1)
                {
                    BasicTile _CheckTile = Grid.GetTileByGridCoordinate(UnmatchedCoordinate + Vector2Int.right) as BasicTile;
                    if (_CheckTile != null && _CheckTile.Color == MatchedColor)
                    {
                        SwapFound = true;
                        SwapCoordinate = UnmatchedCoordinate;
                        break;
                    }
                }

            }

            if (SwapFound) break;

        }

        // If a swap was found, return the appropriate Swap Action object
        if (SwapFound)
        {
            AIAction SwapAction = new AIAction(nameof(FindVerticalSingleSwitchMatch), ActionType.Swap, SwapCoordinate);
            return SwapAction;
        }

        // If no swap was found, return null
        return null;

    }

    static public AIAction FindHorizontalSingleSwitchMatch(PuzzleGrid Grid)
    {

        Vector2Int GridSize = Grid.GridSize;
        bool SwapFound = false;
        Vector2Int SwapCoordinate = Vector2Int.down;

        for (int i = 0; i < GridSize.x - 1; i++)
        {

            for (int j = PuzzleGrid.FLOOR_ROW; j <= PuzzleGrid.CEILING_ROW; j++)
            {

                // Look for two consecutive tiles to be present
                BasicTile[] CheckTiles = new BasicTile[2];
                bool SearchFailed = false;

                for (int k = 0; k < 2; k++)
                {
                    CheckTiles[k] = Grid.GetTileByGridCoordinate(i + k, j) as BasicTile;
                    if (CheckTiles[k] == null || !CheckTiles[k].IsSet())
                    {
                        SearchFailed = true;
                        break;
                    }
                }

                if (SearchFailed) continue;

                // Determine if the colors match
                BasicTile.TileColor MatchedColor;
                if (CheckTiles[0].Color == CheckTiles[1].Color)
                {
                    MatchedColor = CheckTiles[0].Color;
                }
                else
                {
                    // Check fails if the colors do not match, proceed to next iteration
                    continue;
                }

                // Check for the correct color twice to the left side of the matched pair, if the position is valid
                Vector2Int CheckCoordinate = CheckTiles[0].GridCoordinate + Vector2Int.left * 2;
                if (CheckCoordinate.x >= 0)
                {
                    BasicTile _CheckTile = Grid.GetTileByGridCoordinate(CheckCoordinate) as BasicTile;
                    if (_CheckTile != null && _CheckTile.Color == MatchedColor && _CheckTile.IsSet() && Grid.CoordinateIsSupported(_CheckTile.GridCoordinate))
                    {
                        SwapFound = true;
                        SwapCoordinate = CheckCoordinate;
                        break;
                    }
                }

                // If the left check failed, try again on the right side
                CheckCoordinate = CheckTiles[1].GridCoordinate + Vector2Int.right * 2;
                if (CheckCoordinate.x < GridSize.x)
                {
                    BasicTile _CheckTile = Grid.GetTileByGridCoordinate(CheckCoordinate) as BasicTile;
                    if (_CheckTile != null && _CheckTile.Color == MatchedColor && _CheckTile.IsSet() && Grid.CoordinateIsSupported(_CheckTile.GridCoordinate))
                    {
                        SwapFound = true;
                        SwapCoordinate = CheckCoordinate + Vector2Int.left;
                        break;
                    }
                }

            }

            if (SwapFound) break;

        }

        // If a swap was found, return the appropriate Swap Action object
        if (SwapFound)
        {
            AIAction SwapAction = new AIAction(nameof(FindHorizontalSingleSwitchMatch), ActionType.Swap, SwapCoordinate);
            return SwapAction;
        }

        // If no swap was found, return null
        return null;

    }

    static public AIAction FindLevelingSwapCrude(PuzzleGrid Grid)
    {

        Vector2Int GridSize = Grid.GridSize;
        bool SwapFound = false;
        Vector2Int SwapCoordinate = Vector2Int.down;

        for (int i = 0; i < GridSize.x; i++)
        {

            for (int j = PuzzleGrid.FLOOR_ROW; j <= PuzzleGrid.CEILING_ROW; j++)
            {

                // Check for the tile to be empty
                Vector2Int _TileCoordinate = new Vector2Int(i, j);
                Griddable _Tile = Grid.GetTileByGridCoordinate(_TileCoordinate);
                if (_Tile != null) continue;

                // Check for a horizontal neighbor that has a tile above it to the left
                Vector2Int CheckCoordinate = _TileCoordinate + Vector2Int.left + Vector2Int.up;
                if (CheckCoordinate.x >= 0)
                {
                    Griddable _CheckTileA = Grid.GetTileByGridCoordinate(CheckCoordinate);
                    Griddable _CheckTileB = Grid.GetTileByGridCoordinate(CheckCoordinate + Vector2Int.down);
                    if (_CheckTileA != null && _CheckTileA.IsSet() && _CheckTileB != null && _CheckTileB.IsSet())
                    {
                        SwapFound = true;
                        SwapCoordinate = _TileCoordinate + Vector2Int.left;
                        break;
                    }
                }

                // If the left check failed, try again on the right side
                CheckCoordinate = _TileCoordinate + Vector2Int.right + Vector2Int.up;
                if (CheckCoordinate.x < GridSize.x)
                {
                    Griddable _CheckTileA = Grid.GetTileByGridCoordinate(CheckCoordinate);
                    Griddable _CheckTileB = Grid.GetTileByGridCoordinate(CheckCoordinate + Vector2Int.down);
                    if (_CheckTileA != null && _CheckTileA.IsSet() && _CheckTileB != null && _CheckTileB.IsSet())
                    {
                        SwapFound = true;
                        SwapCoordinate = _TileCoordinate;
                        break;
                    }
                }

            }

            if (SwapFound) break;

        }

        // If a swap was found, return the appropriate Swap Action object
        if (SwapFound)
        {
            AIAction SwapAction = new AIAction(nameof(FindLevelingSwapCrude), ActionType.Swap, SwapCoordinate);
            return SwapAction;
        }

        // If no swap was found, return null
        return null;

    }

    static public AIAction FindLevelingSwap(PuzzleGrid Grid)
    {

        Vector2Int GridSize = Grid.GridSize;
        bool SwapFound = false;
        Vector2Int SwapCoordinate = Vector2Int.down;

        // Organize Columns by the Highest Swappable Tile
        List<int> ColumnIndices = new List<int>();
        for (int i = 0; i < GridSize.x; i++) ColumnIndices.Add(i);
        ColumnIndices.Sort(Grid.CompareColumnsByHighestSwappableTileAscending);

        // Iterate through the columns in preferred order
        for (int i = 0; i < GridSize.x; i++)
        {

            // Tracks the current x position and swappable height
            int Column = ColumnIndices[i];
            int ColumnHeight = Grid.GetColumnHeightFromSwappableTiles(Column);
            
            // If the height is zero, all viable options have been checked
            if (ColumnHeight == 0) break;

            // Iterate from the floor row to the column height
            for (int j = PuzzleGrid.FLOOR_ROW; j <= PuzzleGrid.FLOOR_ROW + ColumnHeight; j++)
            {

                // If the tile is not the highest, it can be rejected into any empty space to lower the column
                if (j < PuzzleGrid.FLOOR_ROW + ColumnHeight)
                {

                    // Check for the tile on the left side to be empty
                    Vector2Int _TileCoordinate = new Vector2Int(Column, j) + Vector2Int.left;
                    if (_TileCoordinate.x >= 0)
                    {
                        Griddable _Tile = Grid.GetTileByGridCoordinate(_TileCoordinate);
                        if (_Tile == null)
                        {
                            SwapFound = true;
                            SwapCoordinate = _TileCoordinate;
                        }
                    }
                    if (SwapFound == true) break;

                    // If the left check fails, try on the right
                    _TileCoordinate = new Vector2Int(Column, j) + Vector2Int.right;
                    if (_TileCoordinate.x < GridSize.x)
                    {
                        Griddable _Tile = Grid.GetTileByGridCoordinate(_TileCoordinate);
                        if (_Tile == null)
                        {
                            SwapFound = true;
                            SwapCoordinate = _TileCoordinate + Vector2Int.left;
                        }
                    }
                    if (SwapFound == true) break;
                }
                // If not found, and the tile is the highest swappable tile in the column, check multi-switch solutions
                else
                {

                    // Search on either side for a tile to reject to. There must be an empty tile on the same row with another empty tile on the row below it
                    bool[] DirectionViable = new bool[2] { false, false };
                    int[] DirectionDistance = new int[2] { 0, 0 };
                    int[] Direction = new int[2] {-1, 1};

                    // Search each direction
                    for (int k = 0; k <= 1; k++)
                    {
                        //Go until a block is in the way, or the swap direction is viable
                        for (int w = Column + Direction[k]; w >= 0 && w < GridSize.x; w += Direction[k])
                        {
                            
                            Vector2Int _CheckCoordinate = new Vector2Int(w, j);

                            // Give up on the current direction if it is blocked
                            if (Grid.CoordinateContainsAnyTile(_CheckCoordinate)) break;

                            if (!Grid.CoordinateIsSupported(_CheckCoordinate))
                            {
                                DirectionViable[k] = true;
                                DirectionDistance[k] = Mathf.Abs(Column - w);
                                break;
                            }

                        }
                    }

                    // If neither direction is viable, give up on this column
                    if (!DirectionViable[0] && !DirectionViable[1]) break;

                    // Prepare to select a direction
                    Vector2Int SelectedOffset = Vector2Int.zero;

                    // If both directions are viable, compare their distance, prefer left in case of a tie
                    if(DirectionViable[0] && DirectionViable[1])
                    {
                        if (DirectionDistance[0] <= DirectionDistance[1])
                        {
                            SelectedOffset = Vector2Int.left; // Select Left
                        }
                        else
                        {
                            SelectedOffset = Vector2Int.zero; // Select Right
                        }
                    } else if (DirectionViable[0])
                    {
                        SelectedOffset = Vector2Int.left; // Select Left
                    }
                    else
                    {
                        SelectedOffset = Vector2Int.zero; // Select Right
                    }

                    SwapFound = true;
                    SwapCoordinate = new Vector2Int(Column, j) + SelectedOffset;

                }

            }

            if (SwapFound) break;

        }

        // If a swap was found, return the appropriate Swap Action object
        if (SwapFound)
        {
            AIAction SwapAction = new AIAction(nameof(FindLevelingSwap), ActionType.Swap, SwapCoordinate);
            return SwapAction;
        }

        // If no swap was found, return null
        return null;

    }

}
