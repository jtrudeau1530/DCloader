using System;

namespace DCLoader.Config
{
    // Non-generic interface so ConfigFile can work with entries without generics gymnastics.
    public interface IConfigEntryBase
    {
        string Key { get; }
        string Description { get; }

        void SetFromToml(object tomlValue);
        void SetFromTomlAndNotify(object tomlValue);
        string ToTomlValueString();

        Type ValueType { get; }
        object ValueObject { get; set; }
        object DefaultValueObject { get; }

        // Non-null for enum types (Enum.GetNames), null otherwise.
        string[] EnumValues { get; }
    }

    /// <summary>
    /// Typed config entry. Created via ConfigFile.Bind&lt;T&gt;().
    /// Value setter fires OnValueChanged; InternalSet() updates silently.
    /// </summary>
    public sealed class ConfigEntry<T> : IConfigEntryBase
    {
        private T _value;

        public string Key { get; }
        public string Description { get; }
        public T DefaultValue { get; }

        public T Value
        {
            get => _value;
            set
            {
                if (Equals(_value, value)) return;
                var old = _value;
                _value = value;
                OnValueChanged?.Invoke(old, value);
            }
        }

        // Sets value without firing OnValueChanged -- used during load/reload from disk.
        internal void InternalSet(T value) => _value = value;

        /// <summary>Fired when Value changes: (oldValue, newValue). Not fired by InternalSet().</summary>
        public event Action<T, T> OnValueChanged;

        internal ConfigEntry(string key, T defaultValue, string description)
        {
            Key = key;
            DefaultValue = defaultValue;
            Description = description;
            _value = defaultValue;
        }

        // -- IConfigEntryBase impl --

        Type IConfigEntryBase.ValueType => typeof(T);

        object IConfigEntryBase.ValueObject
        {
            get => (object)_value;
            set => Value = ConvertFromToml(value);
        }

        object IConfigEntryBase.DefaultValueObject => (object)DefaultValue;

        string[] IConfigEntryBase.EnumValues =>
            typeof(T).IsEnum ? Enum.GetNames(typeof(T)) : null;

        void IConfigEntryBase.SetFromToml(object tomlValue)
        {
            T converted = ConvertFromToml(tomlValue);
            InternalSet(converted);
        }

        void IConfigEntryBase.SetFromTomlAndNotify(object tomlValue)
        {
            T converted = ConvertFromToml(tomlValue);
            Value = converted; // goes through setter so OnValueChanged fires
        }

        string IConfigEntryBase.ToTomlValueString()
        {
            if (_value is bool b) return b ? "true" : "false";
            if (_value is string s) return $"\"{EscapeTomlString(s)}\"";
            if (_value is float f) return f.ToString("G", System.Globalization.CultureInfo.InvariantCulture);
            if (_value is double d) return d.ToString("G", System.Globalization.CultureInfo.InvariantCulture);
            if (_value is int || _value is long) return Convert.ToString(_value, System.Globalization.CultureInfo.InvariantCulture);
            if (typeof(T).IsEnum) return $"\"{_value}\"";
            return Convert.ToString(_value, System.Globalization.CultureInfo.InvariantCulture) ?? "";
        }

        private T ConvertFromToml(object raw)
        {
            try
            {
                Type target = typeof(T);

                if (target == typeof(bool))   return (T)(object)(bool)raw;
                if (target == typeof(string)) return (T)(object)raw.ToString();
                if (target == typeof(long))   return (T)(object)(long)raw;
                if (target == typeof(int))    return (T)(object)(int)(long)raw;
                if (target == typeof(double)) return (T)(object)(double)raw;
                if (target == typeof(float))  return (T)(object)(float)(double)raw;
                if (target.IsEnum)            return (T)Enum.Parse(target, raw.ToString(), ignoreCase: true);

                // fallback
                return (T)Convert.ChangeType(raw, target, System.Globalization.CultureInfo.InvariantCulture);
            }
            catch
            {
                return _value; // conversion failed, keep current
            }
        }

        private static string EscapeTomlString(string s)
            => s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
    }
}
