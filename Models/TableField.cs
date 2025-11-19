namespace DbMetaTool.Models
{
    public class TableField
    {
        public string Name { get; set; }
        public string DataType { get; set; }
        public bool NotNull { get; set; }
        public string DefaultValue { get; set; }
        public int Position { get; set; }
    }
}
