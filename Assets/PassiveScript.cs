using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using TMPro;
using UnityEngine.Events;


[RequireComponent(typeof(XRGrabInteractable))]
public class PassiveScript : MonoBehaviour
{

    [Header("The word")]
    [Tooltip("The word that is shown.")]
    [SerializeField] string word = "";

    [Tooltip("The recording of the word.")]
    [SerializeField] AudioClip wordSound;

 //   [Tooltip("Minimum seconds the item must be held.")]
   // [SerializeField] float minHoldSeconds = 3f;


    [Header("Pause between sound")]
    [Tooltip("Pause length between word audio repeats.")]
    [SerializeField] float pauseBetweenWord = 1f;

    [Tooltip("Leave empty to use (or add) an Audio Source on this object.")]
    [SerializeField] AudioSource audioSource;

    [Tooltip("Leave empty to find the TextMeshPro text among the children automatically.")]
    [SerializeField] TMP_Text displayText;

    [Header("Automatic setup")]
    [Tooltip("Hide this object's own cube when a 3D model is placed inside it.")]
    [SerializeField] bool hideCubeIfModel = true;

    [Tooltip("Resize the Box Collider to fit the 3D model when the game starts.")]
    [SerializeField] bool fitColliderToModel = true;

    [Tooltip("Extra margin around the model for the collider (metres).")]
    [SerializeField] float colliderPadding = 0.01f;


    XRGrabInteractable grab;
    Coroutine soundLoop;

    void Awake(){
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (displayText == null) displayText = GetComponentInChildren<TMP_Text>();
        grab = GetComponent<XRGrabInteractable>();
        displayText.text = "";

    }

    void OnEnable()
    {
        grab.selectEntered.AddListener(OnGrabbed);
        grab.selectExited.AddListener(OnReleased);
    }

    void OnDisable()
    {
        grab.selectEntered.RemoveListener(OnGrabbed);
        grab.selectExited.RemoveListener(OnReleased);
    }


    void OnGrabbed(SelectEnterEventArgs args)
    {
        displayText.text = word;
        soundLoop = StartCoroutine(RepeatSound());

    }

    void OnReleased(SelectExitEventArgs args)
    {
        displayText.text = "";
        audioSource.Stop();
        if(soundLoop != null){
            StopCoroutine(soundLoop);
            soundLoop = null;
        }
    }

//IEnumerator gör metoden till en coroutine, dvs en loop
    IEnumerator RepeatSound(){

        while(true){
            audioSource.PlayOneShot(wordSound);
            yield return new WaitForSeconds(wordSound.length + pauseBetweenWord);
        }
    }


    void SetUpModelAndCollider()
    {
        Renderer ownRenderer = GetComponent<Renderer>();
        bool foundModel = false;
        Bounds localBounds = new Bounds();

        foreach (Renderer r in GetComponentsInChildren<Renderer>())
        {
            if (r == ownRenderer || !r.enabled) continue;
            if (displayText != null && r.transform.IsChildOf(displayText.transform)) continue;

            // Convert the model part's box into this object's local space.
            Bounds b = r.bounds;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = new Vector3(
                    (i & 1) == 0 ? b.min.x : b.max.x,
                    (i & 2) == 0 ? b.min.y : b.max.y,
                    (i & 4) == 0 ? b.min.z : b.max.z);
                Vector3 local = transform.InverseTransformPoint(corner);

                if (!foundModel) { localBounds = new Bounds(local, Vector3.zero); foundModel = true; }
                else localBounds.Encapsulate(local);
            }
        }

        if (!foundModel) return;   // no 3D model inside: keep the cube as it is

        if (hideCubeIfModel && ownRenderer != null) ownRenderer.enabled = false;

        if (fitColliderToModel)
        {
            BoxCollider box = GetComponent<BoxCollider>();
            if (box != null)
            {
                box.center = localBounds.center;
                box.size = localBounds.size + Vector3.one * (colliderPadding * 2f);
            }
        }
    }
}
