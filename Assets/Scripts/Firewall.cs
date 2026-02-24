using UnityEngine;

public class Firewall : Entity
{
    private int health = 3;

    [Header("Death Effect")]
    [SerializeField] private GameObject deathEffectPrefab;
    [SerializeField] private float deathEffectDuration = 0.2f;

    public override void Onhit(Vector2Int attackDirection)
    {
        if (GridManager == null)
        {
            return;
        }
        health--;
        if (health <= 0)
        {
            Die();
        }   
    }

    public override void Die()
    {
        Vector3 deathPosition = transform.position;
        Quaternion deathRotation = transform.rotation;

        base.Die();

        if (deathEffectPrefab != null)
        {
            GameObject effect = Instantiate(deathEffectPrefab, deathPosition, deathRotation);
            if (deathEffectDuration > 0f)
            {
                Destroy(effect, deathEffectDuration);
            }
        }
    }
}
