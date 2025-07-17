using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMove : MonoBehaviour
{
    public Rigidbody2D rb;
    public float speed = 5f;

    private Vector2 moveInput;
    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            Debug.Log("Interact pressed!");
            // Your interaction logic here
        }
    }

    private void FixedUpdate()
    {
        Vector2 lockedInput = Vector2.zero;

        // Lock movement to a single axis (horizontal or vertical)
        if (Mathf.Abs(moveInput.x) > Mathf.Abs(moveInput.y))
        {
            lockedInput = new Vector2(moveInput.x, 0f);
        }
        else if (Mathf.Abs(moveInput.y) > 0f)
        {
            lockedInput = new Vector2(0f, moveInput.y);
        }

        rb.linearVelocity = lockedInput.normalized * speed;
        HandleAnimation(lockedInput);
    }

    private void HandleAnimation(Vector2 input)
    {
        bool isMoving = input != Vector2.zero;

        // Set basic movement parameters
        animator.SetFloat("moveX", input.x);
        animator.SetFloat("moveY", input.y);
        animator.SetBool("isMoving", isMoving);

        // If the player is moving, update the last direction
        if (isMoving)
        {
            animator.SetFloat("lastX", input.x);
            animator.SetFloat("lastY", input.y);
        }
    }
}
