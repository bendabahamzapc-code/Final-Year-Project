using UnityEngine;

public class PlayerSFXController : MonoBehaviour
{
    public static PlayerSFXController Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource oneShotSource;
    [SerializeField] private AudioSource grappleChargeSource;
    [SerializeField] private AudioSource grappleShotSource;
    [SerializeField] private AudioSource grappleMoveSource;

    // =========================================================
    // PLAYER
    // =========================================================

    [Header("Jump")]
    [SerializeField] private AudioClip jumpClip;
    [SerializeField] [Range(0f, 1f)] private float jumpVolume = 1f;

    [Header("Landing")]
    [SerializeField] private AudioClip landingClip;
    [SerializeField] [Range(0f, 1f)] private float landingVolume = 1f;

    [Header("Damage")]
    [SerializeField] private AudioClip damageClip;
    [SerializeField] [Range(0f, 1f)] private float damageVolume = 1f;

    // =========================================================
    // ATTACK
    // =========================================================

    [Header("Attack Swing - Multiple")]
    [SerializeField] private AudioClip[] attackMissClips;
    [SerializeField] [Range(0f, 1f)] private float attackMissVolume = 1f;

    [Header("Attack Enemy Hit - Multiple")]
    [SerializeField] private AudioClip[] attackEnemyHitClips;
    [SerializeField] [Range(0f, 1f)] private float attackEnemyHitVolume = 1f;

    [Header("Destructible Destroyed - Multiple")]
    [SerializeField] private AudioClip[] destructibleDestroyedClips;
    [SerializeField] [Range(0f, 1f)] private float destructibleDestroyedVolume = 1f;

    // =========================================================
    // GRAPPLE
    // =========================================================

    [Header("Grapple Charge")]
    [SerializeField] private AudioClip grappleChargeClip;
    [SerializeField] [Range(0f, 1f)] private float grappleChargeVolume = 1f;
    [SerializeField] private bool grappleChargeLoop = true;

    [Header("Grapple Shot Flying")]
    [SerializeField] private AudioClip grappleShotClip;
    [SerializeField] [Range(0f, 1f)] private float grappleShotVolume = 1f;
    [SerializeField] private bool grappleShotLoop = true;

    [Header("Grapple Invalid Hit")]
    [SerializeField] private AudioClip grappleInvalidHitClip;
    [SerializeField] [Range(0f, 1f)] private float grappleInvalidHitVolume = 1f;

    [Header("Grapple Valid Hit")]
    [SerializeField] private AudioClip grappleValidHitClip;
    [SerializeField] [Range(0f, 1f)] private float grappleValidHitVolume = 1f;

    [Header("Grapple Player Movement")]
    [SerializeField] private AudioClip grappleMoveClip;
    [SerializeField] [Range(0f, 1f)] private float grappleMoveVolume = 1f;
    [SerializeField] private bool grappleMoveLoop = true;

    // =========================================================
    // RANDOM MEMORY
    // =========================================================

    private int lastAttackMissIndex = -1;
    private int lastAttackHitIndex = -1;
    private int lastDestructibleIndex = -1;

    // =========================================================
    // ATTACK STATE
    // =========================================================

    private bool attackInProgress;
    private bool enemyHitDuringAttack;

    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        Instance = this;

        SetupAudioSources();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void SetupAudioSources()
    {
        if (oneShotSource == null)
        {
            oneShotSource =
                gameObject.AddComponent<AudioSource>();
        }

        if (grappleChargeSource == null)
        {
            grappleChargeSource =
                gameObject.AddComponent<AudioSource>();
        }

        if (grappleShotSource == null)
        {
            grappleShotSource =
                gameObject.AddComponent<AudioSource>();
        }

        if (grappleMoveSource == null)
        {
            grappleMoveSource =
                gameObject.AddComponent<AudioSource>();
        }

        SetupSource(oneShotSource);
        SetupSource(grappleChargeSource);
        SetupSource(grappleShotSource);
        SetupSource(grappleMoveSource);
    }

    private void SetupSource(AudioSource source)
    {
        source.playOnAwake = false;
        source.spatialBlend = 0f;
    }

    // =========================================================
    // PLAYER
    // =========================================================

    public void PlayJump()
    {
        PlayOneShot(
            jumpClip,
            jumpVolume
        );
    }

    public void PlayLanding()
    {
        PlayOneShot(
            landingClip,
            landingVolume
        );
    }

    public void PlayDamage()
    {
        PlayOneShot(
            damageClip,
            damageVolume
        );
    }

    // =========================================================
    // ATTACK
    // =========================================================

    public void BeginAttack()
    {
        attackInProgress = true;
        enemyHitDuringAttack = false;

        /*
         * Le swing joue immédiatement.
         *
         * Si quelque chose est touché ensuite,
         * le son d'impact s'ajoute au swing.
         */
        PlayRandomNoRepeat(
            attackMissClips,
            attackMissVolume,
            ref lastAttackMissIndex
        );
    }

    public void RegisterEnemyHit()
    {
        if (!attackInProgress)
        {
            return;
        }

        /*
         * Un seul son d'impact ennemi
         * par attaque.
         */
        if (enemyHitDuringAttack)
        {
            return;
        }

        enemyHitDuringAttack = true;

        PlayRandomNoRepeat(
            attackEnemyHitClips,
            attackEnemyHitVolume,
            ref lastAttackHitIndex
        );
    }

    public void PlayDestructibleDestroyed()
    {
        PlayRandomNoRepeat(
            destructibleDestroyedClips,
            destructibleDestroyedVolume,
            ref lastDestructibleIndex
        );
    }

    public void EndAttack()
    {
        attackInProgress = false;
        enemyHitDuringAttack = false;
    }

    // =========================================================
    // GRAPPLE CHARGE
    // =========================================================

    public void StartGrappleCharge()
    {
        PlayContinuousSound(
            grappleChargeSource,
            grappleChargeClip,
            grappleChargeVolume,
            grappleChargeLoop
        );
    }

    public void StopGrappleCharge()
    {
        StopSource(
            grappleChargeSource
        );
    }

    // =========================================================
    // GRAPPLE SHOT
    // =========================================================

    public void StartGrappleShot()
    {
        PlayContinuousSound(
            grappleShotSource,
            grappleShotClip,
            grappleShotVolume,
            grappleShotLoop
        );
    }

    public void StopGrappleShot()
    {
        StopSource(
            grappleShotSource
        );
    }

    // =========================================================
    // GRAPPLE COLLISION
    // =========================================================

    public void PlayGrappleInvalidHit()
    {
        PlayOneShot(
            grappleInvalidHitClip,
            grappleInvalidHitVolume
        );
    }

    public void PlayGrappleValidHit()
    {
        PlayOneShot(
            grappleValidHitClip,
            grappleValidHitVolume
        );
    }

    // =========================================================
    // GRAPPLE MOVEMENT
    // =========================================================

    public void StartGrappleMovement()
    {
        PlayContinuousSound(
            grappleMoveSource,
            grappleMoveClip,
            grappleMoveVolume,
            grappleMoveLoop
        );
    }

    public void StopGrappleMovement()
    {
        StopSource(
            grappleMoveSource
        );
    }

    public void StopAllGrappleSounds()
    {
        StopGrappleCharge();
        StopGrappleShot();
        StopGrappleMovement();
    }

    // =========================================================
    // AUDIO HELPERS
    // =========================================================

    private void PlayOneShot(
        AudioClip clip,
        float volume
    )
    {
        if (clip == null)
        {
            return;
        }

        if (oneShotSource == null)
        {
            return;
        }

        oneShotSource.PlayOneShot(
            clip,
            volume
        );
    }

    private void PlayRandomNoRepeat(
        AudioClip[] clips,
        float volume,
        ref int lastIndex
    )
    {
        if (
            clips == null ||
            clips.Length == 0
        )
        {
            return;
        }

        if (clips.Length == 1)
        {
            lastIndex = 0;

            PlayOneShot(
                clips[0],
                volume
            );

            return;
        }

        int newIndex;

        do
        {
            newIndex =
                Random.Range(
                    0,
                    clips.Length
                );
        }
        while (newIndex == lastIndex);

        lastIndex = newIndex;

        PlayOneShot(
            clips[newIndex],
            volume
        );
    }

    private void PlayContinuousSound(
        AudioSource source,
        AudioClip clip,
        float volume,
        bool loop
    )
    {
        if (
            source == null ||
            clip == null
        )
        {
            return;
        }

        source.Stop();

        source.clip = clip;
        source.volume = volume;
        source.loop = loop;

        source.Play();
    }

    private void StopSource(
        AudioSource source
    )
    {
        if (source == null)
        {
            return;
        }

        source.Stop();
        source.clip = null;
    }
}