using UnityEngine;

public class MemoryLeakHelper : MonoBehaviour
{
    private int frameCounter = 0;
    
    void LateUpdate()
    {
        frameCounter++;
        
        // Force batched job scheduling every frame
        Unity.Jobs.JobHandle.ScheduleBatchedJobs();
        
        // Periodic cleanup every 2 seconds
        if (frameCounter % 120 == 0)
        {
            System.GC.Collect();
            Resources.UnloadUnusedAssets();
        }
    }
}
