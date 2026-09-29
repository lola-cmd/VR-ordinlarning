using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using TMPro;

// Put this on the same object as the XR Grab Interactable.
// When the object has been held for "holdSeconds", onHeldLongEnough fires
// (hook up sound + word in the Inspector). Letting go early resets the timer.
[RequireComponent(typeof(XRGrabInteractable))]
public class HoldTimer : MonoBehaviour
{
    [Tooltip("How many seconds the object must be held.")]
    [SerializeField] float holdSeconds = 3f;

    [Tooltip("Optional: a TextMeshPro text that shows the countdown.")]
    [SerializeField] TMP_Text countdownText;

    [Tooltip("Word shown in the countdown text when the time is up. Leave empty to show nothing.")]
    [SerializeField] string word = "";

    [Tooltip("If on, the event only fires the first time.")]
    [SerializeField] bool triggerOnlyOnce = true;

    [Tooltip("What happens when the time is up (play sound, show word, ...).")]
    public UnityEvent onHeldLongEnough;

    XRGrabInteractable grab;
    float timeLeft;
    bool isHeld;
    bool hasTriggered;

    void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        ClearText();
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
        if (triggerOnlyOnce && hasTriggered) return;
        isHeld = true;
        timeLeft = holdSeconds;
    }

    void OnReleased(SelectExitEventArgs args)
    {
        isHeld = false;   // let go too early -> start over next time
        if (!hasTriggered) ClearText();   // keep the word visible once it has appeared
    }

    void Update()
    {
        if (!isHeld) return;

        timeLeft -= Time.deltaTime;

        if (countdownText != null)
            countdownText.text = Mathf.CeilToInt(timeLeft).ToString();

        if (timeLeft <= 0f)
        {
            isHeld = false;
            hasTriggered = true;
            if (countdownText != null) countdownText.text = word;   // show the word where the numbers were
            onHeldLongEnough.Invoke();
        }
    }

    void ClearText()
    {
        if (countdownText != null) countdownText.text = "";
    }
}