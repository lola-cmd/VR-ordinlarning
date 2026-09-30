using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Video;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using TMPro;
using System.Collections;

// Put this on the item's root object (the cube with Scale 1, 1, 1), for items WITH video.
// Children: a TextMeshPro text (with Float Above), a VideoScreen (Quad with a Video Player,
// WITHOUT Float Above) and optionally a 3D model.
//
// When the item is grabbed:
//  - the video screen is placed in front of the player (where they are looking),
//    and stays still there while the video plays
//  - when the video is done, the screen is hidden, the word is shown and its sound is played
[RequireComponent(typeof(XRGrabInteractable))]
public class InteractiveScript : MonoBehaviour
{
    /*
    [Header("The word")]
    [Tooltip("The word that is shown after the video.")]
    [SerializeField] string word = "";
    */

    [Tooltip("The recording of the word.")]
    [SerializeField] AudioClip wordSound;

    [Tooltip("Leave empty to use the Audio Source on this object.")]
    [SerializeField] AudioSource audioSource;

    [Header("Pause between sound")]
    [Tooltip("Pause length between word audio repeats.")]
    [SerializeField] float pauseBetweenWord = 1f;


    [Tooltip("Leave empty to find the TextMeshPro text among the children automatically.")]
    [SerializeField] TMP_Text displayText;

    [Header("Video")]
    [Tooltip("The video to play (a different one on each item).")]
    [SerializeField] VideoClip videoClip;

    [Tooltip("Leave empty to find the Video Player among the children automatically.")]
    [SerializeField] VideoPlayer videoPlayer;

    [Tooltip("Leave empty to use the object that has the Video Player.")]
    [SerializeField] GameObject videoScreen;

    [Tooltip("Play the video without its own sound.")]
    [SerializeField] bool muteVideo = true;

    [Header("Where the video appears")]
    [Tooltip("How far in front of the player the screen is placed (metres).")]
    [SerializeField] float screenDistance = 1.5f;

    [Tooltip("Height compared to the player's eyes (metres). Negative = a bit lower.")]
    [SerializeField] float heightOffset = -0.2f;

    [Tooltip("Size of the screen in the world. Keep X and Y at 16:9 for widescreen video.")]
    [SerializeField] Vector3 screenSize = new Vector3(1.2f, 0.675f, 1f);

    [Header("Rules")]
    [Tooltip("If you let go before the video is finished, it stops and starts over next time.")]
    [SerializeField] bool resetIfReleasedEarly = true;

    [Tooltip("Only play the video and show the word the first time.")]
    [SerializeField] bool triggerOnlyOnce = true;

    [Header("Automatic setup")]
    [Tooltip("Hide this object's own cube when a 3D model is placed inside it.")]
    [SerializeField] bool hideCubeIfModel = true;

    [Tooltip("Resize the Box Collider to fit the 3D model when the game starts.")]
    [SerializeField] bool fitColliderToModel = true;

    [Tooltip("Extra margin around the model for the collider (metres).")]
    [SerializeField] float colliderPadding = 0.01f;

    [Tooltip("Optional: anything extra that should happen when the word is shown.")]
    public UnityEvent onWordShown;

    XRGrabInteractable grab;
    bool running;
    bool done;
    bool screenDetached;
    Coroutine soundLoop;


    void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();

        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (displayText == null) displayText = GetComponentInChildren<TMP_Text>(true);
        if (videoPlayer == null) videoPlayer = GetComponentInChildren<VideoPlayer>(true);
        if (videoScreen == null && videoPlayer != null) videoScreen = videoPlayer.gameObject;

        SetUpModelAndCollider();

        if (videoPlayer != null)
        {
            videoPlayer.playOnAwake = false;
            videoPlayer.isLooping = true;
            if (muteVideo) videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
        }

        SetText("");
        if (videoScreen != null) videoScreen.SetActive(false);
    }

    void OnDestroy()
    {

        // The screen was moved out of the item, so remove it together with the item.
        if (screenDetached && videoScreen != null) Destroy(videoScreen);
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

    // ---------- Grab / release ----------

    void OnGrabbed(SelectEnterEventArgs args)
    {
        if (triggerOnlyOnce && done) return;
        if (running) return;

        running = true;
        SetText("");
        StartVideo();
        soundLoop = StartCoroutine(RepeatSound());
    }

    void OnReleased(SelectExitEventArgs args)
    {

        audioSource.Stop();
        if(soundLoop != null){
            StopCoroutine(soundLoop);
            soundLoop = null;
        }

        if (!running || !resetIfReleasedEarly) return;

        running = false;
        StopVideo();
    }

    // ---------- Video ----------

    void StartVideo()
    {
        if (videoPlayer == null || videoClip == null || videoScreen == null)
        {
            Debug.LogWarning($"{name}: Video Clip, Video Player or Video Screen is missing. Showing the word directly.");
            return;
        }

        PlaceScreenInFrontOfPlayer();
        videoScreen.SetActive(true);
        videoPlayer.clip = videoClip;
        videoPlayer.Play();
    }

    void PlaceScreenInFrontOfPlayer()
    {
        Transform screen = videoScreen.transform;

        // Let go of the item so the screen doesn't move when the item moves.
        if (!screenDetached)
        {
            screen.SetParent(null, true);
            screenDetached = true;
        }
        screen.localScale = screenSize;

        Camera cam = Camera.main;
        if (cam == null) return;
        Transform head = cam.transform;

        // The direction the player is looking, but flat (so it doesn't point into the floor).
        Vector3 forward = head.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f)   // looking straight down or up
        {
            forward = head.up;
            forward.y = 0f;
        }
        forward.Normalize();

        Vector3 position = head.position + forward * screenDistance;
        position.y = head.position.y + heightOffset;

        screen.SetPositionAndRotation(position, Quaternion.LookRotation(forward));
    }

    void StopVideo()
    {
        if (videoPlayer != null) videoPlayer.Stop();
        if (videoScreen != null) videoScreen.SetActive(false);
    }

    // ---------- The word ----------


    void SetText(string text)
    {
        if (displayText != null) displayText.text = text;
    }

    // ---------- Automatic setup ----------

    bool IsTextOrScreen(Transform t)
    {
        if (displayText != null && t.IsChildOf(displayText.transform)) return true;
        if (videoScreen != null && t.IsChildOf(videoScreen.transform)) return true;
        return false;
    }

    void SetUpModelAndCollider()
    {
        Renderer ownRenderer = GetComponent<Renderer>();
        bool foundModel = false;
        Bounds localBounds = new Bounds();

        foreach (Renderer r in GetComponentsInChildren<Renderer>())
        {
            if (r == ownRenderer || !r.enabled || IsTextOrScreen(r.transform)) continue;

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

        if (!foundModel) return;

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

        IEnumerator RepeatSound(){

        while(true){
            audioSource.PlayOneShot(wordSound);
            yield return new WaitForSeconds(wordSound.length + pauseBetweenWord);
        }
    }
}