using System;
using System.IO;
using System.Net;
using System.Net.Http;
using Android;
using Android.App;
using Android.Content.PM;
using Android.Graphics;
using Android.OS;
using Android.Support.V7.App;
using Android.Widget;
using Plugin.Media;
using Plugin.Media.Abstractions;
using Plugin.Permissions;

namespace XamarinCamera
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        private readonly string[] permissionGroup =
        {
            Manifest.Permission.ReadExternalStorage,
            Manifest.Permission.WriteExternalStorage,
            Manifest.Permission.Camera
        };

        private Button captureButton;
        private ImageView thisImageView;
        private Button uploadButton;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);

            captureButton = (Button) FindViewById(Resource.Id.captureButton);
            uploadButton = (Button) FindViewById(Resource.Id.uploadButton);
            thisImageView = (ImageView) FindViewById(Resource.Id.thisImageView);

            captureButton.Click += CaptureButton_Click;
            uploadButton.Click += UploadButton_Click;
            RequestPermissions(permissionGroup, 0);

            ServicePointManager.ServerCertificateValidationCallback += (o, cert, chain, errors) => true;
        }

        private void UploadButton_Click(object sender, EventArgs e)
        {
            UploadPhoto();
        }

        private void CaptureButton_Click(object sender, EventArgs e)
        {
            TakePhoto();
        }

        private async void TakePhoto()
        {
            await CrossMedia.Current.Initialize();

            var file = await CrossMedia.Current.TakePhotoAsync(new StoreCameraMediaOptions
            {
                PhotoSize = PhotoSize.Medium,
                CompressionQuality = 40,
                Name = "myimage.jpg",
                Directory = "sample"
            });

            if (file == null) return;

            var imageArray = File.ReadAllBytes(file.Path);
            var bitmap = BitmapFactory.DecodeByteArray(imageArray, 0, imageArray.Length);
            thisImageView.SetImageBitmap(bitmap);
            var uri = new Uri("https://uploadfilesserver.azurewebsites.net/api/Upload");
            var content = new MultipartFormDataContent();


            content.Add(new StreamContent(new MemoryStream(imageArray)),
                "\"file\"",
                $"\"{file.Path}\"");
            var clientHandler = new HttpClientHandler();
            clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
            {
                return true;
            };

            var httpClient = new HttpClient(clientHandler);

            var httpResponseMessage = await httpClient.PostAsync(uri, content);
            Console.WriteLine(httpResponseMessage);
        }

        private async void UploadPhoto()
        {
            await CrossMedia.Current.Initialize();

            if (!CrossMedia.Current.IsPickPhotoSupported)
            {
                Toast.MakeText(this, "Upload not supported on this device", ToastLength.Short).Show();
                return;
            }

            var file = await CrossMedia.Current.PickPhotoAsync(new PickMediaOptions
            {
                PhotoSize = PhotoSize.Full,
                CompressionQuality = 40
            });

            // Convert file to byre array, to bitmap and set it to our ImageView

            var imageArray = File.ReadAllBytes(file.Path);
            var bitmap = BitmapFactory.DecodeByteArray(imageArray, 0, imageArray.Length);
            thisImageView.SetImageBitmap(bitmap);
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions,
            Permission[] grantResults)
        {
            PermissionsImplementation.Current.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
}