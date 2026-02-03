using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    // Ссылки на UI элементы
    [Header("HUD Elements")]
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private GameObject fleecePanel;

    [Header("Event Notifications")]
    [SerializeField] private GameObject damageNotification;
    [SerializeField] private GameObject snakeNotification;
    [SerializeField] private GameObject healNotification;
    [SerializeField] private GameObject fleeceNotification;

    [Header("Game State Panels")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject winPanel;

    [Header("Settings")]
    [SerializeField] private float notificationDuration = 3f;    // Для обычных уведомлений
    [SerializeField] private float gameEndDuration = 5f;         // Для смерти/победы

    [Header("Adaptive UI Settings")]
    [SerializeField] private float referenceWidth = 1920f;
    [SerializeField] private float referenceHeight = 1080f;
    [SerializeField] private float minFontSize = 14f;
    [SerializeField] private float maxFontSize = 22f;
    [SerializeField] private bool enableDynamicScaling = true;

    // Ссылки на существующие компоненты
    private PlayerHealth playerHealth;
    private GameSceneManager sceneManager;
    private CanvasScaler canvasScaler;
    private bool isGamePaused = false;
    private Vector2Int lastScreenSize;

    // Метод для паузы игры
    public void PauseGame()
    {
        if (!isGamePaused)
        {
            Time.timeScale = 0f; // Останавливаем время
            isGamePaused = true;

            // Также можно отключить управление игроком
            var player = FindFirstObjectByType<PlayerController>();
            if (player != null) player.enabled = false;
        }
    }

    // Метод для возобновления игры
    public void ResumeGame()
    {
        if (isGamePaused)
        {
            Time.timeScale = 1f; // Восстанавливаем время
            isGamePaused = false;

            // Включаем управление игроком
            var player = FindFirstObjectByType<PlayerController>();
            if (player != null) player.enabled = true;
        }
    }

    private void Start()
    {
        // Находим компоненты
        playerHealth = FindFirstObjectByType<PlayerHealth>();
        sceneManager = FindFirstObjectByType<GameSceneManager>();

        // Настраиваем адаптивный UI
        SetupAdaptiveUI();

        // Инициализация UI
        if (playerHealth != null)
        {
            UpdateHealth(playerHealth.health);
        }

        SetFleeceCollected(false);
        HideAllNotifications();
        gameOverPanel.SetActive(false);
        winPanel.SetActive(false);

        // Сохраняем начальный размер экрана
        lastScreenSize = new Vector2Int(Screen.width, Screen.height);
    }

    private void SetupAdaptiveUI()
    {
        // Находим или создаем CanvasScaler
        canvasScaler = GetComponentInParent<CanvasScaler>();
        if (canvasScaler == null)
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                canvasScaler = canvas.gameObject.AddComponent<CanvasScaler>();
            }
        }

        if (canvasScaler != null)
        {
            // Настраиваем CanvasScaler для адаптивного масштабирования
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(referenceWidth, referenceHeight);
            canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            canvasScaler.matchWidthOrHeight = 0.5f; // Баланс между шириной и высотой
        }

        // Инициализируем адаптивные шрифты
        InitializeAdaptiveFonts();
    }

    private void InitializeAdaptiveFonts()
    {
        if (!enableDynamicScaling) return;

        float screenRatio = (float)Screen.width / Screen.height;
        float referenceRatio = referenceWidth / referenceHeight;
        float scaleFactor = Mathf.Clamp(screenRatio / referenceRatio, 0.7f, 1.3f);

        // Настраиваем все текстовые элементы
        TextMeshProUGUI[] allTexts = GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI text in allTexts)
        {
            if (text != null)
            {
                // Сохраняем оригинальный размер шрифта при первом запуске
                if (!PlayerPrefs.HasKey("OriginalFontSize_" + text.name))
                {
                    PlayerPrefs.SetFloat("OriginalFontSize_" + text.name, text.fontSize);
                }

                // Применяем масштабирование
                float originalSize = PlayerPrefs.GetFloat("OriginalFontSize_" + text.name, text.fontSize);
                float newSize = Mathf.Clamp(originalSize * scaleFactor, minFontSize, maxFontSize);

                if (text.enableAutoSizing)
                {
                    text.fontSizeMin = minFontSize;
                    text.fontSizeMax = maxFontSize;
                }
                else
                {
                    text.fontSize = newSize;
                }
            }
        }
    }

    private void Update()
    {
        // Проверяем изменение разрешения экрана
        if (enableDynamicScaling)
        {
            Vector2Int currentScreenSize = new Vector2Int(Screen.width, Screen.height);
            if (currentScreenSize != lastScreenSize)
            {
                lastScreenSize = currentScreenSize;
                OnResolutionChanged();
            }
        }
    }

    private void OnResolutionChanged()
    {
        Debug.Log($"Разрешение изменилось: {Screen.width}x{Screen.height}");

        // Обновляем адаптивные шрифты
        if (enableDynamicScaling)
        {
            InitializeAdaptiveFonts();
        }

        // Обновляем позиции и размеры панелей
        AdjustPanelLayouts();
    }

    private void AdjustPanelLayouts()
    {
        // Адаптируем позиции HUD элементов
        AdjustHUDElements();

        // Адаптируем уведомления
        AdjustNotifications();

        // Адаптируем модальные окна
        AdjustModalPanels();
    }

    private void AdjustHUDElements()
    {
        // Здоровье - верхний левый угол с адаптивными отступами
        if (healthText != null && healthText.transform is RectTransform healthRT)
        {
            float marginX = Screen.width * 0.02f; // 2% от ширины экрана
            float marginY = Screen.height * 0.02f; // 2% от высоты экрана

            // Учитываем безопасную зону на мобильных устройствах
            float safeMarginTop = Screen.safeArea.y / Screen.height;
            float safeMarginBottom = (Screen.height - Screen.safeArea.height - Screen.safeArea.y) / Screen.height;

            healthRT.anchorMin = new Vector2(0, 1);
            healthRT.anchorMax = new Vector2(0, 1);
            healthRT.pivot = new Vector2(0, 1);
            healthRT.anchoredPosition = new Vector2(marginX + 10, -marginY - safeMarginTop * Screen.height);
        }

        // Руно - верхний правый угол
        if (fleecePanel != null && fleecePanel.transform is RectTransform fleeceRT)
        {
            float marginX = Screen.width * 0.02f;
            float marginY = Screen.height * 0.02f;
            float safeMarginTop = Screen.safeArea.y / Screen.height;

            fleeceRT.anchorMin = new Vector2(1, 1);
            fleeceRT.anchorMax = new Vector2(1, 1);
            fleeceRT.pivot = new Vector2(1, 1);
            fleeceRT.anchoredPosition = new Vector2(-marginX - 10, -marginY - safeMarginTop * Screen.height);
        }
    }

    private void AdjustNotifications()
    {
        GameObject[] notifications = { damageNotification, snakeNotification, healNotification, fleeceNotification };

        foreach (GameObject notification in notifications)
        {
            if (notification != null && notification.transform is RectTransform rt)
            {
                // Центрируем уведомления по горизонтали, позиционируем сверху
                rt.anchorMin = new Vector2(0.5f, 1);
                rt.anchorMax = new Vector2(0.5f, 1);
                rt.pivot = new Vector2(0.5f, 1);

                // Адаптивный отступ сверху
                float topMargin = Screen.height * 0.15f; // 15% от высоты экрана
                float safeMarginTop = Screen.safeArea.y;
                rt.anchoredPosition = new Vector2(0, -topMargin - safeMarginTop);

                // Адаптивная ширина
                float maxWidth = Screen.width * 0.8f; // 80% ширины экрана
                float minWidth = 300f;
                rt.sizeDelta = new Vector2(Mathf.Clamp(maxWidth, minWidth, maxWidth), rt.sizeDelta.y);
            }
        }
    }

    private void AdjustModalPanels()
    {
        // Game Over панель
        if (gameOverPanel != null && gameOverPanel.transform is RectTransform gameOverRT)
        {
            SetupModalPanel(gameOverRT);
        }

        // Win панель
        if (winPanel != null && winPanel.transform is RectTransform winRT)
        {
            SetupModalPanel(winRT);
        }
    }

    private void SetupModalPanel(RectTransform panelRT)
    {
        // Растягиваем на весь экран с небольшими отступами

        panelRT.anchorMin = Vector2.zero;
        panelRT.anchorMax = Vector2.one;
        panelRT.offsetMin = Vector2.zero;  // Left, Bottom = 0
        panelRT.offsetMax = Vector2.zero;  // Right, Top = 0

        // Центрируем содержимое
        if (panelRT.GetComponent<VerticalLayoutGroup>() == null)
        {
            VerticalLayoutGroup layout = panelRT.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = false;
            layout.spacing = 20f;
            layout.padding = new RectOffset(20, 20, 20, 20);
        }
    }

    // Обновление здоровья (вызывается из PlayerHealth)
    public void UpdateHealth(int currentHealth)
    {
        if (healthText != null)
            healthText.text = $"Здоровье: {currentHealth}";
    }

    // Показать/скрыть статус руна
    public void SetFleeceCollected(bool collected)
    {
        if (fleecePanel != null)
            fleecePanel.SetActive(collected);
    }

    // === УВЕДОМЛЕНИЯ ===

    public void ShowDamageNotification()
    {
        ShowNotification(damageNotification, "Ваш герой атакован Минотавром!");
    }

    public void ShowSnakeNotification()
    {
        ShowNotification(snakeNotification, "Ваш герой укушен змеёй!");
    }

    public void ShowHealNotification()
    {
        ShowNotification(healNotification, "Ваш герой вкусил священную амброзию!");
    }

    public void ShowFleeceNotification()
    {
        ShowNotification(fleeceNotification, "Ваш герой взял Золотое руно!");
    }

    private void ShowNotification(GameObject notification, string message = "")
    {
        if (notification == null) return;

        // Устанавливаем текст если нужно
        if (!string.IsNullOrEmpty(message))
        {
            var text = notification.GetComponent<TextMeshProUGUI>();
            if (text != null)
            {
                text.text = message;

                // Применяем адаптивный размер шрифта при показе
                if (enableDynamicScaling)
                {
                    float screenRatio = (float)Screen.width / Screen.height;
                    float referenceRatio = referenceWidth / referenceHeight;
                    float scaleFactor = Mathf.Clamp(screenRatio / referenceRatio, 0.7f, 1.3f);

                    float originalSize = PlayerPrefs.GetFloat("OriginalFontSize_" + text.name, text.fontSize);
                    float newSize = Mathf.Clamp(originalSize * scaleFactor, minFontSize, maxFontSize);

                    if (!text.enableAutoSizing)
                    {
                        text.fontSize = newSize;
                    }
                }
            }
        }

        HideAllNotifications();
        notification.SetActive(true);
        StartCoroutine(HideNotificationAfterDelay(notification));
    }

    private IEnumerator HideNotificationAfterDelay(GameObject notification)
    {
        yield return new WaitForSeconds(notificationDuration);
        notification.SetActive(false);
    }

    private void HideAllNotifications()
    {
        if (damageNotification != null) damageNotification.SetActive(false);
        if (snakeNotification != null) snakeNotification.SetActive(false);
        if (healNotification != null) healNotification.SetActive(false);
        if (fleeceNotification != null) fleeceNotification.SetActive(false);
    }

    // === КОНЕЦ ИГРЫ ===

    public void ShowGameOver()
    {
        if (gameOverPanel != null)
        {
            // ВОСПРОИЗВОДИМ ЗВУК ПОРАЖЕНИЯ
            SoundManager soundManager = SoundManager.Instance;
            if (soundManager != null)
                soundManager.PlayDefeat();

            // ПАУЗА ИГРЫ
            PauseGame();

            // ПРИ КОНЦЕ ИГРЫ: показываем курсор
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Обновляем адаптивную верстку перед показом
            if (gameOverPanel.transform is RectTransform rt)
            {
                SetupModalPanel(rt);
            }

            gameOverPanel.SetActive(true);
            StartCoroutine(ReturnToMenuAfterDelay());
        }
    }

    public void ShowWin()
    {
        if (winPanel != null)
        {
            // ВОСПРОИЗВОДИМ ЗВУК ПОБЕДЫ
            SoundManager soundManager = SoundManager.Instance;
            if (soundManager != null)
                soundManager.PlayVictory();

            // ПАУЗА ИГРЫ
            PauseGame();

            // ПРИ ПОБЕДЕ: показываем курсор
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Обновляем адаптивную верстку перед показом
            if (winPanel.transform is RectTransform rt)
            {
                SetupModalPanel(rt);
            }

            winPanel.SetActive(true);
            StartCoroutine(ReturnToMenuAfterDelay());
        }
    }

    private IEnumerator ReturnToMenuAfterDelay()
    {
        // Ждём gameEndDuration секунд РЕАЛЬНОГО времени
        float elapsed = 0f;
        while (elapsed < gameEndDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        // Восстанавливаем время перед загрузкой меню
        ResumeGame();

        // Используем существующий GameSceneManager
        if (sceneManager != null)
        {
            sceneManager.ReturnToMainMenu();
        }
        else
        {
            // Запасной вариант
            UnityEngine.SceneManagement.SceneManager.LoadScene(0);
        }
    }

    // Публичный метод для ручного обновления UI (можно вызывать из других скриптов)
    public void RefreshUI()
    {
        OnResolutionChanged();
        Debug.Log("UI обновлен вручную");
    }

    // Метод для включения/выключения адаптивного масштабирования
    public void SetDynamicScaling(bool enabled)
    {
        enableDynamicScaling = enabled;
        if (enabled)
        {
            InitializeAdaptiveFonts();
        }
    }
}