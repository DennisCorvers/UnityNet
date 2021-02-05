using System;
using System.Collections.Generic;
using System.Text;

#if UNITY
using UnityEngine;
#endif
namespace UnityNet.Utils
{
    internal static class Logger
    {
        internal static void Error(Exception ex)
        {
#if UNITY
            UnityEngine.Debug.LogException(ex);
#else
            Console.WriteLine("Error: " + ex.Message);
#endif
        }

        internal static void Error(object message)
        {
#if UNITY
            UnityEngine.Debug.LogError(message);
#else
            Console.WriteLine("Error: " + message);
#endif
        }

        internal static void Warning(object message)
        {
#if UNITY
            UnityEngine.Debug.LogWarning(message);
#else
            Console.WriteLine("Warning: " + message);
#endif
        }

        internal static void Info(object message)
        {
#if UNITY
            UnityEngine.Debug.Log(message);
#else
            Console.WriteLine("Info: " + message);
#endif
        }
    }
}
