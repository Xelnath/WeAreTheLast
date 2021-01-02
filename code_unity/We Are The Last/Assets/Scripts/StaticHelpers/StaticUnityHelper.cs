using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public static class StaticUnityHelper
{
    public static T DeepClone<T>(this T toClone)
    {
        using (var stream = new MemoryStream())
        {
            var formatter = new BinaryFormatter();
            formatter.Serialize(stream, toClone);
            stream.Seek(0, SeekOrigin.Begin);
            return (T)formatter.Deserialize(stream);
        }
    }
}