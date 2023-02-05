using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;

namespace maa.perf.test.core.Utils
{
    public class SerializationHelper
    {
        private static ConcurrentDictionary<string, object> ParsedObjects = new ConcurrentDictionary<string, object>();

        public static T ReadFromFile<T>(string filePath)
        {
            ConstructorInfo defaultConstructorInfo = typeof(T).GetConstructor(new Type[] { });
            T persistedObject = (T)defaultConstructorInfo.Invoke(new object[] { });

            try
            {
                if (File.Exists(filePath))
                {
                    var deserializedObject = JsonConvert.DeserializeObject<T>(File.ReadAllText(filePath));
                    if (deserializedObject != null)
                    {
                        persistedObject = deserializedObject;
                    }
                }
            }
            catch (Exception x)
            {
                Tracer.TraceWarning($"Ignoring failed read from file '{filePath}'.  Exception: {x.Message}");
                // Ignore on purpose and return default object value
            }

            return persistedObject;
        }

        public static T ReadFromFileCached<T>(string filePath)
        {
            if (!ParsedObjects.ContainsKey(filePath))
            {
                ParsedObjects[filePath] = ReadFromFile<T>(filePath);
            }

            return (T)ParsedObjects[filePath];
        }

        public static void WriteToFile<T>(string filePath, T persistedObject)
        {
            try
            {
                File.WriteAllText(filePath, JsonConvert.SerializeObject(persistedObject));
            }
            catch (Exception)
            {
                // Tracer.TraceWarning($"Ignoring failed write to file '{filePath}'.  Exception: {x.Message}");
                // Ignore on purpose and return default object value
            }
        }
    }
}