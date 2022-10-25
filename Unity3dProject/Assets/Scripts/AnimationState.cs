using UnityEngine;

public enum AttackAnimationState
{
    attackStart = 0,
    attackPreHit = 1,
    attackHitStart = 2,
    attackHitFinish = 3,
    attackRecoverStart = 4,
    attackfinish = 5
}

public enum BlockAnimationState
{
    blockStart = 0,
    blockHitStart = 1,
    blockHitFinish = 2,
    blockRecoverStart = 3,
    blockfinish = 4
}
public enum BashAnimationState
{
    start = 0,
    preHit = 1,
    hitStart = 2,
    hitFinish = 3,
    finish = 4
}
public enum DodgeAnimationState
{
    dodgeStart = 0,
    dodgeEvadeStart = 1,
    dodgeEvadeFinish = 2,
    dodgefinish = 3
}
public enum AnimState_Casting
{
    castStart = 0,
    castPreFire = 1,
    castingFireStart = 2,
    castingFireFinish = 3,
    castfinish = 4
}
public enum DrawAnimationState
{
    start = 0,
    weaponGrab = 1,
    weaponOut = 2,
    weaponReady = 3,
    finish = 4
}

public enum SheathAnimationState
{
    start = 0,
    inactive = 1,
    mid = 2,
    finish = 3
}
public enum AnimState_Stagger
{
    start = 0,
    mid = 1,
    finish = 2
}
public enum AnimState_Knockdown
{
    knockdownStart = 0,
    knockdownMid = 1,
    knockdownFinish = 2,

    getUpStart = 3,
    getUpRecoverStart = 4,
    getUpFinish = 5,
}
public enum AnimState_GetUp
{
    start = 0,
    mid = 1,
    finish = 2
}

public enum AttackWeaponType
{
    melee = 0,
    missile = 1,
    magic = 2
}

public enum AttackType
{
    standard = 0,
    bash = 1,
}

public enum AttackSlotType
{
    right = 0,
    left = 1,
    both = 2,
    other
}


public class AnimationState : MonoBehaviour
{
    public bool bDisableAttacking = false;
    public bool bDisableBlocking = false;
    public bool isAttacking = false;
    public bool isBlocking = false;
    public bool isBashing = false;
    public bool isDodging = false;
    public bool isCasting = false;
    public bool isFlying = false;
    public bool isWeaponDrawn = false;
    public bool isMoving = false;
    public bool isSprinting = false;
    public bool isStaggered = false;
    public bool isKnockedDown = false;
    public bool isRagdolled = false;

    private AttackSlotType _attackSlotType;
    public AttackSlotType attackSlotType
    {
        get => _attackSlotType;
        set
        {
            _attackSlotType = value;
        }
    }

    [SerializeField] private AttackAnimationState _attackAnimationState = 0;
    public AttackAnimationState attackAnimationState
    {
        get => _attackAnimationState;
        set
        {
            _attackAnimationState = value;
            isAttacking = (_attackAnimationState < AttackAnimationState.attackHitFinish);
            // if (_attackAnimationState >= AttackAnimationState.attackHitFinish)
            // {
            //     isAttacking = false;
            // }
            // else
            // {
            //     isAttacking = true;
            // }
        }
    }

     [SerializeField] private BlockAnimationState _blockAnimationState = 0;
    public BlockAnimationState blockAnimationState
    {
        get => _blockAnimationState;
        set
        {
            _blockAnimationState = value;
            // if (_blockAnimationState > BlockAnimationState.blockHit) {
            //     isBlocking = false;
            // } else {
            //     isBlocking = true;
            // }
        }
    }
    private BashAnimationState _bashAnimationState = 0;
    public BashAnimationState bashAnimationState
    {
        get => _bashAnimationState;
        set
        {
            _bashAnimationState = value;
            isBashing = (_bashAnimationState < BashAnimationState.finish);
        }
    }

    [SerializeField] private DrawAnimationState _drawAnimationState = DrawAnimationState.finish;
    public DrawAnimationState drawAnimationState
    {
        get => _drawAnimationState;
        set
        {
            _drawAnimationState = value;
            isWeaponDrawn = (_drawAnimationState > DrawAnimationState.weaponOut);
        }
    }

    [SerializeField] private SheathAnimationState _sheathAnimationState = SheathAnimationState.finish;
    public SheathAnimationState sheathAnimationState
    {
        get => _sheathAnimationState;
        set
        {
            _sheathAnimationState = value;
            if (_sheathAnimationState > SheathAnimationState.start) isWeaponDrawn = false;
        }
    }
    private DodgeAnimationState _dodgeAnimationState = 0;
    public DodgeAnimationState dodgeAnimationState
    {
        get => _dodgeAnimationState;
        set
        {
            _dodgeAnimationState = value;
            isDodging = (_dodgeAnimationState < DodgeAnimationState.dodgefinish);
        }
    }

    private AnimState_Casting _castAnimationState = 0;
    public AnimState_Casting castAnimationState
    {
        get => _castAnimationState;
        set
        {
            _castAnimationState = value;
            isCasting = (_castAnimationState < AnimState_Casting.castfinish);
        }
    }

    [SerializeField]private AnimState_Stagger _animState_Stagger = 0;
    public AnimState_Stagger animState_Stagger
    {
        get => _animState_Stagger;
        set
        {
            _animState_Stagger = value;
            isStaggered = (_animState_Stagger < AnimState_Stagger.finish);
        }
    }

    [SerializeField]private AnimState_Knockdown _anim_knockdownstate = AnimState_Knockdown.getUpFinish;
    public AnimState_Knockdown anim_knockdownstate
    {
        get => _anim_knockdownstate;
        set
        {
            _anim_knockdownstate = value;
            isKnockedDown = (_anim_knockdownstate < AnimState_Knockdown.getUpRecoverStart);
        }
    }

    AttackWeaponType attackWeaponType = 0;

    public bool IsInAttackHitFame() => isAttacking && attackAnimationState == AttackAnimationState.attackHitStart;
    public bool IsInBlockHitFame() => isBlocking && blockAnimationState == BlockAnimationState.blockHitStart;
    public bool IsInBashHitFame() => isBashing && bashAnimationState == BashAnimationState.hitStart;
    public bool IsInCastFireFame() => isCasting && castAnimationState == AnimState_Casting.castingFireStart;
    public bool IsInDodgeEvadeFame() => isDodging && dodgeAnimationState == DodgeAnimationState.dodgeEvadeStart;
    public bool isSheathingWeapon() => sheathAnimationState < SheathAnimationState.finish;
    public bool isDrawingWeapon() => drawAnimationState < DrawAnimationState.finish;
    public bool IsStaggered() => isStaggered && animState_Stagger < AnimState_Stagger.finish;
    public bool IsAbleToAttack() => !isCasting && !bDisableAttacking && !IsInAttackHitFame() && !IsInBlockHitFame() && !IsStaggered() && !IsInBashHitFame() && !isSheathingWeapon() && !isDrawingWeapon();
    public bool IsAbleToBlock() =>  !isCasting && !bDisableBlocking && !IsInAttackHitFame() && !IsInBashHitFame() && !isSheathingWeapon() && !isDrawingWeapon();
}
