using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using System.Collections;

public class GameSceneManager : MonoBehaviour
{
    private SoundManager soundManager;
    private bool isLoading = false;

    void Start()
    {
        Debug.Log("GameSceneManager Start вызван");
        Debug.Log($"Текущая сцена: {SceneManager.GetActiveScene().name}");

        soundManager = SoundManager.Instance;

        if (soundManager == null)
        {
            Debug.LogError("SoundManager не найден! Создаем новый...");
            GameObject sm = new GameObject("SoundManager");
            soundManager = sm.AddComponent<SoundManager>();
            DontDestroyOnLoad(sm);
        }

        // Проверяем назначение звуков
        soundManager.CheckAudioAssignments();

        // НАСТРОЙКА КУРСОРА В ЗАВИСИМОСТИ ОТ СЦЕНЫ
        if (SceneManager.GetActiveScene().name == "MainMenu")
        {
            // В МЕНЮ: курсор виден и свободен
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Debug.Log("Главное меню: курсор видим и свободен");

            // УБРАТЬ ВЫЗОВ PlayMenuSounds() здесь - он уже вызывается в OnSceneLoaded
            // soundManager.PlayMenuSounds();
        }
        else if (SceneManager.GetActiveScene().name == "MainGame_Working")
        {
            // В ИГРЕ: курсор скрыт и заблокирован
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            Debug.Log("Игровая сцена: курсор скрыт и заблокирован");

            soundManager.LogStatus();
            soundManager.StartGameMusic();
        }
    }

    public void LoadGameScene()
    {
        if (isLoading) return;
        isLoading = true;

        Debug.Log("Начинаем асинхронную загрузку игровой сцены");

        if (soundManager != null)
        {
            soundManager.PlayButtonClick();
        }

        StartCoroutine(LoadGameSceneAsync());
    }

    private IEnumerator LoadGameSceneAsync()
    {
        // Ждем немного для звука кнопки (0.2 секунды)
        yield return new WaitForSeconds(0.2f);

        // Асинхронная загрузка с индикатором прогресса
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("MainGame_Working");
        asyncLoad.allowSceneActivation = false; // Не активируем сразу

        // Ждем загрузки (минимум 0.5 секунд для гарантированного воспроизведения звука кнопки)
        float timer = 0f;
        while (timer < 0.5f || asyncLoad.progress < 0.9f)
        {
            timer += Time.unscaledDeltaTime;
            yield return null;
        }

        // Блокируем курсор перед активацией сцены
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Активируем сцену
        asyncLoad.allowSceneActivation = true;

        Time.timeScale = 1f;
        isLoading = false;

        // НЕ вызываем StartGameMusic() здесь - она вызовется в Start() загруженной сцены
        // Звук кнопки уже воспроизвелся, игровая музыка запустится автоматически
        Debug.Log("Загрузка завершена, игра стартует...");
    }

    public void QuitGame()
    {
        if (soundManager != null)
            soundManager.PlayButtonClick();

        Debug.Log("Выход из игры");

        // Задержка перед выходом для звука
        StartCoroutine(QuitWithDelay());
    }

    private IEnumerator QuitWithDelay()
    {
        yield return new WaitForSeconds(0.3f);

        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    public void ReturnToMainMenu()
    {
        Debug.Log("Возвращаемся в главное меню");
        Time.timeScale = 1f;

        // ПЕРЕД ЗАГРУЗКОЙ: разблокируем курсор для меню
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Останавливаем все звуки перед возвратом в меню
        if (soundManager != null)
        {
            soundManager.StopAllSounds();
        }

        SceneManager.LoadScene("MainMenu");
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ReturnToMainMenu();
        }
    }
}