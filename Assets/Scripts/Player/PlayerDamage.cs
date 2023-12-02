using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;
using UnityTutorial.PlayerControl;

public class PlayerDamage : MonoBehaviour
{
    public float playerHealth = 100;
    public float maxHealth = 100;
    private float nextPossibleDamage = 0.0f;
    public float damageCooldown = 1.0f;
    public HealthBar healthBar;
    private bool isHealing = false;
    private float regenerationDuration = 0.0f;
    private float regeneration = 0.0f;
    private int regenerationCounter = 0;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (isHealing)
        {
            heal();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (PlayerDamagePossible(collision.gameObject))
        {

            float minMagnitude = PlayerManager.Instance.minPlayerDamageMagnitude;
            float collisionMagnitude = collision.relativeVelocity.magnitude;
            Debug.Log("Collision Velocity: " + collision.relativeVelocity);
            Debug.Log("Collision Magnitude: " + collisionMagnitude);

            if (collisionMagnitude > minMagnitude)
            {
                playerHealth -= calculatePlayerDamage(collision.gameObject, collisionMagnitude);
                
                Debug.Log("Player Health reduced to: " + playerHealth);
                healthBar.SetHealth(playerHealth);
            }
        }
    }

    private bool PlayerDamagePossible(GameObject collisionObject)
    {
        if (collisionObject.tag == ObjectManager.Instance.throwableObjectTag && 
            collisionObject.GetComponent<ObjectDurability>().spawnProtectionActive &&
            this.gameObject.GetComponent<PlayerController>().getShieldStatus() &&
            Time.time > nextPossibleDamage) 
        {
            nextPossibleDamage = Time.time + damageCooldown;
            return true; 
        }

        return false;
    }

    private float calculatePlayerDamage(GameObject collisionObject, float collisionMagnitude)
    {
        float damageMultiplicator = PlayerManager.Instance.playerDamageMultiplicator;
        float maxPossibleDamage = PlayerManager.Instance.maxPossiblePlayerDamage;
        float durabilityDamage = collisionObject.GetComponent<ObjectDurability>().currentDurability;

        float calculatedDamage = durabilityDamage * collisionMagnitude * damageMultiplicator;
        return Mathf.Clamp(calculatedDamage, 0.0f, maxPossibleDamage);
    }

    public void regenerateHealth(float regeneration, float regernerationDuration)
    {
        this.regeneration = regeneration;
        this.regenerationDuration = regernerationDuration;
        this.isHealing = true;
    }

    private IEnumerator heal()
    {
        if(this.regenerationCounter < this.regenerationDuration)
        {
            playerHealth = Mathf.Clamp(playerHealth + this.regeneration, 0.0f, maxHealth);
            healthBar.SetHealth(playerHealth);
            this.regenerationCounter += 1;
            yield return new WaitForSeconds(1);
        }
        else
        {
            this.isHealing=false;
            yield return null;
        }
       
    }
}
