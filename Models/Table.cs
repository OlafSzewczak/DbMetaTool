namespace DbMetaTool.Models
{
    public class Table
    {
        public string Name { get; set; }
        public List<TableField> Fields { get; set; } = new List<TableField>();
    }
}
