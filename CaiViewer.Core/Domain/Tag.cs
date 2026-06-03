namespace CaiViewer.Core.Domain;

public class Tag
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class ModelTag
{
    public int ModelId { get; set; }
    public int TagId { get; set; }
}
