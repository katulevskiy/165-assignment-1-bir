using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class SpawnMenu : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private TMP_Text itemNameLabel;

    [Header("Input")]
    [SerializeField] private InputActionReference cycleAction;   // Vector2 thumbstick
    [SerializeField] private InputActionReference spawnAction;   // button (thumbstick click)

    [Header("Spawnables")]
    [SerializeField] private List<GameObject> spawnPrefabs = new();

    [Header("Tuning")]
    [SerializeField] private float cycleDeadzone = 0.7f;
    [SerializeField] private float spawnForwardOffset = 0.3f;

    private int currentIndex = 0;
    private bool cycleReady = true;

    private void OnEnable()
    {
        cycleAction.action.Enable();
        spawnAction.action.Enable();
    }

    private void OnDisable()
    {
        cycleAction.action.Disable();
        spawnAction.action.Disable();
    }

    private void Start() => UpdateLabel();

    private void Update()
    {
        HandleCycle();
        HandleSpawn();
    }

    private void HandleCycle()
    {
        if (spawnPrefabs.Count == 0) return;

        Vector2 stick = cycleAction.action.ReadValue<Vector2>();

        if (Mathf.Abs(stick.x) < cycleDeadzone)
        {
            cycleReady = true;
            return;
        }

        if (!cycleReady) return;

        if (stick.x > 0) currentIndex = (currentIndex + 1) % spawnPrefabs.Count;
        else             currentIndex = (currentIndex - 1 + spawnPrefabs.Count) % spawnPrefabs.Count;

        cycleReady = false;
        UpdateLabel();
    }

    private void HandleSpawn()
    {
        if (!spawnAction.action.WasPressedThisFrame()) return;
        if (spawnPrefabs.Count == 0) return;

        var prefab = spawnPrefabs[currentIndex];
        if (prefab == null) return;

        // Spawn slightly in front of the spawn point so it doesn't appear inside the user's hand.
        Vector3 pos = spawnPoint.position + spawnPoint.forward * spawnForwardOffset;
        Instantiate(prefab, pos, Quaternion.identity);
    }

    private void UpdateLabel()
    {
        if (itemNameLabel == null) return;
        if (spawnPrefabs.Count == 0) { itemNameLabel.text = "(none)"; return; }

        var prefab = spawnPrefabs[currentIndex];
        itemNameLabel.text = prefab != null ? prefab.name : "(missing)";
    }
}