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

    const float FALL_SPEED = 0.4F;
    public abstract bool Swappable { get; protected set; }
    readonly static int SWAP_FRAMES = 4;
    readonly static int CLEAR_FLASH_FRAMES = 40;
    readonly static int CLEAR_BUST_DELAY_FRAMES = 10;

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

    public bool FallAllowed()
    {
        return (state == State.Set);
    }

    virtual protected void OnSwapComplete()
    {
        state = State.Set;
        ParentGrid.PingUpdate(this);
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
        SR.material = GameAssets.Material.Default;

        OnSwapComplete();
    }

    private IEnumerator AnimateClear(int ClearOrder, int ClearTotal) // ClearOrder is the position of this tile in a clear set (zero-indexed), ClearTotal is the total number of tiles in the clear set
    {

        SR.material = GameAssets.Material.ClearingFlash;

        // Flash for a set time
        for (int i = 0; i < CLEAR_FLASH_FRAMES; i++)
        {
            yield return new WaitForFixedUpdate();
        }

        // Wait To Bust
        SR.material = GameAssets.Material.WaitingToBust;
        for (int i = 0; i < (ClearOrder + 1) * CLEAR_BUST_DELAY_FRAMES; i++)
        {
            yield return new WaitForFixedUpdate();
        }

        // Bust
        SR.sprite = null;
        GameAssets.Sound.DefaultBust.Play();

        // Wait for others in the clear set to all bust
        for (int i = 0; i < (ClearTotal - ClearOrder) * CLEAR_BUST_DELAY_FRAMES; i++)
        {
            yield return new WaitForFixedUpdate();
        }

        // Request Destruction
        RequestDestruction();

    }

    public void Unattach()
    {
        state = State.Free;
    }

    public void Attach(Vector2Int _TileCoordinate)
    {
        state = State.Set;
        GridCoordinate = _TileCoordinate;
        GameAssets.Sound.TileLand.Play();
    }

    public void ChangeAttachmentCoordinate(Vector2Int _TileCoordinate)
    {
        GridCoordinate = _TileCoordinate;
    }

    public void FreeFall()
    {
        // Determine predicted new position
        Vector2 newGridPosition = GridPosition + new Vector2(0, -FALL_SPEED * ParentGrid.TIME);

        // Determine if new GridPosition intersects a locked Tile
        Vector2Int GridCheck = new Vector2Int((int)(newGridPosition.x + 0.5f), (int)(newGridPosition.y));
        Vector2Int AttachPoint = GridCheck;

        if (GridCheck.y < 0)
        {
            // Request Attachment
            do
            {
                AttachPoint = AttachPoint + Vector2Int.up;
            } while (!ParentGrid.RequestAttachment(this, AttachPoint));
        }
        else if (ParentGrid.GetTileKeyAtGridCoordinate(GridCheck) != 0)
        {
            // Request Attachment
            do
            {
                AttachPoint = AttachPoint + Vector2Int.up;
            } while (!ParentGrid.RequestAttachment(this, AttachPoint));
        }
        else
        {
            SetGridPosition(newGridPosition);
        }

    }

    protected void RequestDestruction()
    {
        state = State.Clearing;
        ParentGrid.DestroyRequest(this);
    }

    public void Destroy()
    {
        GameObject.Destroy(GO);
    }

    public void Clear(int ClearOrder, int ClearTotal)
    {
        state = State.Clearing;
        _ = mono.StartCoroutine(AnimateClear(ClearOrder, ClearTotal));
    }

    public bool ClearAllowed()
    {
        return (state == State.Set);
    }

}
