using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Media.Capture;
using Windows.ApplicationModel;
using Windows.System.Display;
using Windows.Storage;
using Windows.Graphics.Imaging;
using Windows.Graphics.Display;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;
using Windows.Media.Core;
using Windows.Media.Effects;
using Windows.Media.MediaProperties;
using Windows.Storage.FileProperties;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Media.Devices;
using Windows.Media.Capture.Frames;
using Windows.Media;
using System.Linq.Expressions;
using System.ServiceModel.Channels;

//Szablon elementu Pusta strona jest udokumentowany na stronie https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x415

namespace UP
{
    /// <summary>
    /// Pusta strona, która może być używana samodzielnie lub do której można nawigować wewnątrz ramki.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        MediaCapture mediaCapture;
        DisplayRequest displayRequest = new DisplayRequest();
        LowLagMediaRecording _mediaRecording;
        bool isPreviewing = true;
        string chosenCameraID = string.Empty;        

        public MainPage()
        {
            this.InitializeComponent();

            initMediaCapture();
            //ExposureSlider.Visibility = Visibility.Visible;
            // Application.Current.Suspending += Application_Suspending;
        }

        private async void initMediaCapture()
        {
            mediaCapture = new MediaCapture();
            string videoDeviceId = await GetVideoProfileSupportedDeviceIdAsync(Windows.Devices.Enumeration.Panel.Back);

            if (string.IsNullOrEmpty(videoDeviceId))
            {
                // No devices on the specified panel support video profiles. .
                System.Diagnostics.Debug.WriteLine("isnullorempty");
            }

            MediaCaptureInitializationSettings settings = new MediaCaptureInitializationSettings();
            settings.VideoDeviceId = videoDeviceId;
            settings.StreamingCaptureMode = StreamingCaptureMode.Video;
            await mediaCapture.InitializeAsync(settings);

            PopulateStreamPropertiesUI(MediaStreamType.VideoRecord, comboBoxFPS);
                /*new MediaCaptureInitializationSettings
            {
                StreamingCaptureMode = StreamingCaptureMode.Video
            });*/
            // mediaCapture.Failed += MediaCapture_Failed;
        }

        public async Task<string> GetVideoProfileSupportedDeviceIdAsync(Windows.Devices.Enumeration.Panel panel)
        {
            string deviceId = string.Empty;

            // Finds all video capture devices
            DeviceInformationCollection devices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);

            List<string> items = new List<string>();

            foreach (var device in devices)
            {
                items.Add(device.Name);
                // deviceId = device.Id;
                // Check if the device on the requested panel supports Video Profile
                
                if (MediaCapture.IsVideoProfileSupported(device.Id) )//&& device.EnclosureLocation.Panel == panel)
                {
                    // We've located a device that supports Video Profiles on expected panel
                    deviceId = device.Id;    
                    // break;
                }
            }

            cameraList.ItemsSource = items;
            
            return deviceId;
        }

        private void Button1_Click(object sender, RoutedEventArgs e)
        {
            // takePhoto();
            mediaPhoto();
        }

        private void Button2_Click(object sender, RoutedEventArgs e)
        {
            mediaVideoStart();// takeVideo();
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

            //mediaPlayerElement.Source = MediaSource.CreateFromStorageFile(videoFile);
            //mediaPlayerElement.MediaPlayer.Play();

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

        private void CheckIfStreamsAreIdentical()
        {
            if (mediaCapture.MediaCaptureSettings.VideoDeviceCharacteristic == VideoDeviceCharacteristic.AllStreamsIdentical ||
                mediaCapture.MediaCaptureSettings.VideoDeviceCharacteristic == VideoDeviceCharacteristic.PreviewRecordStreamsIdentical)
            {
                System.Diagnostics.Debug.WriteLine("Preview and video streams for this device are identical. Changing one will affect the other");
            }
        }

        private void PopulateStreamPropertiesUI(MediaStreamType streamType, ComboBox comboBox, bool showFrameRate = true)
        {
            // Query all properties of the specified stream type 
            IEnumerable<StreamPropertiesHelper> allStreamProperties =
                mediaCapture.VideoDeviceController.GetAvailableMediaStreamProperties(streamType).Select(x => new StreamPropertiesHelper(x));

            // Order them by resolution then frame rate
            allStreamProperties = allStreamProperties.OrderByDescending(x => x.Height * x.Width).ThenByDescending(x => x.FrameRate);
            
            // Populate the combo box with the entries
            foreach (var property in allStreamProperties)
            {
                ComboBoxItem comboBoxItem = new ComboBoxItem();
                comboBoxItem.Content = property.GetFriendlyName(showFrameRate);
                comboBoxItem.Tag = property;
                comboBox.Items.Add(comboBoxItem);
            }
        }

        private async void VideoSettings_Changed(object sender, RoutedEventArgs e)
        {
            if (isPreviewing)
            {
                var selectedItem = (sender as ComboBox).SelectedItem as ComboBoxItem;
                var encodingProperties = (selectedItem.Tag as StreamPropertiesHelper).EncodingProperties;
                await mediaCapture.VideoDeviceController.SetMediaStreamPropertiesAsync(MediaStreamType.VideoRecord, encodingProperties);
            }
        }

        private async void cameraList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // CleanupCameraAsync();
            if (isPreviewing)
            {
                StopPreviewAsync();
                mediaCapture.Dispose();
                mediaCapture = null;
            }

            string name = (sender as ListBox).SelectedItem.ToString();
            
            chosenCameraID = await getDeviceIDFromName(name);

            // mediaCapture.Dispose();

            mediaCaptureInit();

            //uiExposure();
        }

        private async Task<string> getDeviceIDFromName(string name)
        {
            string ID = string.Empty;
            
            // Finds all video capture devices
            DeviceInformationCollection devices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);

            foreach (var device in devices)
            {
                if (device.Name == name)
                {
                    ID = device.Id;
                    break;
                }
            }

            return ID;
        }

        private async void mediaCaptureInit()
        {
            mediaCapture = new MediaCapture();

            MediaCaptureInitializationSettings settings = new MediaCaptureInitializationSettings();
            settings.VideoDeviceId = chosenCameraID;
            settings.StreamingCaptureMode = StreamingCaptureMode.Video;

            await mediaCapture.InitializeAsync(settings);

            await StartPreviewAsync();

            comboBoxFPS.Items.Clear();
            PopulateStreamPropertiesUI(MediaStreamType.VideoRecord, comboBoxFPS);

            var focusControl = mediaCapture.VideoDeviceController.FocusControl;

            uiWhiteBalance();
            uiContrast();
            uiExposure();
            uiFocus();
        }       

        private void uiWhiteBalance()
        {
            var whiteBalanceControl = mediaCapture.VideoDeviceController.WhiteBalanceControl;

            if (whiteBalanceControl.Supported)
            {
                WbSlider.Visibility = Visibility.Visible;
                
                if (whiteBalanceControl.Max - whiteBalanceControl.Min > whiteBalanceControl.Step)
                {

                    WbSlider.Minimum = whiteBalanceControl.Min;
                    WbSlider.Maximum = whiteBalanceControl.Max;
                    WbSlider.StepFrequency = whiteBalanceControl.Step;

                    WbSlider.ValueChanged -= WbSlider_ValueChanged;
                    WbSlider.Value = whiteBalanceControl.Value;
                    WbSlider.ValueChanged += WbSlider_ValueChanged;
                }
                else
                {
                    WbSlider.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                if (mediaCapture.VideoDeviceController.WhiteBalance.Capabilities.Supported)
                {
                    if (mediaCapture.VideoDeviceController.WhiteBalance.Capabilities.AutoModeSupported)
                        WbAutoCheckBox.Visibility = Visibility.Visible;
                    else
                        WbAutoCheckBox.Visibility = Visibility.Collapsed;

                    WbSlider.Visibility = Visibility.Visible;

                    double currentWB = 0;
                    if (mediaCapture.VideoDeviceController.WhiteBalance.Capabilities.Max - mediaCapture.VideoDeviceController.WhiteBalance.Capabilities.Min > mediaCapture.VideoDeviceController.WhiteBalance.Capabilities.Step)
                    {

                        WbSlider.Minimum = mediaCapture.VideoDeviceController.WhiteBalance.Capabilities.Min;
                        WbSlider.Maximum = mediaCapture.VideoDeviceController.WhiteBalance.Capabilities.Max;
                        WbSlider.StepFrequency = mediaCapture.VideoDeviceController.WhiteBalance.Capabilities.Step;

                        WbSlider.ValueChanged -= WbSlider_ValueChanged;
                        mediaCapture.VideoDeviceController.WhiteBalance.TryGetValue(out currentWB);
                        WbSlider.Value = currentWB;
                        WbSlider.ValueChanged += WbSlider_ValueChanged;
                    }

                    mediaCapture.VideoDeviceController.WhiteBalance.TrySetAuto(true);
                    WbAutoCheckBox.IsChecked = true;
                }
                else
                {
                    WbSlider.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void uiContrast()
        {
            if (mediaCapture.VideoDeviceController.Contrast.Capabilities.Supported)
            {
                ContrastSlider.Visibility = Visibility.Visible;

                double currentWB = 0;
                if (mediaCapture.VideoDeviceController.Contrast.Capabilities.Max - mediaCapture.VideoDeviceController.Contrast.Capabilities.Min > mediaCapture.VideoDeviceController.Contrast.Capabilities.Step)
                {
                    ContrastSlider.Minimum = mediaCapture.VideoDeviceController.Contrast.Capabilities.Min;
                    ContrastSlider.Maximum = mediaCapture.VideoDeviceController.Contrast.Capabilities.Max;
                    ContrastSlider.StepFrequency = mediaCapture.VideoDeviceController.Contrast.Capabilities.Step;

                    ContrastSlider.ValueChanged -= ContrastSlider_ValueChanged;
                    mediaCapture.VideoDeviceController.Contrast.TryGetValue(out currentWB);
                    ContrastSlider.Value = currentWB;
                    ContrastSlider.ValueChanged += ContrastSlider_ValueChanged;
                }

                if (mediaCapture.VideoDeviceController.Contrast.Capabilities.AutoModeSupported)
                    ContrastAutoCheckBox.Visibility = Visibility.Visible;
                else
                    ContrastAutoCheckBox.Visibility = Visibility.Collapsed;

                ContrastAutoCheckBox.IsChecked = mediaCapture.VideoDeviceController.Contrast.TrySetAuto(true);
            }
            else
            {
                ContrastSlider.Visibility = Visibility.Collapsed;
            }            
        }
        private void uiExposure()
        {
            if (mediaCapture.VideoDeviceController.Exposure.Capabilities.Supported)
            {
                ExposureSlider.Visibility = Visibility.Visible;
                double currentWB = 0;

                if (mediaCapture.VideoDeviceController.Exposure.Capabilities.Max - mediaCapture.VideoDeviceController.Exposure.Capabilities.Min > mediaCapture.VideoDeviceController.Exposure.Capabilities.Step)
                {
                    ExposureSlider.Minimum = mediaCapture.VideoDeviceController.Exposure.Capabilities.Min;
                    ExposureSlider.Maximum = mediaCapture.VideoDeviceController.Exposure.Capabilities.Max;
                    ExposureSlider.StepFrequency = mediaCapture.VideoDeviceController.Exposure.Capabilities.Step;

                    ExposureSlider.ValueChanged -= ExposureSlider_ValueChanged;
                    mediaCapture.VideoDeviceController.Exposure.TryGetValue(out currentWB);
                    ExposureSlider.Value = currentWB;

                    ExposureSlider.ValueChanged += ExposureSlider_ValueChanged;
                }
                
                if (mediaCapture.VideoDeviceController.Exposure.Capabilities.AutoModeSupported)
                    ExposureAutoCheckBox.Visibility = Visibility.Visible;
                else
                    ExposureAutoCheckBox.Visibility = Visibility.Collapsed;

                ExposureAutoCheckBox.IsChecked = mediaCapture.VideoDeviceController.Exposure.TrySetAuto(true);
            }
            else
            {
                ExposureSlider.Visibility = Visibility.Collapsed;
            }
        }

        private void uiFocus()
        {
            // var focusControl = mediaCapture.VideoDeviceController.Focus;
            if (mediaCapture.VideoDeviceController.Focus.Capabilities.Supported)
            {
                FocusSlider.Visibility = Visibility.Visible;
                double currentWB = 0;

                if (mediaCapture.VideoDeviceController.Focus.Capabilities.Max - mediaCapture.VideoDeviceController.Focus.Capabilities.Min > mediaCapture.VideoDeviceController.Focus.Capabilities.Step)
                {
                    FocusSlider.Minimum = mediaCapture.VideoDeviceController.Focus.Capabilities.Min;
                    FocusSlider.Maximum = mediaCapture.VideoDeviceController.Focus.Capabilities.Max;
                    FocusSlider.StepFrequency = mediaCapture.VideoDeviceController.Focus.Capabilities.Step;

                    FocusSlider.ValueChanged -= FocusSlider_ValueChanged;
                    mediaCapture.VideoDeviceController.Focus.TryGetValue(out currentWB);
                    FocusSlider.Value = currentWB;

                    FocusSlider.ValueChanged += FocusSlider_ValueChanged;
                }

                if (mediaCapture.VideoDeviceController.Exposure.Capabilities.AutoModeSupported)
                    ExposureAutoCheckBox.Visibility = Visibility.Visible;
                else
                    ExposureAutoCheckBox.Visibility = Visibility.Collapsed;

                FocusAutoCheckBox.IsChecked = mediaCapture.VideoDeviceController.Focus.TrySetAuto(true);
            }
            else
            {
                FocusSlider.Visibility = Visibility.Collapsed;
            }
        }

        private async void ExposureCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!isPreviewing)
            {
                // Auto exposure only supported while preview stream is running.
                return;
            }

            var autoExposure = ((sender as CheckBox).IsChecked == true);
            mediaCapture.VideoDeviceController.Exposure.TrySetAuto(autoExposure);
        }
        private void WbCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!isPreviewing)
            {
                // Auto exposure only supported while preview stream is running.
                return;
            }

            var autoWb = ((sender as CheckBox).IsChecked == true);
            mediaCapture.VideoDeviceController.WhiteBalance.TrySetAuto(autoWb);
        }

        private void ContrastCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!isPreviewing)
            {
                // Auto exposure only supported while preview stream is running.
                return;
            }

            var autoContrast = ((sender as CheckBox).IsChecked == true);
            mediaCapture.VideoDeviceController.Contrast.TrySetAuto(autoContrast);
        }

        private void FocusCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!isPreviewing)
            {
                // Auto exposure only supported while preview stream is running.
                return;
            }

            var autoContrast = ((sender as CheckBox).IsChecked == true);
            mediaCapture.VideoDeviceController.Focus.TrySetAuto(autoContrast);
        }

        private async void WbSlider_ValueChanged(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (!isPreviewing)
            {
                // Do not set white balance values unless the preview stream is running.
                return;
            }

            double value = (sender as Slider).Value;
            WbAutoCheckBox.IsChecked = false;
            if (mediaCapture.VideoDeviceController.WhiteBalance.TrySetValue(value));
                
        }

        private async void ContrastSlider_ValueChanged(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (!isPreviewing)
            {
                // Do not set white balance values unless the preview stream is running.
                return;
            }

            double value = (sender as Slider).Value;
            ContrastAutoCheckBox.IsChecked = false;
            if (mediaCapture.VideoDeviceController.Contrast.TrySetValue(value));

        }

        private async void ExposureSlider_ValueChanged(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (!isPreviewing)
            {
                // Do not set white balance values unless the preview stream is running.
                return;
            }

            double value = (sender as Slider).Value;
            ExposureAutoCheckBox.IsChecked = false;
            mediaCapture.VideoDeviceController.Exposure.TrySetValue(value);

        }

        private void FocusSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (!isPreviewing)
            {
                // Do not set white balance values unless the preview stream is running.
                return;
            }

            double value = (sender as Slider).Value;
            mediaCapture.VideoDeviceController.Focus.TrySetValue(value);
        }

        private async Task StartPreviewAsync()
        {
            /*
            try
            {

                mediaCapture = new MediaCapture();
                await mediaCapture.InitializeAsync();

                displayRequest.RequestActive();
                DisplayInformation.AutoRotationPreferences = DisplayOrientations.Landscape;
            }
            catch (UnauthorizedAccessException)
            {
                // This will be thrown if the user denied access to the camera in privacy settings
                System.Diagnostics.Debug.WriteLine("The app was denied access to the camera");
                return;
            }
            */
            displayRequest.RequestActive();
            DisplayInformation.AutoRotationPreferences = DisplayOrientations.Landscape;


            PreviewControl.Source = mediaCapture;
            await mediaCapture.StartPreviewAsync();
            isPreviewing = true;

            /* try
            {
                
            }
            catch (System.IO.FileLoadException)
            {
                mediaCapture.CaptureDeviceExclusiveControlStatusChanged += _mediaCapture_CaptureDeviceExclusiveControlStatusChanged;
            }
            */
        }

        private async void _mediaCapture_CaptureDeviceExclusiveControlStatusChanged(MediaCapture sender, MediaCaptureDeviceExclusiveControlStatusChangedEventArgs args)
        {
            if (args.Status == MediaCaptureDeviceExclusiveControlStatus.SharedReadOnlyAvailable)
            {
                System.Diagnostics.Debug.WriteLine("The camera preview can't be displayed because another app has exclusive access");
            }
            else if (args.Status == MediaCaptureDeviceExclusiveControlStatus.ExclusiveControlAvailable && !isPreviewing)
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                {
                    await StartPreviewAsync();
                });
            }
        }

        private async Task CleanupCameraAsync()
        {
            if (mediaCapture != null)
            {
                if (isPreviewing)
                {
                    await mediaCapture.StopPreviewAsync();
                }

                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    PreviewControl.Source = null;
                    if (displayRequest != null)
                    {
                        displayRequest.RequestRelease();
                    }

                    mediaCapture.Dispose();
                    mediaCapture = null;
                });
            }
        }

        private async Task StopPreviewAsync()
        {
            // Stop the preview
            isPreviewing = false;
            await mediaCapture.StopPreviewAsync();

            // Use the dispatcher because this method is sometimes called from non-UI threads
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                // Cleanup the UI
                PreviewControl.Source = null;

                // Allow the device screen to sleep now that the preview is stopped
                displayRequest.RequestRelease();
            });
        }

        protected async override void OnNavigatedFrom(NavigationEventArgs e)
        {
            await CleanupCameraAsync();
        }

        private async void Application_Suspending(object sender, SuspendingEventArgs e)
        {
            // Handle global application events only if this page is active
            if (Frame.CurrentSourcePageType == typeof(MainPage))
            {
                var deferral = e.SuspendingOperation.GetDeferral();
                await CleanupCameraAsync();
                deferral.Complete();
            }
        }

        private void MatchPreviewAspectRatio(MediaStreamType streamType, ComboBox comboBox)
        {
            // Query all properties of the specified stream type
            IEnumerable<StreamPropertiesHelper> allVideoProperties =
                mediaCapture.VideoDeviceController.GetAvailableMediaStreamProperties(streamType).Select(x => new StreamPropertiesHelper(x));

            // Query the current preview settings
            StreamPropertiesHelper previewProperties = new StreamPropertiesHelper(mediaCapture.VideoDeviceController.GetMediaStreamProperties(MediaStreamType.VideoPreview));

            // Get all formats that have the same-ish aspect ratio as the preview
            // Allow for some tolerance in the aspect ratio comparison
            const double ASPECT_RATIO_TOLERANCE = 0.015;
            var matchingFormats = allVideoProperties.Where(x => Math.Abs(x.AspectRatio - previewProperties.AspectRatio) < ASPECT_RATIO_TOLERANCE);

            // Order them by resolution then frame rate
            allVideoProperties = matchingFormats.OrderByDescending(x => x.Height * x.Width).ThenByDescending(x => x.FrameRate);

            // Clear out old entries and populate the video combo box with new matching entries
            comboBox.Items.Clear();
            foreach (var property in allVideoProperties)
            {
                ComboBoxItem comboBoxItem = new ComboBoxItem();
                comboBoxItem.Content = property.GetFriendlyName();
                comboBoxItem.Tag = property;
                comboBox.Items.Add(comboBoxItem);
            }
            comboBox.SelectedIndex = -1;
        }

        private void Button4_Click(object sender, RoutedEventArgs e)
        {
            takePhoto();
        }

        private void Button5_Click(object sender, RoutedEventArgs e)
        {
            takeVideo();
        }

        private void TextBlock_SelectionChanged(object sender, RoutedEventArgs e)
        {

        }

        private void TextBlock_SelectionChanged_1(object sender, RoutedEventArgs e)
        {

        }

        
    }

    class StreamPropertiesHelper
    {
        private IMediaEncodingProperties _properties;

        public StreamPropertiesHelper(IMediaEncodingProperties properties)
        {
            if (properties == null)
            {
                throw new ArgumentNullException(nameof(properties));
            }

            // This helper class only uses VideoEncodingProperties or VideoEncodingProperties
            if (!(properties is ImageEncodingProperties) && !(properties is VideoEncodingProperties))
            {
                throw new ArgumentException("Argument is of the wrong type. Required: " + typeof(ImageEncodingProperties).Name
                    + " or " + typeof(VideoEncodingProperties).Name + ".", nameof(properties));
            }

            // Store the actual instance of the IMediaEncodingProperties for setting them later
            _properties = properties;
        }

        public uint Width
        {
            get
            {
                if (_properties is ImageEncodingProperties)
                {
                    return (_properties as ImageEncodingProperties).Width;
                }
                else if (_properties is VideoEncodingProperties)
                {
                    return (_properties as VideoEncodingProperties).Width;
                }

                return 0;
            }
        }

        public uint Height
        {
            get
            {
                if (_properties is ImageEncodingProperties)
                {
                    return (_properties as ImageEncodingProperties).Height;
                }
                else if (_properties is VideoEncodingProperties)
                {
                    return (_properties as VideoEncodingProperties).Height;
                }

                return 0;
            }
        }

        public uint FrameRate
        {
            get
            {
                if (_properties is VideoEncodingProperties)
                {
                    if ((_properties as VideoEncodingProperties).FrameRate.Denominator != 0)
                    {
                        return (_properties as VideoEncodingProperties).FrameRate.Numerator /
                            (_properties as VideoEncodingProperties).FrameRate.Denominator;
                    }
                }

                return 0;
            }
        }

        public double AspectRatio
        {
            get { return Math.Round((Height != 0) ? (Width / (double)Height) : double.NaN, 2); }
        }

        public IMediaEncodingProperties EncodingProperties
        {
            get { return _properties; }
        }

        public string GetFriendlyName(bool showFrameRate = true)
        {
            if (_properties is ImageEncodingProperties ||
                !showFrameRate)
            {
                return Width + "x" + Height + " [" + AspectRatio + "] " + _properties.Subtype;
            }
            else if (_properties is VideoEncodingProperties)
            {
                return Width + "x" + Height + " [" + AspectRatio + "] " + FrameRate + "FPS " + _properties.Subtype;
            }

            return String.Empty;
        }

    }
}
