namespace DbMetaTool.Models
{
    public class DomainMetadata
    {
        public string Name { get; set; }
        public string DataType { get; set; }
        public bool NotNull { get; set; }
        public string DefaultValue { get; set; }
        public string CheckConstraint { get; set; }
    }
}
