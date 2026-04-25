using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(LineRenderer))]
public class RaySelector : MonoBehaviour
{
    [Header("Scene References")]
    [Tooltip("The hand controller used to cast the selection ray.")]
    [SerializeField] private Transform rayOrigin;

    [Header("Input")]
    [SerializeField] private InputActionReference selectAction;

    [Header("Ray Settings")]
    [SerializeField] private float maxDistance = 10f;
    [SerializeField] private LayerMask selectableLayers = ~0;
    [SerializeField] private string selectableTag = "Selectable";

    [Header("Highlight Materials")]
    [SerializeField] private Material hoverMaterial;
    [SerializeField] private Material selectedMaterial;

    private LineRenderer ray;
    private GameObject hoveredObject;
    private GameObject selectedObject;

    // Remember each object's original material so we can restore it.
    private Material hoveredOriginalMat;
    private Material selectedOriginalMat;

    public GameObject SelectedObject => selectedObject;

    private void Awake()
    {
        ray = GetComponent<LineRenderer>();
        ray.positionCount = 2;
    }

    private void OnEnable() => selectAction.action.Enable();
    private void OnDisable() => selectAction.action.Disable();

    private void Update()
    {
        UpdateRayAndHover();

        if (selectAction.action.WasPressedThisFrame())
            ConfirmSelection();
    }

    private void UpdateRayAndHover()
    {
        Vector3 origin = rayOrigin.position;
        Vector3 direction = rayOrigin.forward;
        Vector3 rayEnd = origin + direction * maxDistance;

        GameObject newHover = null;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, maxDistance, selectableLayers))
        {
            rayEnd = hit.point;
            if (hit.collider.CompareTag(selectableTag))
                newHover = hit.collider.gameObject;
        }

        // Update line renderer to draw to whatever the ray hit (or to max distance).
        ray.SetPosition(0, origin);
        ray.SetPosition(1, rayEnd);

        // Update hover state if we're now hovering a different object.
        if (newHover != hoveredObject)
        {
            ClearHover();
            hoveredObject = newHover;
            ApplyHover();
        }
    }

    private void ApplyHover()
    {
        if (hoveredObject == null) return;
        if (hoveredObject == selectedObject) return; // Don't re-color what's already selected.

        var renderer = hoveredObject.GetComponent<Renderer>();
        if (renderer == null) return;

        hoveredOriginalMat = renderer.sharedMaterial;
        renderer.material = hoverMaterial;
    }

    private void ClearHover()
    {
        if (hoveredObject == null) return;
        if (hoveredObject == selectedObject) { hoveredObject = null; return; }

        var renderer = hoveredObject.GetComponent<Renderer>();
        if (renderer != null && hoveredOriginalMat != null)
            renderer.material = hoveredOriginalMat;

        hoveredObject = null;
        hoveredOriginalMat = null;
    }

    private void ConfirmSelection()
    {
        // Deselect previous.
        if (selectedObject != null)
        {
            var prevRenderer = selectedObject.GetComponent<Renderer>();
            if (prevRenderer != null && selectedOriginalMat != null)
                prevRenderer.material = selectedOriginalMat;
        }

        selectedObject = hoveredObject;
        selectedOriginalMat = null;

        if (selectedObject == null) return;

        var renderer = selectedObject.GetComponent<Renderer>();
        if (renderer == null) return;

        // The hover state was already overriding the material; the truly original is what we stashed.
        selectedOriginalMat = hoveredOriginalMat;
        renderer.material = selectedMaterial;

        // Hover is now subsumed into selection — clear hover tracking but don't restore the material.
        hoveredObject = null;
        hoveredOriginalMat = null;
    }
}