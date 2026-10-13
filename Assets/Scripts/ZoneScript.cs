using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class ZoneScript : MonoBehaviour
{

    [Header("Area")]
    [Tooltip("The collider that marks the area on the floor. Leave empty to find one among the children. " +
     "Use a Box Collider (or a flattened Cylinder for a round area). Mesh Colliders must be Convex.")]
    [SerializeField] Collider area;


    [Header("Item")]
    [Tooltip("The item that must be held. Drag an item from the Hierarchy (e.g. ObjC) here.")]
    [SerializeField] XRGrabInteractable item;

    [Tooltip("The effect that is switched on and off.")]
    [SerializeField] GameObject vfx;

    Transform head;
 


    void Start()
    {
        head = Camera.main.transform;   // the player's head
        vfx.SetActive(false);
        
    }

    // Update is called once per frame
    void Update()
    {
        bool inside = IsInside(head.position);
        bool holding = item.isSelected;   // true while a controller holds the item
        vfx.SetActive(inside && holding);
        vfx.transform.position = item.transform.position; 
    }

    bool IsInside(Vector3 point)
    {
        // Move the point down to the area's height (the head is above the floor).
        point.y = area.bounds.center.y;
 
        // ClosestPoint gives back the same point if the point is inside the collider.
        return area.ClosestPoint(point) == point;
    }
}
