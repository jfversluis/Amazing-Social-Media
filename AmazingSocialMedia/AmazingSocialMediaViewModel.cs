using System.Threading.Tasks;
using System.Windows.Input;
using AmazingSocialMedia.Services;
using Plugin.Media.Abstractions;
using Xamarin.Forms;

namespace AmazingSocialMedia
{
    public class AmazingSocialMediaViewModel : ViewModelBase
    {
        public AmazingSocialMediaViewModel()
        {
            TakePhotoCommand = new Command(async () => await TakePhoto());   
            PostCommand = new Command(async () => await Post());    
        }

        MediaFile _photo;
        StreamImageSource _photoSource;
        public StreamImageSource PhotoSource
        {
            get => _photoSource;
            set
            {
                if (Set(ref _photoSource, value))
                {
                    RaisePropertyChanged(nameof(ShowPhoto));
                    RaisePropertyChanged(nameof(ShowImagePlaceholder));
                }
            }
        }

        string _comment;
        public string Comment
        {
            get => _comment;
            set => Set(ref _comment, value);
        }

        private bool _isPosting;
        public bool IsPosting
        {
            get => _isPosting;
            set => Set(ref _isPosting, value);
        }

        public ICommand TakePhotoCommand { get; } 
        public ICommand PostCommand { get; } 

        private async Task TakePhoto()
        {
            _photo = await Plugin.Media.CrossMedia.Current.TakePhotoAsync(new StoreCameraMediaOptions
            {
                RotateImage = false,
                PhotoSize = PhotoSize.Medium,
                DefaultCamera = CameraDevice.Front
            });
            PhotoSource = (StreamImageSource)ImageSource.FromStream(() => _photo.GetStreamWithImageRotatedForExternalStorage());
        }

        private readonly ContentModeratorService _contentModerator = new ContentModeratorService();

        private async Task Post()
        {
            try
            {
                IsPosting = true;

                if (await _contentModerator.ContainsProfanity(Comment))
                {
                    await Application.Current.MainPage.DisplayAlert("Rude!", "Your comment contains naughty language!", "OK");
                    Comment = "";
                    return;
                }

                if (await _contentModerator.IsFace(_photo) &&
                    await _contentModerator.IsDuckFace(_photo))
                {
                    await Application.Current.MainPage.DisplayAlert("D'oh!", "DuckFace is not allowed!", "OK");
                    PhotoSource = null;
                    _photo = null;
                    return;
                }

                await Application.Current.MainPage.DisplayAlert("Posted", "Your image has been posted!", "OK");
                Comment = "";
                PhotoSource = null;
                _photo = null;
            }
            finally
            {
                IsPosting = false;
            }
        }

        public bool ShowImagePlaceholder => !ShowPhoto;
        public bool ShowPhoto => _photoSource != null;

    }
}
