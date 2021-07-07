using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Griddable
{

    protected int KeyID;
    protected GameObject GO;
    protected SpriteRenderer SR;
    protected PuzzleGrid ParentGrid;
    protected Vector2 GridPosition;
    protected enum State { Free, Set, Swapping, Clearing, Special}
    public bool LockedToGrid { get; private set; }
    

    protected Griddable(PuzzleGrid newParentGrid, int newKeyID, Vector2 newGridPosition, bool newLockedToGrid)
    {

        ParentGrid = newParentGrid;
        GridPosition = newGridPosition;
        LockedToGrid = newLockedToGrid;
        KeyID = newKeyID;

        GO = Object.Instantiate<GameObject>(Resources.Load<GameObject>("PuzzleTile"), ParentGrid.transform);
        SR = GO.GetComponent<SpriteRenderer>();
        GO.transform.position = ParentGrid.GridWorldPosition + GridPosition;

    }

    protected virtual void UpdateSprite() { }

    public void ShiftPosition(float _ShiftAmount)
    {
        if (LockedToGrid) Debug.LogError("Tried to shift a locked Griddable. Unlock first.");
        GridPosition += new Vector2(0, _ShiftAmount);
        GO.transform.position = ParentGrid.GridWorldPosition + GridPosition;
    }

    public void SetGridPosition(Vector2 newGridPosition)
    {
        GridPosition = newGridPosition;
        GO.transform.position = ParentGrid.GridWorldPosition + GridPosition;
    }

}
