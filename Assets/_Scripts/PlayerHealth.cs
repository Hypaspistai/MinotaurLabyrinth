using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    private UIManager uiManager;
    public int health = 100;
    public int maxHealth = 100;
    public float minotaurDamageDelay = 3f; // время для добивания

    private bool hasFleece = false;
    private float minotaurContactTime = 0f;
    private bool isTouchingMinotaur = false;

    private SoundManager soundManager;

    void Start()
    {
        soundManager = SoundManager.Instance; // Используем синглтон

        uiManager = FindFirstObjectByType<UIManager>();
        if (uiManager != null)
        {
            uiManager.UpdateHealth(health);
        }
    }

    public void TakeDamage(int damage)
    {
        health -= damage;
        if (uiManager != null)
        {
            uiManager.UpdateHealth(health);
        }
        Debug.Log("Player health: " + health);

        if (health <= 0)
        {
            Die();
        }
    }

    public void Heal(int amount)
    {
        // УБРАТЬ звук! Он уже в CollectibleItem
        // if (soundManager != null) soundManager.PlayVasePickup(transform.position);

        health += amount;
        if (uiManager != null)
        {
            uiManager.UpdateHealth(health);
            uiManager.ShowHealNotification();
        }
        if (health > maxHealth * 2) health = maxHealth * 2;
        Debug.Log("Player healed. Health: " + health);
    }

    public void PickUpFleece()
    {
        // УБРАТЬ звук! Он уже в CollectibleItem
        // if (soundManager != null) soundManager.PlayFleecePickup(transform.position);

        hasFleece = true;
        if (uiManager != null)
        {
            uiManager.SetFleeceCollected(true);
            uiManager.ShowFleeceNotification();
        }
        Debug.Log("Golden Fleece picked up!");
    }

    public bool HasFleece()
    {
        return hasFleece;
    }

    void Update()
    {
        if (isTouchingMinotaur)
        {
            minotaurContactTime += Time.deltaTime;

            if (minotaurContactTime >= minotaurDamageDelay)
            {
                TakeDamage(maxHealth);
                isTouchingMinotaur = false;
                minotaurContactTime = 0f;
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Finish"))
        {
            if (hasFleece)
            {
                var playerController = GetComponent<PlayerController>();
                if (playerController != null) playerController.enabled = false;

                WinGame();
            }
            else
            {
                Debug.Log("Нужно золотое руно, чтобы сбежать!");
            }
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            // ДОБАВЛЯЕМ ПОЗИЦИЮ
            if (soundManager != null)
                soundManager.PlayMinotaurAttack(collision.GetContact(0).point);

            if (health <= maxHealth)
            {
                TakeDamage(100);
                if (uiManager != null) uiManager.ShowDamageNotification();
            }
            else
            {
                int bonusDamage = health - maxHealth;
                TakeDamage(bonusDamage);

                isTouchingMinotaur = true;
                minotaurContactTime = 0f;
                Debug.Log("Минотавр схватил героя! Смерть через " + minotaurDamageDelay + " секунд!");

                if (uiManager != null) uiManager.ShowDamageNotification();
            }
        }
    }

    void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            isTouchingMinotaur = false;
            minotaurContactTime = 0f;
        }
    }

    void Die()
    {
        // ДОБАВЛЯЕМ ПОЗИЦИЮ
        if (soundManager != null)
        {
            soundManager.PlayHeroDeath(transform.position);
            soundManager.PlayDefeat();
        }

        Debug.Log("Player died!");

        var playerController = GetComponent<PlayerController>();
        if (playerController != null) playerController.enabled = false;

        if (uiManager != null)
        {
            uiManager.ShowGameOver();
        }
        else
        {
            FindFirstObjectByType<GameSceneManager>()?.ReturnToMainMenu();
        }
    }

    void WinGame()
    {
        if (soundManager != null)
            soundManager.PlayVictory();

        Debug.Log("ПОБЕДА! Ты сбежал с золотым руном!");

        if (uiManager != null)
        {
            uiManager.ShowWin();
        }
        else
        {
            ReturnToMenuAfterWin();
        }
    }

    System.Collections.IEnumerator WinWithDelay()
    {
        Debug.Log("ПОБЕДА! Возвращаемся в меню через 2 секунды...");
        yield return new WaitForSeconds(2f);
        ReturnToMenuAfterWin();
    }

    void ReturnToMenuAfterWin()
    {
        Time.timeScale = 1f;
        FindFirstObjectByType<GameSceneManager>()?.ReturnToMainMenu();
    }
}