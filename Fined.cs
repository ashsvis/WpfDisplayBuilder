using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfDisplayBuilder
{
    public static class Fined
    {
        public static string Value(object value)
        {
            if (value == null) return String.Empty;
            var list = new Dictionary<Type, TypeConverter>
                {
                    /* примерчик особенного конвертера
                {typeof (bool), new BooleadTypeConverter()},
                {typeof (TableKind), new EnumTypeConverter(typeof(TableKind))},
                 */
                };
            var type = value.GetType();
            if (type == typeof(bool))
            {
                var convertTo = (new BooleadTypeConverter()).ConvertTo(value, typeof(string));
                if (convertTo != null)
                    return convertTo.ToString();
            }
            else if (type.IsEnum)
            {
                var convertTo = (new EnumTypeConverter(type)).ConvertTo(value, typeof(string));
                if (convertTo != null)
                    return convertTo.ToString();
            }
            else if (list.ContainsKey(type))
            {
                var convertTo = list[type].ConvertTo(value, typeof(string));
                if (convertTo != null)
                    return convertTo.ToString();
            }
            return value.ToString();
        }

        /// <summary>
        /// TypeConverter для Enum, преобразовывающий Enum к строке с
        /// учетом атрибута Description
        /// </summary>
        private class EnumTypeConverter : EnumConverter
        {
            private readonly Type _enumType;
            /// <summary>Инициализирует экземпляр</summary>
            /// <param name="type">тип Enum</param>
            public EnumTypeConverter(Type type)
                : base(type)
            {
                _enumType = type;
            }

            public override bool CanConvertTo(ITypeDescriptorContext context,
              Type destType)
            {
                return destType == typeof(string);
            }

            public override object ConvertTo(ITypeDescriptorContext context,
                                             CultureInfo culture,
                                             object value, Type destType)
            {
                if (value == null) return "";
                var fi = _enumType.GetField(Enum.GetName(_enumType, value));
                var dna =
                    (DescriptionAttribute)Attribute.GetCustomAttribute(
                        fi, typeof(DescriptionAttribute));
                return dna != null ? dna.Description : value.ToString();
            }

            public override bool CanConvertFrom(ITypeDescriptorContext context,
              Type srcType)
            {
                return srcType == typeof(string);
            }

            public override object ConvertFrom(ITypeDescriptorContext context,
              CultureInfo culture, object value)
            {
                if (value == null) return Enum.GetValues(_enumType).GetValue(0);
                foreach (var fi in from fi in _enumType.GetFields()
                                   let dna = (DescriptionAttribute)Attribute.GetCustomAttribute(
                                                                  fi, typeof(DescriptionAttribute))
                                   where (dna != null) && ((string)value == dna.Description)
                                   select fi)
                {
                    return Enum.Parse(_enumType, fi.Name);
                }

                return Enum.Parse(_enumType, (string)value);
            }

        }

        private class BooleadTypeConverter : BooleanConverter
        {
            private const string Yes = "Да";
            private const string No = "Нет";

            public override object ConvertTo(ITypeDescriptorContext context,
                                             CultureInfo culture, object value, Type destType)
            {
                if (value != null)
                    return (bool)value ? Yes : No;
                return "";
            }

            public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
            {
                return (string)value == Yes;
            }
        }
    }
}
