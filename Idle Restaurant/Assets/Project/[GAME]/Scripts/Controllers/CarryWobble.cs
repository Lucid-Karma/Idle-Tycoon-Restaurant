using UnityEngine;

// The chef carries food in a hurry: whatever is in his hands leans against his acceleration, sways with
// his steps while he runs and settles with a little spring when he stops. Messy burgers wobble more
// (BurgerReview.Wobbliness). Purely visual: it only rotates the hand the food hangs from.
[RequireComponent(typeof(PlayerFSM))]
public class CarryWobble : MonoBehaviour
{
    [SerializeField] private float leanPerAcceleration = 1.6f;  // degrees per unit/s²
    [SerializeField] private float stepSway = 6f;               // degrees, side to side, at full run
    [SerializeField] private float stepsPerSecond = 2.6f;
    [SerializeField] private float maxLean = 14f;
    [SerializeField] private float stiffness = 140f, damping = 9f;

    private PlayerFSM fsm;
    private Transform hand;
    private Quaternion restLocal;
    private Vector3 lastVelocity;
    private Vector2 angle, angularVelocity;   // x = forward/back (about right), y = sideways (about forward)

    private void Start()
    {
        fsm = GetComponent<PlayerFSM>();
        hand = fsm.Hand;
        restLocal = hand.localRotation;
    }

    private void LateUpdate()
    {
        float dt = Mathf.Min(Time.deltaTime, 1f / 30f);
        if (dt <= 0f) return;

        var agent = fsm.Agent;
        Vector3 velocity = agent.velocity;
        Vector3 acceleration = (velocity - lastVelocity) / dt;
        lastVelocity = velocity;
        Vector3 local = transform.InverseTransformDirection(acceleration);
        float run01 = agent.speed > 0f ? Mathf.Clamp01(velocity.magnitude / agent.speed) : 0f;

        float wobbly = fsm.HeldFood is Hamburger burger ? burger.Review.Wobbliness : 1f;
        float step = Mathf.Sin(Time.time * stepsPerSecond * 2f * Mathf.PI);
        var target = new Vector2(-local.z, local.x) * leanPerAcceleration
                   + new Vector2(-2f * run01, step * stepSway * run01);
        target = Vector2.ClampMagnitude(target * wobbly, maxLean * wobbly);

        angularVelocity += (target - angle) * (stiffness * dt);
        angularVelocity *= Mathf.Exp(-damping * dt);
        angle += angularVelocity * dt;

        Quaternion rest = hand.parent != null ? hand.parent.rotation * restLocal : restLocal;
        hand.rotation = Quaternion.AngleAxis(angle.x, transform.right) * Quaternion.AngleAxis(angle.y, transform.forward) * rest;
    }
}
