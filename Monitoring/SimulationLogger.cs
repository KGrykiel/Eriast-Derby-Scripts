using System.Collections.Generic;
using UnityEngine;

public static class SimulationLogger
{
    private static List<string> simulationLog = new List<string>();
    public static IReadOnlyList<string> Log => simulationLog;

    public static void LogEvent(string message)
    {
        simulationLog.Add(message);

        // Remove oldest entries if we exceed the limit
        while (simulationLog.Count > 1000)
        {
            simulationLog.RemoveAt(0);
        }

        Debug.Log(message);
    }
}
