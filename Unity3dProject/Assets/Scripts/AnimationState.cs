using UnityEngine;

public enum AttackAnimationState
{
    attackStart = 0,
    attackPreHit = 1,
    attackHitStart = 2,
    attackHitFinish = 3,
    attackfinish = 4    
}

public enum BlockAnimationState
{
    blockStart = 0,
    blockHitStart = 1,
    blockHitFinish = 2,
    blockfinish = 3    
}
public enum DodgeAnimationState
{
    dodgeStart = 0,
    dodgeEvadeStart = 1,
    dodgeEvadeFinish = 2,
    dodgefinish = 3    
}
public enum CastAnimationState
{
    castStart = 0,
    castingFireStart = 1,
    castingFireFinish = 2,
    castfinish = 3    
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

    public bool isAttacking = false;
    public bool isBlocking = false;
    public bool isDodging = false;
    public bool isCasting = false;
    public bool isWeaponDrawn = false;


    private AttackSlotType _attackSlotType;
    public AttackSlotType attackSlotType {

        get => _attackSlotType;
        set {
            _attackSlotType = value;
        }
    }

    private AttackAnimationState _attackAnimationState = 0;
    public AttackAnimationState attackAnimationState {

        get => _attackAnimationState;
        set {
            _attackAnimationState = value;
            if (_attackAnimationState > AttackAnimationState.attackHitFinish) {
                isAttacking = false;
            } else {
                isAttacking = true;
            }
        }
    }

    private BlockAnimationState _blockAnimationState = 0;
    public BlockAnimationState blockAnimationState {

        get => _blockAnimationState;
        set {
            _blockAnimationState = value;
            // if (_blockAnimationState > BlockAnimationState.blockHit) {
            //     isBlocking = false;
            // } else {
            //     isBlocking = true;
            // }
        }
    }

    private DrawAnimationState _drawAnimationState = DrawAnimationState.finish;
    public DrawAnimationState drawAnimationState {

        get => _drawAnimationState;
        set {
            _drawAnimationState = value;
            if (_drawAnimationState >= DrawAnimationState.weaponReady) {
                isWeaponDrawn = true;
            } else {
                isWeaponDrawn = false;
            }
        }
    }

    private SheathAnimationState _sheathAnimationState = SheathAnimationState.finish;
    public SheathAnimationState sheathAnimationState {

        get => _sheathAnimationState;
        set {
            _sheathAnimationState = value;
            if (_sheathAnimationState > SheathAnimationState.start) {
                isWeaponDrawn = false;
            }
        }
    }
    private DodgeAnimationState _dodgeAnimationState = 0;
    public DodgeAnimationState dodgeAnimationState {

        get => _dodgeAnimationState;
        set {
            _dodgeAnimationState = value;
            if (_dodgeAnimationState < DodgeAnimationState.dodgefinish) {
                isDodging= true;
            }else {
                isDodging = false;
            }
        }
    }

    CastAnimationState castAnimationState = 0;
    AttackWeaponType attackWeaponType = 0;

    public bool IsInAttackHitFame() {
        return isAttacking && attackAnimationState == AttackAnimationState.attackHitStart;
    }
    public bool IsInBlockHitFame() {
        return isBlocking && blockAnimationState == BlockAnimationState.blockHitStart;
    }
    public bool IsInDodgeEvadeFame() {
        return isDodging && dodgeAnimationState == DodgeAnimationState.dodgeEvadeStart;
    }
    public bool isSheathingWeapon() {
        return  sheathAnimationState < SheathAnimationState.finish;
    }
    public bool isDrawingWeapon() {
        return  drawAnimationState < DrawAnimationState.finish;
    }

}
