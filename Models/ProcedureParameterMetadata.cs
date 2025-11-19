namespace DbMetaTool.Models
{
    public class ProcedureParameterMetadata
    {
        public string Name { get; set; }
        public string DataType { get; set; }
        public int ParameterType { get; set; } // 0 = INPUT, 1 = OUTPUT
        public int Position { get; set; }
    }
}
