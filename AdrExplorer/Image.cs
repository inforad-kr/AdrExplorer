using System.ComponentModel;
using System.Text.RegularExpressions;

namespace AdrExplorer;

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
            m_Status ??= ProtocolName != null && m_AdrResultRegex.IsMatch(ProtocolName) ? ImageStatus.Done : ImageStatus.Pending;
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

    Regex m_AdrResultRegex = new("[0-1]");

    public void SetAdrResult(bool value)
    {
        if (ProtocolName != null)
        {
            ProtocolName = m_AdrResultRegex.Replace(ProtocolName, "");
        }
        ProtocolName += value ? "1" : "0";
    }

    public event PropertyChangedEventHandler PropertyChanged;
}
