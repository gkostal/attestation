using Newtonsoft.Json;
using System;
using System.IO;
using System.Reflection;

namespace maa.perf.test.core.Utils
{
    public class SerializationHelper
    {
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
            catch (Exception)
            {
                //Tracer.TraceWarning($"Ignoring failed read from file '{filePath}'.  Exception: {x.Message}");
                // Ignore on purpose and return default object value
            }

            return persistedObject;
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