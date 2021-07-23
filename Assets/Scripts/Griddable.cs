using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Mirror;

public abstract class Griddable: NetworkBehaviour
{

    // Component Fields
    protected SpriteRenderer SR_Background;
    protected SpriteRenderer SR_Icon;
    protected PuzzleGrid ParentGrid;

    // Type Fields
    public enum TileType { Basic, SwapTemp }
    public abstract TileType Type { get; protected set; }

    // Grid Fields
    [SyncVar] public Vector2 GridPosition;
    public Vector2Int GridCoordinate { get; private set; }
    public int KeyID { get; private set; }

    // Constant or Read-Only Fields
    const float FALL_SPEED = 0.4F;
    readonly static int SWAP_FRAMES = 4;
    readonly static int CLEAR_FLASH_FRAMES = 40;
    readonly static int CLEAR_BUST_DELAY_FRAMES = 10;

    // State Fields
    protected enum State { Free, Set, Swapping, Clearing, Dying, Special }
    protected State state;

    // Animation Control Fields
    public enum Animation { None, Swap, Land, Clear, Bounce };
    Coroutine AnimationRoutine = null;
    Animation CurentAnimation = Animation.None;
    public bool LockedToGrid { get; private set; }
    public int ChainLevel { get; set; } = 0;

    // Permission Fields
    public abstract bool Swappable { get; protected set; }


    protected void Awake()
    {
        enabled = false;
    }

    public virtual void Initialize(PuzzleGrid _ParentGrid, int _KeyID, Vector2 _GridPosition, bool _LockedToGrid)
    {
        ParentGrid = _ParentGrid;
        GridPosition = _GridPosition;
        LockedToGrid = _LockedToGrid;
        KeyID = _KeyID;
        SR_Background = gameObject.transform.Find("TileBackground").GetComponent<SpriteRenderer>();
        SR_Icon = gameObject.transform.Find("TileIcon").GetComponent<SpriteRenderer>();
        gameObject.transform.position = ParentGrid.GridWorldPosition + GridPosition;

        state = LockedToGrid ? State.Set : State.Free;
        enabled = true;
    }

    public virtual void UpdateSpriteServer() { }
    public virtual void UpdateSpriteClient() { }

    public void SetGridPosition(Vector2 newGridPosition)
    {
        GridPosition = newGridPosition;
        UpdateObjectPositionServer();
    }

    protected void UpdateObjectPositionServer() {
        gameObject.transform.position = ParentGrid.GridWorldPosition + GridPosition + new Vector2(0, ParentGrid.GridScrollOffset);
    }

    [ClientRpc]
    protected void UpdateObjectPositionClient()
    {
        gameObject.transform.position = ParentGrid.GridWorldPosition + GridPosition + new Vector2(0, ParentGrid.GridScrollOffset);
    }

    #region Public Field Accessors

    public Vector3 GetWorldPosition()
    {
        return gameObject.transform.position;
    }

    public bool IsClearing()
    {
        return (state == State.Clearing);
    }

    public bool IsSwapping()
    {
        return (state == State.Swapping);
    }

    #endregion

    #region Parent Grid Requests

    protected void RequestDestruction(bool _Chain)
    {
        state = State.Dying;
        ParentGrid.DestroyRequest(this, _Chain);
    }

    #endregion

    #region Parent Grid Commands

    public void SetChain(int _ChainLevel)
    {
        ChainLevel = _ChainLevel;
    }

    public void ResetChainLevel()
    {
        SetChain(0);
    }

    public void ChangeAttachmentCoordinate(Vector2Int _TileCoordinate)
    {
        GridCoordinate = _TileCoordinate;
    }

    public void Destroy()
    {
        GameObject.Destroy(gameObject);
    }

    public void Clear(int ClearOrder, int ClearTotal)
    {
        state = State.Clearing;
        ChangeAnimation(Animation.Clear, IntCommand1: ClearOrder, IntCommand2: ClearTotal);
    }

    public void ShiftPosition(float _ShiftAmount)
    {
        //if (LockedToGrid) Debug.LogError("Tried to shift a locked Griddable. Unlock first.");
        GridPosition += new Vector2(0, _ShiftAmount);
        UpdateObjectPositionServer();
    }

    public void FreeFall()
    {
        // Determine predicted new position
        Vector2 newGridPosition = GridPosition + new Vector2(0, -FALL_SPEED);

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

    public void RequestBounceStart()
    {
        if (state != State.Set) return;
        if (CurentAnimation == Animation.None) ChangeAnimation(Animation.Bounce);
    }

    public void RequestBounceStop()
    {
        if (CurentAnimation == Animation.Bounce) ChangeAnimation(Animation.None);
    }

    public void Swap(bool SwapRight)
    {
        if (!Swappable || !SwappingAllowed()) Debug.LogError("Illegal Swap Requested.");
        state = State.Swapping;
        ChangeAnimation(Animation.Swap, BoolCommand: SwapRight);
    }

    public void Unattach()
    {
        state = State.Free;
    }

    public void Attach(Vector2Int _TileCoordinate)
    {
        state = State.Set;
        GridCoordinate = _TileCoordinate;

        // Play landing animation
        ChangeAnimation(Animation.Land);
    }

    #endregion

    #region Permissions

    public bool ClearAllowed()
    {
        return (state == State.Set);
    }

    public bool SwappingAllowed()
    {
        return (Swappable && (state == State.Set));
    }

    public bool FallAllowed()
    {
        return (state == State.Set);
    }

    #endregion

    #region Animations

    public void ChangeAnimation(Animation _Animation, bool BoolCommand = false, int IntCommand1 = 0, int IntCommand2 = 0)
    {

        // Update Current Animation
        CurentAnimation = _Animation;

        // Stop any running Animation Routine
        if (AnimationRoutine != null)
        {
            StopCoroutine(AnimationRoutine);
        }

        // Reset Materials
        SR_Icon.material = GameAssets.Material.Default;
        SR_Background.material = GameAssets.Material.Default;

        // Determine which animation routine to play
        switch (_Animation)
        {
            case Animation.None:
                
                break;
            case Animation.Swap:
                AnimationRoutine = StartCoroutine(AnimateSwap(BoolCommand));
                break;
            case Animation.Clear:
                AnimationRoutine = StartCoroutine(AnimateClear(IntCommand1, IntCommand2));
                break;
            case Animation.Land:
                AnimationRoutine = StartCoroutine(AnimateLand());
                break;
            case Animation.Bounce:
                AnimationRoutine = StartCoroutine(AnimateBounce());
                break;
            default:
                Debug.LogError("Unrecognized animation requested: " + _Animation);
                break;
        }

    }
    private IEnumerator AnimateLand()
    {
        SR_Icon.material = GameAssets.Material.TileLand;
        SR_Icon.material.SetFloat("_StartTime", Time.time);
        yield return new WaitForSecondsRealtime(SR_Icon.material.GetFloat("_LifeTime"));
        ChangeAnimation(Animation.None);
    }

    private IEnumerator AnimateSwap(bool SwapRight)
    {

        SR_Background.material = GameAssets.Material.Swap;
        SR_Icon.material = GameAssets.Material.Swap;
        float SwapOffset, OffsetChange;
        if (SwapRight)
        {
            SwapOffset = -1f;
            OffsetChange = 1f / (float)SWAP_FRAMES;
        }
        else
        {
            SwapOffset = 1f;
            OffsetChange = -1f / (float)SWAP_FRAMES;
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
        ChangeAnimation(Animation.None);
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

        GameObject ParticleControllerObject = GameObject.Instantiate(Resources.Load<GameObject>("ParticleController"));
        ParticleController Particles = ParticleControllerObject.GetComponent<ParticleController>();
        Particles.StartParticle("TilePop", gameObject.transform.position + new Vector3(0.5f, 0.5f, 0f), 0.5f);

        // Wait for others in the clear set to all bust
        for (int i = 0; i < (ClearTotal - ClearOrder) * CLEAR_BUST_DELAY_FRAMES; i++)
        {
            yield return new WaitForFixedUpdate();
        }

        // Request Destruction
        RequestDestruction(true);

    }
    private IEnumerator AnimateBounce()
    {
        SR_Icon.material = GameAssets.Material.TileBounce;
        SR_Icon.material.SetFloat("_StartTime", Time.time);
        yield return null;
    }



    #endregion

    #region Animation Completion Methods
    virtual protected void OnSwapComplete()
    {
        state = State.Set;
        ParentGrid.PingUpdate(this);
    }

    #endregion

}
