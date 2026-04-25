using UnityEngine;
using UnityEngine.InputSystem;

public class Manipulator : MonoBehaviour
{
    [Header("Scene References")]
    [Tooltip("Selector that tells us which object is currently selected via ray.")]
    [SerializeField] private RaySelector selector;

    [Tooltip("Right controller (primary grab hand).")]
    [SerializeField] private Transform rightHand;

    [Tooltip("Left controller (used for two-handed scaling).")]
    [SerializeField] private Transform leftHand;

    [Tooltip("Optional: direct-touch grab sensor on the right hand. If set, falls back to grabbing whatever is overlapping when no ray-selected object exists.")]
    [SerializeField] private DirectGrabSensor directSensor;

    [Header("Input")]
    [SerializeField] private InputActionReference rightGrip;
    [SerializeField] private InputActionReference leftGrip;

    [Header("Tuning")]
    [SerializeField] private float gripThreshold = 0.5f;
    [SerializeField] private float minScale = 0.1f;
    [SerializeField] private float maxScale = 5f;

    // State while grabbing
    private bool isGrabbing;
    private Transform grabbed;
    private Rigidbody grabbedRb;
    private bool grabbedWasKinematic;

    // Offsets captured at the moment of grab, so the object doesn't snap to the controller.
    private Vector3 grabPositionOffset;   // object pos relative to right hand at grab time
    private Quaternion grabRotationOffset; // object rot relative to right hand at grab time

    // Two-handed scaling state
    private bool isScaling;
    private float initialHandDistance;
    private Vector3 initialScale;

    private void OnEnable()
    {
        rightGrip.action.Enable();
        leftGrip.action.Enable();
    }

    private void OnDisable()
    {
        rightGrip.action.Disable();
        leftGrip.action.Disable();
    }

    private void Update()
    {
        bool rightDown = rightGrip.action.ReadValue<float>() > gripThreshold;
        bool leftDown = leftGrip.action.ReadValue<float>() > gripThreshold;

        // Handle grab start/stop based on right grip.
        if (rightDown && !isGrabbing)
            TryStartGrab();
        else if (!rightDown && isGrabbing)
            EndGrab();

        if (!isGrabbing) return;

        // Right hand drives translation + rotation.
        UpdateGrabTransform();

        // Left grip while grabbing → enter / update / exit two-handed scale mode.
        if (leftDown && !isScaling)
            StartScaling();
        else if (leftDown && isScaling)
            UpdateScaling();
        else if (!leftDown && isScaling)
            EndScaling();
    }

    private void TryStartGrab()
    {
        // Direct touch takes priority — if hand is physically on something, grab that.
        GameObject target = directSensor != null ? directSensor.ClosestSelectable : null;

        // Otherwise, fall back to ray-selected object.
        if (target == null && selector != null)
            target = selector.SelectedObject;

        if (target == null) return;

        grabbed = target.transform;
        grabbedRb = grabbed.GetComponent<Rigidbody>();

        if (grabbedRb != null)
        {
            grabbedWasKinematic = grabbedRb.isKinematic;
            grabbedRb.isKinematic = true;
        }

        grabPositionOffset = Quaternion.Inverse(rightHand.rotation) * (grabbed.position - rightHand.position);
        grabRotationOffset = Quaternion.Inverse(rightHand.rotation) * grabbed.rotation;

        isGrabbing = true;
    }

    private void UpdateGrabTransform()
    {
        if (grabbed == null) { isGrabbing = false; return; }

        // Apply the captured offset so rotation of the hand rotates the object around its grab point.
        grabbed.position = rightHand.position + rightHand.rotation * grabPositionOffset;
        grabbed.rotation = rightHand.rotation * grabRotationOffset;
    }

    private void EndGrab()
    {
        if (isScaling) EndScaling();

        if (grabbedRb != null)
            grabbedRb.isKinematic = grabbedWasKinematic;

        grabbed = null;
        grabbedRb = null;
        isGrabbing = false;
    }

    private void StartScaling()
    {
        if (grabbed == null) return;

        initialHandDistance = Vector3.Distance(rightHand.position, leftHand.position);
        if (initialHandDistance < 0.001f) initialHandDistance = 0.001f; // avoid divide-by-zero
        initialScale = grabbed.localScale;
        isScaling = true;
    }

    private void UpdateScaling()
    {
        if (grabbed == null) return;

        float currentDistance = Vector3.Distance(rightHand.position, leftHand.position);
        float ratio = currentDistance / initialHandDistance;

        Vector3 newScale = initialScale * ratio;
        // Clamp to keep things sane.
        float clamped = Mathf.Clamp(newScale.x, minScale, maxScale);
        grabbed.localScale = new Vector3(clamped, clamped, clamped);
    }

    private void EndScaling()
    {
        isScaling = false;
    }
}