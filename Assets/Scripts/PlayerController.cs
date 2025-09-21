using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Enhanced PlayerController with improved mobile touch controls.
/// Features multi-touch support, gesture recognition, and smooth responsive movement.
/// The player can move in all 4 directions (up, down, left, right) within defined boundaries.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("How smoothly the basket follows the input. Lower values are faster and more responsive.")]
    public float smoothTime = 0.05f;
    [Tooltip("The speed of the player when using keyboard controls.")]
    public float keyboardSpeed = 10f;
    [Tooltip("Maximum speed the player can move.")]
    public float maxSpeed = 15f;

    [Header("Boundaries")]
    [Tooltip("Horizontal padding from the screen edges in world units.")]
    public float horizontalPadding = 1f;
    [Tooltip("Vertical padding from the screen edges in world units.")]
    public float verticalPadding = 1f;
    [Tooltip("Enable movement boundaries. If false, player can move freely.")]
    public bool enableBoundaries = true;
    
    [Header("Touch Controls")]
    [Tooltip("Minimum distance a touch must move to be considered intentional (in pixels).")]
    public float touchDeadZone = 10f;
    [Tooltip("How sensitive the touch controls are. Higher values = more sensitive.")]
    public float touchSensitivity = 1.2f;
    [Tooltip("Enable haptic feedback on touch (requires mobile device).")]
    public bool enableHapticFeedback = true;
    [Tooltip("Minimum time between haptic feedback pulses (in seconds).")]
    public float hapticCooldown = 0.1f;
    [Tooltip("Enable swipe gesture controls for movement.")]
    public bool enableSwipeGestures = true;
    [Tooltip("Minimum swipe distance to trigger movement (in pixels).")]
    public float minSwipeDistance = 50f;
    [Tooltip("Speed multiplier for horizontal swipe-based movement.")]
    public float horizontalSwipeSpeedMultiplier = 2f;
    [Tooltip("Speed multiplier for vertical swipe-based movement.")]
    public float verticalSwipeSpeedMultiplier = 2f;

    // --- Private Fields ---
    private Rigidbody rb;
    private Vector3 targetPosition;
    private Vector3 velocity = Vector3.zero;
    private Camera mainCamera;
    private float minX, maxX;
    private float minY, maxY;
    
    // Touch tracking
    private Dictionary<int, TouchData> activeTouches = new Dictionary<int, TouchData>();
    private Vector2 primaryTouchStart;
    private int primaryTouchId = -1;
    private bool hasPrimaryTouch = false;
    private float lastHapticTime;
    private Vector2 swipeAccumulation = Vector2.zero;
    
    // Touch data structure
    private struct TouchData
    {
        public Vector2 startPosition;
        public Vector2 currentPosition;
        public Vector2 lastPosition;
        public float startTime;
        public bool isMoving;
    }

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Used to initialize components and variables.
    /// </summary>
    void Awake()
    {
        // Cache components for performance
        rb = GetComponent<Rigidbody>();
        mainCamera = Camera.main;

        // Null check for main camera
        if (mainCamera == null)
        {
            Debug.LogError("PlayerController: No main camera found! Please tag a camera as 'MainCamera'.");
            return;
        }

        // Calculate screen boundaries based on camera view
        SetupBoundaries();

        // Initialize target position to the player's starting position
        // This prevents the player from snapping to a default position on game start
        targetPosition = rb.position;
    }

    /// <summary>
    /// Calculates the movement boundaries based on the screen size and camera view for 4-directional movement.
    /// </summary>
    private void SetupBoundaries()
    {
        if (!enableBoundaries || mainCamera == null) return;
        
        // Calculate the distance from the camera to the object.
        float distance = Mathf.Abs(transform.position.z - mainCamera.transform.position.z);

        // Get the world coordinates for all edges of the viewport at the player's Z position.
        Vector3 leftBoundary = mainCamera.ViewportToWorldPoint(new Vector3(0, 0.5f, distance));
        Vector3 rightBoundary = mainCamera.ViewportToWorldPoint(new Vector3(1, 0.5f, distance));
        Vector3 bottomBoundary = mainCamera.ViewportToWorldPoint(new Vector3(0.5f, 0, distance));
        Vector3 topBoundary = mainCamera.ViewportToWorldPoint(new Vector3(0.5f, 1, distance));

        // Apply padding to all boundaries.
        minX = leftBoundary.x + horizontalPadding;
        maxX = rightBoundary.x - horizontalPadding;
        minY = bottomBoundary.y + verticalPadding;
        maxY = topBoundary.y - verticalPadding;

        // Ensure boundaries are valid (min < max)
        if (minX >= maxX)
        {
            Debug.LogWarning("PlayerController: Horizontal boundaries are invalid. Reducing horizontal padding.");
            float centerX = (leftBoundary.x + rightBoundary.x) * 0.5f;
            minX = centerX - 0.1f;
            maxX = centerX + 0.1f;
        }
        
        if (minY >= maxY)
        {
            Debug.LogWarning("PlayerController: Vertical boundaries are invalid. Reducing vertical padding.");
            float centerY = (bottomBoundary.y + topBoundary.y) * 0.5f;
            minY = centerY - 0.1f;
            maxY = centerY + 0.1f;
        }
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
    /// Enhanced touch input handling with multi-touch support and gesture recognition.
    /// </summary>
    private void HandleTouchInput()
    {
        // Early exit if camera is null
        if (mainCamera == null) return;
        
        // Process all active touches
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);
            ProcessTouch(touch);
        }
        
        // Clean up ended touches
        CleanupEndedTouches();
        
        // Handle swipe gestures if enabled
        if (enableSwipeGestures)
        {
            ProcessSwipeGestures();
        }
    }
    
    /// <summary>
    /// Processes individual touch input with enhanced phase handling.
    /// </summary>
    /// <param name="touch">The touch to process</param>
    private void ProcessTouch(Touch touch)
    {
        switch (touch.phase)
        {
            case TouchPhase.Began:
                OnTouchBegan(touch);
                break;
                
            case TouchPhase.Moved:
                OnTouchMoved(touch);
                break;
                
            case TouchPhase.Stationary:
                OnTouchStationary(touch);
                break;
                
            case TouchPhase.Ended:
            case TouchPhase.Canceled:
                OnTouchEnded(touch);
                break;
        }
    }
    
    /// <summary>
    /// Handles the beginning of a touch.
    /// </summary>
    /// <param name="touch">The touch that began</param>
    private void OnTouchBegan(Touch touch)
    {
        TouchData touchData = new TouchData
        {
            startPosition = touch.position,
            currentPosition = touch.position,
            lastPosition = touch.position,
            startTime = Time.time,
            isMoving = false
        };
        
        activeTouches[touch.fingerId] = touchData;
        
        // Set primary touch (first touch or closest to center)
        if (!hasPrimaryTouch || IsBetterPrimaryTouch(touch.position))
        {
            primaryTouchStart = touch.position;
            primaryTouchId = touch.fingerId;
            hasPrimaryTouch = true;
            
            // Provide haptic feedback
            if (enableHapticFeedback && Time.time - lastHapticTime > hapticCooldown)
            {
                ProvideTouchFeedback();
                lastHapticTime = Time.time;
            }
            
            // Don't update target position immediately on touch begin
            // Let the object stay where it is and only move when touch moves
        }
    }
    
    /// <summary>
    /// Handles touch movement.
    /// </summary>
    /// <param name="touch">The moving touch</param>
    private void OnTouchMoved(Touch touch)
    {
        if (activeTouches.ContainsKey(touch.fingerId))
        {
            TouchData touchData = activeTouches[touch.fingerId];
            touchData.lastPosition = touchData.currentPosition;
            touchData.currentPosition = touch.position;
            
            // Check if touch moved beyond dead zone
            float moveDistance = Vector2.Distance(touchData.startPosition, touch.position);
            if (moveDistance > touchDeadZone)
            {
                touchData.isMoving = true;
            }
            
            activeTouches[touch.fingerId] = touchData;
            
            // Update position if this is the primary touch and it's moving
            if (hasPrimaryTouch && touchData.isMoving && touch.fingerId == primaryTouchId)
            {
                // Calculate the movement delta in screen space
                Vector2 deltaMovement = touch.position - touchData.lastPosition;
                
                // Convert delta to world space and apply to target position
                UpdateTargetPositionWithDelta(deltaMovement);
            }
        }
    }
    
    /// <summary>
    /// Handles stationary touch (touch held in place).
    /// </summary>
    /// <param name="touch">The stationary touch</param>
    private void OnTouchStationary(Touch touch)
    {
        // For stationary touches, don't move the object
        // The object should only move when the touch actually moves
        // This prevents the object from jumping to the touch position when held
    }
    
    /// <summary>
    /// Handles the end of a touch.
    /// </summary>
    /// <param name="touch">The ended touch</param>
    private void OnTouchEnded(Touch touch)
    {
        if (activeTouches.ContainsKey(touch.fingerId))
        {
            // If this was the primary touch, find a new primary touch
            if (hasPrimaryTouch && touch.fingerId == primaryTouchId)
            {
                hasPrimaryTouch = false;
                primaryTouchId = -1;
                swipeAccumulation = Vector2.zero;
                
                // Find another active touch to become primary
                foreach (var kvp in activeTouches)
                {
                    if (kvp.Key != touch.fingerId)
                    {
                        primaryTouchStart = kvp.Value.currentPosition;
                        primaryTouchId = kvp.Key;
                        hasPrimaryTouch = true;
                        break;
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Cleans up touches that have ended.
    /// </summary>
    private void CleanupEndedTouches()
    {
        List<int> touchesToRemove = new List<int>();
        
        foreach (var touchId in activeTouches.Keys)
        {
            bool touchStillActive = false;
            for (int i = 0; i < Input.touchCount; i++)
            {
                if (Input.GetTouch(i).fingerId == touchId)
                {
                    touchStillActive = true;
                    break;
                }
            }
            
            if (!touchStillActive)
            {
                touchesToRemove.Add(touchId);
            }
        }
        
        foreach (int touchId in touchesToRemove)
        {
            activeTouches.Remove(touchId);
        }
    }
    
    /// <summary>
    /// Processes swipe gestures for 4-directional movement.
    /// </summary>
    private void ProcessSwipeGestures()
    {
        if (!hasPrimaryTouch) return;
        
        foreach (var touchData in activeTouches.Values)
        {
            if (touchData.isMoving)
            {
                Vector2 swipeDelta = touchData.currentPosition - touchData.lastPosition;
                
                // Accumulate both horizontal and vertical swipe for smoother movement
                swipeAccumulation.x += swipeDelta.x * horizontalSwipeSpeedMultiplier * Time.deltaTime;
                swipeAccumulation.y += swipeDelta.y * verticalSwipeSpeedMultiplier * Time.deltaTime;
                
                // Apply accumulated horizontal swipe movement
                if (Mathf.Abs(swipeAccumulation.x) > 1f)
                {
                    Vector3 worldDeltaX = mainCamera.ScreenToWorldPoint(new Vector3(swipeAccumulation.x, 0, Mathf.Abs(transform.position.z - mainCamera.transform.position.z))) - 
                                          mainCamera.ScreenToWorldPoint(new Vector3(0, 0, Mathf.Abs(transform.position.z - mainCamera.transform.position.z)));
                    
                    targetPosition.x += worldDeltaX.x;
                    swipeAccumulation.x = 0f;
                }
                
                // Apply accumulated vertical swipe movement (note: Y is inverted for screen coordinates)
                if (Mathf.Abs(swipeAccumulation.y) > 1f)
                {
                    Vector3 worldDeltaY = mainCamera.ScreenToWorldPoint(new Vector3(0, swipeAccumulation.y, Mathf.Abs(transform.position.z - mainCamera.transform.position.z))) - 
                                          mainCamera.ScreenToWorldPoint(new Vector3(0, 0, Mathf.Abs(transform.position.z - mainCamera.transform.position.z)));
                    
                    // Invert Y movement to match screen coordinate system
                    targetPosition.y -= worldDeltaY.y;
                    swipeAccumulation.y = 0f;
                }
                
                break; // Only process primary touch for swipes
            }
        }
    }
    
    /// <summary>
    /// Determines if a touch position would be a better primary touch.
    /// </summary>
    /// <param name="touchPosition">Position to evaluate</param>
    /// <returns>True if this would be a better primary touch</returns>
    private bool IsBetterPrimaryTouch(Vector2 touchPosition)
    {
        // Prefer touches closer to the center of the screen horizontally
        float screenCenterX = Screen.width * 0.5f;
        float currentDistance = Mathf.Abs(primaryTouchStart.x - screenCenterX);
        float newDistance = Mathf.Abs(touchPosition.x - screenCenterX);
        
        return newDistance < currentDistance;
    }
    
    /// <summary>
    /// Provides haptic feedback for touch interactions.
    /// </summary>
    private void ProvideTouchFeedback()
    {
        #if UNITY_ANDROID || UNITY_IOS
        if (SystemInfo.supportsVibration)
        {
            Handheld.Vibrate();
        }
        #endif
    }

    /// <summary>
    /// Checks for and processes mouse input.
    /// </summary>
    private void HandleMouseInput()
    {
        // Use the left mouse button as a substitute for touch in the editor
        if (Input.GetMouseButtonDown(0))
        {
            // Store the initial mouse position when first clicked
            primaryTouchStart = Input.mousePosition;
        }
        else if (Input.GetMouseButton(0))
        {
            // Calculate mouse movement delta and apply relative movement
            Vector2 currentMousePos = Input.mousePosition;
            Vector2 deltaMovement = currentMousePos - primaryTouchStart;
            
            // Only move if there's significant movement (similar to touch dead zone)
            if (deltaMovement.magnitude > touchDeadZone)
            {
                UpdateTargetPositionWithDelta(deltaMovement);
                primaryTouchStart = currentMousePos; // Update for next frame
            }
        }
    }

    /// <summary>
    /// Checks for and processes keyboard input for testing purposes (4-directional movement).
    /// </summary>
    private void HandleKeyboardInput()
    {
        // Horizontal movement
        if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
        {
            targetPosition.x -= keyboardSpeed * Time.deltaTime;
        }

        if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
        {
            targetPosition.x += keyboardSpeed * Time.deltaTime;
        }
        
        // Vertical movement
        if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W))
        {
            targetPosition.y += keyboardSpeed * Time.deltaTime;
        }
        
        if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S))
        {
            targetPosition.y -= keyboardSpeed * Time.deltaTime;
        }
    }

    /// <summary>
    /// Converts a screen position (from touch or mouse) to a world position
    /// and updates the target position for 4-directional movement.
    /// </summary>
    /// <param name="screenPosition">The position on the screen (in pixels).</param>
    private void UpdateTargetPosition(Vector2 screenPosition)
    {
        // Convert screen position to world position at the player's Z depth
        Vector3 worldPosition = mainCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, Mathf.Abs(transform.position.z - mainCamera.transform.position.z)));

        // Update the target position on both X and Y axes for 4-directional movement
        targetPosition.x = worldPosition.x;
        targetPosition.y = worldPosition.y;
    }
    
    /// <summary>
    /// Updates target position with touch sensitivity applied for enhanced 4-directional control.
    /// </summary>
    /// <param name="screenPosition">The position on the screen (in pixels).</param>
    private void UpdateTargetPositionWithSensitivity(Vector2 screenPosition)
    {
        // Convert screen position to world position at the player's Z depth
        Vector3 worldPosition = mainCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, Mathf.Abs(transform.position.z - mainCamera.transform.position.z)));
        
        // Apply touch sensitivity for more responsive control on both axes
        float deltaX = (worldPosition.x - targetPosition.x) * touchSensitivity;
        float deltaY = (worldPosition.y - targetPosition.y) * touchSensitivity;
        
        // Clamp the deltas to prevent overly sensitive movement
        float maxDelta = maxSpeed * Time.deltaTime;
        deltaX = Mathf.Clamp(deltaX, -maxDelta, maxDelta);
        deltaY = Mathf.Clamp(deltaY, -maxDelta, maxDelta);
        
        // Update the target position with sensitivity applied on both axes
        targetPosition.x += deltaX;
        targetPosition.y += deltaY;
    }
    
    /// <summary>
    /// Updates target position based on touch movement delta (relative movement).
    /// </summary>
    /// <param name="screenDelta">The movement delta in screen space (in pixels).</param>
    private void UpdateTargetPositionWithDelta(Vector2 screenDelta)
    {
        // Convert screen delta to world delta at the player's Z depth
        float distance = Mathf.Abs(transform.position.z - mainCamera.transform.position.z);
        
        Vector3 worldDelta = mainCamera.ScreenToWorldPoint(new Vector3(screenDelta.x, screenDelta.y, distance)) - 
                            mainCamera.ScreenToWorldPoint(new Vector3(0, 0, distance));
        
        // Apply touch sensitivity for more responsive control
        worldDelta *= touchSensitivity;
        
        // Clamp the deltas to prevent overly sensitive movement
        float maxDelta = maxSpeed * Time.deltaTime;
        worldDelta.x = Mathf.Clamp(worldDelta.x, -maxDelta, maxDelta);
        worldDelta.y = Mathf.Clamp(worldDelta.y, -maxDelta, maxDelta);
        
        // Update the target position with the relative movement
        targetPosition.x += worldDelta.x;
        targetPosition.y += worldDelta.y;
    }

    /// <summary>
    /// Called at a fixed interval. Used for physics-based 4-directional movement.
    /// </summary>
    void FixedUpdate() {
        // Clamp the target position to stay within the defined boundaries (if enabled)
        if (enableBoundaries)
        {
            targetPosition.x = Mathf.Clamp(targetPosition.x, minX, maxX);
            targetPosition.y = Mathf.Clamp(targetPosition.y, minY, maxY);
        }

        // Smoothly move the Rigidbody's position towards the clamped target position.
        // This creates a fluid, non-jittery movement in all directions.
        Vector3 newPosition = Vector3.SmoothDamp(rb.position, targetPosition, ref velocity, smoothTime);
        
        // Apply the new position to the Rigidbody
        rb.MovePosition(newPosition);
    }
}
