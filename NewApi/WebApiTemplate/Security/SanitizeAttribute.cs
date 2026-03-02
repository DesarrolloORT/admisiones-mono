using Ganss.Xss;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Reflection;

namespace Sanitization.Code
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class SanitizeAttribute : ActionFilterAttribute
    {
        private readonly HtmlSanitizer _sanitizer;

        public SanitizeAttribute()
        {
            _sanitizer = new HtmlSanitizer();
            //_sanitizer.AllowedTags.Clear(); // Sanitizado agresivo
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var keys = context.ActionArguments.Keys.ToList();

            foreach (var key in keys)
            {
                var originalValue = context.ActionArguments[key];

                if (originalValue != null)
                {
                    var sanitized = SanitizeObject(originalValue);

                    // 🔥 Asegurarse de actualizar el valor modificado
                    context.ActionArguments[key] = sanitized;
                }
            }


            base.OnActionExecuting(context);
        }

        private  object? SanitizeObject(object obj)
        {
            if (obj == null)
                return null;

            if (obj is string str)
                return _sanitizer.Sanitize(str);

            var type = obj.GetType();

            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!EsPropiedadSanitizable(prop))
                    continue;

                try
                {
                    SanitizeProperty(obj, prop);
                }
                catch (Exception ex)
                {
                    if (prop.GetIndexParameters().Length > 0)
                        continue; // Omitir propiedades indexadas
                    throw new InputSanitizationException($"Error sanitizing property {prop.Name} of type {type.Name}: {ex.Message}");
                }
            }

            return obj;
        }

        private static bool EsPropiedadSanitizable(PropertyInfo prop)
        {
            return prop.CanRead
                && prop.CanWrite
                && !prop.IsDefined(typeof(AllowHtmlAttribute), inherit: true);
        }

        private void SanitizeProperty(object obj, PropertyInfo prop)
        {
            if (prop.PropertyType == typeof(string))
            {
                var original = (string?)prop.GetValue(obj);
                if (original != null)
                {
                    var sanitized = _sanitizer.Sanitize(original);
                    prop.SetValue(obj, sanitized);
                }
            }
            else if (EsTipoComplejo(prop.PropertyType))
            {
                var nested = prop.GetValue(obj);
                if (nested != null)
                    SanitizeObject(nested);
            }
        }

        private static bool EsTipoComplejo(Type type)
        {
            return !type.IsPrimitive
                && !type.IsEnum
                && !type.IsValueType
                && type != typeof(DateTime);
        }



    }



    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class AllowHtmlAttribute : Attribute
    {
    }

    public class InputSanitizationException : Exception
    {
        public InputSanitizationException(string message) : base(message) { }
    }
}
