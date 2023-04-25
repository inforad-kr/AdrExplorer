namespace AdrExplorer
{
    class Image
    {
        public int Id { get; set; }

        public string Modality { get; set; }

        public int? SeriesNumber { get; set; }

        public string SeriesDescription { get; set; }

        public string ProtocolName { get; set; }
    }
}
