using UnityEngine;

public class MiniBossVisualController : MonoBehaviour
{
    [System.Serializable]
    public class StateVisual
    {
        [Header("Prototype Sprite")]
        [Tooltip("Sprite statique utilisé pendant le prototype.")]
        public Sprite sprite;

        [Header("Future Animation")]
        [Tooltip(
            "Nom complet de l’état dans l’Animator. " +
            "Exemple : Base Layer.Boss_Charging"
        )]
        public string animationStateName;
    }

    [Header("References")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;

    [Header("Visual Mode")]
    [Tooltip(
        "Désactivé : utilise les sprites statiques. " +
        "Activé : utilise les animations."
    )]
    [SerializeField] private bool useAnimator = false;

    [Tooltip("Index du layer utilisé dans l’Animator.")]
    [SerializeField] private int animatorLayer = 0;

    [Tooltip("Durée de transition entre deux animations.")]
    [SerializeField] private float animationTransitionDuration = 0.05f;

    [Header("Seeking Distance")]
    [SerializeField] private StateVisual seekingDistanceVisual;

    [Header("Preparing Charge")]
    [SerializeField] private StateVisual preparingChargeVisual;

    [Header("Charging")]
    [SerializeField] private StateVisual chargingVisual;

    [Header("Recoil")]
    [SerializeField] private StateVisual recoilVisual;

    [Header("Stunned")]
    [SerializeField] private StateVisual stunnedVisual;

    [Header("Recovery")]
    [SerializeField] private StateVisual recoveryVisual;

    [Header("Debug")]
    [SerializeField]
    private MiniBossPatternController.BossState currentState;

    public MiniBossPatternController.BossState CurrentState =>
        currentState;

    private void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer =
                GetComponent<SpriteRenderer>();
        }

        if (animator == null)
        {
            animator =
                GetComponent<Animator>();
        }

        UpdateAnimatorActivation();
    }

    public void SetState(
        MiniBossPatternController.BossState newState
    )
    {
        currentState = newState;

        StateVisual selectedVisual =
            GetVisualForState(newState);

        if (selectedVisual == null)
        {
            return;
        }

        if (CanUseAnimation(selectedVisual))
        {
            PlayAnimation(
                selectedVisual.animationStateName
            );

            return;
        }

        DisplaySprite(selectedVisual.sprite);
    }

    private StateVisual GetVisualForState(
        MiniBossPatternController.BossState state
    )
    {
        switch (state)
        {
            case MiniBossPatternController.BossState.SeekingDistance:
                return seekingDistanceVisual;

            case MiniBossPatternController.BossState.PreparingCharge:
                return preparingChargeVisual;

            case MiniBossPatternController.BossState.Charging:
                return chargingVisual;

            case MiniBossPatternController.BossState.Recoil:
                return recoilVisual;

            case MiniBossPatternController.BossState.Stunned:
                return stunnedVisual;

            case MiniBossPatternController.BossState.Recovery:
                return recoveryVisual;

            default:
                return seekingDistanceVisual;
        }
    }

    private bool CanUseAnimation(StateVisual stateVisual)
    {
        return
            useAnimator &&
            animator != null &&
            !string.IsNullOrWhiteSpace(
                stateVisual.animationStateName
            );
    }

    private void DisplaySprite(Sprite newSprite)
    {
        if (animator != null)
        {
            animator.enabled = false;
        }

        if (spriteRenderer == null)
        {
            return;
        }

        if (newSprite != null)
        {
            spriteRenderer.sprite = newSprite;
        }
    }

    private void PlayAnimation(
        string animationStateName
    )
    {
        if (animator == null)
        {
            return;
        }

        animator.enabled = true;

        int animationHash =
            Animator.StringToHash(
                animationStateName
            );

        animator.CrossFadeInFixedTime(
            animationHash,
            animationTransitionDuration,
            animatorLayer,
            0f
        );
    }

    public void RefreshCurrentVisual()
    {
        SetState(currentState);
    }

    public void SetUseAnimator(bool shouldUseAnimator)
    {
        useAnimator = shouldUseAnimator;

        UpdateAnimatorActivation();
        RefreshCurrentVisual();
    }

    private void UpdateAnimatorActivation()
    {
        if (animator == null)
        {
            return;
        }

        animator.enabled = useAnimator;
    }

    private void OnValidate()
    {
        animatorLayer =
            Mathf.Max(0, animatorLayer);

        animationTransitionDuration =
            Mathf.Max(
                0f,
                animationTransitionDuration
            );

        if (Application.isPlaying)
        {
            UpdateAnimatorActivation();
            RefreshCurrentVisual();
        }
    }
}