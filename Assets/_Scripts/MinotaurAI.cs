using UnityEngine;
using UnityEngine.AI;

public class MinotaurAI : MonoBehaviour
{
    public Transform player;
    private NavMeshAgent agent;
    private SoundManager soundManager;

    private bool wasMoving = false;
    private float lastRoarTime = 0f;
    private float roarInterval = 10f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        // ÈÑÏÐÀÂËÅÍÎ: Èñïîëüçóåì ñèíãëòîí
        soundManager = SoundManager.Instance;

        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player").transform;

        agent.speed = 3.5f;
        agent.acceleration = 8f;
    }

    void Update()
    {
        // ÏÐÎÂÅÐÊÀ: åñëè Time.timeScale == 0 (ïàóçà) - íå îáíîâëÿåì çâóê
        if (Time.timeScale == 0f)
        {
            if (soundManager != null)
            {
                soundManager.PlayMinotaurRun(false, transform.position);
            }
            return;
        }

        if (player != null)
            agent.SetDestination(player.position);

        // ÇÂÓÊ ÁÅÃÀ ÌÈÍÎÒÀÂÐÀ - âñåãäà îáíîâëÿåì ïîçèöèþ, äàæå íà ïàóçå
        bool isMoving = agent.velocity.magnitude > 0.1f;

        if (soundManager != null)
        {
            // ÓÏÐÎÑÒÈÒÅ äî ýòîãî (áåç ïðîâåðêè Time.timeScale):
            soundManager.PlayMinotaurRun(isMoving, transform.position);
        }

        wasMoving = isMoving;

        // ÑËÓ×ÀÉÍÛÉ ÐÛÊ ÌÈÍÎÒÀÂÐÀ - òîëüêî åñëè èãðà íå íà ïàóçå
        if (Time.timeScale > 0f && Time.time > lastRoarTime + roarInterval)
        {
            if (soundManager != null)
                soundManager.PlayMinotaurRoar(transform.position);
            lastRoarTime = Time.time;
        }
    }

    void OnDrawGizmos()
    {
        if (agent != null && agent.hasPath)
        {
            Gizmos.color = Color.red;
            for (int i = 0; i < agent.path.corners.Length - 1; i++)
                Gizmos.DrawLine(agent.path.corners[i], agent.path.corners[i + 1]);
        }
    }
}