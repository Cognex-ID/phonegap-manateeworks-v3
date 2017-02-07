using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

using System.Resources;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Markup;
using System.Windows.Resources;
using System.IO;
using System.Windows.Media.Imaging;
using System.Reflection;
using System.Xml.Linq;

using Windows.Phone.Media.Capture;
using Windows.Phone.Media.Devices;
using System.Threading.Tasks;
using Windows.Foundation;
using System.Diagnostics;
using System.Windows.Media;
using Microsoft.Devices;

using System.Threading;
using System.Windows.Threading;
using System.ComponentModel;

using BarcodeLib;
using BarcodeScanners;
using WPCordovaClassLib.Cordova.JSON;
using WPCordovaClassLib.Cordova;
using Cordova.Extension.Commands;

namespace BarcodeScannerPage
{
    public partial class ScannerPage : PhoneApplicationPage   
    {

        public static bool isPage = false;

        public enum OverlayMode
         {
             OM_MW = 1,
             OM_IMAGE = 2
         };

        public static String floatToStrDot(float value)
        {
            return ((int)(value)).ToString() + "." + ((int)(value * 100) % 100).ToString();
        }
        public ScannerPage()
        {


            InitializeComponent();

            int ver = Scanner.MWBgetLibVersion();
            int v1 = (ver >> 16);
            int v2 = (ver >> 8) & 0xff;
            int v3 = (ver & 0xff);
            libVersion = String.Format("{0}.{1}.{2}", v1, v2, v3);

            System.Diagnostics.Debug.WriteLine("Lib version: " + libVersion);


            processingHandler = new DoWorkEventHandler(bw_DoWork);
            previewFrameHandler = new TypedEventHandler<ICameraCaptureDevice, Object>(cam_PreviewFrameAvailable);

            bw.WorkerReportsProgress = false;
            bw.WorkerSupportsCancellation = true;

            bw.DoWork += processingHandler;

        }
        
        public static int CPU_CORES = -1;
        public static int param_maxThreads = 4;
        public static int activeThreads;
        public static int currentWorker = 0;

        public static Boolean isClosing = false;
        public static Boolean closeScanner = false;

        public static Boolean resultDisplayed = false;

        public static OverlayMode param_OverlayMode = OverlayMode.OM_MW;
        public static bool param_EnableHiRes = true;
        public static bool param_EnableFlash = true;
        public static bool param_DefaultFlashOn = false;
        public static bool param_CloseScannerOnDecode = true;
        public static int param_parserMask = Scanner.MWP_PARSER_MASK_NONE;

        public static SupportedPageOrientation param_Orientation = SupportedPageOrientation.PortraitOrLandscape;

        public int MAX_RESOLUTION = 1280 * 768;

        public static PhotoCaptureDevice cameraDevice;
        //  public static AudioVideoCaptureDevice cameraDevice;
        public static Boolean isProcessing = false;
        private byte[] pixels = null;
        private DateTime lastTime;
        private BackgroundWorker bw = new BackgroundWorker();
        public static DoWorkEventHandler processingHandler;
        private String libVersion;
        private bool flashAvailable;
        private bool flashActive = false;
        DispatcherTimer focusTimer;

        public static TypedEventHandler<ICameraCaptureDevice, Object> previewFrameHandler;


        public void stopCamera()
        {
            focusTimer.Stop();
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            closeScanner = false;
            isPage = true;
            if (param_Orientation == SupportedPageOrientation.Landscape)
            {

                this.Dispatcher.BeginInvoke((Action)(() =>
                {
                    this.SupportedOrientations = SupportedPageOrientation.Landscape;
                }));
            } else
                if (param_Orientation == SupportedPageOrientation.Portrait)
                {

                    this.Dispatcher.BeginInvoke((Action)(() =>
                    {
                        this.SupportedOrientations = SupportedPageOrientation.Portrait;
                    }));
                }


            if (CPU_CORES <= 0)
            {
                CPU_CORES = BarcodeHelper.getCPUCores();
            }
            if (param_maxThreads > CPU_CORES)
            {
                param_maxThreads = CPU_CORES;
            }

            InitializeCamera(CameraSensorLocation.Back);

            isProcessing = false;
            resultDisplayed = false;
            isClosing = false;
            fixOrientation(Orientation);
            if ((param_OverlayMode & OverlayMode.OM_MW) > 0)
            {
                MWOverlay.addOverlay(canvas);
            }

            

            if ((param_OverlayMode & OverlayMode.OM_IMAGE) > 0)
            {
                cameraOverlay.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                cameraOverlay.Visibility = System.Windows.Visibility.Collapsed;
            }

        }
       

        protected override void OnNavigatingFrom(System.Windows.Navigation.NavigatingCancelEventArgs e)
        {
            isClosing = true;
            if (cameraDevice != null)
            {
                try
                {
                    isProcessing = true;

                    stopCamera();

                }
                catch (Exception e2)
                {
                    Debug.WriteLine("Camera closing error: " + e2.Message);
                }
            }
            if ((param_OverlayMode & OverlayMode.OM_MW) > 0)
            {
                MWOverlay.removeOverlay();
            }
            base.OnNavigatingFrom(e);
        }
        

        public async Task InitializeCamera(CameraSensorLocation sensorLocation)
        {
            activeThreads = 0;
            isClosing = false;
            resultDisplayed = false;
            Windows.Foundation.Size captureResolution = new Windows.Foundation.Size(1280, 720);
            Windows.Foundation.Size previewResolution = new Windows.Foundation.Size(1280, 720);

            IReadOnlyList<Windows.Foundation.Size> prevSizes = PhotoCaptureDevice.GetAvailablePreviewResolutions(sensorLocation);
            IReadOnlyList<Windows.Foundation.Size> captSizes = PhotoCaptureDevice.GetAvailableCaptureResolutions(sensorLocation);

          
            double bestAspect = 1000;

            int bestAspectResIndex = 0;


            double aspect =  Application.Current.Host.Content.ActualHeight / Application.Current.Host.Content.ActualWidth;

           
            for (int i = 0; i < captSizes.Count; i++)
            {
                double w = captSizes[i].Width;
                double h = captSizes[i].Height;

                double resAspect = w / h;

                double diff = aspect - resAspect;
                if (diff < 0)
                    diff = -diff;


                if (diff < bestAspect)
                {
                    bestAspect = diff;
                    bestAspectResIndex = i;
                }

               
            }

            if (bestAspectResIndex >= 0)
            {
                captureResolution.Width = captSizes[bestAspectResIndex].Width;
                captureResolution.Height = captSizes[bestAspectResIndex].Height;
            }

            Windows.Foundation.Size initialResolution = captureResolution;

            try
            {
                PhotoCaptureDevice d = null;
             

                System.Diagnostics.Debug.WriteLine("Settinge camera initial resolution: " + initialResolution.Width + "x" + initialResolution.Height + "......");

                bool initialized = false;

                try
                {
                    d = await PhotoCaptureDevice.OpenAsync(sensorLocation, initialResolution);
                    System.Diagnostics.Debug.WriteLine("Success " + initialResolution);
                    initialized = true;
                    captureResolution = initialResolution;
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine("Failed to set initial resolution: " + initialResolution + " error:" + e.Message);

                }

                if (!initialized)
                    try
                    {
                        d = await PhotoCaptureDevice.OpenAsync(sensorLocation, captSizes.ElementAt<Windows.Foundation.Size>(0));
                        System.Diagnostics.Debug.WriteLine("Success " + captSizes.ElementAt<Windows.Foundation.Size>(0));
                        initialized = true;
                        captureResolution = captSizes.ElementAt<Windows.Foundation.Size>(0);
                    }
                    catch
                    {
                        System.Diagnostics.Debug.WriteLine("Failed to set initial resolution: " + captSizes.ElementAt<Windows.Foundation.Size>(0));
                    }

                //try to not use too high resolution

                if (param_EnableHiRes)
                {
                    MAX_RESOLUTION = 1280 * 800;
                }
                else
                {
                    MAX_RESOLUTION = 800 * 480;
                }


                if (d.PreviewResolution.Height * d.PreviewResolution.Width > MAX_RESOLUTION)
                {

                    bestAspectResIndex = -1;

                    aspect = (double)captureResolution.Width / captureResolution.Height;

                    for (int i = 0; i < prevSizes.Count; i++)
                    {
                        double w = prevSizes[i].Width;
                        double h = prevSizes[i].Height;

                        double resAspect = w / h;

                        double diff = aspect - resAspect;
                        if (diff < 0.01 && diff > -0.01)
                        {

                            if (w * h <= MAX_RESOLUTION)
                            {
                                previewResolution = prevSizes.ElementAt<Windows.Foundation.Size>(i);
                                bestAspectResIndex = i;
                                break;
                            }
                        }

                    }


                    if (bestAspectResIndex >= 0)
                        try
                        {
                            await d.SetPreviewResolutionAsync(previewResolution);
                        }
                        finally
                        {

                        }

                }

                System.Diagnostics.Debug.WriteLine("Preview resolution: " + d.PreviewResolution);

                d.SetProperty(KnownCameraGeneralProperties.EncodeWithOrientation,
                                d.SensorLocation == CameraSensorLocation.Back ?
                                d.SensorRotationInDegrees : -d.SensorRotationInDegrees);


                cameraDevice = d;
                

                cameraDevice.PreviewFrameAvailable += previewFrameHandler;

                IReadOnlyList<object> flashProperties = PhotoCaptureDevice.GetSupportedPropertyValues(sensorLocation, KnownCameraAudioVideoProperties.VideoTorchMode);

                if (param_EnableFlash)
                {

                    if (flashProperties.ToList().Contains((UInt32)VideoTorchMode.On))
                    {
                        flashAvailable = true;

                        if (param_DefaultFlashOn)
                        {
                            flashActive = true;
                            cameraDevice.SetProperty(KnownCameraAudioVideoProperties.VideoTorchMode, VideoTorchMode.On);
                            flashButtonImage.Source = new BitmapImage(new Uri("/Plugins/manateeworks-barcodescanner/flashbuttonon.png", UriKind.Relative));
                        }
                        else
                        {
                            flashActive = false;
                            cameraDevice.SetProperty(KnownCameraAudioVideoProperties.VideoTorchMode, VideoTorchMode.Off);
                            flashButtonImage.Source = new BitmapImage(new Uri("/Plugins/manateeworks-barcodescanner/flashbuttonoff.png", UriKind.Relative));
                        }

                        flashButton.Visibility = System.Windows.Visibility.Visible;
                        
                        
                    }
                    else
                    {
                        flashAvailable = false;
                        flashButton.Visibility = System.Windows.Visibility.Collapsed;

                    }
                }
                else
                {
                    flashButton.Visibility = System.Windows.Visibility.Collapsed;
                }

                videoBrush.SetSource(cameraDevice);
                focusTimer = new DispatcherTimer();
                focusTimer.Interval = TimeSpan.FromSeconds(3);
                focusTimer.Tick += delegate
                {
                    cameraDevice.FocusAsync();
                };
                focusTimer.Start();

               
            }
            catch (Exception e)
            {
                Debug.WriteLine("Camera initialization error: " + e.Message);
            }

        }

        private void bw_DoWork(object sender, DoWorkEventArgs e)
        {

            Byte[] result = new Byte[10000];
            BackgroundWorker worker = sender as BackgroundWorker;

            ThreadArguments ta = e.Argument as ThreadArguments;

            int resLen = Scanner.MWBscanGrayscaleImage(ta.pixels, ta.width, ta.height, result);

            //ignore positive result if closing is in progress
            if (isClosing)
            {
                resLen = -1;
            }

            if (resLen > 0 && resultDisplayed)
            {
                resLen = -1;
            }

            MWResult mwResult = null;

            if (resLen > 0 && Scanner.MWBgetResultType() == Scanner.MWB_RESULT_TYPE_MW)
            {
                MWResults results = new MWResults(result);

                if (results.count > 0)
                {
                    mwResult = results.getResult(0);
                    result = mwResult.bytes;
                }

            }

            if (lastTime != null && lastTime.Ticks > 0)
            {
                long timePrev = lastTime.Ticks;
                long timeNow = DateTime.Now.Ticks;
                long timeDifference = (timeNow - timePrev) / 10000;
                //System.Diagnostics.Debug.WriteLine("frame time: {0}", timeDifference);

            }

            lastTime = DateTime.Now;
            //ignore results shorter than 4 characters for barcodes with weak checksum
             if (mwResult != null && mwResult.bytesLength > 4 || (mwResult != null && mwResult.bytesLength > 0 && mwResult.type != Scanner.FOUND_39 && mwResult.type != Scanner.FOUND_25_INTERLEAVED && mwResult.type != Scanner.FOUND_25_STANDARD))
            {

                Scanner.MWBsetDuplicate(mwResult.bytes, mwResult.bytesLength);
				resultDisplayed = true;
                String typeName = BarcodeHelper.getBarcodeName(mwResult);


                byte[] parserResult = new byte[10000];
                double parserRes = -1;
                if (param_parserMask != Scanner.MWP_PARSER_MASK_NONE && !(param_parserMask == Scanner.MWP_PARSER_MASK_GS1 && !mwResult.isGS1))
                {

                    parserRes = Scanner.MWPgetJSON(param_parserMask, mwResult.encryptedResult, parserResult);

                    if (parserRes >= 0)
                    {

                        mwResult.text = Encoding.UTF8.GetString(parserResult,0,parserResult.Length);


                        int index = mwResult.text.IndexOf('\0');
                        if (index >= 0)
                            mwResult.text = mwResult.text.Remove(index);

                        if (param_parserMask == Scanner.MWP_PARSER_MASK_AAMVA)
                        {
                            typeName = typeName + " (AAMVA)";
                        }
                        else if (param_parserMask == Scanner.MWP_PARSER_MASK_IUID)
                        {
                            typeName = typeName + " (IUID)";
                        }
                        else if (param_parserMask == Scanner.MWP_PARSER_MASK_ISBT)
                        {
                            typeName = typeName + " (ISBT)";
                        }
                        else if (param_parserMask == Scanner.MWP_PARSER_MASK_HIBC)
                        {
                            typeName = typeName + " (HIBC)";
                        }
                        else if (param_parserMask == Scanner.MWP_PARSER_MASK_SCM)
                        {
                            typeName = typeName + " (SCM)";
                        }



                    }




                }


                Deployment.Current.Dispatcher.BeginInvoke(delegate()
                {
                    BarcodeHelper.scannerResult = new ScannerResult();

                    BarcodeHelper.resultAvailable = true;
                    BarcodeHelper.scannerResult.code = mwResult.text;
                    BarcodeHelper.scannerResult.type = BarcodeHelper.getBarcodeName(mwResult);
                    BarcodeHelper.scannerResult.isGS1 = mwResult.isGS1;
                    if (mwResult.locationPoints != null) {
                        BarcodeHelper.scannerResult.location = "{\"p1\":{\"x\":" + floatToStrDot(mwResult.locationPoints.p1.x) + ", \"y\":" + floatToStrDot(mwResult.locationPoints.p1.y) + "},"
                          + "\"p2\":{\"x\":" + floatToStrDot(mwResult.locationPoints.p2.x) + ", \"y\":" + floatToStrDot(mwResult.locationPoints.p2.y) + "},"
                          + "\"p3\":{\"x\":" + floatToStrDot(mwResult.locationPoints.p3.x) + ", \"y\":" + floatToStrDot(mwResult.locationPoints.p3.y) + "},"
                          + "\"p4\":{\"x\":" + floatToStrDot(mwResult.locationPoints.p4.x) + ", \"y\":" + floatToStrDot(mwResult.locationPoints.p4.y) + "}}";

                    }
                    else
                    {
                        BarcodeHelper.scannerResult.location = "false";
                    }

                    BarcodeHelper.scannerResult.imageWidth = mwResult.imageWidth;
                    BarcodeHelper.scannerResult.imageHeight = mwResult.imageHeight;

                    Byte[] binArray = new Byte[mwResult.bytesLength];
                    for (int i = 0; i < mwResult.bytesLength; i++)
                        binArray[i] = mwResult.bytes[i];


                    BarcodeHelper.scannerResult.bytes = binArray;

                    stopCamera();
                    
                    string resultString = "{\"code\":" + JsonHelper.Serialize(BarcodeHelper.scannerResult.code) + ","
                    + "\"type\":" + JsonHelper.Serialize(BarcodeHelper.scannerResult.type) + ","
                    + "\"bytes\":" + JsonHelper.Serialize(BarcodeHelper.scannerResult.bytes) + ","
                    + "\"isGS1\":" + JsonHelper.Serialize(BarcodeHelper.scannerResult.isGS1) + ","
                    + "\"location\":" + BarcodeHelper.scannerResult.location + ","
                    + "\"imageWidth\":" + BarcodeHelper.scannerResult.imageWidth + ","
                    + "\"imageHeight\":" + BarcodeHelper.scannerResult.imageHeight
                    + "}";
                    PluginResult pResult = new PluginResult(PluginResult.Status.OK, resultString);
                    pResult.KeepCallback = true;
                    MWBarcodeScanner.mwbScanner.DispatchCommandResult(pResult, MWBarcodeScanner.kallbackID);
                     if (param_CloseScannerOnDecode)
                     {
                         isClosing = true;
                         if (isPage) { 
                            NavigationService.GoBack();
                         }
                         else
                         {
                             MWBarcodeScanner.mwbScanner.stopScanner("");
                         }
                         resultDisplayed = false;

                     }

                    isProcessing = false;
                });


            }
            else
            {
                isProcessing = false;
            }
            activeThreads--;

        }
  

        public void flashButton_Click(object sender, RoutedEventArgs e)
        {
            if (flashAvailable)
            {
                flashActive = !flashActive;

                if (flashActive)
                {
                    cameraDevice.SetProperty(KnownCameraAudioVideoProperties.VideoTorchMode, VideoTorchMode.On);
                    if (isPage)
                    {
                        flashButtonImage.Source = new BitmapImage(new Uri("/Plugins/manateeworks-barcodescanner/flashbuttonon.png", UriKind.Relative));
                    }
                }
                else
                {
                    cameraDevice.SetProperty(KnownCameraAudioVideoProperties.VideoTorchMode, VideoTorchMode.Off);
                    if (isPage)
                    {
                        flashButtonImage.Source = new BitmapImage(new Uri("/Plugins/manateeworks-barcodescanner/flashbuttonoff.png", UriKind.Relative));
                    }
                }
            }
        }

        class ThreadArguments
        {
            public int width { get; set; }
            public int height { get; set; }
            public byte[] pixels { get; set; }
        }

        void cam_PreviewFrameAvailable(ICameraCaptureDevice device, object sender)
        {

            if (closeScanner) {
                closeScanner = false;
                Deployment.Current.Dispatcher.BeginInvoke(delegate()
                {
                   
                    string resultString = "{\"type\":" + JsonHelper.Serialize("Cancel") + "}";
                    PluginResult pResult = new PluginResult(PluginResult.Status.OK, resultString);
                    pResult.KeepCallback = true;
                    MWBarcodeScanner.mwbScanner.DispatchCommandResult(pResult, MWBarcodeScanner.kallbackID);
                    if (this.NavigationService != null && this.NavigationService.CanGoBack) { 
                        this.NavigationService.GoBack();
                    }
                   return;
               });
            }

            if (activeThreads >= param_maxThreads || resultDisplayed)
            {
                return;
            }


            if (isClosing)
            {
                return;
            }


            activeThreads++;

           // System.Diagnostics.Debug.WriteLine("ActiveThreads: " + activeThreads.ToString());

            isProcessing = true;

            int len = (int)device.PreviewResolution.Width * (int)device.PreviewResolution.Height;
            if (pixels == null)
                pixels = new byte[len];
            device.GetPreviewBufferY(pixels);
            // Byte[] result = new Byte[10000];
            if (isClosing)
            {
                return;
            }

            int width = (int)device.PreviewResolution.Width;
            int height = (int)device.PreviewResolution.Height;

            ThreadArguments ta = new ThreadArguments();
            ta.height = height;
            ta.width = width;
            ta.pixels = pixels;


            BackgroundWorker bw1 = new BackgroundWorker();
            bw1.WorkerReportsProgress = false;
            bw1.WorkerSupportsCancellation = false;
            bw1.DoWork += new DoWorkEventHandler(bw_DoWork);
            bw1.RunWorkerAsync(ta);

        }
        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            string resultString = "{\"type\":" + JsonHelper.Serialize("Cancel") +  "}";
            PluginResult pResult = new PluginResult(PluginResult.Status.OK, resultString);
            pResult.KeepCallback = true;
            MWBarcodeScanner.mwbScanner.DispatchCommandResult(pResult, MWBarcodeScanner.kallbackID);
            base.OnBackKeyPress(e);
        }
        public void fixOrientation(PageOrientation orientation)
        {
            if ((orientation & PageOrientation.LandscapeLeft) == (PageOrientation.LandscapeLeft))
            {

                videoBrush.RelativeTransform = new CompositeTransform()
                {
                    CenterX = 0.5,
                    CenterY = 0.5,
                    Rotation = 360
                };


            }

            else
                if ((orientation & PageOrientation.LandscapeRight) == (PageOrientation.LandscapeRight))
                {

                    videoBrush.RelativeTransform = new CompositeTransform()
                    {
                        CenterX = 0.5,
                        CenterY = 0.5,
                        Rotation = 180
                    };


                }
                else
                    if ((orientation & PageOrientation.PortraitUp) == (PageOrientation.PortraitUp))
                    {

                        videoBrush.RelativeTransform = new CompositeTransform()
                        {
                            CenterX = 0.5,
                            CenterY = 0.5,
                            Rotation = 90
                        };


                    }
                    else
                        if ((orientation & PageOrientation.PortraitDown) == (PageOrientation.PortraitDown))
                        {

                            videoBrush.RelativeTransform = new CompositeTransform()
                            {
                                CenterX = 0.5,
                                CenterY = 0.5,
                                Rotation = 270
                            };


                        }


        }

        private void PhoneApplicationPage_OrientationChanged(object sender, OrientationChangedEventArgs e)
        {

            if (videoBrush == null)
                return;
            MWOverlay.removeOverlay();
            MWOverlay.addOverlay(canvas);
            fixOrientation(e.Orientation);

        }

    


    }

}