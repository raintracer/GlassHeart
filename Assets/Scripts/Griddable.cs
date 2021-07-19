using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public abstract class Griddable
{

    public int KeyID { get; private set; }
    readonly protected GameObject GO;
    protected SpriteRenderer SR_Background;
    protected SpriteRenderer SR_Icon;
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

    public int ChainLevel { get; set; } = 0;

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
        mono = GO.AddComponent<Mono>(); 
        GO.transform.position = ParentGrid.GridWorldPosition + GridPosition;

        SR_Background = GO.transform.Find("TileBackground").GetComponent<SpriteRenderer>();
        SR_Icon = GO.transform.Find("TileIcon").GetComponent<SpriteRenderer>();

        state = LockedToGrid ? State.Set : State.Free;

    }

    public void SetChain(int _ChainLevel)
    {
        // GameObject.Find("ChainText").GetComponent<TextMeshPro>().text = _ChainLevel.ToString();
        ChainLevel = _ChainLevel;
    }

    public bool IsClearing()
    {
        return (state == State.Clearing);
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

    public void ResetChainLevel()
    {
        SetChain(0);
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

        SR_Background.material = GameAssets.Material.Swap;
        SR_Icon.material = GameAssets.Material.Swap;
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
            SR_Background.material.SetFloat("_Offset", SwapOffset);
            SR_Icon.material.SetFloat("_Offset", SwapOffset);
            yield return new WaitForFixedUpdate();
            SwapOffset += OffsetChange;
        }

        SR_Background.material.SetFloat("_Offset", 0f);
        SR_Background.material = GameAssets.Material.Default;

        SR_Icon.material.SetFloat("_Offset", 0f);
        SR_Icon.material = GameAssets.Material.Default;

        OnSwapComplete();
    }

    private IEnumerator AnimateClear(int ClearOrder, int ClearTotal) // ClearOrder is the position of this tile in a clear set (zero-indexed), ClearTotal is the total number of tiles in the clear set
    {

        SR_Background.material = GameAssets.Material.ClearingFlash;

        // Flash for a set time
        for (int i = 0; i < CLEAR_FLASH_FRAMES; i++)
        {
            yield return new WaitForFixedUpdate();
        }

        // Wait To Bust
        SR_Background.material = GameAssets.Material.WaitingToBust;
        for (int i = 0; i < (ClearOrder + 1) * CLEAR_BUST_DELAY_FRAMES; i++)
        {
            yield return new WaitForFixedUpdate();
        }

        // Bust
        SR_Background.sprite = null;
        SR_Icon.sprite = null;
        GameAssets.Sound.DefaultBust.Play();

        ParticleController Particles = GameObject.Instantiate(Resources.Load<GameObject>("ParticleController")).GetComponent<ParticleController>();
        Particles.StartParticle("TilePop", GO.transform.position + new Vector3(0.5f, 0.5f, 0f), 1f);

        // Wait for others in the clear set to all bust
        for (int i = 0; i < (ClearTotal - ClearOrder) * CLEAR_BUST_DELAY_FRAMES; i++)
        {
            yield return new WaitForFixedUpdate();
        }

        // Request Destruction
        RequestDestruction(true);

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
            GameAssets.Sound.TileLand.Play();
        }
        else if (ParentGrid.GetTileKeyAtGridCoordinate(GridCheck) != 0)
        {
            // Request Attachment
            do
            {
                AttachPoint = AttachPoint + Vector2Int.up;
            } while (!ParentGrid.RequestAttachment(this, AttachPoint));
            GameAssets.Sound.TileLand.Play();
        }
        else
        {
            SetGridPosition(newGridPosition);
        }

    }

    protected void RequestDestruction(bool _Chain)
    {
        state = State.Dying;
        ParentGrid.DestroyRequest(this, _Chain);
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
