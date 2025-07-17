using UnityEngine;
using UnityEngine.SceneManagement;

public class DoorTrigger : MonoBehaviour
{
    [Tooltip("Build index of the scene to load when the player enters the trigger.")]
    public int sceneIndexToLoad;

    [Tooltip("Tag used to identify the player object.")]
    public string playerTag = "Player";

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            if (sceneIndexToLoad >= 0 && sceneIndexToLoad < SceneManager.sceneCountInBuildSettings)
            {
                SceneManager.LoadScene(sceneIndexToLoad);
            }
            else
            {
                Debug.LogWarning("Invalid scene index set on DoorTrigger.");
            }
        }
    }
}
