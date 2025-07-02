using System.Reflection;

namespace Messenger;

[AttributeUsage(AttributeTargets.Class)]
public class NameAttribute(string Name) : Attribute
{
    public string Name { get; } = Name;

    public static string GetName(Type t)
    {
        if (t.GetCustomAttribute<NameAttribute>() is { } attr)
        {
            return attr.Name;
        }

        return t.Name;
    }
}