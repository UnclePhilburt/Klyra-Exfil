using UnityEngine;

/// <summary>
/// Handles AI surrender behavior - drops weapon and plays surrender voice line.
/// Works with TacticalAI's Compliant state.
///
/// SETUP:
/// 1. Attach this to the same GameObject as TacticalAI
/// 2. Add surrender voice clips to the Surrender Voice Clips array
/// 3. AI will automatically drop weapon and say "I surrender!" when compliant
/// </summary>
[RequireComponent(typeof(TacticalAI))]
public class AISurrenderAnimation : MonoBehaviour
{
    [Header("Surrender Voice Lines")]
    [Tooltip("Random voice clips to play when surrendering (e.g., 'I surrender!', 'Don't shoot!', etc.)")]
    public AudioClip[] surrenderVoiceClips;

    [Tooltip("Volume for surrender voice lines")]
    [Range(0f, 1f)]
    public float voiceVolume = 0.8f;

    [Header("Weapon Handling")]
    [Tooltip("Drop weapon when surrendering?")]
    public bool dropWeaponOnSurrender = true;

    [Header("Debug")]
    public bool debugSurrender = false;

    private TacticalAI tacticalAI;
    private AudioSource audioSource;
    private bool hasSurrendered = false;

    void Start()
    {
        tacticalAI = GetComponent<TacticalAI>();

        if (tacticalAI == null)
        {
            Debug.LogError($"[AISurrenderAnimation] No TacticalAI found on {gameObject.name}!");
            enabled = false;
            return;
        }

        // Get or create audio source for voice lines
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f; // 3D sound
            audioSource.maxDistance = 20f;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            audioSource.minDistance = 1f;
        }

        if (surrenderVoiceClips == null || surrenderVoiceClips.Length == 0)
        {
            Debug.LogWarning($"[AISurrenderAnimation] No surrender voice clips assigned on {gameObject.name}! Will work without voice lines.");
        }
    }

    void Update()
    {
        if (tacticalAI == null) return;

        // Check if just entered compliant state
        if (tacticalAI.currentState == TacticalAI.AIState.Compliant && !hasSurrendered)
        {
            Surrender();
        }
        // Reset when leaving compliant state
        else if (tacticalAI.currentState != TacticalAI.AIState.Compliant && hasSurrendered)
        {
            hasSurrendered = false;
        }
    }

    void Surrender()
    {
        if (debugSurrender)
        {
            Debug.Log($"[{gameObject.name}] Surrendering!");
        }

        hasSurrendered = true;

        // Drop weapon
        if (dropWeaponOnSurrender)
        {
            DropWeapon();
        }

        // Play random surrender voice line
        PlaySurrenderVoiceLine();
    }

    void DropWeapon()
    {
        // Try to find and drop the weapon using UCC's inventory system
        var characterLocomotion = GetComponent<Opsive.UltimateCharacterController.Character.UltimateCharacterLocomotion>();
        if (characterLocomotion == null) return;

        var inventory = characterLocomotion.GetComponent<Opsive.UltimateCharacterController.Inventory.InventoryBase>();
        if (inventory == null) return;

        // Get active item
        var activeItem = inventory.GetActiveCharacterItem(0); // Slot 0 (primary weapon)
        if (activeItem != null)
        {
            if (debugSurrender)
            {
                Debug.Log($"[{gameObject.name}] Dropping weapon: {activeItem.name}");
            }

            // Drop the item
            activeItem.Drop(0, true);
        }
    }

    void PlaySurrenderVoiceLine()
    {
        if (surrenderVoiceClips == null || surrenderVoiceClips.Length == 0)
        {
            if (debugSurrender)
            {
                Debug.LogWarning($"[{gameObject.name}] No surrender voice clips to play!");
            }
            return;
        }

        if (audioSource == null) return;

        // Pick random surrender line
        AudioClip randomClip = surrenderVoiceClips[Random.Range(0, surrenderVoiceClips.Length)];

        if (randomClip != null)
        {
            audioSource.PlayOneShot(randomClip, voiceVolume);

            if (debugSurrender)
            {
                Debug.Log($"[{gameObject.name}] Playing surrender voice line: {randomClip.name}");
            }

            // Optional: Show subtitle if you have SubtitleManager
            var subtitleManager = FindObjectOfType<SubtitleManager>();
            if (subtitleManager != null)
            {
                // Try to extract text from clip name (e.g., "ISurrender.wav" -> "I surrender!")
                string subtitleText = GetSubtitleFromClipName(randomClip.name);
                // Note: You'd need to add a method to SubtitleManager to show subtitles
                // subtitleManager.ShowSubtitle(subtitleText, randomClip.length);
            }
        }
    }

    string GetSubtitleFromClipName(string clipName)
    {
        // Convert clip name to readable text
        // "ISurrender" -> "I surrender!"
        // "DontShoot" -> "Don't shoot!"

        // Remove file extension
        clipName = clipName.Replace(".wav", "").Replace(".mp3", "").Replace(".ogg", "");

        // Add spaces before capitals
        string result = "";
        for (int i = 0; i < clipName.Length; i++)
        {
            if (i > 0 && char.IsUpper(clipName[i]))
            {
                result += " ";
            }
            result += clipName[i];
        }

        // Add exclamation mark
        result += "!";

        return result;
    }
}
