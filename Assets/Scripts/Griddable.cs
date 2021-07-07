using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Griddable
{

    public int KeyID { get; private set; }
    readonly protected GameObject GO;
    readonly protected SpriteRenderer SR;
    readonly protected Mono mono;
    readonly protected PuzzleGrid ParentGrid;
    public enum TileType { Basic, SwapTemp }
    public abstract TileType Type { get; protected set; }

    public Vector2 GridPosition { get; private set; }
    public Vector2Int GridCoordinate { get; private set; }

    const float FALL_SPEED = 0.01F;
    public abstract bool Swappable { get; protected set; }
    readonly static int SWAP_FRAMES = 4;
    protected enum State { Free, Set, Swapping, Clearing, Dying, Special }
    protected State state;
    public bool LockedToGrid { get; private set; }
    
    protected Griddable(PuzzleGrid newParentGrid, int newKeyID, Vector2 newGridPosition, bool newLockedToGrid)
    {

        ParentGrid = newParentGrid;
        GridPosition = newGridPosition;
        LockedToGrid = newLockedToGrid;
        KeyID = newKeyID;

        GO = Object.Instantiate<GameObject>(Resources.Load<GameObject>("BasicTile"), ParentGrid.transform);
        SR = GO.GetComponent<SpriteRenderer>();
        mono = GO.AddComponent<Mono>(); 
        GO.transform.position = ParentGrid.GridWorldPosition + GridPosition;
        
        state = LockedToGrid ? State.Set : State.Free;

    }

    protected virtual void UpdateSprite() { }

    public void ShiftPosition(float _ShiftAmount)
    {
        //if (LockedToGrid) Debug.LogError("Tried to shift a locked Griddable. Unlock first.");
        GridPosition += new Vector2(0, _ShiftAmount);
        UpdateObjectPosition();
    }

    public void SetGridPosition(Vector2 newGridPosition)
    {
        GridPosition = newGridPosition;
        UpdateObjectPosition();
    }

    protected void UpdateObjectPosition() {
        GO.transform.position = ParentGrid.GridWorldPosition + GridPosition + new Vector2(0, ParentGrid.GridScrollOffset);
    }

    public bool SwappingAllowed()
    {
        return (Swappable && (state == State.Set));
    }

    abstract protected void OnSwapComplete();

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
        SR.material = GameAssets.Material.Default;

        ParentGrid.PingUpdate(this);
        OnSwapComplete();

    }

    public void Unattach()
    {
        state = State.Free;
    }

    public void Attach(Vector2Int _TileCoordinate)
    {
        state = State.Set;
        GridCoordinate = _TileCoordinate;
    }

    public void ChangeAttachmentCoordinate(Vector2Int _TileCoordinate)
    {
        GridCoordinate = _TileCoordinate;
    }

    public void FreeFall()
    {
        // Determine predicted new position
        Vector2 newGridPosition = GridPosition + new Vector2(0, -FALL_SPEED);

        // Determine if new GridPosition intersects a locked Tile
        Vector2Int GridCheck = new Vector2Int((int)(newGridPosition.x + 0.5f), (int)(newGridPosition.y));

        if (GridCheck.y < 0)
        {
            // Request Attachment
            ParentGrid.RequestAttachment(this, GridCheck + Vector2Int.up);
        }
        else if (ParentGrid.GetTileKeyAtGridCoordinate(GridCheck) != 0)
        {
            // Request Attachment
            ParentGrid.RequestAttachment(this, GridCheck + Vector2Int.up);
        }
        else
        {
            SetGridPosition(newGridPosition);
        }

    }

    protected void RequestDestruction()
    {
        state = State.Dying;
        ParentGrid.DestroyRequest(this);
    }

    public void Destroy()
    {
        GameObject.Destroy(GO);
    }

}
