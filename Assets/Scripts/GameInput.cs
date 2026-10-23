using UnityEngine;
using UnityEngine.InputSystem;

public class GameInput : MonoBehaviour
{
    public static GameInput Instance { get; private set; }

    [Header("Movement")]
    [SerializeField] private InputActionReference moveHorizontalAction;
    [SerializeField] private InputActionReference climbVerticalAction;

    [Header("Actions")]
    [SerializeField] private InputActionReference jumpAction;
    [SerializeField] private InputActionReference attackAction;
    [SerializeField] private InputActionReference grappleAction;
    [SerializeField] private InputActionReference pauseAction;

    private bool isMainInstance;

    public float MoveHorizontal
    {
        get
        {
            if (moveHorizontalAction == null)
                return 0f;

            return moveHorizontalAction.action.ReadValue<float>();
        }
    }

    public float ClimbVertical
    {
        get
        {
            if (climbVerticalAction == null)
                return 0f;

            return climbVerticalAction.action.ReadValue<float>();
        }
    }

    public Vector2 GrappleDirection
    {
        get
        {
            return new Vector2(
                MoveHorizontal,
                ClimbVertical
            );
        }
    }

    public bool JumpPressedThisFrame
    {
        get
        {
            return jumpAction != null &&
                   jumpAction.action.WasPressedThisFrame();
        }
    }

    public bool JumpReleasedThisFrame
    {
        get
        {
            return jumpAction != null &&
                   jumpAction.action.WasReleasedThisFrame();
        }
    }

    public bool JumpHeld
    {
        get
        {
            return jumpAction != null &&
                   jumpAction.action.IsPressed();
        }
    }

    public bool AttackPressedThisFrame
    {
        get
        {
            return attackAction != null &&
                   attackAction.action.WasPressedThisFrame();
        }
    }

    public bool AttackHeld
    {
        get
        {
            return attackAction != null &&
                   attackAction.action.IsPressed();
        }
    }

    public bool GrapplePressedThisFrame
    {
        get
        {
            return grappleAction != null &&
                   grappleAction.action.WasPressedThisFrame();
        }
    }

    public bool GrappleHeld
    {
        get
        {
            return grappleAction != null &&
                   grappleAction.action.IsPressed();
        }
    }

    public bool GrappleReleasedThisFrame
    {
        get
        {
            return grappleAction != null &&
                   grappleAction.action.WasReleasedThisFrame();
        }
    }

    public bool PausePressedThisFrame
    {
        get
        {
            return pauseAction != null &&
                   pauseAction.action.WasPressedThisFrame();
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // Cet objet est seulement un doublon créé
            // lors du rechargement de la scène.
            Destroy(gameObject);
            return;
        }

        Instance = this;
        isMainInstance = true;

        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        // Seul le vrai GameInput a le droit
        // d'activer les actions.
        if (!isMainInstance)
        {
            return;
        }

        EnableAllActions();
    }

    private void Start()
    {
        /*
         * Sécurité :
         * Awake est exécuté avant OnEnable.
         * Selon la situation de chargement de scène,
         * on s'assure ici que les actions du vrai
         * GameInput sont bien activées.
         */
        if (isMainInstance)
        {
            EnableAllActions();
        }
    }

    private void OnDisable()
    {
        /*
         * Très important :
         * un GameInput doublon ne doit JAMAIS
         * désactiver les actions du vrai GameInput.
         */
        if (!isMainInstance)
        {
            return;
        }

        DisableAllActions();
    }

    private void EnableAllActions()
    {
        EnableAction(moveHorizontalAction);
        EnableAction(climbVerticalAction);
        EnableAction(jumpAction);
        EnableAction(attackAction);
        EnableAction(grappleAction);
        EnableAction(pauseAction);
    }

    private void DisableAllActions()
    {
        DisableAction(moveHorizontalAction);
        DisableAction(climbVerticalAction);
        DisableAction(jumpAction);
        DisableAction(attackAction);
        DisableAction(grappleAction);
        DisableAction(pauseAction);
    }

    private void EnableAction(InputActionReference actionReference)
    {
        if (actionReference != null)
        {
            actionReference.action.Enable();
        }
    }

    private void DisableAction(InputActionReference actionReference)
    {
        if (actionReference != null)
        {
            actionReference.action.Disable();
        }
    }
}