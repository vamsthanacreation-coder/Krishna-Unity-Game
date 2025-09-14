using UnityEngine;

/// <summary>
/// Manages the player's basket movement based on mouse or touch input.
/// The basket is restricted to horizontal movement within defined boundaries.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("How smoothly the basket follows the input. Lower values are faster and more responsive.")]
    public float smoothTime = 0.05f;
    [Tooltip("The speed of the player when using keyboard controls.")]
    public float keyboardSpeed = 10f;

    [Header("Boundaries")]
    [Tooltip("Padding from the screen edges in world units.")]
    public float padding = 1f;

    // --- Private Fields ---
    private Rigidbody rb;
    private Vector3 targetPosition;
    private Vector3 velocity = Vector3.zero;
    private Camera mainCamera;
    private float minX;
    private float maxX;

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Used to initialize components and variables.
    /// </summary>
    void Awake()
    {
        // Cache components for performance
        rb = GetComponent<Rigidbody>();
        mainCamera = Camera.main;

        // Calculate screen boundaries based on camera view
        SetupBoundaries();

        // Initialize target position to the player's starting position
        // This prevents the player from snapping to a default position on game start
        targetPosition = rb.position;
    }

    /// <summary>
    /// Calculates the movement boundaries based on the screen size and camera view.
    /// </summary>
    private void SetupBoundaries()
    {
        // Calculate the distance from the camera to the object.
        float distance = transform.position.z - mainCamera.transform.position.z;

        // Get the world coordinates for the left and right edges of the viewport at the calculated distance.
        Vector3 leftBoundary = mainCamera.ViewportToWorldPoint(new Vector3(0, 0.5f, distance));
        Vector3 rightBoundary = mainCamera.ViewportToWorldPoint(new Vector3(1, 0.5f, distance));

        // Apply padding to the boundaries.
        minX = leftBoundary.x + padding;
        maxX = rightBoundary.x - padding;
    }

    /// <summary>
    /// Called every frame. Used to handle player input.
    /// </summary>
    void Update()
    {
        // Handle touch input for mobile devices
        HandleTouchInput();

        // Handle mouse input for easy testing in the Unity Editor
        // The UNITY_EDITOR directive ensures this code only runs in the Unity Editor
        #if UNITY_EDITOR
        HandleMouseInput();
        HandleKeyboardInput();
        #endif
    }

    /// <summary>
    /// Checks for and processes touch input.
    /// </summary>
    private void HandleTouchInput()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            // When the finger is on the screen (began or moved)
            if (touch.phase == TouchPhase.Began || touch.phase == TouchPhase.Moved)
            {
                UpdateTargetPosition(touch.position);
            }
        }
    }

    /// <summary>
    /// Checks for and processes mouse input.
    /// </summary>
    private void HandleMouseInput()
    {
        // Use the left mouse button as a substitute for touch in the editor
        if (Input.GetMouseButton(0))
        {
            UpdateTargetPosition(Input.mousePosition);
        }
    }

    /// <summary>
    /// Checks for and processes keyboard input for testing purposes.
    /// </summary>
    private void HandleKeyboardInput()
    {
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            targetPosition.x -= keyboardSpeed * Time.deltaTime;
        }

        if (Input.GetKey(KeyCode.RightArrow))
        {
            targetPosition.x += keyboardSpeed * Time.deltaTime;
        }
    }

    /// <summary>
    /// Converts a screen position (from touch or mouse) to a world position
    /// and updates the horizontal target position for the basket.
    /// </summary>
    /// <param name="screenPosition">The position on the screen (in pixels).</param>
    private void UpdateTargetPosition(Vector2 screenPosition)
    {
        // Create a ray from the camera to the screen position
        Ray ray = mainCamera.ScreenPointToRay(screenPosition);

        // We need a plane to determine the world position at the player's depth.
        // This plane is horizontal (normal is Vector3.up) and is at the player's current Y level.
        Plane gamePlane = new Plane(Vector3.up, new Vector3(0, rb.position.y, 0));

        // Check if the ray intersects with our game plane
        if (gamePlane.Raycast(ray, out float distance))
        {
            // Get the point where the ray intersects the plane
            Vector3 worldPosition = ray.GetPoint(distance);

            // Update the target position, but only on the horizontal (X) axis,
            // as the basket's movement is restricted to left and right.
            targetPosition.x = worldPosition.x;
        }
    }

    /// <summary>
    /// Called at a fixed interval. Used for physics-based movement.
    /// </summary>
    void FixedUpdate() {
        // Clamp the target's X position to stay within the defined boundaries
        targetPosition.x = Mathf.Clamp(targetPosition.x, minX, maxX);

        // Smoothly move the Rigidbody's position towards the clamped target position.
        // This creates a fluid, non-jittery movement.
        Vector3 newPosition = Vector3.SmoothDamp(rb.position, targetPosition, ref velocity, smoothTime);
        
        // Apply the new position to the Rigidbody
        rb.MovePosition(newPosition);
    }
}