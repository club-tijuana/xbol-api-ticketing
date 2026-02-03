using System.ComponentModel;
using System.Reflection;

namespace XBOL.Ticketing.Core.Commons.Extensions
{
    public static class EnumExtensions
    {
        public static string GetDescription(this Enum value)
        {
            FieldInfo? field = value.GetType().GetField(value.ToString());

            DescriptionAttribute? attribute =
                field?.GetCustomAttribute<DescriptionAttribute>();

            return attribute?.Description ?? value.ToString();
        }
    }
}
