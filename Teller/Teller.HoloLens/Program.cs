﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Foundation.Collections;
using Windows.Media.Capture;
using Windows.Media.Effects;
using Windows.Media.MediaProperties;
using Windows.Storage.Streams;
using Microsoft.ProjectOxford.Vision;
using Urho;
using Urho.Actions;
using Urho.SharpReality;
using Urho.Resources;
using Urho.Shapes;
using Urho.Urho2D;
using ImageTextRecognition;
using TTSSample;
using Windows.Storage;

namespace Teller.HoloLens
{
    internal class Program
    {
        [MTAThread]
        static void Main() => CoreApplication.Run(new UrhoAppViewSource<CognitiveServicesApp>());
    }


    public class CognitiveServicesApp : StereoApplication
    {
        //the key can be obtained for free here: https://www.microsoft.com/cognitive-services/en-us/computer-vision-api
        //click on "Get started for free"
        const string VisionApiKey = "07549dfbb13940c8ada6ffc9a8913b5b";

        Node busyIndicatorNode;
        MediaCapture mediaCapture;
        bool inited;
        bool busy;
        bool withPreview;

        public CognitiveServicesApp(ApplicationOptions opts) : base(opts) { }

        protected override async void Start()
        {
            ResourceCache.AutoReloadResources = true;
            base.Start();

            EnableGestureTapped = true;

            busyIndicatorNode = Scene.CreateChild();
            busyIndicatorNode.SetScale(0.06f);
            busyIndicatorNode.CreateComponent<BusyIndicator>();

            mediaCapture = new MediaCapture();
            await mediaCapture.InitializeAsync();
            await mediaCapture.AddVideoEffectAsync(new MrcVideoEffectDefinition(), MediaStreamType.Photo);
            await RegisterCortanaCommands(new Dictionary<string, Action> {
                    {"Print", () => CaptureAndShowResult(true)},
                    {"Help", Help }
                });

            ShowBusyIndicator(true);
            await TextToSpeech("Welcome to the Microsoft Cognitive Services sample for HoloLens and UrhoSharp.");
            ShowBusyIndicator(false);

            inited = true;

        }

        async void Help()
        {
            await TextToSpeech("Available commands are:");
            foreach (var cortanaCommand in CortanaCommands.Keys)
                await TextToSpeech(cortanaCommand);
        }

        async void EnablePreview(bool enable)
        {
            withPreview = enable;
            await TextToSpeech("Preview mode is " + (enable ? "enabled" : "disabled"));
        }

        public override void OnGestureDoubleTapped()
        {
            CaptureAndShowResult(false);
        }

        async Task CaptureAndShowResult(bool readText)
        {
            if (!inited || busy)
                return;

            ShowBusyIndicator(true);
            var desc = await CaptureAndAnalyze(readText);
            InvokeOnMain(() => ShowBusyIndicator(false));

            await TextToSpeech(desc);
        }

        void ShowBusyIndicator(bool show)
        {
            busy = show;
            busyIndicatorNode.Position = LeftCamera.Node.WorldPosition + LeftCamera.Node.WorldDirection * 1f;
            busyIndicatorNode.GetComponent<BusyIndicator>().IsBusy = show;
        }

        async Task<string> CaptureAndAnalyze(bool readText = false)
        {
            StorageFile file = await GetCurrentCapture();
            var stream = await file.OpenStreamForReadAsync();
            stream.Seek(0, SeekOrigin.Begin);
            InvokeOnMain(async () =>
            {
                var image = new Image();
                var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                memoryStream.Position = 0;
                image.Load(new Urho.MemoryBuffer(memoryStream.ToArray()));

                Node child = Scene.CreateChild();
                child.Position = LeftCamera.Node.WorldPosition + LeftCamera.Node.WorldDirection * 2f;
                child.LookAt(LeftCamera.Node.WorldPosition, Vector3.Up, TransformSpace.World);

                child.Scale = new Vector3(1f, image.Height / (float)image.Width, 0.1f) / 10;
                var texture = new Texture2D();
                texture.SetData(image, true);

                var material = new Material();
                material.SetTechnique(0, CoreAssets.Techniques.Diff, 0, 0);
                material.SetTexture(TextureUnit.Diffuse, texture);

                var box = child.CreateComponent<Box>();
                box.SetMaterial(material);

                child.RunActions(new EaseBounceOut(new ScaleBy(1f, 5)));
            });

            try
            {
                return await ImageToText.GetTextFromImage(await ImageToText.GetImageAsByteArray(file.Path));

            }
            catch (ClientException exc)
            {
                return exc?.Error?.Message ?? "Failed";
            }
            catch (Exception exc)
            {
                return "Failed";
            }
        }

        private async Task<StorageFile> GetCurrentCapture()
        {
            var imgFormat = ImageEncodingProperties.CreateJpeg();

            //NOTE: this is how you can save a frame to the CameraRoll folder:
            var file = await KnownFolders.CameraRoll.CreateFileAsync($"MCS_Photo{DateTime.Now:HH-mm-ss}.jpg", CreationCollisionOption.GenerateUniqueName);
            await mediaCapture.CapturePhotoToStorageFileAsync(imgFormat, file);
            return file;
        }
    }


    public class MrcVideoEffectDefinition : IVideoEffectDefinition
    {
        public string ActivatableClassId => "Windows.Media.MixedRealityCapture.MixedRealityCaptureVideoEffect";

        public IPropertySet Properties { get; }

        public MrcVideoEffectDefinition()
        {
            Properties = new PropertySet
                {
                    {"HologramCompositionEnabled", false},
                    {"RecordingIndicatorEnabled", false},
                    {"VideoStabilizationEnabled", false},
                    {"VideoStabilizationBufferLength", 0},
                    {"GlobalOpacityCoefficient", 0.9f},
                    {"StreamType", (int)MediaStreamType.Photo}
                };
        }
    }
}