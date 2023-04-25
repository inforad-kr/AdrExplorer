using System.ComponentModel;

namespace AdrExplorer
{
    class Image : INotifyPropertyChanged
    {
        public int Id { get; set; }

        public string SeriesInstanceUid { get; set; }

        public string Modality { get; set; }

        public int? SeriesNumber { get; set; }

        public string SeriesDescription { get; set; }

        string m_ProtocolName;

        public string ProtocolName
        {
            get => m_ProtocolName;
            set
            {
                if (m_ProtocolName != value)
                {
                    m_ProtocolName = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ProtocolName)));
                }
            }
        }

        ImageStatus? m_Status;

        public ImageStatus Status
        {
            get
            {
                m_Status ??= ProtocolName != null && ProtocolName.Contains('0') || ProtocolName.Contains('1') ? ImageStatus.Done : ImageStatus.Pending;
                return m_Status.Value;
            }
            set
            {
                if (m_Status != value)
                {
                    m_Status = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Status)));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
