using UnityEngine;

/// <summary>
/// Defines the behavior of a bomb object.
/// </summary>
public class Bomb : MonoBehaviour
{
    /// <summary>
    /// Called when the bomb collides with another object.
    /// </summary>
    /// <param name="other">The Collider of the object the bomb collided with.</param>
    private void OnTriggerEnter(Collider other)
    {
        // Check if the bomb was "caught" by the player
        if (other.CompareTag("Player"))
        {
            // End the game immediately
            GameManager.Instance.GameOver("You caught a bomb!");

            // Destroy the bomb object
            Destroy(gameObject);
        }
        // Check if the bomb fell off the bottom of the screen
        else if (other.CompareTag("Boundary"))
        {
            // Unlike fruits, nothing happens if a bomb is missed.
            // It's simply removed from the game.
            Destroy(gameObject);
        }
    }
}
