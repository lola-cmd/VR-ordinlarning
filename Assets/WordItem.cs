using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Video;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using TMPro;

// Put this on the item's root object (the cube with Scale 1, 1, 1).
// Expected children: a 3D model (optional), a TextMeshPro text, and (for video) a VideoScreen.
//
// When grabbed, the item EITHER counts down while held OR plays a short video.
// After that the word is shown in the text and its sound is played.
//
// Automatic when the game starts:
//  - finds the text, video player, video screen and audio source if the fields are empty
//  - hides the cube itself if a 3D model has been placed inside it
//  - sizes the Box Collider to fit the 3D model
[RequireComponent(typeof(XRGrabInteractable))]
public class WordItem : MonoBehaviour
{
    public enum Mode { Countdown, Video }

    [Header("What happens when the item is grabbed")]
    [Tooltip("Countdown = hold for a number of seconds. Video = play a video.")]
    [SerializeField] Mode mode = Mode.Countdown;

    [Tooltip("Countdown mode: how many seconds the item must be held.")]
    [SerializeField] float holdSeconds = 3f;

    [Tooltip("Video mode: the video to play (a different one on each item).")]
    [SerializeField] VideoClip videoClip;

    [Tooltip("Video mode: leave empty to find the Video Player among the children automatically.")]
    [SerializeField] VideoPlayer videoPlayer;

    [Tooltip("Video mode: leave empty to use the object that has the Video Player.")]
    [SerializeField] GameObject videoScreen;

    [Tooltip("Video mode: hide the screen again when the video is done.")]
    [SerializeField] bool hideVideoWhenDone = true;

    [Header("The word")]
    [Tooltip("The word that is shown at the end.")]
    [SerializeField] string word = "";

    [Tooltip("The recording of the word.")]
    [SerializeField] AudioClip wordSound;

    [Tooltip("Leave empty to use (or add) an Audio Source on this object.")]
    [SerializeField] AudioSource audioSource;

    [Tooltip("Leave empty to find the TextMeshPro text among the children automatically.")]
    [SerializeField] TMP_Text displayText;

    [Header("Rules")]
    [Tooltip("If you let go before it is finished, it stops and starts over next time.")]
    [SerializeField] bool resetIfReleasedEarly = true;

    [Tooltip("Only show the word the first time.")]
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
    float timeLeft;
    bool running;
    bool done;

    void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();

        FindMissingReferences();
        SetUpModelAndCollider();

        if (videoPlayer != null)
        {
            videoPlayer.playOnAwake = false;
            videoPlayer.isLooping = false;
            videoPlayer.loopPointReached += OnVideoFinished;
        }

        SetText("");
        if (videoScreen != null) videoScreen.SetActive(false);
    }

    void OnDestroy()
    {
        if (videoPlayer != null) videoPlayer.loopPointReached -= OnVideoFinished;
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

    // ---------- Automatic setup ----------

    void FindMissingReferences()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        if (displayText == null) displayText = GetComponentInChildren<TMP_Text>(true);
        if (videoPlayer == null) videoPlayer = GetComponentInChildren<VideoPlayer>(true);
        if (videoScreen == null && videoPlayer != null) videoScreen = videoPlayer.gameObject;

        if (displayText == null)
            Debug.LogWarning($"{name}: no TextMeshPro text found among the children.");
        if (mode == Mode.Video && videoPlayer == null)
            Debug.LogWarning($"{name}: Video mode, but no Video Player found among the children.");
    }

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

    // ---------- Grab / release ----------

    void OnGrabbed(SelectEnterEventArgs args)
    {
        if (triggerOnlyOnce && done) return;
        if (running) return;

        running = true;
        SetText("");

        if (mode == Mode.Countdown) timeLeft = holdSeconds;
        else StartVideo();
    }

    void OnReleased(SelectExitEventArgs args)
    {
        if (!running || !resetIfReleasedEarly) return;

        running = false;
        SetText("");
        StopVideo();
    }

    void Update()
    {
        if (!running || mode != Mode.Countdown) return;

        timeLeft -= Time.deltaTime;
        SetText(Mathf.CeilToInt(timeLeft).ToString());

        if (timeLeft <= 0f) Finish();
    }

    // ---------- Video ----------

    void StartVideo()
    {
        if (videoPlayer == null || videoClip == null)
        {
            Debug.LogWarning($"{name}: Video mode, but Video Player or Video Clip is missing. Showing the word directly.");
            Finish();
            return;
        }

        if (videoScreen != null) videoScreen.SetActive(true);
        videoPlayer.clip = videoClip;
        videoPlayer.Play();
    }

    void StopVideo()
    {
        if (videoPlayer != null) videoPlayer.Stop();
        if (videoScreen != null) videoScreen.SetActive(false);
    }

    void OnVideoFinished(VideoPlayer vp)
    {
        if (running) Finish();
    }

    // ---------- The word ----------

    void Finish()
    {
        running = false;
        done = true;

        if (mode == Mode.Video && hideVideoWhenDone) StopVideo();

        SetText(word);
        if (audioSource != null && wordSound != null) audioSource.PlayOneShot(wordSound);

        onWordShown.Invoke();
    }

    void SetText(string text)
    {
        if (displayText != null) displayText.text = text;
    }
}
