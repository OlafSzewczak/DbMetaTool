namespace DbMetaTool.Models
{
    public class DatabaseMetadata
    {
        public List<DomainMetadata> DomainsMetadata { get; set; } = [];
        public List<TableMetadata> TablesMetadata { get; set; } = [];
    }
}
