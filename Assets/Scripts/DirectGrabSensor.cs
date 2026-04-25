using System.Collections.Generic;
using UnityEngine;

public class DirectGrabSensor : MonoBehaviour
{
    [SerializeField] private string selectableTag = "Selectable";

    // Objects currently inside the trigger sphere.
    private readonly HashSet<GameObject> overlapping = new();

    public GameObject ClosestSelectable
    {
        get
        {
            // Clean out any destroyed or null entries.
            overlapping.RemoveWhere(go => go == null);

            GameObject closest = null;
            float bestDist = float.MaxValue;
            Vector3 here = transform.position;

            foreach (var go in overlapping)
            {
                float d = (go.transform.position - here).sqrMagnitude;
                if (d < bestDist)
                {
                    bestDist = d;
                    closest = go;
                }
            }
            return closest;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(selectableTag))
            overlapping.Add(other.gameObject);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(selectableTag))
            overlapping.Remove(other.gameObject);
    }
}