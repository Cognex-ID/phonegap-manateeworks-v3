using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Tasks;
using System.Windows;

using WPCordovaClassLib.Cordova;
using WPCordovaClassLib.Cordova.Commands;
using WPCordovaClassLib.Cordova.JSON;

using BarcodeLib;
using BarcodeScanners;
using BarcodeScannerPage;
using System.Windows.Controls;
using System.Diagnostics;
using Windows.Phone.Media.Devices;
using Windows.Phone.Media.Capture;
using Microsoft.Devices;
using Windows.Foundation;
using System.Windows.Media;
using Microsoft.Phone.Reactive;
using System.Windows.Media.Imaging;

namespace Cordova.Extension.Commands
{
    class MWBarcodeScanner : BaseCommand
    {


        public static MWBarcodeScanner mwbScanner;
        public static string kallbackID;
        System.Windows.Media.VideoBrush videoBrush;
        public Canvas canvas;
        double width;
        double height;
        float widthClip;
        ScannerPage scannerPage;
        float heightClip;
        public static PhoneApplicationPage currentPage;
        Image imgOverlay;
        public static bool useAutoRect = true;

        Button flashButton;

        public Dictionary<int, float[]> scanningRects;

        public void initDecoder(string options)
        {

            BarcodeScanners.BarcodeHelper.initDecoder();
            DispatchCommandResult(new PluginResult(PluginResult.Status.OK));
           // var appId = Windows.ApplicationModel.Store.CurrentApp.AppId;
        }

        public void startScanner(string options)
        {

            BarcodeHelper.resultAvailable = false;

            Deployment.Current.Dispatcher.BeginInvoke(delegate()
            {

                var root = Application.Current.RootVisual as PhoneApplicationFrame;
                mwbScanner = this;
                kallbackID = JsonHelper.Deserialize<string[]>(options)[0];
                root.Navigate(new System.Uri("/Plugins/manateeworks-barcodescanner/ScannerPage.xaml", UriKind.Relative));
                
                root.Navigated += new System.Windows.Navigation.NavigatedEventHandler(root_Navigated);
            });


        }



        public void togglePauseResume(string options)
        {
            if (canvas != null)
            {
                ScannerPage.isClosing = !ScannerPage.isClosing;
            }
        }

        public void setActiveParser(string options)
        {
            string[] paramsList = JsonHelper.Deserialize<string[]>(options);
            ScannerPage.param_parserMask = Convert.ToInt32(paramsList[0]);
        }
     
        
        public void setUseAutorect(string options)
        {
           useAutoRect = Convert.ToBoolean(JsonHelper.Deserialize<string[]>(options)[0]);
        }

        
        public void startScannerView(string options)
        {

            Deployment.Current.Dispatcher.BeginInvoke(delegate()
            {
                if (canvas == null)
                {
                    string[] paramsList = JsonHelper.Deserialize<string[]>(options);
                    mwbScanner = this;
                    kallbackID = paramsList[4];

                    bool firstTimePageLoad = false;
                    if (currentPage == null)
                    {
                        firstTimePageLoad = true;
                        currentPage = (((PhoneApplicationFrame)Application.Current.RootVisual).Content as PhoneApplicationPage);
                    }

                    var screenWidth = (currentPage.Orientation == PageOrientation.Portrait || currentPage.Orientation == PageOrientation.PortraitUp || currentPage.Orientation == PageOrientation.PortraitDown) ? Application.Current.Host.Content.ActualWidth : Application.Current.Host.Content.ActualHeight;
                    var screenHeight = (currentPage.Orientation == PageOrientation.Landscape || currentPage.Orientation == PageOrientation.LandscapeLeft || currentPage.Orientation == PageOrientation.LandscapeRight) ? Application.Current.Host.Content.ActualWidth : Application.Current.Host.Content.ActualHeight;


                    var pX = Convert.ToInt32(paramsList[0]);
                    var pY = Convert.ToInt32(paramsList[1]);
                    var pWidth = Convert.ToInt32(paramsList[2]);
                    var pHeight = Convert.ToInt32(paramsList[3]);

                    var x = (float)pX / 100 * screenWidth;
                    var y = (float)pY / 100 * screenHeight;
                    width = (float)pWidth / 100 * screenWidth;
                    height = (float)pHeight / 100 * screenHeight;

                    videoBrush = new System.Windows.Media.VideoBrush();

                    canvas = new Canvas()
                    {
                        Background = videoBrush,
                        VerticalAlignment = VerticalAlignment.Top,
                        HorizontalAlignment = HorizontalAlignment.Left
                    };
                    float heightTmp = (float)height;
                    float widthTmp = (float)width;
                    heightClip = 0;
                    widthClip = 0;

                    float AR = (float)screenHeight / (float)screenWidth;
                    if (width * AR >= height)
                    {
                        heightTmp = (int)(width * AR);
                        heightClip = (float)(heightTmp - height) / 2;
                    }
                    else
                    {
                        widthTmp = (int)(height / AR);
                        widthClip = (float)(widthTmp - width) / 2;
                    }
                    canvas.Width = widthTmp;
                    canvas.Height = heightTmp;

                    RectangleGeometry rg = new RectangleGeometry();
                    rg.Rect = new System.Windows.Rect(widthClip, heightClip, width, height);
                    canvas.Clip = rg;
                    canvas.Margin = new Thickness(x - widthClip, y - heightClip, 0, 0);

                    if (pX == 0 && pY == 0 && pWidth ==1 && pHeight == 1) {
                        canvas.Visibility = Visibility.Collapsed;
                    }
                    (currentPage.FindName("LayoutRoot") as Grid).Children.Add(canvas);

                    scannerPage = new ScannerPage();
                    scannerPage.videoBrush = videoBrush;
                    ScannerPage.isPage = false;
                    scannerPage.InitializeCamera(CameraSensorLocation.Back);
                    scannerPage.fixOrientation(currentPage.Orientation);
                    setAutoRect();
                    if ((int)ScannerPage.param_OverlayMode == 1)
                    {
                        MWOverlay.addOverlay(canvas);
                    }
                    else if((int)ScannerPage.param_OverlayMode == 2)
                    {
                        imgOverlay = new Image() { Stretch = Stretch.Fill,
                        Width = width,
                        Height = height,
                        Margin = new Thickness(widthClip,heightClip,0,0)
                        };
                        
                        BitmapImage BitImg = new BitmapImage(new Uri("/Plugins/manateeworks-barcodescanner/overlay_mw.png", UriKind.Relative));
                        imgOverlay.Source = BitImg;
                        canvas.Children.Add(imgOverlay);
                    }
                    if (ScannerPage.param_EnableFlash)
                    {
                        flashButton = new Button()
                        {
                            Width = 70,
                            Height = 70,
                            BorderThickness = new Thickness(0, 0, 0, 0),
                            Margin = new Thickness(x + 5, y + 5, 0, 0),
                            Background = new ImageBrush()
                            {
                                ImageSource = new BitmapImage(new Uri("/Plugins/manateeworks-barcodescanner/flashbuttonoff.png", UriKind.Relative))
                            },
                            HorizontalAlignment = HorizontalAlignment.Left,
                            VerticalAlignment = VerticalAlignment.Top

                        };
                        flashButton.Click += delegate
                        {
                            scannerPage.flashButton_Click(null, null);
                        };

                        (currentPage.FindName("LayoutRoot") as Grid).Children.Add(flashButton);

                    }

                    if (firstTimePageLoad)
                    {



                        currentPage.NavigationService.Navigating += delegate
                        {
                            stopScanner("");
                        };

                      
                        currentPage.OrientationChanged += delegate
                        {
                            Deployment.Current.Dispatcher.BeginInvoke(delegate()
                            {
                                if (canvas != null)
                                {
                                    if ((int)ScannerPage.param_OverlayMode == 1)
                                    {
                                        MWOverlay.removeOverlay();
                                    } 
                                   

                                    screenWidth = (currentPage.Orientation == PageOrientation.Portrait || currentPage.Orientation == PageOrientation.PortraitUp || currentPage.Orientation == PageOrientation.PortraitDown) ? Application.Current.Host.Content.ActualWidth : Application.Current.Host.Content.ActualHeight;
                                    screenHeight = (currentPage.Orientation == PageOrientation.Landscape || currentPage.Orientation == PageOrientation.LandscapeLeft || currentPage.Orientation == PageOrientation.LandscapeRight) ? Application.Current.Host.Content.ActualWidth : Application.Current.Host.Content.ActualHeight;
                                    x = (float)pX / 100 * screenWidth;
                                    y = (float)pY / 100 * screenHeight;
                                    width = (float)pWidth / 100 * screenWidth;
                                    height = (float)pHeight / 100 * screenHeight;
                                    heightTmp = (float)height;
                                    widthTmp = (float)width;
                                    heightClip = 0;
                                    widthClip = 0;

                                    AR = (float)screenHeight / (float)screenWidth;
                                    if (width * AR >= height)
                                    {
                                        heightTmp = (float)(width * AR);
                                        heightClip = (float)(heightTmp - height) / 2;
                                    }
                                    else
                                    {
                                        widthTmp = (float)(height / AR);
                                        widthClip = (float)(widthTmp - width) / 2;
                                    }

                                    canvas.Width = widthTmp;
                                    canvas.Height = heightTmp;
                                    rg = new RectangleGeometry();
                                    rg.Rect = new System.Windows.Rect(widthClip, heightClip, width, height);
                                    canvas.Clip = rg;
                                    canvas.Margin = new Thickness(x - widthClip, y - heightClip, 0, 0);

                                    if (flashButton != null) {
                                        flashButton.Margin = new Thickness(x + 5, y + 5, 0, 0);

                                    }
                                    scannerPage.fixOrientation(currentPage.Orientation);
                                    setAutoRect();
                                    if ((int)ScannerPage.param_OverlayMode == 1) {
                                        Observable
                                         .Timer(TimeSpan.FromMilliseconds(100))
                                         .SubscribeOnDispatcher()
                                         .Subscribe(_ =>
                                         {
                                             Deployment.Current.Dispatcher.BeginInvoke(delegate()
                                             {
                                                 MWOverlay.addOverlay(canvas);
                                             });
                                         });
                                    }
                                    else if ((int)ScannerPage.param_OverlayMode == 2)
                                    {
                                         imgOverlay.Width = width;
                                         imgOverlay.Height = height;
                                         imgOverlay.Margin = new Thickness(widthClip, heightClip, 0, 0);
                                    }


                                   

                                }
                                
                            });
                        };

                        
                    }
                    
                }
                else
                {
                    setAutoRect();
                }

            });



        }

        public void toggleFlash(string options)
        {
            if (canvas != null)
            {
                scannerPage.flashButton_Click(null,null);
            }
        }
        public void registerSDK(string options)
        {
            string[] paramsList = JsonHelper.Deserialize<string[]>(options);
            int rigistration = Scanner.MWBregisterSDK(Convert.ToString(paramsList[0]));
            DispatchCommandResult(new PluginResult(PluginResult.Status.OK, Convert.ToString(rigistration)));

        }

        public void setActiveCodes(string options)
        {
            string[] paramsList = JsonHelper.Deserialize<string[]>(options);
            Scanner.MWBsetActiveCodes(Convert.ToInt32(paramsList[0]));
        }

        public void setActiveSubcodes(string options)
        {
            string[] paramsList = JsonHelper.Deserialize<string[]>(options);
            Scanner.MWBsetActiveSubcodes(Convert.ToInt32(paramsList[0]), Convert.ToInt32(paramsList[1]));
        }

        public void setFlags(string options)
        {
            string[] paramsList = JsonHelper.Deserialize<string[]>(options);
            Scanner.MWBsetFlags(Convert.ToInt32(paramsList[0]), Convert.ToInt32(paramsList[1]));

        }

        public void setMinLength(string options)
        {
            string[] paramsList = JsonHelper.Deserialize<string[]>(options);
            Scanner.MWBsetMinLength(Convert.ToInt32(paramsList[0]), Convert.ToInt32(paramsList[1]));

        }

        public void setDirection(string options)
        {
            string[] paramsList = JsonHelper.Deserialize<string[]>(options);
            Scanner.MWBsetDirection((uint)Convert.ToInt32(paramsList[0]));
        }

        public void setScanningRect(string options)
        {
            string[] paramsList = JsonHelper.Deserialize<string[]>(options);
            Scanner.MWBsetScanningRect(Convert.ToInt32(paramsList[0]), Convert.ToInt32(paramsList[1]),
                Convert.ToInt32(paramsList[2]), Convert.ToInt32(paramsList[3]), Convert.ToInt32(paramsList[4]));
        }

        public void setLevel(string options)
        {
            string[] paramsList = JsonHelper.Deserialize<string[]>(options);
            Scanner.MWBsetLevel(Convert.ToInt32(paramsList[0]));
        }

        public void setInterfaceOrientation(string options)
        {
            string[] paramsList = JsonHelper.Deserialize<string[]>(options);
            String orientation = Convert.ToString(paramsList[0]);

            if (orientation.Equals("Portrait"))
            {
                BarcodeScannerPage.ScannerPage.param_Orientation = SupportedPageOrientation.Portrait;
            }
            else
            {
                BarcodeScannerPage.ScannerPage.param_Orientation = SupportedPageOrientation.Landscape;
            }

        }

        public void setOverlayMode(string options)
        {
            string[] paramsList = JsonHelper.Deserialize<string[]>(options);
            BarcodeScannerPage.ScannerPage.param_OverlayMode = (BarcodeScannerPage.ScannerPage.OverlayMode)Convert.ToInt32(paramsList[0]);
        }

        public void enableHiRes(string options)
        {
            string[] paramsList = JsonHelper.Deserialize<string[]>(options);
            BarcodeScannerPage.ScannerPage.param_EnableHiRes = Convert.ToBoolean(paramsList[0]);
        }

        public void enableFlash(string options)
        {
            string[] paramsList = JsonHelper.Deserialize<string[]>(options);
            BarcodeScannerPage.ScannerPage.param_EnableFlash = Convert.ToBoolean(paramsList[0]);
        }

        public void turnFlashOn(string options)
        {
            string[] paramsList = JsonHelper.Deserialize<string[]>(options);
            BarcodeScannerPage.ScannerPage.param_DefaultFlashOn = Convert.ToBoolean(paramsList[0]);
        }

        public void enableZoom(string options)
        {
            //not supported currently on WP8 (technical limitation)
        }

        public void setZoomLevels(string options)
        {
            //not supported currently on WP8 (technical limitation)
        }
        public void setParam(string options)
        {
            string[] paramsList = JsonHelper.Deserialize<string[]>(options);
            //            Scanner.MWBsetParam(Convert.ToInt32(paramsList[0]), Convert.ToInt32(paramsList[1]),Convert.ToInt32(paramsList[1]));

        }
        public int getAvailableCores()
        {
            return BarcodeHelper.getCPUCores();
        }

        public void setMaxThreads(string options)
        {
            string[] paramsList = JsonHelper.Deserialize<string[]>(options);
            BarcodeScannerPage.ScannerPage.param_maxThreads = Convert.ToInt32(paramsList[0]);

            if (BarcodeScannerPage.ScannerPage.param_maxThreads > BarcodeScannerPage.ScannerPage.CPU_CORES && BarcodeScannerPage.ScannerPage.CPU_CORES > 0)
            {
                BarcodeScannerPage.ScannerPage.param_maxThreads = BarcodeScannerPage.ScannerPage.CPU_CORES;
            }

        }

        public void closeScannerOnDecode(string options)
        {
            string[] paramsList = JsonHelper.Deserialize<string[]>(options);
            BarcodeScannerPage.ScannerPage.param_CloseScannerOnDecode = Convert.ToBoolean(paramsList[0]);


        }
        public void resumeScanning(string options)
        {
            BarcodeScannerPage.ScannerPage.resultDisplayed = false;
            BarcodeScannerPage.ScannerPage.isClosing = false;

        }

        public void closeScanner(string options)
        {
            if (canvas != null) 
                stopScanner("");
            else
                BarcodeScannerPage.ScannerPage.closeScanner = true;

        }
        public void stopScanner(string options)
        {
            if (canvas != null)
            {
                ScannerPage.isClosing = true;
                Deployment.Current.Dispatcher.BeginInvoke(delegate()
               {
                   if (scannerPage != null) {
                       BarcodeScannerPage.ScannerPage.cameraDevice.Dispose();
                       if ((int)ScannerPage.param_OverlayMode == 1)
                       {

                           MWOverlay.removeOverlay();
                       }
                       else if ((int)ScannerPage.param_OverlayMode == 1)
                       {
                           canvas.Children.Remove(imgOverlay);
                       }

                       (currentPage.FindName("LayoutRoot") as Grid).Children.Remove(canvas);
                       if (flashButton != null)
                       {
                           (currentPage.FindName("LayoutRoot") as Grid).Children.Remove(flashButton);
                           flashButton = null;                
                       }

                       scannerPage.stopCamera();
                       scannerPage = null;
                       canvas = null;
                       videoBrush = null;

                   }

               });
            }

        }
        public void duplicateCodeDelay(string options)
        {
            string[] paramsList = JsonHelper.Deserialize<string[]>(options);

            Scanner.MWBsetDuplicatesTimeout(Convert.ToInt32(paramsList[0]));

        }

        void setAutoRect()
        {

            double p1x;
            double p1y;

            double p2x;
            double p2y;


            p1x = widthClip / canvas.Width;
            p1y = heightClip / canvas.Height;

            p2x = width / canvas.Width;
            p2y = height / canvas.Height;

            if (canvas.Width < canvas.Height)
            {
                double tmp = p1x;
                p1x = p1y;
                p1y = tmp;
                tmp = p2x;
                p2x = p2y;
                p2y = tmp;
            }

            if (useAutoRect) { 
            
            p1x += 0.02f;
            p1y += 0.02f;
            p2x -= 0.04f;
            p2y -= 0.04f;
            Scanner.MWBsetScanningRect(Convert.ToInt32(Scanner.MWB_CODE_MASK_25), Convert.ToInt32(p1x * 100),
                             Convert.ToInt32(p1y * 100), Convert.ToInt32(p2x * 100), Convert.ToInt32(p2y * 100));
            Scanner.MWBsetScanningRect(Convert.ToInt32(Scanner.MWB_CODE_MASK_39), Convert.ToInt32(p1x * 100),
                             Convert.ToInt32(p1y * 100), Convert.ToInt32(p2x * 100), Convert.ToInt32(p2y * 100));
            Scanner.MWBsetScanningRect(Convert.ToInt32(Scanner.MWB_CODE_MASK_93), Convert.ToInt32(p1x * 100),
                             Convert.ToInt32(p1y * 100), Convert.ToInt32(p2x * 100), Convert.ToInt32(p2y * 100));
            Scanner.MWBsetScanningRect(Convert.ToInt32(Scanner.MWB_CODE_MASK_128), Convert.ToInt32(p1x * 100),
                             Convert.ToInt32(p1y * 100), Convert.ToInt32(p2x * 100), Convert.ToInt32(p2y * 100));
            Scanner.MWBsetScanningRect(Convert.ToInt32(Scanner.MWB_CODE_MASK_AZTEC), Convert.ToInt32(p1x * 100),
                             Convert.ToInt32(p1y * 100), Convert.ToInt32(p2x * 100), Convert.ToInt32(p2y * 100));
            Scanner.MWBsetScanningRect(Convert.ToInt32(Scanner.MWB_CODE_MASK_DM), Convert.ToInt32(p1x * 100),
                             Convert.ToInt32(p1y * 100), Convert.ToInt32(p2x * 100), Convert.ToInt32(p2y * 100));
            Scanner.MWBsetScanningRect(Convert.ToInt32(Scanner.MWB_CODE_MASK_EANUPC), Convert.ToInt32(p1x * 100),
                             Convert.ToInt32(p1y * 100), Convert.ToInt32(p2x * 100), Convert.ToInt32(p2y * 100));
            Scanner.MWBsetScanningRect(Convert.ToInt32(Scanner.MWB_CODE_MASK_PDF), Convert.ToInt32(p1x * 100),
                             Convert.ToInt32(p1y * 100), Convert.ToInt32(p2x * 100), Convert.ToInt32(p2y * 100));
            Scanner.MWBsetScanningRect(Convert.ToInt32(Scanner.MWB_CODE_MASK_QR), Convert.ToInt32(p1x * 100),
                             Convert.ToInt32(p1y * 100), Convert.ToInt32(p2x * 100), Convert.ToInt32(p2y * 100));
            Scanner.MWBsetScanningRect(Convert.ToInt32(Scanner.MWB_CODE_MASK_RSS), Convert.ToInt32(p1x * 100),
                             Convert.ToInt32(p1y * 100), Convert.ToInt32(p2x * 100), Convert.ToInt32(p2y * 100));
            Scanner.MWBsetScanningRect(Convert.ToInt32(Scanner.MWB_CODE_MASK_CODABAR), Convert.ToInt32(p1x * 100),
                             Convert.ToInt32(p1y * 100), Convert.ToInt32(p2x * 100), Convert.ToInt32(p2y * 100));
            Scanner.MWBsetScanningRect(Convert.ToInt32(Scanner.MWB_CODE_MASK_DOTCODE), Convert.ToInt32(p1x * 100),
                             Convert.ToInt32(p1y * 100), Convert.ToInt32(p2x * 100), Convert.ToInt32(p2y * 100));
            Scanner.MWBsetScanningRect(Convert.ToInt32(Scanner.MWB_CODE_MASK_11), Convert.ToInt32(p1x * 100),
                             Convert.ToInt32(p1y * 100), Convert.ToInt32(p2x * 100), Convert.ToInt32(p2y * 100));
            Scanner.MWBsetScanningRect(Convert.ToInt32(Scanner.MWB_CODE_MASK_MSI), Convert.ToInt32(p1x * 100),
                             Convert.ToInt32(p1y * 100), Convert.ToInt32(p2x * 100), Convert.ToInt32(p2y * 100));
            Scanner.MWBsetScanningRect(Convert.ToInt32(Scanner.MWB_CODE_MASK_MAXICODE), Convert.ToInt32(p1x * 100),
                                Convert.ToInt32(p1y * 100), Convert.ToInt32(p2x * 100), Convert.ToInt32(p2y * 100));
            Scanner.MWBsetScanningRect(Convert.ToInt32(Scanner.MWB_CODE_MASK_POSTAL), Convert.ToInt32(p1x * 100),
                                Convert.ToInt32(p1y * 100), Convert.ToInt32(p2x * 100), Convert.ToInt32(p2y * 100));
            }
            else
            {
                if (scanningRects == null) {
                    scanningRects = new Dictionary<int, float[]>();
                    float left,top,rWidth,rHeight;


                    Scanner.MWBgetScanningRect(Scanner.MWB_CODE_MASK_25,out left,out top,out rWidth,out rHeight);
                    scanningRects.Add(Scanner.MWB_CODE_MASK_25,new float[]{left,top,rWidth,rHeight});

                    Scanner.MWBgetScanningRect(Scanner.MWB_CODE_MASK_39, out left, out top, out rWidth, out rHeight);
                    scanningRects.Add(Scanner.MWB_CODE_MASK_39, new float[] { left, top, rWidth, rHeight });

                    Scanner.MWBgetScanningRect(Scanner.MWB_CODE_MASK_93, out left, out top, out rWidth, out rHeight);
                    scanningRects.Add(Scanner.MWB_CODE_MASK_93, new float[] { left, top, rWidth, rHeight });

                    Scanner.MWBgetScanningRect(Scanner.MWB_CODE_MASK_128, out left, out top, out rWidth, out rHeight);
                    scanningRects.Add(Scanner.MWB_CODE_MASK_128, new float[] { left, top, rWidth, rHeight });

                    Scanner.MWBgetScanningRect(Scanner.MWB_CODE_MASK_AZTEC, out left, out top, out rWidth, out rHeight);
                    scanningRects.Add(Scanner.MWB_CODE_MASK_AZTEC, new float[] { left, top, rWidth, rHeight });

                    Scanner.MWBgetScanningRect(Scanner.MWB_CODE_MASK_DM, out left, out top, out rWidth, out rHeight);
                    scanningRects.Add(Scanner.MWB_CODE_MASK_DM, new float[] { left, top, rWidth, rHeight });

                    Scanner.MWBgetScanningRect(Scanner.MWB_CODE_MASK_EANUPC, out left, out top, out rWidth, out rHeight);
                    scanningRects.Add(Scanner.MWB_CODE_MASK_EANUPC, new float[] { left, top, rWidth, rHeight });
                    
                    Scanner.MWBgetScanningRect(Scanner.MWB_CODE_MASK_PDF, out left, out top, out rWidth, out rHeight);
                    scanningRects.Add(Scanner.MWB_CODE_MASK_PDF, new float[] { left, top, rWidth, rHeight });

                    Scanner.MWBgetScanningRect(Scanner.MWB_CODE_MASK_QR, out left, out top, out rWidth, out rHeight);
                    scanningRects.Add(Scanner.MWB_CODE_MASK_QR, new float[] { left, top, rWidth, rHeight });

                    Scanner.MWBgetScanningRect(Scanner.MWB_CODE_MASK_RSS, out left, out top, out rWidth, out rHeight);
                    scanningRects.Add(Scanner.MWB_CODE_MASK_RSS, new float[] { left, top, rWidth, rHeight });

                    Scanner.MWBgetScanningRect(Scanner.MWB_CODE_MASK_CODABAR, out left, out top, out rWidth, out rHeight);
                    scanningRects.Add(Scanner.MWB_CODE_MASK_CODABAR, new float[] { left, top, rWidth, rHeight });

                    Scanner.MWBgetScanningRect(Scanner.MWB_CODE_MASK_DOTCODE, out left, out top, out rWidth, out rHeight);
                    scanningRects.Add(Scanner.MWB_CODE_MASK_DOTCODE, new float[] { left, top, rWidth, rHeight });

                    Scanner.MWBgetScanningRect(Scanner.MWB_CODE_MASK_11, out left, out top, out rWidth, out rHeight);
                    scanningRects.Add(Scanner.MWB_CODE_MASK_11, new float[] { left, top, rWidth, rHeight });

                    Scanner.MWBgetScanningRect(Scanner.MWB_CODE_MASK_MSI, out left, out top, out rWidth, out rHeight);
                    scanningRects.Add(Scanner.MWB_CODE_MASK_MSI, new float[] { left, top, rWidth, rHeight });

                    Scanner.MWBgetScanningRect(Scanner.MWB_CODE_MASK_MAXICODE, out left, out top, out rWidth, out rHeight);
                    scanningRects.Add(Scanner.MWB_CODE_MASK_MAXICODE, new float[] { left, top, rWidth, rHeight });

                    Scanner.MWBgetScanningRect(Scanner.MWB_CODE_MASK_POSTAL, out left, out top, out rWidth, out rHeight);
                    scanningRects.Add(Scanner.MWB_CODE_MASK_POSTAL, new float[] { left, top, rWidth, rHeight });

                }
                else
                {
                    foreach (KeyValuePair<int, float[]> sRect in scanningRects)
                    {
                        Scanner.MWBsetScanningRect(Convert.ToInt32(sRect.Key), Convert.ToInt32(sRect.Value[0]),
                             Convert.ToInt32(sRect.Value[1]), Convert.ToInt32(sRect.Value[2]), Convert.ToInt32(sRect.Value[3]));
                    }
                }


                    float rcLeft, rcTop, rcWidth, rcHeight;
                    Scanner.MWBgetScanningRect(Convert.ToInt32(Scanner.MWB_CODE_MASK_128), out rcLeft,out rcTop, out rcWidth, out rcHeight);
                    Scanner.MWBsetScanningRect(Convert.ToInt32(Scanner.MWB_CODE_MASK_128), Convert.ToInt32((p1x + (1 - p1x * 2) * (rcLeft / 100)) * 100), Convert.ToInt32((p1y + (1 - p1y * 2) * (rcTop / 100)) * 100), Convert.ToInt32((p2x*(rcWidth/100))*100), Convert.ToInt32((p2y*(rcHeight/100))*100));

                //
            }

        }

        void root_Navigated(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {

            if ((e.Content is BarcodeScannerPage.ScannerPage)) return;

            (Application.Current.RootVisual as PhoneApplicationFrame).Navigated -= root_Navigated;

            /*if (BarcodeHelper.resultAvailable)
            {
                
               // string resultString = JsonHelper.Serialize (BarcodeHelper.scannerResult);
                string resultString = "{\"code\":" +JsonHelper.Serialize ( BarcodeHelper.scannerResult.code) +","
                    +"\"type\":" + JsonHelper.Serialize (BarcodeHelper.scannerResult.type) +","
                    + "\"bytes\":" + JsonHelper.Serialize (BarcodeHelper.scannerResult.bytes) + ","
                    + "\"isGS1\":" + JsonHelper.Serialize (BarcodeHelper.scannerResult.isGS1) + ","
                    + "\"location\":" + BarcodeHelper.scannerResult.location + ","
                    + "\"imageWidth\":" + BarcodeHelper.scannerResult.imageWidth + ","
                    + "\"imageHeight\":" + BarcodeHelper.scannerResult.imageHeight
                    +"}";

                DispatchCommandResult(new PluginResult(PluginResult.Status.OK, resultString));
            }*/

            BarcodeScannerPage.ScannerPage.cameraDevice.Dispose();


        }


    }
}
