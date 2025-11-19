namespace DbMetaTool.Models
{
    public class TableMetadata
    {
        public string Name { get; set; }
        public List<TableField> FieldsMetadata { get; set; } = new List<TableField>();
    }
}
