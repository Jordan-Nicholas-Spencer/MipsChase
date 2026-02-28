using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Target : MonoBehaviour
{
    public Player m_player;
    public enum eState : int
    {
        kIdle,
        kHopStart,
        kHop,
        kCaught,
        kNumStates
    }

    private Color[] stateColors = new Color[(int)eState.kNumStates]
   {
        new Color(255, 0,   0),
        new Color(0,   255, 0),
        new Color(0,   0,   255),
        new Color(255, 255, 255)
   };

    // External tunables.
    public float m_fHopTime = 0.2f;
    public float m_fHopSpeed = 6.5f;
    public float m_fScaredDistance = 3.0f;
    public int m_nMaxMoveAttempts = 50;

    // Internal variables.
    public eState m_nState;
    public float m_fHopStart;
    public Vector3 m_vHopStartPos;
    public Vector3 m_vHopEndPos;

    void Start()
    {
        // Setup the initial state and get the player GO.
        m_nState = eState.kIdle;
        m_player = GameObject.FindObjectOfType(typeof(Player)) as Player;
    }

    void FixedUpdate()
    {
        GetComponent<Renderer>().material.color = stateColors[(int)m_nState];
    }

    void Update()
    {
        // If caught, just follow the player
        if (m_nState == eState.kCaught)
        {
            return;
        }

        // Bounds of the screen
        Camera cam = Camera.main;        float z = 0.0f;
        Vector3 min = cam.ViewportToWorldPoint(new Vector3(0f, 0f, z - cam.transform.position.z));
        Vector3 max = cam.ViewportToWorldPoint(new Vector3(1f, 1f, z - cam.transform.position.z));

        switch (m_nState)
        {
            case eState.kIdle:
            {
                // If the player is close enough, start hopping away
                if (m_player != null)
                {
                    float dist = Vector3.Distance(transform.position, m_player.transform.position);
                    if (dist <= m_fScaredDistance)
                    {
                        // Hop away
                        m_nState = eState.kHopStart;
                    }
                }
                break;
            }

            case eState.kHopStart:
            {
                // Choose a hop end position away from the player without going off-screen
                m_vHopStartPos = transform.position;

                Vector3 away = Vector3.right;
                if (m_player != null)
                {
                    away = (transform.position - m_player.transform.position);
                    if (away.sqrMagnitude < 0.0001f) away = Random.insideUnitCircle.normalized;
                    away.Normalize();
                }

                float hopDistance = m_fHopSpeed * m_fHopTime;
                Vector3 chosenEnd = m_vHopStartPos;

                bool found = false;
                for (int i = 0; i < m_nMaxMoveAttempts; i++)
                {
                    // Add some randomness while still biased away from the player
                    float angleJitter = Random.Range(-70.0f, 70.0f);
                    Vector3 dir = Quaternion.Euler(0f, 0f, angleJitter) * away;

                    Vector3 candidate = m_vHopStartPos + dir * hopDistance;

                    // Make sure the candidate is within the screen bounds with some padding
                    float pad = 0.25f;
                    if (candidate.x >= min.x + pad && candidate.x <= max.x - pad &&
                        candidate.y >= min.y + pad && candidate.y <= max.y - pad)
                    {
                        chosenEnd = candidate;
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    // If we couldn't find a good candidate after many attempts, just hop toward the center of the screen
                    Vector3 screenCenter = (min + max) * 0.5f;
                    Vector3 towardCenter = (screenCenter - m_vHopStartPos).normalized;
                    
                    // Hop in that direction but make sure we don't go off-screen
                    Vector3 candidate = m_vHopStartPos + towardCenter * hopDistance;
                    float pad = 0.25f;
                    
                    chosenEnd = candidate;
                    chosenEnd.x = Mathf.Clamp(chosenEnd.x, min.x + pad, max.x - pad);
                    chosenEnd.y = Mathf.Clamp(chosenEnd.y, min.y + pad, max.y - pad);
                }

                m_vHopEndPos = chosenEnd;
                m_fHopStart = Time.time;
                m_nState = eState.kHop;
                break;
            }

            case eState.kHop:
            {
                // Lerp from the start to end position over the hop time
                float t = (Time.time - m_fHopStart) / m_fHopTime;
                t = Mathf.Clamp01(t);
                transform.position = Vector3.Lerp(m_vHopStartPos, m_vHopEndPos, t);

                if (t >= 1.0f)
                {
                    m_nState = eState.kIdle;
                }
                break;
            }
        }
    }

    void OnTriggerStay2D(Collider2D collision)
    {
        // If the player is still colliding and is diving, catch the target
        if (collision.gameObject == GameObject.Find("Player"))
        {
            if (m_player.IsDiving())
            {
                m_nState = eState.kCaught;
                transform.parent = m_player.transform;
                transform.localPosition = new Vector3(0.0f, -0.5f, 0.0f);
            }
        }
    }
}