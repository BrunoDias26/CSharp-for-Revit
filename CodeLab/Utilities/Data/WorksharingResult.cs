namespace CodeLab.Utilities
{
    // This is a class to make WorksharingResult objects
    public class WorksharingResult
    {
        public List<Element> EditableElements { get; set; }
        public List<Element> NoneditableElements { get; set; }
        public bool Cancelled { get; set; }
    }
}
