using UnityEngine;

public class MeshSocket : MonoBehaviour
{
    public MeshSockets.SocketId socketId;
    Transform attachPoint;

    void Start()
    {
        attachPoint = transform.GetChild(0);
    }

    public void Attach(Transform objectTransform)
    {
        objectTransform.SetParent(attachPoint, false);
    }
}
