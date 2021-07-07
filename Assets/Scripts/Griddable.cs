using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Griddable
{

    readonly protected int KeyID;
    readonly protected GameObject GO;
    readonly protected SpriteRenderer SR;
    readonly protected Mono mono;
    readonly protected PuzzleGrid ParentGrid;
    protected Vector2 GridPosition;
    public abstract bool Swappable { get; protected set; }
    readonly static int SWAP_FRAMES = 8;
    protected enum State { Free, Set, Swapping, Clearing, Special }
    protected State state;
    public bool LockedToGrid { get; private set; }
    
    protected Griddable(PuzzleGrid newParentGrid, int newKeyID, Vector2 newGridPosition, bool newLockedToGrid)
    {

        ParentGrid = newParentGrid;
        GridPosition = newGridPosition;
        LockedToGrid = newLockedToGrid;
        KeyID = newKeyID;

        GO = Object.Instantiate<GameObject>(Resources.Load<GameObject>("PuzzleTile"), ParentGrid.transform);
        SR = GO.GetComponent<SpriteRenderer>();
        mono = GO.AddComponent<Mono>(); 
        GO.transform.position = ParentGrid.GridWorldPosition + GridPosition;
        
        state = LockedToGrid ? State.Set : State.Free;

    }

    protected virtual void UpdateSprite() { }

    public void ShiftPosition(float _ShiftAmount)
    {
        if (LockedToGrid) Debug.LogError("Tried to shift a locked Griddable. Unlock first.");
        GridPosition += new Vector2(0, _ShiftAmount);
        UpdateObjectPosition();
    }

    public void SetGridPosition(Vector2 newGridPosition)
    {
        GridPosition = newGridPosition;
        UpdateObjectPosition();
    }

    protected void UpdateObjectPosition() {
        GO.transform.position = ParentGrid.GridWorldPosition + GridPosition;
    }

    public bool SwappingAllowed()
    {
        return (Swappable && (state == State.Set));
    }

    public void Swap(bool SwapRight)
    {
        if (!Swappable || !SwappingAllowed()) Debug.LogError("Illegal Swap Requested.");
        state = State.Swapping;

        mono.StartCoroutine(AnimateSwap(SwapRight));
    }

    private IEnumerator AnimateSwap(bool SwapRight)
    {

        SR.material = GameAssets.Material.Swap;
        float SwapOffset, OffsetChange;
        if (SwapRight)
        {
            SwapOffset = -1f;
            OffsetChange = 1f / (float) SWAP_FRAMES;
        }
        else
        {
            SwapOffset = 1f;
            OffsetChange = -1f / (float) SWAP_FRAMES;
        }

        for (int i = 0; i < SWAP_FRAMES; i++)
        {
            SR.material.SetFloat("_Offset", SwapOffset);
            yield return new WaitForFixedUpdate();
            SwapOffset += OffsetChange;
        }

        SR.material.SetFloat("_Offset", 0f);
        state = State.Set;

    }

}
