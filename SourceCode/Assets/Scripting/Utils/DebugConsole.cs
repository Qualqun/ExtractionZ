#if !UNITY_SERVER
using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class DebugConsole : MonoBehaviour
{
    [DllImport("kernel32.dll")]
    private static extern bool AllocConsole();

    [DllImport("kernel32.dll")]
    private static extern bool FreeConsole();

    public static void Show()
    {
        AllocConsole();
        Debug.Log("Console opened!");
    }

    public static void Hide()
    {
        FreeConsole();
    }
    void Awake()
    {
        Show();
        Application.logMessageReceived += HandleLog;
    }

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        ConsoleColor originalColor = Console.ForegroundColor;

        switch (type)
        {
            case LogType.Error:
            case LogType.Exception:
                Console.ForegroundColor = ConsoleColor.Red;
                break;
            case LogType.Warning:
                Console.ForegroundColor = ConsoleColor.Yellow;
                break;
            default:
                Console.ForegroundColor = ConsoleColor.White;
                break;
        }

        Console.WriteLine(logString);
        Console.ForegroundColor = originalColor;
    }
}
#endif