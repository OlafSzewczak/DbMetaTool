namespace DbMetaTool.Models
{
    public class ProcedureMetadata
    {
        public string Name { get; set; }
        public string Source { get; set; }
        public List<ProcedureParameterMetadata> Parameters { get; set; } = [];
    }
}
