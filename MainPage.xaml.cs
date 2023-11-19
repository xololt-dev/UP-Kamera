using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Media.Capture;
using Windows.Storage;
using Windows.Graphics.Imaging;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;
using Windows.Media.Core;
using Windows.Media.Effects;
using Windows.Media.MediaProperties;
using Windows.Storage.FileProperties;


//Szablon elementu Pusta strona jest udokumentowany na stronie https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x415

namespace UP
{
    /// <summary>
    /// Pusta strona, która może być używana samodzielnie lub do której można nawigować wewnątrz ramki.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        MediaCapture mediaCapture;
        LowLagMediaRecording _mediaRecording;
        bool isPreviewing;

        public MainPage()
        {
            this.InitializeComponent();
            initMediaCapture();
        }

        private async void initMediaCapture()
        {
            mediaCapture = new MediaCapture();
            MediaCaptureInitializationSettings settings = new MediaCaptureInitializationSettings();
            await mediaCapture.InitializeAsync(new MediaCaptureInitializationSettings
            {
                StreamingCaptureMode = StreamingCaptureMode.Video
            });
            // mediaCapture.Failed += MediaCapture_Failed;
        }

        private void Button1_Click(object sender, RoutedEventArgs e)
        {
            // takePhoto();
            mediaPhoto();
        }

        private void Button2_Click(object sender, RoutedEventArgs e)
        {
            takeVideo();
        }

        private void Button3_Click(object sender, RoutedEventArgs e)
        {
            mediaVideoStop();
        }

        // https://learn.microsoft.com/en-us/windows/uwp/audio-video-camera/basic-photo-video-and-audio-capture-with-mediacapture
        public async void takePhoto()
        {
            CameraCaptureUI captureUI = new CameraCaptureUI();
            captureUI.PhotoSettings.Format = CameraCaptureUIPhotoFormat.Jpeg;
            captureUI.PhotoSettings.CroppedSizeInPixels = new Size(200, 200);

            StorageFile photo = await captureUI.CaptureFileAsync(CameraCaptureUIMode.Photo);

            if (photo == null)
            {
                // User cancelled photo capture
                return;
            }

            StorageFolder destinationFolder =
                await ApplicationData.Current.LocalFolder.CreateFolderAsync("ProfilePhotoFolder",
                    CreationCollisionOption.OpenIfExists);

            await photo.CopyAsync(destinationFolder, "ProfilePhoto.jpg", NameCollisionOption.ReplaceExisting);
            await photo.DeleteAsync();

            IRandomAccessStream stream = await photo.OpenAsync(FileAccessMode.Read);
            BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);
            SoftwareBitmap softwareBitmap = await decoder.GetSoftwareBitmapAsync();

            SoftwareBitmap softwareBitmapBGR8 = SoftwareBitmap.Convert(softwareBitmap,
                    BitmapPixelFormat.Bgra8,
                    BitmapAlphaMode.Premultiplied);

            SoftwareBitmapSource bitmapSource = new SoftwareBitmapSource();
            await bitmapSource.SetBitmapAsync(softwareBitmapBGR8);

            imageControl.Source = bitmapSource;
        }

        public async void takeVideo()
        {
            CameraCaptureUI captureUI = new CameraCaptureUI();
            captureUI.VideoSettings.Format = CameraCaptureUIVideoFormat.Mp4;

            StorageFile videoFile = await captureUI.CaptureFileAsync(CameraCaptureUIMode.Video);

            if (videoFile == null)
            {
                // User cancelled photo capture
                return;
            }

            mediaPlayerElement.Source = MediaSource.CreateFromStorageFile(videoFile);
            mediaPlayerElement.MediaPlayer.Play();

            StorageFolder destinationFolder =
                await ApplicationData.Current.LocalFolder.CreateFolderAsync("ProfilePhotoFolder",
                    CreationCollisionOption.OpenIfExists);
            await videoFile.CopyAsync(destinationFolder, "wideo.mp4");
        }

        public async void mediaPhoto()
        {
            // Prepare and capture photo
            var lowLagCapture = await mediaCapture.PrepareLowLagPhotoCaptureAsync(ImageEncodingProperties.CreateUncompressed(MediaPixelFormat.Bgra8));

            var capturedPhoto = await lowLagCapture.CaptureAsync();
            var softwareBitmap = capturedPhoto.Frame.SoftwareBitmap;

            await lowLagCapture.FinishAsync();

            var myPictures = await Windows.Storage.StorageLibrary.GetLibraryAsync(Windows.Storage.KnownLibraryId.Pictures);
            StorageFile file = await myPictures.SaveFolder.CreateFileAsync("photo.jpg", CreationCollisionOption.GenerateUniqueName);

            using (var captureStream = new InMemoryRandomAccessStream())
            {
                await mediaCapture.CapturePhotoToStreamAsync(ImageEncodingProperties.CreateJpeg(), captureStream);

                using (var fileStream = await file.OpenAsync(FileAccessMode.ReadWrite))
                {
                    var decoder = await BitmapDecoder.CreateAsync(captureStream);
                    var encoder = await BitmapEncoder.CreateForTranscodingAsync(fileStream, decoder);

                    var properties = new BitmapPropertySet {
                        { "System.Photo.Orientation", new BitmapTypedValue(PhotoOrientation.Normal, PropertyType.UInt16) }
                    };
                    await encoder.BitmapProperties.SetPropertiesAsync(properties);

                    await encoder.FlushAsync();
                }
            }
            SoftwareBitmapSource bitmapSource = new SoftwareBitmapSource();
            SoftwareBitmap softwareBitmapBGR8 = SoftwareBitmap.Convert(softwareBitmap,
                    BitmapPixelFormat.Bgra8,
                    BitmapAlphaMode.Premultiplied);

            await bitmapSource.SetBitmapAsync(softwareBitmapBGR8);

            imageControl.Source = bitmapSource;
        }

        public async void mediaVideoStart()
        {
            var myVideos = await Windows.Storage.StorageLibrary.GetLibraryAsync(Windows.Storage.KnownLibraryId.Videos);
            StorageFile file = await myVideos.SaveFolder.CreateFileAsync("video.mp4", CreationCollisionOption.GenerateUniqueName);
            _mediaRecording = await mediaCapture.PrepareLowLagRecordToStorageFileAsync(
                    MediaEncodingProfile.CreateMp4(VideoEncodingQuality.Auto), file);
            await _mediaRecording.StartAsync();
            mediaCapture.RecordLimitationExceeded += MediaCapture_RecordLimitationExceeded;
        }

        private void MediaCapture_RecordLimitationExceeded(MediaCapture sender)
        {
            mediaVideoStop();
            System.Diagnostics.Debug.WriteLine("Record limitation exceeded.");
        }

        public async void mediaVideoStop()
        {
            await _mediaRecording.StopAsync();
            await _mediaRecording.FinishAsync();
        }
    }
}
