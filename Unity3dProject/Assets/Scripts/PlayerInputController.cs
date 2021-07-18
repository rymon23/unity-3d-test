using UnityEngine;

public class PlayerInputController : MonoBehaviour
{
    [SerializeField] private float _moveZ;
    [SerializeField] private float _moveX;
    [SerializeField] private bool _spaceKey;
    [SerializeField] private bool _leftShift;
    [SerializeField] private bool _leftControl;
    [SerializeField] private bool _leftMouse;
    [SerializeField] private bool _rightMouse;

    // Update is called once per frame
    void Update()
    {
        GetInputs();
    }

    private void GetInputs() {
        _moveX = Input.GetAxis("Horizontal");
        _moveZ = Input.GetAxis("Vertical");
        _leftShift = Input.GetKey(KeyCode.LeftShift);
        _spaceKey = Input.GetKeyDown(KeyCode.Space);
        _leftControl = Input.GetKeyDown(KeyCode.LeftControl);
        _leftMouse = Input.GetKeyDown(KeyCode.Mouse0);
        _rightMouse = Input.GetKeyDown(KeyCode.Mouse0);
    }
}
