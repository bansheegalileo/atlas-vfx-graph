using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.InputSystem;

public class ClickToVFX : MonoBehaviour
{
    public VisualEffect vfx;
    public float depth = 10f; // How far from the camera to project the cursor into world space

    void Update()
    {
        // Check if the left mouse button is being held down
        if (Mouse.current.leftButton.isPressed)
        {
            Vector2 mouseScreen = Mouse.current.position.ReadValue();
            Vector3 screenPos = new Vector3(mouseScreen.x, mouseScreen.y, depth);

            Vector3 worldPos = Camera.main.ScreenToWorldPoint(screenPos);

            // Send to VFX Graph every frame while dragging
            vfx.SetVector3("ClickPosition", worldPos);
        }
    }
}
