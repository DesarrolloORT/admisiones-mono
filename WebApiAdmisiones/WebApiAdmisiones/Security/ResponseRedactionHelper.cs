using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace WebApiAdmisiones.Security;

/// <summary>
/// Utiliza los mismos atributos [Redact] definidos en los DTO para producir
/// una versiÃ³n redactada de objetos que se van a loguear como salida.
/// No modifica el objeto original; crea estructuras (diccionarios/listas) limpias.
/// </summary>
public static class ResponseRedactionHelper
{
    public static object? Redact(object? value)
    {
        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
        return RedactInternal(value, visited);
    }

    private static object? RedactInternal(object? obj, HashSet<object> visited)
    {
        if (obj == null) return null;
        var type = obj.GetType();
        if (IsSimple(type)) return obj;
        if (!visited.Add(obj)) return null; // evitar ciclos

        if (obj is IEnumerable enumerable && obj is not string)
        {
            var list = new List<object?>();
            foreach (var item in enumerable)
                list.Add(RedactInternal(item, visited));
            return list;
        }

        var dict = new Dictionary<string, object?>();
        foreach (var p in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!p.CanRead) continue;
            var raw = p.GetValue(obj);
            var redactAttr = p.GetCustomAttributes().FirstOrDefault(a => a.GetType().Name == "RedactAttribute");
            if (redactAttr != null)
            {
                var modeObj = redactAttr.GetType().GetProperty("Mode")?.GetValue(redactAttr);
                dict[p.Name] = ApplyMode(raw, modeObj);
            }
            else
            {
                dict[p.Name] = RedactInternal(raw, visited);
            }
        }
        return dict;
    }

    private static bool IsSimple(Type t) => t.IsPrimitive || t.IsEnum || t == typeof(string) || t == typeof(decimal) || t == typeof(DateTime) || t == typeof(DateTimeOffset) || t == typeof(Guid);

    private static string? ApplyMode(object? val, object? modeObj)
    {
        if (val == null) return null;
        var token = "***";
        string str = val switch
        {
            DateTime dt => dt.ToString("O"),
            DateTimeOffset dto => dto.ToString("O"),
            byte[] bytes => Convert.ToBase64String(bytes), // para calcular Hash / length si hace falta
            _ => val.ToString() ?? string.Empty
        };
        var mode = modeObj?.ToString();
        if (string.IsNullOrWhiteSpace(mode)) mode = "Full";
        return mode switch
        {
            "Full" => token,
            "PreserveLength" => new string('*', str.Length),
            "First4Last4" => str.Length <= 8 ? token : str[..4] + token + str[^4..],
            "HashSha256" => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(str))),
            "BinaryLength" => val is byte[] b ? $"<bin:{b.Length}>" : $"<len:{str.Length}>",
            _ => token
        };
    }

    private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
    {
        public static readonly ReferenceEqualityComparer Instance = new();
        public new bool Equals(object? x, object? y) => ReferenceEquals(x, y);
        public int GetHashCode(object obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
    }
}
