namespace AdrExplorer;

class Study
{
    public int Id { get; set; }

    public string ComponentName { get; set; }

    public string ComponentId { get; set; }

    public DateTime? ComponentManufacturingDate { get; set; }

    public string AccessionNumber { get; set; }

    public string ComponentOwnerName { get; set; }

    public string RequestedJobId { get; set; }

    public string RequestedJobDescription { get; set; }

    public DateTime StudyDateTime { get; set; }

    public string StudyId { get; set; }

    public string StudyDescription { get; set; }
}
