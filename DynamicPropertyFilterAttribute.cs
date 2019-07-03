using System;

namespace WpfDisplayBuilder
{
    /// <summary>
    /// Атрибут для поддержки динамически показываемых свойств
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = true)]
    public class DynamicPropertyFilterAttribute : Attribute
    {
        readonly string _propertyName;

        /// <summary>
        /// Название свойства, от которого будет зависить видимость
        /// </summary>
        public string PropertyName
        {
            get { return _propertyName; }
        }

        readonly string _showOn;

        /// <summary>
        /// Значения свойства, от которого зависит видимость 
        /// (через запятую, если несколько), при котором свойство, к
        /// которому применен атрибут, будет видимо. 
        /// </summary>
        public string ShowOn
        {
            get { return _showOn; }
        }

        /// <summary>
        /// Конструктор  
        /// </summary>
        /// <param name="propertyName">Название свойства, от которого будет 
        /// зависеть видимость</param>
        /// <param name="value">Значения свойства (через запятую, если несколько), 
        /// при котором свойство, к которому применен атрибут, будет видимо.</param>
        public DynamicPropertyFilterAttribute(string propertyName, string value)
        {
            _propertyName = propertyName;
            _showOn = value;
        }
    }
}
