using System;
using System.Linq;
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
            if (e.PropertyName == nameof(Study.Id) || e.PropertyName == nameof(Image.SeriesInstanceUid))
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

        private async void ProcessButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                IsEnabled = false;
                StudyGrid.SelectionChanged -= StudyGrid_SelectionChanged;

                var studies = StudyGrid.SelectedItems.Cast<Study>().ToList();
                StudyGrid.SelectedItems.Clear();
                foreach (var study in studies)
                {
                    await ProcessStudy(study);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsEnabled = true;
                StudyGrid.SelectionChanged += StudyGrid_SelectionChanged;
            }
        }

        private async Task ProcessStudy(Study study)
        {
            StudyGrid.SelectedItem = study;
            await PopulateImageGrid(study);

            var images = ImageGrid.Items.Cast<Image>().ToList();
            if (!m_Settings.Overwrite)
            {
                images = images.Where(image => image.Status == ImageStatus.Pending).ToList();
            }
            foreach (var image in images)
            {
                await ProcessImage(image);
            }
        }

        readonly IAdrProcessor m_AdrProcessor = new AdrEmulator();

        private async Task ProcessImage(Image image)
        {
            var files = await m_HttpClient.GetFromJsonAsync<ImageFile[]>($"image/{image.Id}/file");
            if (files.Length > 0)
            {
                image.Status = ImageStatus.Downloading;
                var data = await m_HttpClient.GetByteArrayAsync($"file/{files[0].Id}/data?jpeg=true");

                image.Status = ImageStatus.Processing;
                image.SetAdrResult(await m_AdrProcessor.ProcessFile(data));

                var response = await m_HttpClient.PutAsJsonAsync($"image/{image.Id}", image);
                response.EnsureSuccessStatusCode();
                image.Status = ImageStatus.Done;
            }
        }
    }
}
