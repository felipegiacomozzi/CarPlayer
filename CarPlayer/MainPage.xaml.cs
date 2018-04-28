using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Search;
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
        public ObservableCollection<Music> musicList { get; private set; }


        public MainPage()
        {
            this.InitializeComponent();
            musicList = new ObservableCollection<Music>();
            lstMusic.ItemsSource = musicList;
            mediaElement.AutoPlay = true;
            DataContext = this;
        }
        
        public async void btnMusic_Click(object sender, RoutedEventArgs e)
        {
            int hash = (int)((TextBlock)sender).Tag;
            var music = musicList.Where(x => x.Id == hash).FirstOrDefault();
            if (music != null)
            {
                mediaElement.SetSource(await music.File.OpenAsync(Windows.Storage.FileAccessMode.Read), ".mp3");
                mediaElement.TransportControls.Focus(FocusState.Programmatic);
            }
        }

        public async void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            
            var picker = new Windows.Storage.Pickers.FolderPicker();
            picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary;
            picker.FileTypeFilter.Add(".mp3");
            var file = await picker.PickSingleFolderAsync();

            if (file != null)
            {
                musicList.Clear();
                uint offSet = 0;
                const uint limit = 100;
                bool keepDiving = true;

                while(keepDiving)
                { 
                    var files = await file.GetFilesAsync(CommonFileQuery.OrderByName, offSet, limit);

                    files = files.Where(x => x.FileType.ToLower() == ".mp3").ToList();

                    if(files.Count == 0)
                    {
                        keepDiving = false;
                        continue;
                    }
                    
                    foreach (var music in files)
                    {
                        string musicName = music.Name.Split(".mp3".ToCharArray())[0];
                        if(!string.IsNullOrWhiteSpace(musicName))
                            musicList.Add(new Music() { Name = musicName, Id = music.GetHashCode(), File = music });
                    }

                    if (offSet == 0)
                    {
                        mediaElement.SetSource(await musicList.First().File.OpenAsync(Windows.Storage.FileAccessMode.Read), ".mp3");
                        mediaElement.TransportControls.Focus(FocusState.Programmatic);
                        lstMusic.SelectedIndex = 0;
                    }

                    offSet += limit;
                }
            }
            else
            {

            }
        }        
    }

    public class Music
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public StorageFile File { get; set; }
    }
}
