using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace AdrExplorer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        readonly Settings m_Settings = Settings.Load();
        readonly HttpClient m_HttpClient;

        public MainWindow()
        {
            InitializeComponent();

            FilterPanel.DataContext = m_Settings;

            m_HttpClient = new()
            {
                BaseAddress = new($"{m_Settings.ServerUrl}/api/v1/")
            };
            m_HttpClient.DefaultRequestHeaders.Authorization = new("Bearer", m_Settings.ServerToken);
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await PopulateStudyGrid();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            m_Settings.Save();
            m_HttpClient.Dispose();
        }

        private void Grid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.PropertyName == nameof(Study.Id))
            {
                e.Cancel = true;
            }
        }

        private async void DatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
            {
                await PopulateStudyGrid();
            }
        }

        private async void StudyGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var study = StudyGrid.SelectedItems.Count == 1 ? (Study)StudyGrid.SelectedItems[0] : null;
            if (study != null)
            {
                await PopulateImageGrid(study);
            }
            else
            {
                ImageGrid.ItemsSource = null;
            }
        }

        private async Task PopulateStudyGrid()
        {
            try
            {
                var studies = await m_HttpClient.GetFromJsonAsync<Study[]>($"study?StartStudyDateTime={m_Settings.StartDate:yyyy-MM-dd}&EndStudyDateTime={m_Settings.EndDate:yyyy-MM-dd}");
                StudyGrid.ItemsSource = studies;
            }
            catch (Exception ex)
            {
                StudyGrid.ItemsSource = null;
                MessageBox.Show(ex.Message, Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task PopulateImageGrid(Study study)
        {
            try
            {
                var images = await m_HttpClient.GetFromJsonAsync<Image[]>($"study/{study.Id}/image");
                ImageGrid.ItemsSource = images;
            }
            catch (Exception ex)
            {
                ImageGrid.ItemsSource = null;
                MessageBox.Show(ex.Message, Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
