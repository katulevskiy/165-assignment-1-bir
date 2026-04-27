using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(LineRenderer))]
public class TeleportController : MonoBehaviour
{
    [Header("Scene References")]
    [Tooltip("The hand controller used to aim the arc.")]
    [SerializeField] private Transform aimController;

    [Tooltip("The XR Origin root that we move when teleporting.")]
    [SerializeField] private Transform xrOrigin;

    [Tooltip("The Main Camera under the XR Origin.")]
    [SerializeField] private Transform xrCamera;

    [Header("Input")]
    [SerializeField] private InputActionReference teleportActivate;
    [SerializeField] private InputActionReference snapTurn;

    [Header("Arc Settings")]
    [SerializeField] private float launchSpeed = 8f;
    [SerializeField] private float gravity = 9.81f;
    [SerializeField] private int maxSteps = 60;
    [SerializeField] private float stepTime = 0.05f;
    [SerializeField] private float floorDotThreshold = 0.7f;
    [SerializeField] private LayerMask teleportableLayers = ~0;

    [Header("Snap Turn")]
    [SerializeField] private float snapTurnAngle = 30f;
    [SerializeField] private float snapTurnDeadzone = 0.7f;

    private LineRenderer arc;
    private bool isAiming;
    private bool hasValidTarget;
    private Vector3 teleportTarget;
    private bool snapTurnReady = true;

    private void Awake()
    {
        arc = GetComponent<LineRenderer>();
        arc.enabled = false;
    }

    private void OnEnable()
    {
        teleportActivate.action.Enable();
        snapTurn.action.Enable();
    }

    private void OnDisable()
    {
        teleportActivate.action.Disable();
        snapTurn.action.Disable();
    }

    private void Update()
    {
        HandleTeleport();
        HandleSnapTurn();
    }

    private void HandleTeleport()
    {
        if (teleportActivate.action.WasPressedThisFrame())
            isAiming = true;

        if (isAiming)
            DrawArc();

        if (teleportActivate.action.WasReleasedThisFrame())
        {
            if (hasValidTarget) ExecuteTeleport();
            isAiming = false;
            hasValidTarget = false;
            arc.enabled = false;
        }
    }

    private void DrawArc()
    {
        arc.enabled = true;
        Vector3 origin = aimController.position;
        Vector3 velocity = aimController.forward * launchSpeed;

        var points = new List<Vector3> { origin };
        Vector3 prev = origin;
        hasValidTarget = false;

        for (int i = 1; i <= maxSteps; i++)
        {
            float t = i * stepTime;
            Vector3 next = origin + velocity * t + 0.5f * Vector3.down * gravity * (t * t);

            if (Physics.Linecast(prev, next, out RaycastHit hit, teleportableLayers))
            {
                points.Add(hit.point);

                if (Vector3.Dot(hit.normal, Vector3.up) > floorDotThreshold)
                {
                    teleportTarget = hit.point;
                    hasValidTarget = true;
                }
                break;
            }

            points.Add(next);
            prev = next;
        }

        arc.positionCount = points.Count;
        arc.SetPositions(points.ToArray());
        arc.startColor = arc.endColor = hasValidTarget ? Color.green : Color.red;
    }

    private void ExecuteTeleport()
    {
        Vector3 cameraOffsetFromOrigin = xrCamera.position - xrOrigin.position;
        cameraOffsetFromOrigin.y = 0f;

        xrOrigin.position = teleportTarget - cameraOffsetFromOrigin;
    }

    private void HandleSnapTurn()
    {
        Vector2 stick = snapTurn.action.ReadValue<Vector2>();

        if (Mathf.Abs(stick.x) < snapTurnDeadzone)
        {
            snapTurnReady = true;
            return;
        }

        if (!snapTurnReady) return;

        float angle = stick.x > 0 ? snapTurnAngle : -snapTurnAngle;
        xrOrigin.RotateAround(xrCamera.position, Vector3.up, angle);
        snapTurnReady = false;
    }
}