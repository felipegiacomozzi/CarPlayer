using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Search;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace CarPlayer
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        #region ctor
        public ObservableCollection<Music> musicList { get; private set; }
        StorageFolder currentFolder;
        private QueryOptions folderQueryOptions;
        private Music nextMusic;
        private Music currentMusic;
        bool random = false;
        Random rand = new Random();

        public MainPage()
        {
            this.InitializeComponent();
            musicList = new ObservableCollection<Music>();

            ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            if(localSettings.Values["musics"] != null)
                musicList = (ObservableCollection<Music>)localSettings.Values["musics"];



            lstMusic.ItemsSource = musicList;
            media.AutoPlay = true;
            DataContext = this;

            folderQueryOptions = new QueryOptions(CommonFileQuery.OrderByName, new string[] { ".mp3" });
            folderQueryOptions.FolderDepth = FolderDepth.Shallow;
        }
        #endregion
        
        #region Events
        public async void btnMusic_Click(object sender, RoutedEventArgs e)
        {

            int hash = (int)((TextBlock)sender).Tag;
            currentMusic = musicList.Where(x => x.Id == hash).FirstOrDefault();
            nextMusic = GetNextMusic(hash);
            if (currentMusic != null)
            {
                media.SetSource(await currentMusic.File.OpenAsync(Windows.Storage.FileAccessMode.Read), ".mp3");
                media.TransportControls.Focus(FocusState.Programmatic);
            }
        }

        public async void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            await Picker_Populate();
            grdPicker.Visibility = Visibility.Visible;
        }

        public void btnClear_Click(object sender, RoutedEventArgs e)
        {
            ClearList();
        }

        private async void lstFiles_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (lstFiles.SelectedItem != null || currentFolder != null)
            {
                if (await Picker_BrowseTo(lstFiles.SelectedItem.ToString()))
                {
                    SelectFile();
                }
                else
                {
                    lstFiles.Focus(FocusState.Keyboard);
                }
            }
        }
        
        private async void btnSelect_Click(object sender, RoutedEventArgs e)
        {
            if (lstFiles.SelectedItem != null)
            {
                if (await Picker_BrowseTo(lstFiles.SelectedItem.ToString()))
                {
                    SelectFile();
                }
                else
                {
                    lstFiles.Focus(FocusState.Keyboard);
                }
            }
        }
        
        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Picker_Hide();
        }
        
        private async void lstFiles_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (lstFiles.SelectedItem != null && e.Key == Windows.System.VirtualKey.Enter)
            {
                if (await Picker_BrowseTo(lstFiles.SelectedItem.ToString()))
                {
                    SelectFile();
                }
                else
                {
                    lstFiles.Focus(FocusState.Keyboard);
                }
            }
        }
        #endregion

        #region File Methods
        private void ClearList()
        {
            media.Stop();
            nextMusic = null;
            currentMusic = null;
            musicList.Clear();
            ChangeStateLabel();
        }

        private async Task Picker_Populate()
        {
            if (currentFolder == null)
            {
                lstFiles.Items.Clear();
                lstFiles.Items.Add(">Documents");
                lstFiles.Items.Add(">Music");
                lstFiles.Items.Add(">RemovableStorage");
            }
            else
            {
                lstFiles.Items.Clear();
                lstFiles.Items.Add(">..");
                lstFiles.Items.Add("--Select this folder--");

                Stopwatch sw = new Stopwatch();
                sw.Start();

                Debug.WriteLine($"Scanning folders");

                var folders = await currentFolder.GetFoldersAsync();

                Debug.WriteLine($"Got {folders.Count} after {sw.ElapsedMilliseconds}ms");

                foreach (var f in folders)
                {
                    lstFiles.Items.Add(">" + f.Name);
                }

                Debug.WriteLine($"Getting folder files after {sw.ElapsedMilliseconds}ms");

                var query = currentFolder.CreateFileQueryWithOptions(folderQueryOptions);

                Debug.WriteLine($"Got the folder files and going to get files after {sw.ElapsedMilliseconds}ms");

                var files = await query.GetFilesAsync();

                Debug.WriteLine($"Got {files.Count} after {sw.ElapsedMilliseconds}ms");
                sw.Stop();
                foreach (var f in files)
                {
                    lstFiles.Items.Add(f.Name);
                }
            }
        }
        
        private async Task<bool> Picker_BrowseTo(string filename)
        {
            if (currentFolder == null)
            {
                switch (filename)
                {
                    case ">Documents":
                        currentFolder = KnownFolders.DocumentsLibrary;
                        break;
                    case ">Music":
                        currentFolder = KnownFolders.MusicLibrary;
                        break;
                    case ">RemovableStorage":
                        currentFolder = KnownFolders.RemovableDevices;
                        break;
                    default:
                        throw new Exception("unexpected");
                }
                lblBreadcrumb.Text = "> " + filename.Substring(1);
            }
            else
            {
                if (filename == ">..")
                {
                    await Picker_FolderUp();
                }
                else if (filename == "--Select this folder--")
                {
                    SelectFile();
                }
                else if (filename[0] == '>')
                {
                    var foldername = filename.Substring(1);
                    var folder = await currentFolder.GetFolderAsync(foldername);
                    currentFolder = folder;
                    lblBreadcrumb.Text += " > " + foldername;
                }
                else
                {
                    //Picker_SelectedFile = await currentFolder.GetFileAsync(filename);
                    return true;
                }
            }
            await Picker_Populate();
            return false;
        }
        
        async Task Picker_FolderUp()
        {
            if (currentFolder == null)
            {
                return;
            }
            try
            {
                var folder = await currentFolder.GetParentAsync();
                currentFolder = folder;
                if (currentFolder == null)
                {
                    lblBreadcrumb.Text = ">";
                }
                else
                {
                    var breadcrumb = lblBreadcrumb.Text;
                    breadcrumb = breadcrumb.Substring(0, breadcrumb.LastIndexOf('>') - 1);
                    lblBreadcrumb.Text = breadcrumb;
                }
            }
            catch (Exception)
            {
                currentFolder = null;
                lblBreadcrumb.Text = ">";
            }
        }
        
        private void Picker_Hide()
        {
            grdPicker.Visibility = Visibility.Collapsed;
        }
        
        async void SelectFile()
        {
            Picker_Hide();
            try
            {
                musicList.Clear();
                uint offSet = 0;
                const uint limit = 100;
                bool keepDiving = true;

                while (keepDiving)
                {
                    Debug.WriteLine($"Getting files on {currentFolder.Name} offset {offSet} and limit {limit}");

                    Stopwatch sw = new Stopwatch();
                    sw.Start();

                    var files = await currentFolder.GetFilesAsync(CommonFileQuery.OrderByName, offSet, limit);

                    Debug.WriteLine($"Got {files.Count} after {sw.ElapsedMilliseconds}ms.");

                    files = files.Where(x => x.FileType.ToLower() == ".mp3").ToList();

                    Debug.WriteLine($"Filtered to {files.Count} after {sw.ElapsedMilliseconds}ms.");


                    if (files.Count == 0)
                    {
                        keepDiving = false;
                        continue;
                    }

                    foreach (var music in files)
                    {
                        string musicName = music.Name.Split(new string[] { ".mp3" }, StringSplitOptions.None)[0];
                        if (!string.IsNullOrWhiteSpace(musicName))
                            musicList.Add(new Music() { Name = musicName, Id = music.GetHashCode(), File = music });
                    }

                    Debug.WriteLine($"Added to musiclist after {sw.ElapsedMilliseconds}ms.");

                    sw.Stop();

                    if (offSet == 0)
                    {

                        if (random)
                        {
                            currentMusic = musicList.ElementAt(rand.Next(musicList.Count()));
                            nextMusic = musicList.ElementAt(rand.Next(musicList.Count()));
                        }
                        else
                        {
                            currentMusic = musicList.FirstOrDefault();
                            nextMusic = GetNextMusic(currentMusic.Id);
                        }


                        media.SetSource(await currentMusic.File.OpenAsync(Windows.Storage.FileAccessMode.Read), ".mp3");
                        media.TransportControls.Focus(FocusState.Programmatic);
                        lstMusic.SelectedIndex = 0;
                    }

                    offSet += limit;
                }

                ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                localSettings.Values["musics"] = musicList;
            }
            catch (Exception ex)
            {

            }
        }
        #endregion

        #region Player Methods
        private Music GetNextMusic(int hash)
        {
            return musicList.SkipWhile(x => x.Id != hash).Skip(1).FirstOrDefault();
        }

        private void StopMedia(object sender, RoutedEventArgs e)
        {
            media.Stop();
        }

        private void PauseMedia(object sender, RoutedEventArgs e)
        {
            media.Pause();
        }

        private void PlayMedia(object sender, RoutedEventArgs e)
        {
            media.Play();
        }

        private void PlayNext(object sender, RoutedEventArgs e)
        {
            if (nextMusic != null)
            {
                PlayNextMusic();
            }
        }

        private async void PlayNextMusic()
        {
            currentMusic = nextMusic;
            if (random)
                nextMusic = musicList.ElementAt(rand.Next(musicList.Count()));
            else
                nextMusic = GetNextMusic(currentMusic.Id);

            if (currentMusic != null)
            {
                media.SetSource(await currentMusic.File.OpenAsync(Windows.Storage.FileAccessMode.Read), ".mp3");
                media.TransportControls.Focus(FocusState.Programmatic);
            }
        }

        private void SetRandom(object sender, RoutedEventArgs e)
        {
            random = !random;
            if (random)
            {
                nextMusic = musicList.ElementAt(rand.Next(musicList.Count()));
                randomBtn.Background = new SolidColorBrush(Colors.CornflowerBlue);
            }
            else
            {
                nextMusic = GetNextMusic(currentMusic.Id);
                randomBtn.Background = new SolidColorBrush(Colors.LightGray);
            }
            ChangeStateLabel();
        }

        private void Media_State_Changed(object sender, RoutedEventArgs e)
        {
            ChangeStateLabel();
        }

        private void Media_State_Ended(object sender, RoutedEventArgs e)
        {
            if (nextMusic != null)
            {
                PlayNextMusic();
            }
        }

        private void ChangeStateLabel()
        {
            string currentMusicName = currentMusic != null ? currentMusic.Name : "";
            string nextMusicName = nextMusic != null ? nextMusic.Name : "";

            mediaStateTextBlock.Text = $"{media.CurrentState.ToString()} {currentMusicName} - NextUp - {nextMusicName}";
            if (currentMusic != null)
            {
                lstMusic.SelectedItem = currentMusic;
                lstMusic.ScrollIntoView(currentMusic);
            }
        }
        #endregion
    }

    public class Music
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public StorageFile File { get; set; }
    }
}
