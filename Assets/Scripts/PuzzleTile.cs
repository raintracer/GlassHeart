using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PuzzleTile
{

    int KeyID;
    public enum TileColor { Green, Blue, Indigo, Yellow, Red, Purple }
    GameObject GO;
    public bool LockedToGrid { get; private set; }
    SpriteRenderer SR;
    [SerializeField] TileColor Color;
    PuzzleGrid ParentGrid;
    Vector2 GridPosition;

    public PuzzleTile(PuzzleGrid newParentGrid, int newKeyID, TileColor newColor, Vector2 newGridPosition, bool newLockedToGrid)
    {
        ParentGrid = newParentGrid;
        Color = newColor;
        GridPosition = newGridPosition;
        LockedToGrid = newLockedToGrid;
        KeyID = newKeyID;

        GO = Object.Instantiate<GameObject>(Resources.Load<GameObject>("PuzzleTile"), ParentGrid.transform);
        SR = GO.GetComponent<SpriteRenderer>();
        GO.transform.position = ParentGrid.GridWorldPosition + GridPosition;
        UpdateColorSprite();
    }

    void UpdateColorSprite()
    {
        SR.sprite = GameAssets.GetSpriteByTileColor(Color);
    }

    public void ShiftPosition(float _ShiftAmount)
    {
        GridPosition += new Vector2(0, _ShiftAmount);
        GO.transform.position = ParentGrid.GridWorldPosition + GridPosition;
    }

    public void SetGridPosition(Vector2 newGridPosition)
    {
        GridPosition = newGridPosition;
        GO.transform.position = ParentGrid.GridWorldPosition + GridPosition;
    }

}
