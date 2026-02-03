using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Движение")]
    public float moveSpeed = 5f;
    public float turnSpeed = 180f;

    [Header("Плавность камеры")]
    public float cameraSmoothness = 10f;
    private float currentCameraYaw = 0f;
    private float currentCameraPitch = 0f;

    [Header("Камера")]
    public Transform cameraTransform;
    public float cameraSensitivity = 50f;

    [Header("Анимация")]
    public Animator animator;
    public float attackCooldown = 1f;
    public float attackRange = 2f;
    public float attackDamage = 20f;

    // Приватные переменные
    private Vector2 moveInput;
    private Vector2 lookInput;
    private float lastAttackTime;
    private Rigidbody rb;
    private SoundManager soundManager;
    private bool wasMoving = false;

    // НОВЫЕ ПЕРЕМЕННЫЕ ДЛЯ INPUT SYSTEM
    private InputAction attackAction;
    private InputAction testDeathAction;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        if (cameraTransform == null)
        {
            Camera cam = Camera.main;
            if (cam != null) cameraTransform = cam.transform;
        }

        if (cameraTransform != null)
        {
            currentCameraYaw = transform.eulerAngles.y;
            currentCameraPitch = cameraTransform.localEulerAngles.x;
            if (currentCameraPitch > 180) currentCameraPitch -= 360f;
        }

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (animator != null)
            Debug.Log("Animator найден: " + animator.name);
        else
            Debug.LogWarning("Animator не найден.");

        SetupInput();
        Debug.Log("Герой готов! WASD - движение, мышь - вращение камеры, ЛКМ - атака");

        // ИСПРАВЛЕНО: Используем синглтон
        soundManager = SoundManager.Instance;

        if (soundManager == null)
        {
            Debug.LogError("SoundManager не найден в PlayerController!");
        }
        else
        {
            Debug.Log("SoundManager найден в PlayerController");
        }
    }

    void SetupInput()
    {
        InputAction moveAction = new InputAction("Move");
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");
        moveAction.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        moveAction.canceled += ctx => moveInput = Vector2.zero;
        moveAction.Enable();

        InputAction lookAction = new InputAction("Look", binding: "<Mouse>/delta");
        lookAction.performed += ctx => lookInput = ctx.ReadValue<Vector2>();
        lookAction.canceled += ctx => lookInput = Vector2.zero;
        lookAction.Enable();
    }

    // В методе Update замените:
    void Update()
    {
        HandleCameraRotation();
        UpdateAnimations();

        // ЗВУК БЕГА ГЕРОЯ
        bool isMoving = moveInput.magnitude > 0.1f;

        if (soundManager != null)
        {
            // ТОЛЬКО управление звуком героя
            soundManager.PlayHeroRun(isMoving, transform.position);

            // Дебаг информация (только при изменении состояния)
            if (wasMoving != isMoving)
            {
                Debug.Log($"Герой: движение = {isMoving}, позиция = {transform.position}");

                // Дополнительная проверка компонентов
                if (soundManager.heroRunSource != null)
                {
                    Debug.Log($"HeroRunSource: clip={soundManager.heroRunSource.clip?.name}, playing={soundManager.heroRunSource.isPlaying}");
                }
            }
        }

        wasMoving = isMoving;

        // В методе Update() PlayerController.cs:
        if (Keyboard.current != null && Keyboard.current.pKey.wasPressedThisFrame && soundManager != null)
        {
            soundManager.DebugRunSoundPositions();
        }
    }

    void FixedUpdate()
    {
        HandleMovement();
    }

    void HandleCameraRotation()
    {
        if (lookInput.magnitude > 0.1f && cameraTransform != null)
        {
            currentCameraYaw += lookInput.x * cameraSensitivity * Time.deltaTime;
            currentCameraPitch -= lookInput.y * cameraSensitivity * Time.deltaTime;

            // ЖЕСТКОЕ ОГРАНИЧЕНИЕ - не даём камере смотреть слишком вертикально
            // Измените значения под ваш вкус:
            currentCameraPitch = Mathf.Clamp(currentCameraPitch, 20f, 60f); // Было -80f, 80f

            Quaternion targetRotation = Quaternion.Euler(0, currentCameraYaw, 0);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, cameraSmoothness * Time.deltaTime);

            cameraTransform.localEulerAngles = Vector3.Lerp(
                cameraTransform.localEulerAngles,
                new Vector3(currentCameraPitch, 0, 0),
                cameraSmoothness * Time.deltaTime
            );
        }
    }

    void HandleMovement()
    {
        if (moveInput.magnitude > 0.1f)
        {
            Vector3 forwardMove = transform.forward * moveInput.y * moveSpeed;
            Vector3 sideMove = transform.right * moveInput.x * moveSpeed;
            Vector3 movement = forwardMove + sideMove;

            rb.MovePosition(rb.position + movement * Time.fixedDeltaTime);
        }
    }

    void UpdateAnimations()
    {
        if (animator == null) return;

        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
        if (state.IsName("Death")) return;
    }

    void OnAttack()
    {
        if (animator == null) return;

        if (Time.time > lastAttackTime + attackCooldown)
        {
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            if (!state.IsName("Attack"))
            {
                animator.SetTrigger("Attack");
                lastAttackTime = Time.time;
                Debug.Log("Атака!");

                Invoke("PerformAttack", 0.3f);
            }
        }
    }

    void OnTestDeath()
    {
        if (animator == null) return;

        animator.SetTrigger("Die");
        Debug.Log("Герой умер! (тест)");
        moveInput = Vector2.zero;
        lookInput = Vector2.zero;
    }

    void PerformAttack()
    {
        Vector3 attackStart = transform.position + Vector3.up * 1f;
        RaycastHit[] hits = Physics.SphereCastAll(attackStart, 0.5f, transform.forward, attackRange);

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.CompareTag("Enemy"))
            {
                Debug.Log("Попал в минотавра: " + hit.collider.name);
            }
        }

        Debug.DrawRay(attackStart, transform.forward * attackRange, Color.red, 1f);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 attackStart = transform.position + Vector3.up * 1f;
        Gizmos.DrawWireSphere(attackStart + transform.forward * attackRange, 0.5f);
        Gizmos.DrawLine(attackStart, attackStart + transform.forward * attackRange);
    }

    void OnDestroy()
    {
        if (attackAction != null) attackAction.Dispose();
        if (testDeathAction != null) testDeathAction.Dispose();
    }

    // ПАУЗА ПО ESC (ОПЦИОНАЛЬНО - раскомментируйте если нужно)
    /*
    if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
    {
        // Переключаем курсор при паузе
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Debug.Log("Пауза: курсор показан");
            Time.timeScale = 0f; // Останавливаем игру
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            Debug.Log("Продолжение: курсор скрыт");
            Time.timeScale = 1f; // Возобновляем игру
        }
    }
    */
}