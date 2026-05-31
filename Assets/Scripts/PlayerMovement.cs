using UnityEngine;
using UnityEngine.InputSystem; // This is the crucial addition!

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 10f;
    public float turnSpeed = 150f;

    void Update()
    {
        // 1. Safety check: make sure a keyboard is actually connected
        if (Keyboard.current == null) return;

        float verticalInput = 0f;
        float horizontalInput = 0f;

        // 2. Read W and S keys (or Up/Down arrows) for forward and backward
        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
        {
            verticalInput = 1f;
        }
        else if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
        {
            verticalInput = -1f;
        }

        // 3. Read A and D keys (or Left/Right arrows) for turning
        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
        {
            horizontalInput = 1f;
        }
        else if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
        {
            horizontalInput = -1f;
        }

        // 4. Apply the exact same movement and rotation logic
        transform.Translate(Vector3.forward * verticalInput * moveSpeed * Time.deltaTime);
        transform.Rotate(Vector3.up * horizontalInput * turnSpeed * Time.deltaTime);
    }
}