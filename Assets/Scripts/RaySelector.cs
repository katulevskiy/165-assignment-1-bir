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

    private GameObject currentRayTarget;

    private GameObject hoveredObject;
    private Material hoveredOriginalMat;

    private GameObject selectedObject;
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

        GameObject rayTarget = null;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, maxDistance, selectableLayers))
        {
            rayEnd = hit.point;
            if (hit.collider.CompareTag(selectableTag))
                rayTarget = hit.collider.gameObject;
        }

        ray.SetPosition(0, origin);
        ray.SetPosition(1, rayEnd);

        GameObject newHover = (rayTarget != null && rayTarget != selectedObject) ? rayTarget : null;

        if (newHover != hoveredObject)
        {
            ClearHover();
            hoveredObject = newHover;
            ApplyHover();
        }

        currentRayTarget = rayTarget;
    }

    private void ApplyHover()
    {
        if (hoveredObject == null) return;

        var renderer = hoveredObject.GetComponent<Renderer>();
        if (renderer == null) return;

        hoveredOriginalMat = renderer.sharedMaterial;
        renderer.material = hoverMaterial;
    }

    private void ClearHover()
    {
        if (hoveredObject == null) return;

        var renderer = hoveredObject.GetComponent<Renderer>();
        if (renderer != null && hoveredOriginalMat != null)
            renderer.material = hoveredOriginalMat;

        hoveredObject = null;
        hoveredOriginalMat = null;
    }

    private void ConfirmSelection()
    {
        // aiming at selected object - deselect.
        if (currentRayTarget != null && currentRayTarget == selectedObject)
        {
            DeselectCurrent();
            return;
        }

        // different selectable or on nothing.
        DeselectCurrent();

        if (currentRayTarget == null) return;

        var renderer = currentRayTarget.GetComponent<Renderer>();
        if (renderer == null) return;

        selectedObject = currentRayTarget;
        selectedOriginalMat = hoveredOriginalMat != null ? hoveredOriginalMat : renderer.sharedMaterial;

        renderer.material = selectedMaterial;

        // hover consumed by selection.
        hoveredObject = null;
        hoveredOriginalMat = null;
    }

    private void DeselectCurrent()
    {
        if (selectedObject == null) return;

        var renderer = selectedObject.GetComponent<Renderer>();
        if (renderer != null && selectedOriginalMat != null)
            renderer.material = selectedOriginalMat;

        selectedObject = null;
        selectedOriginalMat = null;
    }
}