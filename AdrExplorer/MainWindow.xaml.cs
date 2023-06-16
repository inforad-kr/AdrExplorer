using AdrExplorer.Services;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
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
        HttpClient m_HttpClient;

        public MainWindow()
        {
            InitializeComponent();

            LoginPanel.DataContext = m_Settings;
            FilterPanel.DataContext = m_Settings;
            PasswordBox.Password = new(' ', 5);
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            m_HttpClient?.Dispose();
            m_HttpClient = new()
            {
                BaseAddress = new(m_Settings.ServerUrl)
            };
            m_HttpClient.BaseAddress = new(m_HttpClient.BaseAddress, "api/v1/");

            try
            {
                var token = await Login();
                if (token.ExpirationDateTime == null || token.ExpirationDateTime >= DateTime.Now)
                {
                    m_HttpClient.DefaultRequestHeaders.Authorization = new("Bearer", token.Token);
                    await ActivateStudyTab();
                }
                else
                {
                    DeactivateStudyTab();
                    MessageBox.Show("Expired access token", Title, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                DeactivateStudyTab();
                MessageBox.Show(ex.Message, Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task<ApiToken> Login()
        {
            var response = await m_HttpClient.PostAsJsonAsync("login", new UserCredentials { Name = m_Settings.UserName, Password = m_Settings.Password });
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<ApiToken>();
        }

        private async Task ActivateStudyTab()
        {
            StudyTab.IsEnabled = true;
            TabControl.SelectedItem = StudyTab;

            await PopulateStudyGrid();
        }

        private void DeactivateStudyTab()
        {
            StudyTab.IsEnabled = false;
            TabControl.SelectedItem = LoginTab;

            StudyGrid.ItemsSource = null;
            ImageGrid.ItemsSource = null;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            m_Settings.Save();
            m_HttpClient?.Dispose();
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (IsLoaded)
            {
                m_Settings.Password = PasswordBox.Password;
            }
        }

        private void Grid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.PropertyName == nameof(Study.Id) || e.PropertyName == nameof(Image.SeriesInstanceUid))
            {
                e.Cancel = true;
                return;
            }

            if (m_Settings.CustomStrings?.TryGetValue(e.PropertyName, out var s) == true)
            {
                e.Column.Header = s;
            }
            e.Column.CanUserReorder = false;
        }

        private async void DatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
            {
                await PopulateStudyGrid();
            }
        }

        private async void PendingOnlyCheckBox_Click(object sender, RoutedEventArgs e)
        {
            await PopulateStudyGrid();
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
                var requestUri = $"study?StartStudyDateTime={m_Settings.StartDate:yyyy-MM-dd}&EndStudyDateTime={m_Settings.EndDate:yyyy-MM-dd}&count={m_Settings.StudyCount}";
                var studies = await m_HttpClient.GetFromJsonAsync<Study[]>(requestUri);
                if (m_Settings.PendingOnly)
                {
                    var doneStudies = await m_HttpClient.GetFromJsonAsync<Study[]>(requestUri + "&protocolName=*0*");
                    var doneStudies2 = await m_HttpClient.GetFromJsonAsync<Study[]>(requestUri + "&protocolName=*1*");
                    studies = studies.ExceptBy(doneStudies.Union(doneStudies2).Select(study => study.Id), study => study.Id).ToArray();
                }
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
                ProgressBar.Value = 0;
                ProgressBar.Maximum = studies.Count;
                StudyGrid.SelectedItems.Clear();
                foreach (var study in studies)
                {
                    await ProcessStudy(study);
                    ++ProgressBar.Value;
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

            await PopulateStudyGrid();
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

        private async Task ProcessImage(Image image)
        {
            var file = (await m_HttpClient.GetFromJsonAsync<ImageFile[]>($"image/{image.Id}/file")).FirstOrDefault();
            if (file != null)
            {
                image.Status = ImageStatus.Downloading;
                var data = await m_HttpClient.GetByteArrayAsync($"file/{file.Id}/data?jpeg=true");

                image.Status = ImageStatus.Processing;
                var adrProcessor = CreateAdrProcessor();
                image.SetAdrResult(await adrProcessor.ProcessFile(data));

                var response = await m_HttpClient.PutAsJsonAsync($"image/{image.Id}", image);
                response.EnsureSuccessStatusCode();
                image.Status = ImageStatus.Done;
            }
        }

        private IAdrProcessor CreateAdrProcessor()
        {
            var assembly = Assembly.LoadFrom(m_Settings.AdrProcessorModule);
            var type = assembly.GetTypes().First(type => type.IsAssignableTo(typeof(IAdrProcessor)));
            return (IAdrProcessor)Activator.CreateInstance(type);
        }
    }
}
