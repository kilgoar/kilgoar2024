#if UNITY_EDITOR
using UnityEditor;
#endif

public static class ProgressManager
{
    /// <summary>Removes any finished progress bars with the same name.</summary>
    public static void RemoveProgressBars(string progressName)
    {
        #if UNITY_EDITOR
        if (string.IsNullOrEmpty(progressName)) return; // Check if progressName is null or empty

        for (int i = 0; i < Progress.GetCount(); i++)
        {
            var progressId = Progress.GetId(i);
            if (progressId == null) continue; // Check if the id is null

            var progress = Progress.GetProgressById(progressId);
            if (progress == null) continue; // Check if progress is null

            if (progress.finished && progress.name != null && progress.name.Contains(progressName))
            {
                progress.Remove();
            }
        }
        #endif
    }
}
