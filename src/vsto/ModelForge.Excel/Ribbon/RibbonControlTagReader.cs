using System.Reflection;
using Office = Microsoft.Office.Core;

namespace ModelForge.Excel.Ribbon
{
    public static class RibbonControlTagReader
    {
        public static string ReadTag(Office.IRibbonControl control)
        {
            return control?.Tag ?? string.Empty;
        }

        public static string ReadTag(object control)
        {
            if (control == null)
            {
                return string.Empty;
            }

            var property = control.GetType().GetProperty("Tag", BindingFlags.Public | BindingFlags.Instance);
            return property?.GetValue(control)?.ToString() ?? string.Empty;
        }
    }
}
