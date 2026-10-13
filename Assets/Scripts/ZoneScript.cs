using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class ZoneScript : MonoBehaviour
{

    [Header("Area")]
    [Tooltip("The collider that marks the area on the floor. Leave empty to find one among the children. " +
             "Use a Box Collider (or a flattened Cylinder for a round area). Mesh Colliders must be Convex.")]
    [SerializeField] Collider area;


    [Header("Item")]
    [Tooltip("The item that must be held. Drag an item from the Hierarchy (e.g. ObjC) here.")]
    [SerializeField] XRGrabInteractable item;


    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
