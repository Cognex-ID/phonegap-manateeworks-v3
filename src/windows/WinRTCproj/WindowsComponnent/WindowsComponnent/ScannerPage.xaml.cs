using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Devices.Enumeration;
using Windows.Devices.Sensors;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Media;
using Windows.Media.Capture;
using Windows.Media.Devices;
using Windows.Media.MediaProperties;
using Windows.Phone.UI.Input;
using Windows.System.Display;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using WindowsComponnent;
using System.Runtime.InteropServices;
using Windows.UI.Popups;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Devices.Geolocation;
using System.Threading;
using Windows.Web.Http;
using Windows.Security.ExchangeActiveSyncProvisioning;
using Windows.System.Profile;
using Windows.Security.Cryptography.Core;
using Windows.Security.Cryptography;
using Windows.Web.Http.Filters;
using System.Text;
using Windows.Storage.Streams;
using System.Net;
using System.IO;
using BarcodeLib;
using System.Runtime.InteropServices.WindowsRuntime;
using System.ComponentModel;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace WindowsComponnent
{
    [ComImport]
    [System.Runtime.InteropServices.Guid("5b0d3235-4dba-4d44-865e-8f1d0e4fd04d")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    unsafe interface IMemoryBufferByteAccess
    {
        void GetBuffer(out byte* buffer, out uint capacity);
    }

	
	 public sealed class Example
    {
        public static string GetAnswer() 
        { 
            return "The answer is 42."; 
        }

        public int SampleProperty { get; set; }
    }

    public enum SupportedPageOrientation
    {
        //
        // Summary:
        //     Portrait orientation.
        Portrait = 1,
        //
        // Summary:
        //     Landscape orientation. Landscape supports both left and right views, but there
        //     is no way programmatically to specify one or the other.
        Landscape = 2,
        //
        // Summary:
        //     Landscape or portrait orientation.
        PortraitOrLandscape = 3
    }

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>


    public sealed partial class ScannerPage
    {
        public static bool USE_MWPARSER { get; set; }
        public static int MWPARSER_MASK { get; set; }

        private static Windows.Foundation.Metadata.Platform platform;
        private static Boolean resultDisplayed = false;

        /* Multithreading */
        private static readonly int nThreads = Environment.ProcessorCount;  public static int getHardwareThreads() { return nThreads; }
        private static int aThreads = 0;                                    public static int getActiveThreads() { return aThreads; }
        public static int maxThreads { get; set; }

        private struct conversionResult
        {
            public int width;
            public int height;
            public byte[] returnArray;
        }

        private static Queue<conversionResult> convertedQueue = new Queue<conversionResult>();
        private static Queue<MWResult> decodedQueue = new Queue<MWResult>();

        private static Mutex law_n_order = new Mutex();
        private static int frameCount = 0;

        public static bool pauseDecoder { get; set; }

        public static void iniClear()
        {
            frameCount = 0;
            convertedQueue.Clear();
            decodedQueue.Clear();

            if (maxThreads == 0) maxThreads = nThreads;

            pauseDecoder = false;
        }

        public static MWResult decodeFrame([ReadOnlyArray()] byte[] returnArray, int width, int height)//, out MWResult mwResult)
        {
            MWResult mwResult = null;

            if (returnArray == null) return mwResult;
            //Debug.WriteLine("In DecodeFrame: " + ("\n\taThreads: ") + aThreads + "\tconv_count: " + convertedQueue.Count); //tconv_count always 0, thats why
            law_n_order.WaitOne();
            if(aThreads < maxThreads) //rm && convertedQueue.Count > 0 thaaank you
            {
                aThreads++;

                conversionResult cData = new conversionResult();
                cData.width = width;
                cData.height = height;
                cData.returnArray = returnArray;

                //spawn a new thread
                BackgroundWorker bWorkerX = new BackgroundWorker();
                bWorkerX.WorkerReportsProgress = false;
                bWorkerX.WorkerSupportsCancellation = false;
                bWorkerX.DoWork += new DoWorkEventHandler(bWorkerX_DoWork);
                bWorkerX.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bWorkerX_RunWorkerCompleted);
                bWorkerX.RunWorkerAsync(cData);
            }

            if (decodedQueue.Count > 0)
            {
                mwResult = decodedQueue.Dequeue();

                //reset/clear up
                iniClear();
				//Debug.WriteLine("found it!");
            }
            law_n_order.ReleaseMutex();

            return mwResult;            
        }

        private static void bWorkerX_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            law_n_order.WaitOne();
            Debug.WriteLine("D frame: " + (frameCount));
            aThreads--;
            BackgroundWorker bWorkerX = sender as BackgroundWorker;
            bWorkerX.DoWork -= bWorkerX_DoWork;
            bWorkerX.RunWorkerCompleted -= bWorkerX_RunWorkerCompleted;
            bWorkerX.Dispose();
            law_n_order.ReleaseMutex();
            //throw new NotImplementedException();
        }

        private static void bWorkerX_DoWork(object sender, DoWorkEventArgs e)
        {
            int width = ((conversionResult)e.Argument).width;
            int height = ((conversionResult)e.Argument).height;
            byte[] returnArray = ((conversionResult)e.Argument).returnArray;

            //resString = "";
            int resLen = 0;
            if (returnArray != null)
            {
                MWResult mwResult = null;
                byte[] result = new byte[10000];

                try
                {
                    // Debug.WriteLine("ReturnArat size: " + returnArray.Length);
                    resLen = MWBarcodeScanner.scanGrayscaleImage(returnArray, width, height, result);
                    // Debug.WriteLine("result size: " + result.Length + "ResLen = " + resLen);
                    mwResult = null;
                }
                catch (Exception ee)
                {
                    Debug.WriteLine(ee.Message);
                }

                if (resultDisplayed)
                {
                    resLen = -1;
                }

                if (resLen > 0)
                {
                    MWResults results = new MWResults(result);
                    string s = System.Text.Encoding.UTF8.GetString(result, 0, result.Length);

                    if (results.count > 0)
                    {
                        mwResult = results.getResult(0);
                        result = mwResult.bytes;
                    }

                    if ((resultDisplayed == false) && (mwResult != null))
                    {
                        resultDisplayed = true;

                        string typeName = BarcodeHelper.getBarcodeName(mwResult.type);
                        //  mwResult.typeText = typeName;
                        byte[] parsedResult = new byte[6000];

                        string displayString = "";

                        if (MWPARSER_MASK != Scanner.MWP_PARSER_MASK_NONE) //instead of USE_MWPARSER
                        {
                            double parserRes = -1;
                            //ignore results shorter than 4 characters for barcodes with weak checksum
                            if (mwResult != null && mwResult.bytesLength > 4 || (mwResult != null && mwResult.bytesLength > 0 && mwResult.type != Scanner.FOUND_39 && mwResult.type != Scanner.FOUND_25_INTERLEAVED && mwResult.type != Scanner.FOUND_25_STANDARD))
                            {

                                //Scanner.MWBsetDuplicate(mwResult.bytes, mwResult.bytesLength); //so praa?

                                if (MWPARSER_MASK != Scanner.MWP_PARSER_MASK_NONE && !(MWPARSER_MASK == Scanner.MWP_PARSER_MASK_GS1 && !mwResult.isGS1))
                                {

                                    parserRes = BarcodeLib.Scanner.MWPgetJSON(MWPARSER_MASK, System.Text.Encoding.UTF8.GetBytes(mwResult.encryptedResult), parsedResult);

                                    if (parserRes >= 0)
                                    {

                                        mwResult.text = Encoding.UTF8.GetString(parsedResult, 0, parsedResult.Length);


                                        int index = mwResult.text.IndexOf('\0');
                                        if (index >= 0)
                                            mwResult.text = mwResult.text.Remove(index);

                                        if (MWPARSER_MASK == Scanner.MWP_PARSER_MASK_AAMVA)
                                        {
                                            typeName = typeName + " (AAMVA)";
                                        }
                                        else if (MWPARSER_MASK == Scanner.MWP_PARSER_MASK_IUID)
                                        {
                                            typeName = typeName + " (IUID)";
                                        }
                                        else if (MWPARSER_MASK == Scanner.MWP_PARSER_MASK_ISBT)
                                        {
                                            typeName = typeName + " (ISBT)";
                                        }
                                        else if (MWPARSER_MASK == Scanner.MWP_PARSER_MASK_HIBC)
                                        {
                                            typeName = typeName + " (HIBC)";
                                        }
                                        else if (MWPARSER_MASK == Scanner.MWP_PARSER_MASK_SCM)
                                        {
                                            typeName = typeName + " (SCM)";
                                        }



                                    }

                                }
                            }


                            if (parserRes == -1)
                            {

                                displayString = mwResult.text;
                            }
                            else
                            {

                                displayString = System.Text.Encoding.UTF8.GetString(parsedResult);
                            }

                        }
                        else
                        {
                            displayString = mwResult.text;
                        }

                        //     if (USE_ANALYTICS)
                        {
                            //       sendReport(mwResult);
                        }

                        try
                        {
                            //  Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High, () =>
                            //{
                            //successCallback.barcodeDetected(mwResult);

                            //   displayResultAsync_old(mwResult);
                            // });

                            // displayResultAsync(mwResult);

                        }
                        catch (Exception ee)
                        {
                            Debug.WriteLine(ee.Message);
                        }
                    }
                    resultDisplayed = false;
                    law_n_order.WaitOne();
                    decodedQueue.Enqueue(mwResult);
                    law_n_order.ReleaseMutex();
                }

            }
            //decodedQueue.Enqueue(null); //no need as it's null by default
            //throw new NotImplementedException();
        }

        public static unsafe byte[] convertToGrayscale(SoftwareBitmap bitmap, out int width, out int height)
        {
            width = 0;
            height = 0;
            byte[] returnArray = null;

            //if (pauseDecoder) return returnArray; //this is done in proxy.js

            Debug.WriteLine("Active Threads now: " + aThreads);
            law_n_order.WaitOne();
            if ((aThreads + convertedQueue.Count) < maxThreads) //old: if (aThreads < nThreads && convertedQueue.Count == 0)
            {
                //aThreads = nThreads; //just test one frame
                
                aThreads++;
                
                //spawn a new thread
                BackgroundWorker bWorker = new BackgroundWorker();
                bWorker.WorkerReportsProgress = false;
                bWorker.WorkerSupportsCancellation = false;
                bWorker.DoWork += new DoWorkEventHandler(bWorker_DoWork);
                bWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bWorker_RunWorkerCompleted);
                bWorker.RunWorkerAsync(bitmap);
            }
            else
            {
                //skip this frame
            }
            
            if (convertedQueue.Count > 0)
            {
                conversionResult cResult = convertedQueue.Dequeue();
                width = cResult.width;
                height = cResult.height;
                returnArray = cResult.returnArray;
            }
            law_n_order.ReleaseMutex();

            //special case handling because js doesn't see it as null
            if (returnArray == null)
            {
                returnArray = new byte[1]{ 0 };
            }
            
            return returnArray;
        }

        private static void bWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            law_n_order.WaitOne();
            Debug.WriteLine("GS frame: " + (++frameCount));
            //Debug.WriteLine("conv_count: " + (convertedQueue.Count)); //always 1
            aThreads--;
            BackgroundWorker bWorker = sender as BackgroundWorker;
            bWorker.DoWork -= bWorker_DoWork;
            bWorker.RunWorkerCompleted -= bWorker_RunWorkerCompleted;
            bWorker.Dispose();
            law_n_order.ReleaseMutex();
            //throw new NotImplementedException();
        }

        private static unsafe void bWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            byte* data = null;
            uint capacity = 0;

            SoftwareBitmap bitmap = e.Argument as SoftwareBitmap; //could check for null
            int width = 0;
            int height = 0;
            byte[] returnArray = null;

            // Effect is hard-coded to operate on BGRA8 format only
            if (bitmap.BitmapPixelFormat == BitmapPixelFormat.Bgra8 || bitmap.BitmapPixelFormat == BitmapPixelFormat.Nv12 ||
                bitmap.BitmapPixelFormat == BitmapPixelFormat.Yuy2 || bitmap.BitmapPixelFormat == BitmapPixelFormat.Gray8)
            {
                // In BGRA8 format, each pixel is defined by 4 bytes
                int BYTES_PER_PIXEL = 4;

                using (var buffer = bitmap.LockBuffer(BitmapBufferAccessMode.ReadWrite))
                using (IMemoryBufferReference reference = buffer.CreateReference())
                {
                    if (reference is IMemoryBufferByteAccess)
                    {
                        // Get a pointer to the pixel buffer
                        ((IMemoryBufferByteAccess)reference).GetBuffer(out data, out capacity);
                        var desc = buffer.GetPlaneDescription(0);
                        width = desc.Width;
                        height = desc.Height;
                        returnArray = new byte[desc.Width * desc.Height];
                        if (bitmap.BitmapPixelFormat == BitmapPixelFormat.Yuy2)
                        {
                            int length = desc.Width * desc.Height;
                            for (int i = 0; i < length; i++)
                            {
                                returnArray[i] = data[i << 1];
                            }
                        }
                        else
                        if (bitmap.BitmapPixelFormat == BitmapPixelFormat.Nv12 || bitmap.BitmapPixelFormat == BitmapPixelFormat.Gray8)
                        {

                            Marshal.Copy((IntPtr)data, returnArray, 0, desc.Width * desc.Height);
                        }
                        else

                        if (bitmap.BitmapPixelFormat == BitmapPixelFormat.Bgra8)
                        {
                            BYTES_PER_PIXEL = 4;

                            // Get information about the BitmapBuffer

                            // Iterate over all pixels
                            width = desc.Width;
                            height = desc.Height;
                            for (uint row = 0; row < desc.Height; row++)
                            {
                                for (uint col = 0; col < desc.Width; col++)
                                {
                                    // Index of the current pixel in the buffer (defined by the next 4 bytes, BGRA8)
                                    var currPixel = desc.StartIndex + desc.Stride * row + BYTES_PER_PIXEL * col;

                                    // Read the current pixel information into b,g,r channels (leave out alpha channel)
                                    var b = data[currPixel + 0]; // Blue
                                    var g = data[currPixel + 1]; // Green
                                    var r = data[currPixel + 2]; // Red

                                    int y = (r * 77) + (g * 151) + (b * 28) >> 8;
                                    /*
                                                                    data[currPixel + 0] = (byte)y;
                                                                    data[currPixel + 1] = (byte)y;
                                                                    data[currPixel + 2] = (byte)y;
                                                                    */
                                    returnArray[row * desc.Width + col] = (byte)y;
                                }
                            }
                        }
                    }
                }
            }

            conversionResult cResult = new conversionResult();
            cResult.width = width;
            cResult.height = height;
            cResult.returnArray = returnArray;

            law_n_order.WaitOne();
            convertedQueue.Enqueue(cResult);
            law_n_order.ReleaseMutex();
            //throw new NotImplementedException();
        }

        // scanImage implementation
        public static MWResult scanImage([ReadOnlyArray()] byte[] rawImage, int width, int height)
        {
            MWResult mwResult = null;

            int size = width * height;
            byte[] gray = new byte[size];

            int colorChannels = 4;
            int colorChannelOffset = 1;

            for (int y = 0; y < height; y++)
            {
                int dstOffset = y * width;
                int srcOffset = ((y * width) * colorChannels) + colorChannelOffset;
                for (int x = 0; x < width; x++)
                {
                    gray[dstOffset + x] = rawImage[srcOffset];
                    srcOffset += colorChannels;
                }
            }

            string s = null;

            Scanner.MWBsetResultType(Scanner.MWB_RESULT_TYPE_MW);
            byte[] p_data = new byte[10000];
            int len = Scanner.MWBscanGrayscaleImage(gray, width, height, p_data);

            if (len > 0)
            {
                MWResults mwres = new MWResults(p_data);
                mwResult = null;
                if (mwres != null && mwres.count > 0)
                {
                    mwResult = mwres.getResult(0);

                    // with parser
                    if (MWPARSER_MASK != Scanner.MWP_PARSER_MASK_NONE && !(MWPARSER_MASK == Scanner.MWP_PARSER_MASK_GS1 && !mwResult.isGS1))
                    {
                        double parserRes = -1;
                        byte[] pp_data = new byte[10000];

                        if (mwResult != null && mwResult.bytesLength > 4 || (mwResult != null && mwResult.bytesLength > 0 && mwResult.type != Scanner.FOUND_39 && mwResult.type != Scanner.FOUND_25_INTERLEAVED && mwResult.type != Scanner.FOUND_25_STANDARD))
                        {
                            parserRes = BarcodeLib.Scanner.MWPgetJSON(MWPARSER_MASK, System.Text.Encoding.UTF8.GetBytes(mwResult.encryptedResult), pp_data);

                            if (parserRes >= 0)
                            {
                                mwResult.text = Encoding.UTF8.GetString(pp_data, 0, pp_data.Length);

                                int index = mwResult.text.IndexOf('\0');
                                if (index >= 0)
                                    mwResult.text = mwResult.text.Remove(index);
                            }
                        }
                    }
                }
            }

            return mwResult;
        }
    }

    public sealed class DeviceInfo
    {
        private static DeviceInfo _Instance;
        public static DeviceInfo Instance
        {
            get
            {
                if (_Instance == null)
                    _Instance = new DeviceInfo();
                return _Instance;
            }

        }

        public string Id { get; private set; }
        public string Model { get; private set; }
        public string Manufracturer { get; private set; }
        public string Name { get; private set; }
        public static string OSName { get; set; }

        private DeviceInfo()
        {
            Id = GetId();
            var deviceInformation = new EasClientDeviceInformation();
            Model = deviceInformation.SystemProductName;
            Manufracturer = deviceInformation.SystemManufacturer;
            Name = deviceInformation.FriendlyName;
            OSName = deviceInformation.OperatingSystem;
        }

        private static string GetId()
        {
            if (Windows.Foundation.Metadata.ApiInformation.IsTypePresent("Windows.System.Profile.HardwareIdentification"))
            {
                var token = HardwareIdentification.GetPackageSpecificToken(null);
                var hardwareId = token.Id;
                var dataReader = Windows.Storage.Streams.DataReader.FromBuffer(hardwareId);

                byte[] bytes = new byte[hardwareId.Length];
                dataReader.ReadBytes(bytes);

                return BitConverter.ToString(bytes).Replace("-", "");
            }

            throw new Exception("NO API FOR DEVICE ID PRESENT!");
        }
    }
}
