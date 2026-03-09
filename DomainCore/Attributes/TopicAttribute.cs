namespace DomainObjects.Attributes;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class TopicAttribute(string key) : Attribute
{
    public string Key { get; } = key;
}
