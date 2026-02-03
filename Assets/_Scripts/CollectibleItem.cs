using UnityEngine;

public class CollectibleItem : MonoBehaviour
{
    public enum ItemType { Snake, Vase, Fleece }
    public ItemType itemType;
    public int healthEffect = 20;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            SoundManager soundManager = SoundManager.Instance;
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            UIManager uiManager = FindFirstObjectByType<UIManager>();

            if (playerHealth != null)
            {
                switch (itemType)
                {
                    case ItemType.Snake:
                        if (soundManager != null)
                            soundManager.PlaySnakeAttack(transform.position);
                        playerHealth.TakeDamage(healthEffect);
                        if (uiManager != null) uiManager.ShowSnakeNotification();
                        break;
                    case ItemType.Vase:
                        // ЗВУК ВАЗЫ - ЗДЕСЬ!
                        if (soundManager != null)
                            soundManager.PlayVasePickup(transform.position);
                        playerHealth.Heal(healthEffect);
                        // UI вызывается в методе Heal
                        break;
                    case ItemType.Fleece:
                        // ЗВУК РУНА - ЗДЕСЬ!
                        if (soundManager != null)
                            soundManager.PlayFleecePickup(transform.position);
                        // Музыка переключается здесь
                        soundManager.OnFleeceCollected();
                        playerHealth.PickUpFleece();
                        // UI вызывается в методе PickUpFleece
                        break;
                }

                Destroy(gameObject);
            }
        }
    }
}