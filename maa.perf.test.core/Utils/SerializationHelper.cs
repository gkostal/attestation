using System;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;

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
                var deserializedObject = JsonConvert.DeserializeObject<T>(File.ReadAllText(filePath));
                if (deserializedObject != null)
                {
                    persistedObject = deserializedObject;
                }
            }
            catch (Exception)
            {
                // Ignore on purpose and return default object value
            }

            return persistedObject;
        }

        public static void WriteToFile<T>(string fileName, T persistedObject)
        {
            File.WriteAllText(fileName, JsonConvert.SerializeObject(persistedObject));
        }
    }
}