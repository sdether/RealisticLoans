using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using ColossalFramework.UI;

namespace RealisticLoans
{
    public class Utils
    {
        private static readonly Logger logger =
            new Logger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private const string SPACING = "  ";

        private static readonly HashSet<string> IGNORE = new HashSet<string>(new[]
        {
            "atlas",
            "isVisibleSelf",
            "components",
            "cachedName",
            "cachedTransform",
            "isLocalized",
            "isTooltipLocalized",
            "builtinKeyNavigation",
            "isInteractive",
            "hasFocus",
            "containsMouse",
            "canFocus",
            "containsFocus",
            "childCount",
            "parent",
            "renderOrder",
            "tooltipAnchor",
            "bringTooltipToFront",
            "tooltipLocaleID",
            "tooltip",
            "useGUILayout",
            "transform",
            "gameObject",
            "tag",
            "hideFlags",
            "zOrder",
        });

        public static void DumpHierarchy(UIComponent c, TextWriter writer, string prefix = "")
        {
            writer.WriteLine($"{prefix}{c.name}:");
            prefix += SPACING;
            writer.WriteLine($"{prefix}type: {c.GetType().Name}");
            writer.WriteLine($"{prefix}properties:");
            foreach (PropertyDescriptor descriptor in TypeDescriptor.GetProperties(c).Sort())
            {
                if (IGNORE.Contains(descriptor.Name)) continue;
                var value = descriptor.GetValue(c);
                if(value == null) continue;
                var propertyType = descriptor.PropertyType.ToString();
                
                if (propertyType == "System.String")
                {
                    var strValue = $"\"{value}\"";
                    strValue = strValue.Replace("\n", "\\n");
                    value = strValue;
                }

                writer.WriteLine($"{prefix}{SPACING}{descriptor.Name}: {value}");
            }

            if (c.components.Count > 0)
            {
                writer.WriteLine($"{prefix}children:");
                foreach (var child in c.components)
                {
                    DumpHierarchy(child, writer, prefix + SPACING);
                }
            }
        }
    }
}