using System.Reflection;

namespace Messenger;

[AttributeUsage(AttributeTargets.Class)]
public class MessageNameAttribute(string Name) : Attribute
{
    public string Name { get; } = Name;

    public static string GetMessagingName(Type t)
    {
        if (t.GetCustomAttribute<MessageNameAttribute>() is { } attr)
        {
            return attr.Name;
        }

        return t.Name;
    }
}