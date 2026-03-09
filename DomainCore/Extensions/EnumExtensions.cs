using System.Collections.Concurrent;
using System.ComponentModel;
using System.Reflection;

namespace DomainObjects.Extensions;

public static class EnumExtensions
{
    private static readonly ConcurrentDictionary<Enum, string> _cache = new();

    public static string GetDescription(this Enum value) =>
        _cache.GetOrAdd(value, static v =>
        {
            var member = v.GetType().GetMember(v.ToString()).FirstOrDefault();
            var attr = member?.GetCustomAttribute<DescriptionAttribute>();
            return attr?.Description ?? v.ToString(); // fallback: nome do enum
        });
}
