using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class SoundManager : MonoBehaviour
{
    [Header("Меню и интерфейс")]
    public AudioClip voiceIntro;
    public AudioClip menuMusic;
    public AudioClip buttonClick;

    [Header("Игровая музыка")]
    public AudioClip labyrinthStart;
    public AudioClip labyrinthFinish;

    [Header("Звуки движения")]
    public AudioClip runHero;
    public AudioClip runMinotaur;

    [Header("Звуки существ")]
    public AudioClip minotaurRoar;
    public AudioClip snakeHiss;

    [Header("Звуки событий")]
    public AudioClip heroScreamAttack;
    public AudioClip heroScreamDeath;
    public AudioClip vasePickup;
    public AudioClip fleecePickup;

    [Header("Исход игры")]
    public AudioClip defeatMusic;
    public AudioClip victoryMusic;

    [Header("Настройки")]
    [Range(0f, 1f)] public float masterVolume = 0.7f;
    [Range(0f, 1f)] public float musicVolume = 0.5f;
    [Range(0f, 1f)] public float sfxVolume = 0.8f;

    // Сделайте поле sfxSource публичным (проще):
    [Header("Компоненты (для отладки)")]
    public AudioSource musicSource;
    public AudioSource sfxSource;
    public AudioSource heroRunSource;
    public AudioSource minotaurRunSource;

    // Синглтон для глобального доступа
    private static SoundManager instance;
    public static SoundManager Instance => instance;

    // Состояние
    private bool isInGame = false;
    private bool hasFleece = false;
    private float lastMinotaurRoarTime = 0f;

    // ДОБАВИМ: флаг для вступительного звука
    private static bool hasPlayedIntro = false;

    void Awake()
    {
        // Реализация синглтона
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        // Подписываемся на событие загрузки сцены
        SceneManager.sceneLoaded += OnSceneLoaded;

        // Создаём AudioSource компоненты
        InitializeAudioSources();
    }

    void OnDestroy()
    {
        // Отписываемся от события
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void InitializeAudioSources()
    {
        // Если уже есть компоненты - удаляем их
        AudioSource[] existingSources = GetComponents<AudioSource>();
        foreach (var source in existingSources)
        {
            Destroy(source);
        }

        // Удаляем старые дочерние объекты для звуков бега
        foreach (Transform child in transform)
        {
            if (child.name == "HeroRunSound" || child.name == "MinotaurRunSound")
            {
                Destroy(child.gameObject);
            }
        }

        // Создаём основные компоненты
        musicSource = gameObject.AddComponent<AudioSource>();
        sfxSource = gameObject.AddComponent<AudioSource>();

        // СОЗДАЕМ ОТДЕЛЬНЫЕ GameObject для звуков бега
        GameObject heroRunObj = new GameObject("HeroRunSound");
        heroRunObj.transform.SetParent(transform);
        heroRunSource = heroRunObj.AddComponent<AudioSource>();

        GameObject minotaurRunObj = new GameObject("MinotaurRunSound");
        minotaurRunObj.transform.SetParent(transform);
        minotaurRunSource = minotaurRunObj.AddComponent<AudioSource>();

        // Настройка музыки
        musicSource.loop = true;
        musicSource.volume = musicVolume * masterVolume;
        musicSource.playOnAwake = false;
        musicSource.spatialBlend = 0f; // 2D звук для музыки

        // Настройка SFX
        sfxSource.loop = false;
        sfxSource.volume = sfxVolume * masterVolume;
        sfxSource.playOnAwake = false;
        sfxSource.spatialBlend = 0f;

        // Настройка звука бега героя
        heroRunSource.loop = true;
        heroRunSource.volume = 0.4f * masterVolume;
        heroRunSource.playOnAwake = false;
        heroRunSource.spatialBlend = 1f; // 3D звук!
        heroRunSource.rolloffMode = AudioRolloffMode.Logarithmic;
        heroRunSource.minDistance = 2f;
        heroRunSource.maxDistance = 25f;
        heroRunSource.dopplerLevel = 0f;
        if (runHero != null) heroRunSource.clip = runHero;

        // Настройка звука бега минотавра
        minotaurRunSource.loop = true;
        minotaurRunSource.volume = 0.25f * masterVolume;
        minotaurRunSource.playOnAwake = false;
        minotaurRunSource.spatialBlend = 1f; // 3D звук!
        minotaurRunSource.rolloffMode = AudioRolloffMode.Logarithmic;
        minotaurRunSource.minDistance = 3f;
        minotaurRunSource.maxDistance = 35f;
        minotaurRunSource.dopplerLevel = 0f;
        if (runMinotaur != null) minotaurRunSource.clip = runMinotaur;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"Сцена загружена: {scene.name}");

        // Сбрасываем состояние при загрузке меню
        if (scene.name == "MainMenu")
        {
            isInGame = false;
            hasFleece = false;
            Debug.Log("Сцена MainMenu - вызываем PlayMenuSounds()");
            PlayMenuSounds();
        }
        // Сбрасываем состояние при загрузке игровой сцены
        else if (scene.name == "MainGame_Working" || scene.name.Contains("Game"))
        {
            isInGame = true;
            hasFleece = false;
            Debug.Log("Игровая сцена - isInGame = true");
            // Игровая музыка запустится при старте игры из GameSceneManager
        }
    }

    // ==================== ПУБЛИЧНЫЕ МЕТОДЫ ====================

    public void PlayMenuSounds()
    {
        Debug.Log("Запуск звуков меню");

        // Проверяем, назначены ли клипы
        if (menuMusic == null)
        {
            Debug.LogError("menuMusic не назначен!");
            return;
        }

        if (voiceIntro == null)
        {
            Debug.LogWarning("voiceIntro не назначен!");
        }

        // Проверяем, не играет ли уже музыка меню
        if (musicSource.isPlaying && musicSource.clip == menuMusic)
        {
            Debug.Log("Музыка меню уже играет, пропускаем повторный запуск");
            return;
        }

        // Останавливаем все звуки
        StopAllSounds();

        // Воспроизводим голосовое вступление только ПЕРВЫЙ раз при запуске игры
        if (voiceIntro != null && !hasPlayedIntro)
        {
            sfxSource.PlayOneShot(voiceIntro);
            hasPlayedIntro = true; // Устанавливаем флаг
            Debug.Log("Воспроизводим voiceIntro (первый раз при запуске игры)");
        }
        else if (voiceIntro != null && hasPlayedIntro)
        {
            Debug.Log("VoiceIntro уже воспроизводился, пропускаем");
        }

        // Включаем музыку меню
        musicSource.clip = menuMusic;
        musicSource.Play();
        Debug.Log($"Музыка меню запущена: {menuMusic.name}");
    }

    public void PlayButtonClick()
    {
        if (buttonClick != null)
        {
            sfxSource.PlayOneShot(buttonClick);
        }
        else
        {
            Debug.LogWarning("buttonClick не назначен!");
        }
    }

    public void StartGameMusic()
    {
        Debug.Log("Запуск игровой музыки");

        if (labyrinthStart == null)
        {
            Debug.LogError("labyrinthStart не назначен!");
            return;
        }

        StopAllSounds(); // Останавливаем все звуки

        musicSource.clip = labyrinthStart;
        musicSource.Play();
        Debug.Log($"Игровая музыка запущена: {labyrinthStart.name}");

        hasFleece = false;
        isInGame = true;
    }

    public void OnFleeceCollected()
    {
        if (!hasFleece)
        {
            hasFleece = true;

            if (labyrinthFinish != null)
            {
                StartCoroutine(SwitchMusic(labyrinthFinish, 1f));
                Debug.Log("Переключаем музыку на финальную");
            }
            else
            {
                Debug.LogWarning("labyrinthFinish не назначен!");
            }
        }
    }

    // ЗВУКИ БЕГА
    public void PlayHeroRun(bool play, Vector3 position)
    {
        if (heroRunSource == null || heroRunSource.clip == null)
        {
            Debug.LogWarning("Звук бега героя не настроен!");
            return;
        }

        // Обновляем позицию ВСЕГДА
        heroRunSource.transform.position = position;

        if (play)
        {
            if (!heroRunSource.isPlaying)
            {
                heroRunSource.Play();
                Debug.Log($"Звук бега героя ЗАПУЩЕН на позиции: {position}");
            }
            // Если уже играет - позиция уже обновлена выше
        }
        else
        {
            if (heroRunSource.isPlaying)
            {
                heroRunSource.Stop();
                Debug.Log("Звук бега героя ОСТАНОВЛЕН");
            }
        }
    }

    public void PlayMinotaurRun(bool play, Vector3 position)
    {
        if (minotaurRunSource == null || minotaurRunSource.clip == null)
        {
            Debug.LogWarning("Звук бега минотавра не настроен!");
            return;
        }

        // Обновляем позицию ВСЕГДА
        minotaurRunSource.transform.position = position;

        if (play)
        {
            if (!minotaurRunSource.isPlaying)
            {
                minotaurRunSource.Play();
                // Debug.Log($"Звук бега минотавра ЗАПУЩЕН на позиции: {position}");
            }
            // Если уже играет - позиция уже обновлена выше
        }
        else
        {
            if (minotaurRunSource.isPlaying)
            {
                minotaurRunSource.Stop();
                // Debug.Log("Звук бега минотавра ОСТАНОВЛЕН");
            }
        }
    }

    // ЗВУКИ СОБЫТИЙ
    public void PlayMinotaurRoar(Vector3 position)
    {
        if (minotaurRoar != null && Time.time > lastMinotaurRoarTime + 10f)
        {
            AudioSource.PlayClipAtPoint(minotaurRoar, position, 1.1f * masterVolume);
            lastMinotaurRoarTime = Time.time;
        }
    }

    public void PlayMinotaurAttack(Vector3 position)
    {
        // УБИРАЕМ проверку isInGame для атаки минотавра
        if (minotaurRoar != null)
        {
            AudioSource.PlayClipAtPoint(minotaurRoar, position, 1.2f * masterVolume);
        }

        if (heroScreamAttack != null)
        {
            AudioSource.PlayClipAtPoint(heroScreamAttack, position, 1.2f * masterVolume);
        }
    }

    public void PlaySnakeAttack(Vector3 position)
    {
        if (snakeHiss != null)
        {
            AudioSource.PlayClipAtPoint(snakeHiss, position, 1.3f * masterVolume);
        }

        if (heroScreamAttack != null)
        {
            AudioSource.PlayClipAtPoint(heroScreamAttack, position, 1.1f * masterVolume);
        }
    }

    public void PlayVasePickup(Vector3 position)
    {
        if (vasePickup != null)
        {
            AudioSource.PlayClipAtPoint(vasePickup, position, 1.4f * masterVolume);
        }
    }

    public void PlayFleecePickup(Vector3 position)
    {
        if (fleecePickup != null)
        {
            AudioSource.PlayClipAtPoint(fleecePickup, position, 1.5f * masterVolume);
        }
    }

    public void PlayHeroDeath(Vector3 position)
    {
        if (heroScreamDeath != null)
        {
            AudioSource.PlayClipAtPoint(heroScreamDeath, position, 1.0f * masterVolume);
        }
    }

    public void PlayDefeat()
    {
        Debug.Log("PlayDefeat вызван");

        // ОСТАНАВЛИВАЕМ звуки бега перед запуском музыки
        StopRunSoundsImmediately(); // - вот это!

        if (defeatMusic != null)
        {
            musicSource.Stop();
            musicSource.clip = defeatMusic;
            musicSource.Play();
            Debug.Log($"Музыка поражения запущена: {defeatMusic.name}");
        }
        else
        {
            Debug.LogError("defeatMusic не назначен!");
        }
    }

    public void PlayVictory()
    {
        Debug.Log("PlayVictory вызван");

        // ОСТАНАВЛИВАЕМ звуки бега перед запуском музыки
        StopRunSoundsImmediately(); // - и вот это!

        if (victoryMusic != null)
        {
            musicSource.Stop();
            musicSource.clip = victoryMusic;
            musicSource.Play();
            Debug.Log($"Музыка победы запущена: {victoryMusic.name}");
        }
        else
        {
            Debug.LogError("victoryMusic не назначен!");
        }
    }

    public void StopRunSoundsImmediately() // - измените private на public
    {
        Debug.Log("Немедленная остановка звуков бега");

        if (heroRunSource != null && heroRunSource.isPlaying)
        {
            heroRunSource.Stop();
            Debug.Log("Звук бега героя остановлен");
        }

        if (minotaurRunSource != null && minotaurRunSource.isPlaying)
        {
            minotaurRunSource.Stop();
            Debug.Log("Звук бега минотавра остановлен");
        }
    }

    // ИЗМЕНЕНИЕ: Сделаем метод публичным
    public void StopAllSounds()
    {
        if (musicSource != null && musicSource.isPlaying)
            musicSource.Stop();

        if (heroRunSource != null && heroRunSource.isPlaying)
            heroRunSource.Stop();

        if (minotaurRunSource != null && minotaurRunSource.isPlaying)
            minotaurRunSource.Stop();

        if (sfxSource != null && sfxSource.isPlaying)
            sfxSource.Stop();
    }

    // Вспомогательные методы
    private IEnumerator SwitchMusic(AudioClip newClip, float fadeTime)
    {
        if (newClip == null || musicSource == null)
        {
            Debug.LogError("SwitchMusic: newClip или musicSource равен null!");
            yield break;
        }

        Debug.Log($"Переключаем музыку на: {newClip.name}");

        // Если музыка не играет, просто запускаем новую
        if (!musicSource.isPlaying)
        {
            musicSource.clip = newClip;
            musicSource.Play();
            yield break;
        }

        // Плавное затухание - используем unscaledDeltaTime чтобы работало на паузе
        float startVolume = musicSource.volume;
        float timer = 0f;

        while (timer < fadeTime)
        {
            timer += Time.unscaledDeltaTime; // ИЗМЕНЕНИЕ: используем unscaledDeltaTime
            musicSource.volume = Mathf.Lerp(startVolume, 0f, timer / fadeTime);
            yield return null;
        }

        // Смена трека
        musicSource.Stop();
        musicSource.clip = newClip;
        musicSource.volume = startVolume;
        musicSource.Play();

        Debug.Log($"Музыка успешно переключена на: {newClip.name}");
    }

    // ==================== ДЕБАГ МЕТОДЫ ====================

    public void CheckAudioAssignments()
    {
        Debug.Log("=== Проверка назначения AudioClip ===");
        Debug.Log($"voiceIntro: {voiceIntro?.name ?? "НЕ НАЗНАЧЕН!"}");
        Debug.Log($"menuMusic: {menuMusic?.name ?? "НЕ НАЗНАЧЕН!"}");
        Debug.Log($"buttonClick: {buttonClick?.name ?? "НЕ НАЗНАЧЕН!"}");
        Debug.Log($"labyrinthStart: {labyrinthStart?.name ?? "НЕ НАЗНАЧЕН!"}");
        Debug.Log($"labyrinthFinish: {labyrinthFinish?.name ?? "НЕ НАЗНАЧЕН!"}");
        Debug.Log($"runHero: {runHero?.name ?? "НЕ НАЗНАЧЕН!"}");
        Debug.Log($"runMinotaur: {runMinotaur?.name ?? "НЕ НАЗНАЧЕН!"}");
        Debug.Log($"defeatMusic: {defeatMusic?.name ?? "НЕ НАЗНАЧЕН!"}");
        Debug.Log($"victoryMusic: {victoryMusic?.name ?? "НЕ НАЗНАЧЕН!"}");
        Debug.Log("==================================");
    }

    public void LogStatus()
    {
        Debug.Log($"=== SoundManager Status ===");
        Debug.Log($"isInGame: {isInGame}");
        Debug.Log($"hasFleece: {hasFleece}");
        Debug.Log($"hasPlayedIntro: {hasPlayedIntro}");
        Debug.Log($"Музыка: {musicSource?.clip?.name ?? "null"}, играет: {musicSource?.isPlaying}");
        Debug.Log($"Звук бега героя: {heroRunSource?.clip?.name ?? "null"}, играет: {heroRunSource?.isPlaying}");
        Debug.Log($"Звук бега минотавра: {minotaurRunSource?.clip?.name ?? "null"}, играет: {minotaurRunSource?.isPlaying}");
        Debug.Log($"===========================");
    }

    public void PlayMenuMusic()
    {
        Debug.Log("Запуск музыки меню (публичный метод)");

        if (menuMusic == null)
        {
            Debug.LogError("menuMusic не назначен!");
            return;
        }

        StopAllSounds();

        musicSource.clip = menuMusic;
        musicSource.Play();
        Debug.Log($"Музыка меню запущена: {menuMusic.name}");
    }

    // В SoundManager.cs добавьте метод для ТОЛЬКО музыки меню (без голоса)
    public void PlayMenuMusicOnly()
    {
        Debug.Log("Запуск ТОЛЬКО музыки меню");

        if (menuMusic == null)
        {
            Debug.LogError("menuMusic не назначен!");
            return;
        }

        StopAllSounds();

        musicSource.clip = menuMusic;
        musicSource.Play();
        Debug.Log($"Музыка меню запущена (без голоса): {menuMusic.name}");
    }

    public void DebugRunSoundPositions()
    {
        Debug.Log($"=== ПОЗИЦИИ ЗВУКОВ БЕГА ===");
        if (heroRunSource != null)
        {
            Debug.Log($"Герой: позиция={heroRunSource.transform.position}, играет={heroRunSource.isPlaying}, clip={heroRunSource.clip?.name}");
        }
        else
        {
            Debug.Log($"Герой: AudioSource = NULL!");
        }

        if (minotaurRunSource != null)
        {
            Debug.Log($"Минотавр: позиция={minotaurRunSource.transform.position}, играет={minotaurRunSource.isPlaying}, clip={minotaurRunSource.clip?.name}");
        }
        else
        {
            Debug.Log($"Минотавр: AudioSource = NULL!");
        }
        Debug.Log($"==========================");
    }
}