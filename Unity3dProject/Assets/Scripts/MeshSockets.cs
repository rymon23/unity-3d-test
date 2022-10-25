using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshSockets : MonoBehaviour
{
    public enum SocketId
    {
        Spine = 0,
        RightHand,
        LeftHand,
        HipLeft,
        HipRight,
        MeleeRight,
        MeleeLeft,
        Rifle,
    }

    Dictionary<SocketId, MeshSocket> socketMap = new Dictionary<SocketId, MeshSocket>();
    public static SocketId GetWeaponSocketType(Weapon weapon, HandEquipSide handEquipSide)
    {
        switch (weapon.weaponType)
        {
            case WeaponType.sword:
                return handEquipSide == HandEquipSide.rightHand ? SocketId.MeleeRight : SocketId.MeleeLeft;
            // case WeaponType.gun:
            default:
                return SocketId.RightHand;
        }
    }

    void Start()
    {
        MeshSocket[] sockets = GetComponentsInChildren<MeshSocket>();
        foreach (var socket in sockets)
        {
            Debug.Log("MeshSockets => found socket: " + socket.socketId);

            socketMap[socket.socketId] = socket;
        }

    }

    public void Attach(Transform objectTransform, SocketId socketId)
    {
        Debug.Log("MeshSockets => Attach at: " + socketId);
        socketMap[socketId].Attach(objectTransform);
    }

}
