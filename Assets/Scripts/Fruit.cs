using UnityEngine;

/// <summary>
/// Defines the behavior of a fruit object.
/// </summary>
public class Fruit : MonoBehaviour
{
    /// <summary>
    /// Called when the fruit collides with another object.
    /// </summary>
    /// <param name="other">The Collider of the object the fruit collided with.</param>
    private void OnTriggerEnter(Collider other)
    {
        // Check if the fruit was caught by the player's basket
        if (other.CompareTag("Player"))
        {
            // Increase the score
            GameManager.Instance.AddScore(1);

            // Destroy the fruit object
            Destroy(gameObject);
        }
        // Check if the fruit fell off the bottom of the screen
        else if (other.CompareTag("Boundary"))
        {
            // The player loses a life
            GameManager.Instance.LoseLife();

            // Destroy the fruit object
            Destroy(gameObject);
        }
    }
}
