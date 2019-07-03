using System.Runtime.Serialization;

namespace WpfDisplayBuilder
{
    public enum CustomPropType
    {
        Parameter,
        Point,
        Value
    }

    [DataContract]
    public class CustomProperty
    {
        [DataMember(Name = "n")]
        public string Name { get; set; }

        [DataMember(Name = "t")]
        public CustomPropType Type { get; set; }
        
        [DataMember(Name = "v")]
        public string Default { get; set; }
        
        [DataMember(Name = "d")]
        public string Descriptor { get; set; }
    }
}