using UnityEngine;

// Procedural leg animation using a planted-foot IK approach.
// Each foot is a world-space anchor; a step triggers when the anchor drifts
// too far from its ideal home position. Diagonal pairs (FR+BL, FL+BR) can
// step simultaneously; adjacent pairs block each other to preserve stability.
public class SpiderLegsAnimator : MonoBehaviour
{
    [Header("Leg Transforms")]
    [Tooltip("Front-right leg pivot transform")]
    [SerializeField] private Transform legFR;
    [Tooltip("Front-left leg pivot transform")]
    [SerializeField] private Transform legFL;
    [Tooltip("Back-right leg pivot transform")]
    [SerializeField] private Transform legBR;
    [Tooltip("Back-left leg pivot transform")]
    [SerializeField] private Transform legBL;

    [Header("Home Offsets (local space)")]
    [Tooltip("X = lateral spread from body centre, Y = front/back spread")]
    [SerializeField] private Vector2 footSpread = new Vector2(0.5f, 0.5f);

    [Header("Step")]
    [Tooltip("Distance (XZ) a planted foot must drift from its ideal before a step triggers")]
    [SerializeField] private float stepThreshold = 0.3f;
    [Tooltip("Random +/- variation applied to stepThreshold per leg at startup. Must be less than stepThreshold.")]
    [SerializeField] private float stepThresholdVariation = 0.12f;
    [Tooltip("Peak height of the arc traced during a step")]
    [SerializeField] private float stepHeight = 0.2f;
    [Tooltip("How fast the foot travels to its new target (higher = snappier steps)")]
    [SerializeField] private float stepSpeed = 8f;
    [Tooltip("If a foot drifts further than this, it snaps instantly without animation")]
    [SerializeField] private float snapDistance = 1.5f;

    [Header("Raycast")]
    [Tooltip("Height above the home position from which the ground raycast is fired")]
    [SerializeField] private float raycastOriginHeight = 1f;
    [Tooltip("Maximum downward raycast distance to detect the ground")]
    [SerializeField] private float raycastDistance = 3f;
    [Tooltip("Layers considered as ground for foot placement")]
    [SerializeField] private LayerMask groundMask = ~0;

    [Header("Body Bob")]
    [Tooltip("Root transform of the body mesh. Its Y position is driven by the spring to track average foot height.")]
    [SerializeField] private Transform bodyRoot;
    [Tooltip("Target height of bodyRoot above the average foot Y")]
    [SerializeField] private float bodyRestHeight = 0.5f;
    [Tooltip("Spring stiffness for body height tracking")]
    [SerializeField] private float bodySpring = 35f;
    [Tooltip("Spring damping to prevent oscillation")]
    [SerializeField] private float bodyDamping = 5f;
    [Tooltip("Degrees of tilt per unit of height difference between foot pairs")]
    [SerializeField] private float bodyTiltAmount = 25f;
    [Tooltip("Speed at which body tilt tracks foot height differences")]
    [SerializeField] private float bodyTiltSpeed = 8f;

    // Leg order: 0=FR, 1=FL, 2=BR, 3=BL
    // Adjacent pairs block each other so at most one leg per side steps at a time.
    // Diagonal pairs (FR+BL, FL+BR) are free to step simultaneously.
    private static readonly int[][] adjacents =
    {
        new[] { 1, 2 }, // FR blocked by FL, BR
        new[] { 0, 3 }, // FL blocked by FR, BL
        new[] { 0, 3 }, // BR blocked by FR, BL
        new[] { 1, 2 }  // BL blocked by FL, BR
    };

    private Vector3[] homeOffsets;
    private Transform[] legTransforms;

    // Per-leg rest pose, captured once at Init
    private Quaternion[] legRestLocalRot;
    private Vector3[] legRestLocalScale;
    // Direction from leg pivot to its home position, expressed in leg.parent local space.
    // Used as the "from" vector in FromToRotation so the leg always points toward its foot.
    private Vector3[] legRestForwardLocal;
    // Distance from pivot to home at rest — denominator for stretch-to-reach scaling.
    private float[] legRestLength;
    // Which local axis the mesh extends along (auto-detected). Scaled to make the tip reach the foot.
    private Vector3[] legStretchAxis;

    private float[] legStepThreshold;
    private Vector3[] planted;
    private Vector3[] currentFeet;
    private Vector3[] stepFrom;
    private Vector3[] stepTarget;
    private float[] stepProgress;
    private bool[] isStepping;

    private float bodyVelocity;
    private float bodyPitch;
    private float bodyRoll;

    private void OnEnable()
    {
        Init();
    }

    private void Init()
    {
        homeOffsets = new[]
        {
            new Vector3(footSpread.x, 0f, footSpread.y),
            new Vector3(-footSpread.x, 0f, footSpread.y),
            new Vector3(footSpread.x, 0f, -footSpread.y),
            new Vector3(-footSpread.x, 0f, -footSpread.y),
        };
        legTransforms = new[] { legFR, legFL, legBR, legBL };
        legRestLocalRot = new Quaternion[4];
        legRestLocalScale = new Vector3[4];
        legRestForwardLocal = new Vector3[4];
        legRestLength = new float[4];
        legStretchAxis = new Vector3[4];
        legStepThreshold = new float[4];
        planted = new Vector3[4];
        currentFeet = new Vector3[4];
        stepFrom = new Vector3[4];
        stepTarget = new Vector3[4];
        stepProgress = new float[4];
        isStepping = new bool[4];

        // Randomise per-leg thresholds slightly so legs don't all step in unison.
        for (int i = 0; i < 4; i++)
            legStepThreshold[i] = Mathf.Max(0.05f, stepThreshold + Random.Range(-stepThresholdVariation, stepThresholdVariation));

        for (int i = 0; i < 4; i++)
        {
            legRestLocalRot[i] = legTransforms[i].localRotation;
            legRestLocalScale[i] = legTransforms[i].localScale;

            Vector3 homeWorldPos = transform.TransformPoint(homeOffsets[i]);
            Vector3 homeDir = homeWorldPos - legTransforms[i].position;
            legRestLength[i] = homeDir.magnitude;

            legRestForwardLocal[i] = legTransforms[i].parent.InverseTransformDirection(homeDir.normalized);

            // Project the rest-forward direction into leg local space to find the dominant mesh axis.
            Vector3 forwardInLegLocal = Quaternion.Inverse(legRestLocalRot[i]) * legRestForwardLocal[i];
            legStretchAxis[i] = DominantAxis(forwardInLegLocal);

            planted[i] = SampleGround(homeWorldPos);
            currentFeet[i] = planted[i];
        }
    }

    private void Update()
    {
        if (planted == null) Init();

        UpdateLegs();
        UpdateBodyBob();
    }

    private void UpdateLegs()
    {
        for (int i = 0; i < 4; i++)
        {
            Vector3 ideal = SampleGround(transform.TransformPoint(homeOffsets[i]));
            float xzDist = Vector2.Distance(
                new Vector2(planted[i].x, planted[i].z),
                new Vector2(ideal.x, ideal.z));

            // Snap immediately on large displacement (teleport / spawn) to avoid a long animated catch-up.
            if (xzDist > snapDistance)
            {
                planted[i] = ideal;
                isStepping[i] = false;
            }

            Vector3 currentFoot;

            if (isStepping[i])
            {
                // Keep the target updated so the foot lands at the correct position
                // even if the body moved or rotated during the step animation.
                stepTarget[i] = ideal;

                stepProgress[i] = Mathf.Min(stepProgress[i] + Time.deltaTime * stepSpeed, 1f);

                currentFoot = Vector3.Lerp(stepFrom[i], stepTarget[i], stepProgress[i]);
                // Arc: sine curve peaks at the midpoint of the step.
                currentFoot.y += stepHeight * Mathf.Sin(stepProgress[i] * Mathf.PI);

                if (stepProgress[i] >= 1f)
                {
                    isStepping[i] = false;
                    planted[i] = stepTarget[i];
                    currentFoot = planted[i];
                }
            }
            else
            {
                if (xzDist > legStepThreshold[i] && !AnyAdjacentStepping(i))
                {
                    isStepping[i] = true;
                    stepProgress[i] = 0f;
                    stepFrom[i] = planted[i];
                    stepTarget[i] = ideal;
                }

                currentFoot = planted[i];
            }

            currentFeet[i] = currentFoot;
            OrientLeg(i, currentFoot);
        }
    }

    private void OrientLeg(int index, Vector3 footWorldPos)
    {
        Transform leg = legTransforms[index];
        Vector3 dir = footWorldPos - leg.position;
        if (dir.sqrMagnitude < 0.001f) return;

        // Rotate the leg so its rest-forward direction points toward the current foot position.
        // Working in leg.parent local space keeps this correct as the enemy rotates.
        Vector3 localDir = leg.parent.InverseTransformDirection(dir.normalized);
        Quaternion delta = Quaternion.FromToRotation(legRestForwardLocal[index], localDir);
        leg.localRotation = delta * legRestLocalRot[index];

        // Stretch the mesh along its dominant axis so the tip visually reaches the foot.
        if (legRestLength[index] > 0.001f)
        {
            float stretch = dir.magnitude / legRestLength[index];
            leg.localScale = Vector3.Scale(legRestLocalScale[index],
                Vector3.one + (stretch - 1f) * legStretchAxis[index]);
        }
    }

    // Returns the unit axis (X, Y, or Z) whose component has the largest absolute value in v.
    private static Vector3 DominantAxis(Vector3 v)
    {
        float ax = Mathf.Abs(v.x), ay = Mathf.Abs(v.y), az = Mathf.Abs(v.z);
        if (ax >= ay && ax >= az) return Vector3.right;
        if (ay >= ax && ay >= az) return Vector3.up;
        return Vector3.forward;
    }

    private bool AnyAdjacentStepping(int i)
    {
        foreach (int adj in adjacents[i])
            if (isStepping[adj]) return true;
        return false;
    }

    private Vector3 SampleGround(Vector3 worldPos)
    {
        Vector3 origin = worldPos + Vector3.up * raycastOriginHeight;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, raycastDistance, groundMask))
            return hit.point;
        return worldPos;
    }

    private void UpdateBodyBob()
    {
        if (bodyRoot == null) return;

        float avgY = 0f;
        for (int i = 0; i < 4; i++)
            avgY += planted[i].y;
        avgY /= 4f;

        float targetWorldY = avgY + bodyRestHeight;
        float currentWorldY = bodyRoot.position.y;

        // Simple spring-damper: accelerate toward target, dampen velocity each frame.
        bodyVelocity += (targetWorldY - currentWorldY) * bodySpring * Time.deltaTime;
        bodyVelocity -= bodyVelocity * bodyDamping * Time.deltaTime;

        Vector3 pos = bodyRoot.position;
        pos.y = currentWorldY + bodyVelocity * Time.deltaTime;
        bodyRoot.position = pos;

        // Tilt the body to match the slope defined by the four foot positions.
        // Pitch: front feet higher than back feet -> body tilts nose-up (negative X).
        // Roll:  right feet higher than left feet -> body tilts right (positive Z).
        float frontAvgY = (currentFeet[0].y + currentFeet[1].y) * 0.5f;
        float backAvgY = (currentFeet[2].y + currentFeet[3].y) * 0.5f;
        float rightAvgY = (currentFeet[0].y + currentFeet[2].y) * 0.5f;
        float leftAvgY = (currentFeet[1].y + currentFeet[3].y) * 0.5f;

        float targetPitch = (backAvgY - frontAvgY) * bodyTiltAmount;
        float targetRoll = (rightAvgY - leftAvgY) * bodyTiltAmount;

        bodyPitch = Mathf.Lerp(bodyPitch, targetPitch, bodyTiltSpeed * Time.deltaTime);
        bodyRoll = Mathf.Lerp(bodyRoll, targetRoll, bodyTiltSpeed * Time.deltaTime);
        bodyRoot.localRotation = Quaternion.Euler(bodyPitch, 0f, bodyRoll);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        var offsets = new[]
        {
            new Vector3(footSpread.x, 0f, footSpread.y),
            new Vector3(-footSpread.x, 0f, footSpread.y),
            new Vector3(footSpread.x, 0f, -footSpread.y),
            new Vector3(-footSpread.x, 0f, -footSpread.y),
        };
        for (int i = 0; i < offsets.Length; i++)
        {
            Vector3 home = transform.TransformPoint(offsets[i]);
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(home, 0.07f);
        }
    }
#endif
}
