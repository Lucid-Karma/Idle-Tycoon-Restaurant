using UnityEngine;

// The bun oven opens and shuts like an oven: the door swings down and the tray (with the bun on it) slides out,
// and back - always in that order, so the tray never goes through a closed door:
//   opening - the door swings open first, then the tray slides out;
//   closing - the tray slides in first, then the door shuts.
// It opens when the chef comes over with a raw bun, shuts on the bun while it bakes (it shows through the door's
// window), opens when the bun is ready or burnt so it can be seen and taken, and shuts again once it is taken;
// a short peek on every put-in and take-out. Only the door's hinge angle and the tray's position change; the
// bun rides on the tray (its food slot follows it).
// On the Oven (OvenStuff, the tray), set up by Tools/Chibi Cafe/12 (CafeFlowBuilder): the door is the oven
// model's own oven_door; both are taken out of static batching so they can move.
public class OvenDoor : MonoBehaviour
{
    [SerializeField] private Transform door;
    [SerializeField] private float openAngle = 90f;     // the hinge's local X angle when open (as modelled)
    [SerializeField] private float closedAngle = 15f;   // flush with the front: at 0 the door sinks into the oven body
    [SerializeField] private float swing = 0.22f;       // seconds for a full swing
    [SerializeField] private float trayTravel = 0.4f;   // how far the tray slides in, metres (deeper hid the bun behind the oven top)
    [SerializeField] private float slide = 0.2f;        // seconds for the tray's slide
    [SerializeField] private float welcomeDistance = 2.2f;

    private Oven oven;
    private PlayerFSM chef;
    private Quaternion baseRotation;
    private Vector3 trayOut, inward;
    private float angle, angleVelocity, tray = 1f, trayVelocity, peekUntil;
    private EdibleBase lastFood;

    private void Awake()
    {
        oven = GetComponent<Oven>();
        chef = FindFirstObjectByType<PlayerFSM>();
        if (door == null) { enabled = false; return; }
        // The modelled rotation, with its X (the hinge) taken out: the door turns about X from there.
        var e = door.localEulerAngles;
        baseRotation = Quaternion.Euler(0f, e.y, e.z);
        // The tray is placed out, resting on the open door; "in" is towards the back of the oven.
        trayOut = transform.position;
        var front = door.parent != null ? door.parent.forward : Vector3.forward;
        inward = -new Vector3(front.x, 0f, front.z).normalized;
        // It starts shut, with the tray inside.
        angle = closedAngle;
        tray = 0f;
        Apply();
    }

    private void Update()
    {
        var food = oven.Food;
        if (food != lastFood)
        {
            lastFood = food;
            peekUntil = Time.time + 0.35f;   // something went in or came out: the oven is open for it
        }

        float doorTarget, trayTarget;
        if (WantsOpen(food))
        {
            // Door first; the tray follows once the door is (nearly) down.
            doorTarget = openAngle;
            bool doorOpen = Mathf.Abs(angle - openAngle) < 12f;
            trayTarget = doorOpen ? 1f : tray;
        }
        else
        {
            // Tray first; the door follows once the tray is in.
            trayTarget = 0f;
            doorTarget = tray < 0.05f ? closedAngle : angle;
        }
        angle = Mathf.SmoothDamp(angle, doorTarget, ref angleVelocity, swing * 0.5f);
        tray = Mathf.SmoothDamp(tray, trayTarget, ref trayVelocity, slide * 0.5f);
        Apply();
    }

    private bool WantsOpen(EdibleBase food)
    {
        if (Time.time < peekUntil) return true;
        if (food != null) return food.Preparation != Prep.Raw;           // ready or burnt: show it
        // Empty: open for a chef on his way here with a raw bun.
        if (chef == null || chef.HeldFood is not Bun bun || bun.Preparation != Prep.Raw) return false;
        var offset = chef.transform.position - trayOut;
        offset.y = 0f;
        return offset.magnitude < welcomeDistance;
    }

    private void Apply()
    {
        door.localRotation = baseRotation * Quaternion.Euler(angle, 0f, 0f);
        transform.position = trayOut + inward * (trayTravel * (1f - tray));
        // The bun sits in the oven's food slot, made where the tray was: it rides along.
        if (oven.Slot != null) oven.Slot.position = transform.position;
    }
}
