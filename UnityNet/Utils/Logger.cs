using System;
using System.Collections.Generic;
using System.Text;

namespace UnityNet.Utils
{
    internal static class Logger
    {
        internal static void Error(Exception ex)
        {
#if UNITY_EDITOR
            Debug.LogException(ex);
#else
            Console.WriteLine("Error: " + ex.Message);
#endif
        }

        internal static void Error(object message)
        {
#if UNITY_EDITOR
            Debug.LogError(message);
#else
            Console.WriteLine("Error: " + message);
#endif
        }

        internal static void Warning(object message)
        {
#if UNITY_EDITOR
            Debug.LogWarning(message);
#else
            Console.WriteLine("Warning: " + message);
#endif
        }

        internal static void Info(object message)
        {
#if UNITY_EDITOR
            Debug.LogException(message);
#else
            Console.WriteLine("Info: " + message);
#endif
        }
    }
}
