using UnityEngine;

// Put this on the countdown/word text and on the VideoScreen (children of the item).
// Keeps them floating straight above the item and always facing the player.
// When the game starts it lets go of the item, so the item's scale and rotation
// no longer affect it: "World Scale" is its real size, the same on every item.
public class FloatAbove : MonoBehaviour
{
    [Tooltip("How far above the item's centre (in metres). Y = up.")]
    [SerializeField] Vector3 offset = new Vector3(0f, 0.3f, 0f);

    [Tooltip("Real size in the world. Text: about 0.02, 0.02, 0.02. Video screen: about 0.4, 0.225, 1.")]
    [SerializeField] Vector3 worldScale = new Vector3(0.02f, 0.02f, 0.02f);

    Transform item;
    Camera cam;

    void Start()
    {
        item = transform.parent;
        transform.SetParent(null, true);     // stop inheriting the item's scale
        transform.localScale = worldScale;
        cam = Camera.main;
    }

    void LateUpdate()
    {
        if (item != null) transform.position = item.position + offset;

        if (cam == null) cam = Camera.main;
        if (cam != null)
            transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
    }
}