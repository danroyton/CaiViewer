namespace CaiViewer.Core.Domain;

public class ModelVersionTrainedWord
{
    public int Id { get; set; }
    public int ModelVersionId { get; set; }
    public string Word { get; set; } = string.Empty;
}
