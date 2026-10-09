using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Steering the chef by hand, as well as by tapping where he should go (PlayerFSM.Steer does the moving).
// On a computer: WASD or the arrow keys. On a phone: an invisible joystick - put a finger down anywhere on the
// kitchen and drag; he runs that way while the finger stays down, faster the further it is dragged, and the
// stick's centre follows the finger once it has been dragged all the way, so turning round is instant. A short
// touch that never turned into a drag is still a tap ("go there and use it"), sent when the finger lifts, so a
// drag never starts by sending him somewhere.
// Reads the Input System package where the project has it, the old input manager otherwise.
[RequireComponent(typeof(PlayerFSM))]
public class ChefStick : MonoBehaviour
{
    [SerializeField] private float deadZone = 0.025f;   // of the screen height: dragged less than this, it's a tap
    [SerializeField] private float fullTilt = 0.10f;    // of the screen height: dragged this far, he runs flat out
    [SerializeField] private float tapSeconds = 0.45f;  // held longer than this without moving: not a tap either

    // A finger is in charge: PlayerFSM leaves taps to this (they're sent on release), and ignores the mouse
    // presses a browser makes up from touches. A real mouse click a moment after the last touch takes it back.
    public static bool TouchDriven { get; private set; }

    // Where he should go: -1..1 on each axis, x to the right of the screen, y up it.
    public Vector2 Move { get; private set; }

    private PlayerFSM chef;
    private bool fingerDown, dragging, startedOnUI;
    private Vector2 origin, start;
    private float downAt, lastTouchAt = -10f;
    private static readonly List<RaycastResult> uiHits = new();

    // Past the title screen (the keys would otherwise walk him about behind it).
    public bool Playing { get; private set; }

    private void Awake() => chef = GetComponent<PlayerFSM>();

    private void OnEnable() => EventManager.OnLevelStart.AddListener(OnLevelStart);

    private void OnDisable()
    {
        EventManager.OnLevelStart.RemoveListener(OnLevelStart);
        Move = Vector2.zero;
        fingerDown = false;
    }

    private void OnLevelStart() => Playing = true;

    private void Update()
    {
        Vector2 finger = Finger();
        Move = finger != Vector2.zero ? finger : Keys();
    }

    #region Fingers
    private Vector2 Finger()
    {
        if (!ReadTouch(out bool pressed, out Vector2 position))
        {
            fingerDown = false;
            return Vector2.zero;
        }
        if (pressed) lastTouchAt = Time.unscaledTime;

        if (pressed && !fingerDown)
        {
            fingerDown = true;
            TouchDriven = true;
            dragging = false;
            start = origin = position;
            downAt = Time.unscaledTime;
            startedOnUI = OverUI(position);
            return Vector2.zero;
        }

        if (!pressed)
        {
            if (fingerDown && !startedOnUI && !dragging && Time.unscaledTime - downAt <= tapSeconds)
                chef.HandleScreenTap(start);
            fingerDown = false;
            return Vector2.zero;
        }

        if (startedOnUI) return Vector2.zero;
        float h = Mathf.Max(1, Screen.height);
        var drag = position - origin;
        if (!dragging && (position - start).magnitude > deadZone * h) dragging = true;
        if (!dragging) return Vector2.zero;

        float radius = fullTilt * h;
        if (drag.magnitude > radius) origin = position - drag.normalized * radius;   // the stick follows the finger
        return Vector2.ClampMagnitude((position - origin) / radius, 1f);
    }

    // A HUD button under the finger where it went down: that touch is the button's, not the kitchen's.
    private static bool OverUI(Vector2 position)
    {
        var events = EventSystem.current;
        if (events == null) return false;
        uiHits.Clear();
        events.RaycastAll(new PointerEventData(events) { position = position }, uiHits);
        foreach (var hit in uiHits)
            if (hit.module is UnityEngine.UI.GraphicRaycaster raycaster && raycaster.GetComponent<Canvas>().renderMode != RenderMode.WorldSpace)
                return true;
        return false;
    }

    private bool ReadTouch(out bool pressed, out Vector2 position)
    {
#if ENABLE_INPUT_SYSTEM
        var screen = Touchscreen.current;
        if (screen == null) { (pressed, position) = (false, default); return false; }
        var touch = screen.primaryTouch;
        pressed = touch.press.isPressed;
        position = touch.position.ReadValue();
        return true;
#else
        pressed = Input.touchCount > 0;
        position = pressed ? Input.GetTouch(0).position : default;
        return Input.touchSupported;
#endif
    }
    #endregion

    #region Keys
    private Vector2 Keys()
    {
        float x = 0f, y = 0f;
#if ENABLE_INPUT_SYSTEM
        var keys = Keyboard.current;
        if (keys != null)
        {
            if (keys.dKey.isPressed || keys.rightArrowKey.isPressed) x += 1f;
            if (keys.aKey.isPressed || keys.leftArrowKey.isPressed) x -= 1f;
            if (keys.wKey.isPressed || keys.upArrowKey.isPressed) y += 1f;
            if (keys.sKey.isPressed || keys.downArrowKey.isPressed) y -= 1f;
        }
        var mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame && Time.unscaledTime - lastTouchAt > 0.6f) TouchDriven = false;
#else
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) x += 1f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) x -= 1f;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) y += 1f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) y -= 1f;
        if (Input.GetMouseButtonDown(0) && Input.touchCount == 0 && Time.unscaledTime - lastTouchAt > 0.6f) TouchDriven = false;
#endif
        return Vector2.ClampMagnitude(new Vector2(x, y), 1f);
    }
    #endregion
}
