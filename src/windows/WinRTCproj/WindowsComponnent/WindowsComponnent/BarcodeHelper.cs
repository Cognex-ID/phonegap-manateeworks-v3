using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media.Imaging;
using System.IO;
using BarcodeLib;
using System.Runtime.InteropServices.WindowsRuntime;

using Windows.ApplicationModel;
using Windows.Security.ExchangeActiveSyncProvisioning;
using Windows.System.Profile;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace WindowsComponnent
{
    public sealed class PointF
    {
        public float x { get; set; }
        public float y { get; set; }
    }

    public sealed class MWLocation
    {
        public PointF p1 { get; set; }
        public PointF p2 { get; set; }
        public PointF p3 { get; set; }
        public PointF p4 { get; set; }

        public PointF[] points { get; set; }

        public MWLocation([ReadOnlyArray()] float[] _points)
        {

            points = new PointF[4];

            for (int i = 0; i < 4; i++)
            {
                points[i] = new PointF();
                points[i].x = _points[i * 2];
                points[i].y = _points[i * 2 + 1];
            }
            p1 = new PointF();
            p2 = new PointF();
            p3 = new PointF();
            p4 = new PointF();

            p1.x = _points[0];
            p1.y = _points[1];
            p2.x = _points[2];
            p2.y = _points[3];
            p3.x = _points[4];
            p3.y = _points[5];
            p4.x = _points[6];
            p4.y = _points[7];

        }
    }


    public sealed class MWResult
    {
        public string text { get; set; }
        public byte[] bytes { get; set; }
        public int bytesLength { get; set; }
        public int type { get; set; }
        public int subtype { get; set; }
        public int imageWidth { get; set; }
        public int imageHeight { get; set; }
        public Boolean isGS1 { get; set; }
        public MWLocation locationPoints { get; set; }
        public string encryptedResult { get; set; }

        public MWResult()
        {
            text = null;
            bytes = null;
            bytesLength = 0;
            type = 0;
            subtype = 0;
            isGS1 = false;
           // locationPoints = null;
            imageWidth = 0;
            imageHeight = 0;
        }

    }

    class MWResults
    {

        public int version;
        public List<MWResult> results;
        public int count;

        public MWResults(byte[] buffer)
        {
            results = new List<MWResult>();
            count = 0;
            version = 0;

            if (buffer[0] != 'M' || buffer[1] != 'W' || buffer[2] != 'R')
            {
                return;
            }

            version = buffer[3];

            count = buffer[4];

            int currentPos = 5;

            for (int i = 0; i < count; i++)
            {

                MWResult result = new MWResult();

                int fieldsCount = buffer[currentPos];
                currentPos++;
                for (int f = 0; f < fieldsCount; f++)
                {
                    int fieldType = buffer[currentPos];
                    int fieldNameLength = buffer[currentPos + 1];
                    int fieldContentLength = 256 * (buffer[currentPos + 3 + fieldNameLength] & 0xFF) + (buffer[currentPos + 2 + fieldNameLength] & 0xFF);
                    String fieldName = null;

                    if (fieldNameLength > 0)
                    {
                        fieldName = Encoding.UTF8.GetString(buffer, currentPos + 2, fieldNameLength);
                    }

                    int contentPos = currentPos + fieldNameLength + 4;
                    float[] locations = new float[8];

                    if (fieldType == Scanner.MWB_RESULT_FT_TYPE)
                    {
                        result.type = BitConverter.ToInt32(buffer, contentPos);
                    }
                    else
                     if (fieldType == Scanner.MWB_RESULT_FT_SUBTYPE)
                    {

                        result.subtype = BitConverter.ToInt32(buffer, contentPos);
                    }
                    else
                     if (fieldType == Scanner.MWB_RESULT_FT_ISGS1)
                    {
                        result.isGS1 = BitConverter.ToInt32(buffer, contentPos) == 1;
                    }
                    else
                     if (fieldType == Scanner.MWB_RESULT_FT_IMAGE_WIDTH)
                    {
                        result.imageWidth = BitConverter.ToInt32(buffer, contentPos);
                    }
                    else
                     if (fieldType == Scanner.MWB_RESULT_FT_IMAGE_HEIGHT)
                    {
                        result.imageHeight = BitConverter.ToInt32(buffer, contentPos);
                    }
                    else
                     if (fieldType == Scanner.MWB_RESULT_FT_LOCATION)
                    {
                        for (int l = 0; l < 8; l++)
                        {
                            locations[l] = BitConverter.ToSingle(buffer, contentPos + l * 4);
                        }
                        result.locationPoints = new MWLocation(locations); //uncommented
                    }
                    else
                        if (fieldType == Scanner.MWB_RESULT_FT_TEXT)
                    {
                        result.text = Encoding.UTF8.GetString(buffer, contentPos, fieldContentLength);
                    }
                    else
                    if (fieldType == Scanner.MWB_RESULT_FT_BYTES)
                    {
                        result.bytes = new byte[fieldContentLength];
                        result.bytesLength = fieldContentLength;
                        for (int c = 0; c < fieldContentLength; c++)
                        {
                            result.bytes[c] = buffer[contentPos + c];
                        }
                    }
                    else
                        if (fieldType == Scanner.MWB_RESULT_FT_PARSER_BYTES)
                    {
                        byte[] tmp = new byte[fieldContentLength + 1];

                        for (int c = 0; c < fieldContentLength; c++)
                        {
                            tmp[c] = buffer[contentPos + c];
                        }
                        result.encryptedResult = System.Text.Encoding.UTF8.GetString(tmp);
                    }
                    currentPos += (fieldNameLength + fieldContentLength + 4);
                }
                results.Add(result);
            }
        }

        public MWResult getResult(int index)
        {
            return results.ElementAt(index);
        }
    }

    public sealed class BarcodeHelper
    {

        public static Boolean PDF_OPTIMIZED { get; set; }
       

        public static Windows.Foundation.Rect RECT_LANDSCAPE_1D { get; set; }
       
        public static Windows.Foundation.Rect RECT_LANDSCAPE_2D { get; set; } // = new Windows.Foundation.Rect(20, 5, 60, 90);
        public static Windows.Foundation.Rect RECT_PORTRAIT_1D { get; set; } //= new Windows.Foundation.Rect(20, 2, 60, 96);
        public static Windows.Foundation.Rect RECT_PORTRAIT_2D { get; set; } //= new Windows.Foundation.Rect(20, 5, 60, 90);
        public static Windows.Foundation.Rect RECT_FULL_1D { get; set; } //= new Windows.Foundation.Rect(2, 2, 96, 96);
        public static Windows.Foundation.Rect RECT_FULL_2D { get; set; }// = new Windows.Foundation.Rect(20, 5, 60, 90);
        public static Windows.Foundation.Rect RECT_DOTCODE { get; set; }// = new Windows.Foundation.Rect(30, 20, 40, 60);

        public static void initDecoder()
        {
            WindowsComponnent.ScannerPage.iniClear(); //needed for multithreading
            PDF_OPTIMIZED = false;
            RECT_LANDSCAPE_1D = new Windows.Foundation.Rect(2, 20, 96, 60);
            RECT_LANDSCAPE_2D =  new Windows.Foundation.Rect(20, 5, 60, 90);
            RECT_PORTRAIT_1D = new Windows.Foundation.Rect(20, 5, 60, 90);
            RECT_PORTRAIT_2D = new Windows.Foundation.Rect(2, 2, 96, 96);
            RECT_FULL_1D = new Windows.Foundation.Rect(2, 2, 96, 96); //2, 2, 96, 96
            RECT_FULL_2D = new Windows.Foundation.Rect(2, 2, 96, 96); //20, 5, 60, 90
            RECT_DOTCODE = new Windows.Foundation.Rect(2, 2, 96, 96); //30, 20, 40, 60

            // register your copy of the mobiScan SDK with the given key
            /*int registerResult = Scanner.MWBregisterSDK("key");
            if (registerResult == Scanner.MWB_RTREG_OK)
            {
                Debug.WriteLine("Registration OK");
            }
            else if (registerResult == Scanner.MWB_RTREG_INVALID_KEY)
            {
                Debug.WriteLine("Registration Invalid Key");
            }
            else if (registerResult == Scanner.MWB_RTREG_INVALID_CHECKSUM)
            {
                Debug.WriteLine("Registration Invalid Checksum");
            }
            else if (registerResult == Scanner.MWB_RTREG_INVALID_APPLICATION)
            {
                Debug.WriteLine("Registration Invalid Application");
            }
            else if (registerResult == Scanner.MWB_RTREG_INVALID_SDK_VERSION)
            {
                Debug.WriteLine("Registration Invalid SDK Version");
            }
            else if (registerResult == Scanner.MWB_RTREG_INVALID_KEY_VERSION)
            {
                Debug.WriteLine("Registration Invalid Key Version");
            }
            else if (registerResult == Scanner.MWB_RTREG_INVALID_PLATFORM)
            {
                Debug.WriteLine("Registration Invalid Platform");
            }
            else if (registerResult == Scanner.MWB_RTREG_KEY_EXPIRED)
            {
                Debug.WriteLine("Registration Key Expired");
            }
            else
            {
                Debug.WriteLine("Registration Unknown Error");
            }*/

            // choose code type or types you want to search for

            if (PDF_OPTIMIZED)
            {
                Scanner.MWBsetActiveCodes(Scanner.MWB_CODE_MASK_PDF);
                Scanner.MWBsetDirection((uint)(Scanner.MWB_SCANDIRECTION_HORIZONTAL));
                MWBsetScanningRect(Scanner.MWB_CODE_MASK_PDF, RECT_LANDSCAPE_1D);
            }
            else
            {

                // Our sample app is configured by default to search all supported barcodes...
                Scanner.MWBsetActiveCodes(Scanner.MWB_CODE_MASK_25 |
                                          Scanner.MWB_CODE_MASK_39 |
                                          Scanner.MWB_CODE_MASK_93 |
                                          Scanner.MWB_CODE_MASK_128 |
                                          Scanner.MWB_CODE_MASK_AZTEC |
                                          Scanner.MWB_CODE_MASK_DM |
                                          Scanner.MWB_CODE_MASK_EANUPC |
                                          Scanner.MWB_CODE_MASK_PDF | 
                                          //Scanner.MWB_CODE_MASK_DOTCODE |
                                          Scanner.MWB_CODE_MASK_QR |
                                          Scanner.MWB_CODE_MASK_CODABAR |
                                          Scanner.MWB_CODE_MASK_11 |
                                          Scanner.MWB_CODE_MASK_MSI |
										  Scanner.MWB_CODE_MASK_MAXICODE |
                                          Scanner.MWB_CODE_MASK_POSTAL |
                                          Scanner.MWB_CODE_MASK_RSS);
                // Our sample app is configured by default to search both directions...
                Scanner.MWBsetDirection((uint)(Scanner.MWB_SCANDIRECTION_HORIZONTAL | Scanner.MWB_SCANDIRECTION_VERTICAL));

                // set the scanning rectangle based on scan direction(format in pct: x, y, width, height)
                MWBsetScanningRect(Scanner.MWB_CODE_MASK_25, RECT_FULL_1D);
                MWBsetScanningRect(Scanner.MWB_CODE_MASK_39, RECT_FULL_1D);
                MWBsetScanningRect(Scanner.MWB_CODE_MASK_93, RECT_FULL_1D);
                MWBsetScanningRect(Scanner.MWB_CODE_MASK_128, RECT_FULL_1D);
                MWBsetScanningRect(Scanner.MWB_CODE_MASK_AZTEC, RECT_FULL_2D);
                MWBsetScanningRect(Scanner.MWB_CODE_MASK_DM, RECT_FULL_2D);
                MWBsetScanningRect(Scanner.MWB_CODE_MASK_EANUPC, RECT_FULL_1D);
                MWBsetScanningRect(Scanner.MWB_CODE_MASK_PDF, RECT_FULL_1D);
                MWBsetScanningRect(Scanner.MWB_CODE_MASK_QR, RECT_FULL_2D);
                MWBsetScanningRect(Scanner.MWB_CODE_MASK_RSS, RECT_FULL_1D);
                MWBsetScanningRect(Scanner.MWB_CODE_MASK_CODABAR, RECT_FULL_1D);
                MWBsetScanningRect(Scanner.MWB_CODE_MASK_DOTCODE, RECT_DOTCODE);
                MWBsetScanningRect(Scanner.MWB_CODE_MASK_11, RECT_FULL_1D);
                MWBsetScanningRect(Scanner.MWB_CODE_MASK_MSI, RECT_FULL_1D);
				MWBsetScanningRect(Scanner.MWB_CODE_MASK_MAXICODE, RECT_FULL_2D);
				MWBsetScanningRect(Scanner.MWB_CODE_MASK_POSTAL, RECT_FULL_1D);

            }

            // But for better performance, only activate the symbologies your application requires...
            // Scanner.MWBsetActiveCodes( Scanner.MWB_CODE_MASK_25 ); 
            // Scanner.MWBsetActiveCodes( Scanner.MWB_CODE_MASK_39 ); 
            // Scanner.MWBsetActiveCodes( Scanner.MWB_CODE_MASK_93 ); 
            // Scanner.MWBsetActiveCodes( Scanner.MWB_CODE_MASK_128 ); 
            // Scanner.MWBsetActiveCodes( Scanner.MWB_CODE_MASK_AZTEC ); 
            // Scanner.MWBsetActiveCodes( Scanner.MWB_CODE_MASK_DM ); 
            // Scanner.MWBsetActiveCodes( Scanner.MWB_CODE_MASK_EANUPC ); 
            // Scanner.MWBsetActiveCodes( Scanner.MWB_CODE_MASK_PDF ); 
            // Scanner.MWBsetActiveCodes( Scanner.MWB_CODE_MASK_QR ); 
            // Scanner.MWBsetActiveCodes( Scanner.MWB_CODE_MASK_RSS ); 
            // Scanner.MWBsetActiveCodes( Scanner.MWB_CODE_MASK_CODABAR ); 
            // Scanner.MWBsetActiveCodes( Scanner.MWB_CODE_MASK_DOTCODE ); 
            // Scanner.MWBsetActiveCodes( Scanner.MWB_CODE_MASK_11 ); 
            // Scanner.MWBsetActiveCodes( Scanner.MWB_CODE_MASK_MSI ); 


            // But for better performance, set like this for PORTRAIT scanning...
            // Scanner.MWBsetDirection((uint)Scanner.MWB_SCANDIRECTION_VERTICAL);
            // set the scanning rectangle based on scan direction(format in pct: x, y, width, height)
            // MWBsetScanningRect(Scanner.MWB_CODE_MASK_25,     RECT_PORTRAIT_1D);     
            // MWBsetScanningRect(Scanner.MWB_CODE_MASK_39,     RECT_PORTRAIT_1D);     
            // MWBsetScanningRect(Scanner.MWB_CODE_MASK_93,     RECT_PORTRAIT_1D); 
            // MWBsetScanningRect(Scanner.MWB_CODE_MASK_128,    RECT_PORTRAIT_1D);
            // MWBsetScanningRect(Scanner.MWB_CODE_MASK_AZTEC,  RECT_PORTRAIT_2D);    
            // MWBsetScanningRect(Scanner.MWB_CODE_MASK_DM,     RECT_PORTRAIT_2D);    
            // MWBsetScanningRect(Scanner.MWB_CODE_MASK_EANUPC, RECT_PORTRAIT_1D);     
            // MWBsetScanningRect(Scanner.MWB_CODE_MASK_PDF,    RECT_PORTRAIT_1D);
            // MWBsetScanningRect(Scanner.MWB_CODE_MASK_QR,     RECT_PORTRAIT_2D);     
            // MWBsetScanningRect(Scanner.MWB_CODE_MASK_RSS,    RECT_PORTRAIT_1D);     
            // MWBsetScanningRect(Scanner.MWB_CODE_MASK_CODABAR,RECT_PORTRAIT_1D); 
            // MWBsetScanningRect(Scanner.MWB_CODE_MASK_DOTCODE, RECT_DOTCODE);
            // MWBsetScanningRect(Scanner.MWB_CODE_MASK_11,    RECT_PORTRAIT_1D);
            // MWBsetScanningRect(Scanner.MWB_CODE_MASK_MSI,    RECT_PORTRAIT_1D);

            // or like this for LANDSCAPE scanning - Preferred for dense or wide codes...
            // Scanner.MWBsetDirection((uint)Scanner.MWB_SCANDIRECTION_HORIZONTAL);
            // set the scanning rectangle based on scan direction(format in pct: x, y, width, height)
            // MWBsetScanningRect(Scanner.MWB_CODE_MASK_25,     RECT_LANDSCAPE_1D);     
            // MWBsetScanningRect(Scanner.MWB_CODE_MASK_39,     RECT_LANDSCAPE_1D);     
            // MWBsetScanningRect(Scanner.MWB_CODE_MASK_93,     RECT_LANDSCAPE_1D);    
            // MWBsetScanningRect(Scanner.MWB_CODE_MASK_128,    RECT_LANDSCAPE_1D);
            // MWBsetScanningRect(Scanner.MWB_CODE_MASK_AZTEC,  RECT_LANDSCAPE_2D);    
            // MWBsetScanningRect(Scanner.MWB_CODE_MASK_DM,     RECT_LANDSCAPE_2D);    
            // MWBsetScanningRect(Scanner.MWB_CODE_MASK_EANUPC, RECT_LANDSCAPE_1D);     
            // MWBsetScanningRect(Scanner.MWB_CODE_MASK_PDF,    RECT_LANDSCAPE_1D);
            // MWBsetScanningRect(Scanner.MWB_CODE_MASK_QR,     RECT_LANDSCAPE_2D);     
            // MWBsetScanningRect(Scanner.MWB_CODE_MASK_RSS,    RECT_LANDSCAPE_1D); 
            // MWBsetScanningRect(Scanner.MWB_CODE_MASK_CODABAR,RECT_LANDSCAPE_1D); 
            // MWBsetScanningRect(Scanner.MWB_CODE_MASK_DOTCODE, RECT_DOTCODE);
            // MWBsetScanningRect(Scanner.MWB_CODE_MASK_11,    RECT_LANDSCAPE_1D);
            // MWBsetScanningRect(Scanner.MWB_CODE_MASK_MSI,    RECT_LANDSCAPE_1D);


            // set decoder effort level (1 - 5)
            // for live scanning scenarios, a setting between 1 to 3 will suffice
            // levels 4 and 5 are typically reserved for batch scanning 
            Scanner.MWBsetLevel(2);

            //Set minimum result length for low-protected barcode types
            Scanner.MWBsetMinLength(Scanner.MWB_CODE_MASK_25, 5);
            Scanner.MWBsetMinLength(Scanner.MWB_CODE_MASK_MSI, 5);
            Scanner.MWBsetMinLength(Scanner.MWB_CODE_MASK_39, 5);
            Scanner.MWBsetMinLength(Scanner.MWB_CODE_MASK_CODABAR, 5);
            Scanner.MWBsetMinLength(Scanner.MWB_CODE_MASK_11, 5);

            Scanner.MWBsetResultType(Scanner.MWB_RESULT_TYPE_MW);

        }

        public static void resetScanningRects()
        {
            // set the scanning rectangle based on scan direction(format in pct: x, y, width, height)
            MWBsetScanningRect(Scanner.MWB_CODE_MASK_25, RECT_FULL_1D);
            MWBsetScanningRect(Scanner.MWB_CODE_MASK_39, RECT_FULL_1D);
            MWBsetScanningRect(Scanner.MWB_CODE_MASK_93, RECT_FULL_1D);
            MWBsetScanningRect(Scanner.MWB_CODE_MASK_128, RECT_FULL_1D);
            MWBsetScanningRect(Scanner.MWB_CODE_MASK_AZTEC, RECT_FULL_2D);
            MWBsetScanningRect(Scanner.MWB_CODE_MASK_DM, RECT_FULL_2D);
            MWBsetScanningRect(Scanner.MWB_CODE_MASK_EANUPC, RECT_FULL_1D);
            MWBsetScanningRect(Scanner.MWB_CODE_MASK_PDF, RECT_FULL_1D);
            MWBsetScanningRect(Scanner.MWB_CODE_MASK_QR, RECT_FULL_2D);
            MWBsetScanningRect(Scanner.MWB_CODE_MASK_RSS, RECT_FULL_1D);
            MWBsetScanningRect(Scanner.MWB_CODE_MASK_CODABAR, RECT_FULL_1D);
            MWBsetScanningRect(Scanner.MWB_CODE_MASK_DOTCODE, RECT_DOTCODE);
            MWBsetScanningRect(Scanner.MWB_CODE_MASK_11, RECT_FULL_1D);
            MWBsetScanningRect(Scanner.MWB_CODE_MASK_MSI, RECT_FULL_1D);
            MWBsetScanningRect(Scanner.MWB_CODE_MASK_MAXICODE, RECT_FULL_2D);
            MWBsetScanningRect(Scanner.MWB_CODE_MASK_POSTAL, RECT_FULL_1D);
        }

        public static void MWBsetScanningRect(int codeMask, Windows.Foundation.Rect rect)
        {
            if (codeMask == Scanner.MWB_CODE_MASK_DOTCODE) Debug.WriteLine("managed_rect: " + rect);
            Scanner.MWBsetScanningRect(codeMask, (float)rect.Left, (float)rect.Top, (float)rect.Width, (float)rect.Height);            
        }

        public static Windows.Foundation.Rect MWBgetScanningRect(int codeMask)
        {
            float left, top, width, height;
            Scanner.MWBgetScanningRect(codeMask, out left, out top, out width, out height);

            return new Windows.Foundation.Rect(left, top, width, height);
        }

        public static Windows.Foundation.Rect createRect(float left, float top, float width, float height)
        {
            return new Windows.Foundation.Rect(left, top, width, height);
        }

        /*public static void MWBsetAllScanningRect([ReadOnlyArray()] int[] codeMasksArray, [ReadOnlyArray()] Windows.Foundation.Rect[] rectsArray)
        {
            int i1, i2;
            i1 = codeMasksArray.Length;
            i2 = rectsArray.Length;

            if (i1 == i2)
                for (int i = 0; i < i1; i++)
                    Scanner.MWBsetScanningRect(codeMasksArray[i], (float)rectsArray[i].Left, (float)rectsArray[i].Top, (float)rectsArray[i].Width, (float)rectsArray[i].Height);

            Debug.WriteLine("managed_rect: " + rectsArray[i1-1]);
        }*/

        public static String getBarcodeName(int bcType)
        {
            String typeName = "Unknown";
            if (bcType == Scanner.FOUND_128) typeName = "Code 128";
            if (bcType == Scanner.FOUND_39) typeName = "Code 39";
            if (bcType == Scanner.FOUND_DM) typeName = "Datamatrix";
            if (bcType == Scanner.FOUND_EAN_13) typeName = "EAN 13";
            if (bcType == Scanner.FOUND_EAN_8) typeName = "EAN 8";
            if (bcType == Scanner.FOUND_NONE) typeName = "None";
            if (bcType == Scanner.FOUND_RSS_14) typeName = "Databar 14";
            if (bcType == Scanner.FOUND_RSS_14_STACK) typeName = "Databar 14 Stacked";
            if (bcType == Scanner.FOUND_RSS_EXP) typeName = "Databar Expanded";
            if (bcType == Scanner.FOUND_RSS_LIM) typeName = "Databar Limited";
            if (bcType == Scanner.FOUND_UPC_A) typeName = "UPC A";
            if (bcType == Scanner.FOUND_UPC_E) typeName = "UPC E";
            if (bcType == Scanner.FOUND_PDF) typeName = "PDF417";
            if (bcType == Scanner.FOUND_QR) typeName = "QR";
            if (bcType == Scanner.FOUND_AZTEC) typeName = "Aztec";
            if (bcType == Scanner.FOUND_25_INTERLEAVED) typeName = "Code 25 Interleaved";
            if (bcType == Scanner.FOUND_25_STANDARD) typeName = "Code 25 Standard";
            if (bcType == Scanner.FOUND_93) typeName = "Code 93";
            if (bcType == Scanner.FOUND_CODABAR) typeName = "Codabar";
            if (bcType == Scanner.FOUND_DOTCODE) typeName = "Dotcode";
            if (bcType == Scanner.FOUND_128_GS1) typeName = "Code 128 GS1";
            if (bcType == Scanner.FOUND_ITF14) typeName = "ITF 14";
            if (bcType == Scanner.FOUND_11) typeName = "Code 11";
            if (bcType == Scanner.FOUND_MSI) typeName = "MSI Plessey";
            if (bcType == Scanner.FOUND_25_IATA) typeName = "IATA Code 25";
			if (bcType == Scanner.FOUND_25_MATRIX) typeName = "Code 2/5 Matrix";
            if (bcType == Scanner.FOUND_25_COOP) typeName = "Code 2/5 COOP";
            if (bcType == Scanner.FOUND_25_INVERTED) typeName = "Code 2/5 Inverted";
            if (bcType == Scanner.FOUND_QR_MICRO) typeName = "QR Micro";
            if (bcType == Scanner.FOUND_MAXICODE) typeName = "Maxicode";
            if (bcType == Scanner.FOUND_POSTNET) typeName = "Postnet";
            if (bcType == Scanner.FOUND_PLANET) typeName = "Planet";
            if (bcType == Scanner.FOUND_IMB) typeName = "Intelligent mail";
            if (bcType == Scanner.FOUND_ROYALMAIL) typeName = "Royal mail";
            if (bcType == Scanner.FOUND_32) typeName = "Code 32";

            if (Scanner.MWBisLastGS1() == 1)
            {
                typeName += " (GS1)";
            }

            return typeName;
        }

        static public Byte[] BufferFromImage(BitmapImage imageSource)
        {
            WriteableBitmap wb = new WriteableBitmap(imageSource.PixelWidth, imageSource.PixelHeight);

            int px = wb.PixelWidth;
            int py = wb.PixelHeight;

            Byte[] res = new Byte[px * py];

            for (int y = 0; y < py; y++)
            {
                for (int x = 0; x < px; x++)
                {

                    Stream PixelBufferStream = wb.PixelBuffer.AsStream();
                    byte[] dstArray = wb.PixelBuffer.ToArray();



                    int color = dstArray[y * px + x];
                    res[y * px + x] = (byte)color;
                }
            }

            return res;


        }
    }


    public static class Info
    {
        public static string SystemFamily { get; }
        public static string SystemVersion { get; }
        public static string SystemArchitecture { get; }
        public static string ApplicationName { get; }
        public static string ApplicationVersion { get; }
        public static string DeviceManufacturer { get; }
        public static string DeviceModel { get; }
        public static string OS { get; }

        static Info()
        {
            // get the system family name
            AnalyticsVersionInfo ai = AnalyticsInfo.VersionInfo;
            SystemFamily = ai.DeviceFamily;

            // get the system version number
            string sv = AnalyticsInfo.VersionInfo.DeviceFamilyVersion;
            ulong v = ulong.Parse(sv);
            ulong v1 = (v & 0xFFFF000000000000L) >> 48;
            ulong v2 = (v & 0x0000FFFF00000000L) >> 32;
            ulong v3 = (v & 0x00000000FFFF0000L) >> 16;
            ulong v4 = (v & 0x000000000000FFFFL);
            SystemVersion = $"{v1}.{v2}.{v3}.{v4}";

            // get the package architecure
            Package package = Package.Current;
            SystemArchitecture = package.Id.Architecture.ToString();

            // get the user friendly app name
            ApplicationName = package.DisplayName;

            // get the app version
            PackageVersion pv = package.Id.Version;
            ApplicationVersion = $"{pv.Major}.{pv.Minor}.{pv.Build}.{pv.Revision}";

            // get the device manufacturer and model name
            EasClientDeviceInformation eas = new EasClientDeviceInformation();
            OS = eas.OperatingSystem;
            DeviceManufacturer = eas.SystemManufacturer;

            CanonicalPhoneName name = PhoneNameResolver.Resolve(eas.SystemManufacturer, eas.SystemProductName);
            DeviceModel = name.FullCanonicalName;
        }
    }


    public static class PhoneNameResolver
    {
        public static CanonicalPhoneName Resolve(string manufacturer, string model)
        {
            var manufacturerNormalized = manufacturer.Trim().ToUpper();

            switch (manufacturerNormalized)
            {
                case "NOKIA":
                case "MICROSOFT":
                case "MICROSOFTMDG":
                    return ResolveNokia(manufacturer, model);
                case "HTC":
                    return ResolveHtc(manufacturer, model);
                case "SAMSUNG":
                    return ResolveSamsung(manufacturer, model);
                case "LG":
                    return ResolveLg(manufacturer, model);
                case "HUAWEI":
                    return ResolveHuawei(manufacturer, model);
                default:
                    return new CanonicalPhoneName()
                    {
                        ReportedManufacturer = manufacturer,
                        ReportedModel = model,
                        CanonicalManufacturer = manufacturer,
                        CanonicalModel = model,
                        IsResolved = false
                    };
            }
        }



        private static CanonicalPhoneName ResolveHuawei(string manufacturer, string model)
        {
            var modelNormalized = model.Trim().ToUpper();

            var result = new CanonicalPhoneName()
            {
                ReportedManufacturer = manufacturer,
                ReportedModel = model,
                CanonicalManufacturer = "HUAWEI",
                CanonicalModel = model,
                IsResolved = false
            };


            var lookupValue = modelNormalized;

            if (lookupValue.StartsWith("HUAWEI H883G"))
            {
                lookupValue = "HUAWEI H883G";
            }

            if (lookupValue.StartsWith("HUAWEI W1"))
            {
                lookupValue = "HUAWEI W1";
            }

            if (modelNormalized.StartsWith("HUAWEI W2"))
            {
                lookupValue = "HUAWEI W2";
            }

            if (huaweiLookupTable.ContainsKey(lookupValue))
            {
                var modelMetadata = huaweiLookupTable[lookupValue];
                result.CanonicalModel = modelMetadata.CanonicalModel;
                result.Comments = modelMetadata.Comments;
                result.IsResolved = true;
            }

            return result;
        }



        private static CanonicalPhoneName ResolveLg(string manufacturer, string model)
        {
            var modelNormalized = model.Trim().ToUpper();

            var result = new CanonicalPhoneName()
            {
                ReportedManufacturer = manufacturer,
                ReportedModel = model,
                CanonicalManufacturer = "LG",
                CanonicalModel = model,
                IsResolved = false
            };


            var lookupValue = modelNormalized;

            if (lookupValue.StartsWith("LG-C900"))
            {
                lookupValue = "LG-C900";
            }

            if (lookupValue.StartsWith("LG-E900"))
            {
                lookupValue = "LG-E900";
            }

            if (lgLookupTable.ContainsKey(lookupValue))
            {
                var modelMetadata = lgLookupTable[lookupValue];
                result.CanonicalModel = modelMetadata.CanonicalModel;
                result.Comments = modelMetadata.Comments;
                result.IsResolved = true;
            }

            return result;
        }

        private static CanonicalPhoneName ResolveSamsung(string manufacturer, string model)
        {
            var modelNormalized = model.Trim().ToUpper();

            var result = new CanonicalPhoneName()
            {
                ReportedManufacturer = manufacturer,
                ReportedModel = model,
                CanonicalManufacturer = "SAMSUNG",
                CanonicalModel = model,
                IsResolved = false
            };


            var lookupValue = modelNormalized;

            if (lookupValue.StartsWith("GT-S7530"))
            {
                lookupValue = "GT-S7530";
            }

            if (lookupValue.StartsWith("SGH-I917"))
            {
                lookupValue = "SGH-I917";
            }

            if (samsungLookupTable.ContainsKey(lookupValue))
            {
                var modelMetadata = samsungLookupTable[lookupValue];
                result.CanonicalModel = modelMetadata.CanonicalModel;
                result.Comments = modelMetadata.Comments;
                result.IsResolved = true;
            }

            return result;
        }

        private static CanonicalPhoneName ResolveHtc(string manufacturer, string model)
        {
            var modelNormalized = model.Trim().ToUpper();

            var result = new CanonicalPhoneName()
            {
                ReportedManufacturer = manufacturer,
                ReportedModel = model,
                CanonicalManufacturer = "HTC",
                CanonicalModel = model,
                IsResolved = false
            };


            var lookupValue = modelNormalized;

            if (lookupValue.StartsWith("A620"))
            {
                lookupValue = "A620";
            }

            if (lookupValue.StartsWith("C625"))
            {
                lookupValue = "C625";
            }

            if (lookupValue.StartsWith("C620"))
            {
                lookupValue = "C620";
            }

            if (htcLookupTable.ContainsKey(lookupValue))
            {
                var modelMetadata = htcLookupTable[lookupValue];
                result.CanonicalModel = modelMetadata.CanonicalModel;
                result.Comments = modelMetadata.Comments;
                result.IsResolved = true;
            }

            return result;
        }

        private static CanonicalPhoneName ResolveNokia(string manufacturer, string model)
        {
            var modelNormalized = model.Trim().ToUpper();

            var result = new CanonicalPhoneName()
            {
                ReportedManufacturer = manufacturer,
                ReportedModel = model,
                CanonicalManufacturer = "NOKIA",
                CanonicalModel = model,
                IsResolved = false
            };

            var lookupValue = modelNormalized;
            if (modelNormalized.StartsWith("RM-"))
            {
                var rms = Regex.Match(modelNormalized, "(RM-)([0-9]+)");
                lookupValue = rms.Value;
            }

            if (nokiaLookupTable.ContainsKey(lookupValue))
            {
                var modelMetadata = nokiaLookupTable[lookupValue];

                if (!string.IsNullOrEmpty(modelMetadata.CanonicalManufacturer))
                {
                    result.CanonicalManufacturer = modelMetadata.CanonicalManufacturer;
                }
                result.CanonicalModel = modelMetadata.CanonicalModel;
                result.Comments = modelMetadata.Comments;
                result.IsResolved = true;
            }

            return result;
        }


        private static Dictionary<string, CanonicalPhoneName> huaweiLookupTable = new Dictionary<string, CanonicalPhoneName>()
        {
            // Huawei W1
            { "HUAWEI H883G", new CanonicalPhoneName() { CanonicalModel = "Ascend W1" } },
            { "HUAWEI W1", new CanonicalPhoneName() { CanonicalModel = "Ascend W1" } },
            
            // Huawei Ascend W2
            { "HUAWEI W2", new CanonicalPhoneName() { CanonicalModel = "Ascend W2" } },
        };


        private static Dictionary<string, CanonicalPhoneName> lgLookupTable = new Dictionary<string, CanonicalPhoneName>()
        {
            // Optimus 7Q/Quantum
            { "LG-C900", new CanonicalPhoneName() { CanonicalModel = "Optimus 7Q/Quantum" } },

            // Optimus 7
            { "LG-E900", new CanonicalPhoneName() { CanonicalModel = "Optimus 7" } },

            // Jil Sander
            { "LG-E906", new CanonicalPhoneName() { CanonicalModel = "Jil Sander" } },

            // Lancet
            { "LGVW820", new CanonicalPhoneName() { CanonicalModel = "Lancet" } },
        };

        private static Dictionary<string, CanonicalPhoneName> samsungLookupTable = new Dictionary<string, CanonicalPhoneName>()
        {
            // OMNIA W
            { "GT-I8350", new CanonicalPhoneName() { CanonicalModel = "Omnia W" } },
            { "GT-I8350T", new CanonicalPhoneName() { CanonicalModel = "Omnia W" } },
            { "OMNIA W", new CanonicalPhoneName() { CanonicalModel = "Omnia W" } },

            // OMNIA 7
            { "GT-I8700", new CanonicalPhoneName() { CanonicalModel = "Omnia 7" } },
            { "OMNIA7", new CanonicalPhoneName() { CanonicalModel = "Omnia 7" } },

            // OMNIA M
            { "GT-S7530", new CanonicalPhoneName() { CanonicalModel = "Omnia 7" } },

            // Focus
            { "I917", new CanonicalPhoneName() { CanonicalModel = "Focus" } },
            { "SGH-I917", new CanonicalPhoneName() { CanonicalModel = "Focus" } },

            // Focus 2
            { "SGH-I667", new CanonicalPhoneName() { CanonicalModel = "Focus 2" } },

            // Focus Flash
            { "SGH-I677", new CanonicalPhoneName() { CanonicalModel = "Focus Flash" } },

            // Focus S
            { "HADEN", new CanonicalPhoneName() { CanonicalModel = "Focus S" } },
            { "SGH-I937", new CanonicalPhoneName() { CanonicalModel = "Focus S" } },

            // ATIV S
            { "GT-I8750", new CanonicalPhoneName() { CanonicalModel = "ATIV S" } },
            { "SGH-T899M", new CanonicalPhoneName() { CanonicalModel = "ATIV S" } },

            // ATIV Odyssey
            { "SCH-I930", new CanonicalPhoneName() { CanonicalModel = "ATIV Odyssey" } },
            { "SCH-R860U", new CanonicalPhoneName() { CanonicalModel = "ATIV Odyssey", Comments="US Cellular" } },

            // ATIV S Neo
            { "SPH-I800", new CanonicalPhoneName() { CanonicalModel = "ATIV S Neo", Comments="Sprint" } },
            { "SGH-I187", new CanonicalPhoneName() { CanonicalModel = "ATIV S Neo", Comments="AT&T" } },
            { "GT-I8675", new CanonicalPhoneName() { CanonicalModel = "ATIV S Neo" } },

            // ATIV SE
            { "SM-W750V", new CanonicalPhoneName() { CanonicalModel = "ATIV SE", Comments="Verizon" } },
        };

        private static Dictionary<string, CanonicalPhoneName> htcLookupTable = new Dictionary<string, CanonicalPhoneName>()
        {
            // Surround
            { "7 MONDRIAN T8788", new CanonicalPhoneName() { CanonicalModel = "Surround" } },
            { "T8788", new CanonicalPhoneName() { CanonicalModel = "Surround" } },
            { "SURROUND", new CanonicalPhoneName() { CanonicalModel = "Surround" } },
            { "SURROUND T8788", new CanonicalPhoneName() { CanonicalModel = "Surround" } },

            // Mozart
            { "7 MOZART", new CanonicalPhoneName() { CanonicalModel = "Mozart" } },
            { "7 MOZART T8698", new CanonicalPhoneName() { CanonicalModel = "Mozart" } },
            { "HTC MOZART", new CanonicalPhoneName() { CanonicalModel = "Mozart" } },
            { "MERSAD 7 MOZART T8698", new CanonicalPhoneName() { CanonicalModel = "Mozart" } },
            { "MOZART", new CanonicalPhoneName() { CanonicalModel = "Mozart" } },
            { "MOZART T8698", new CanonicalPhoneName() { CanonicalModel = "Mozart" } },
            { "PD67100", new CanonicalPhoneName() { CanonicalModel = "Mozart" } },
            { "T8697", new CanonicalPhoneName() { CanonicalModel = "Mozart" } },

            // Pro
            { "7 PRO T7576", new CanonicalPhoneName() { CanonicalModel = "7 Pro" } },
            { "MWP6885", new CanonicalPhoneName() { CanonicalModel = "7 Pro" } },
            { "USCCHTC-PC93100", new CanonicalPhoneName() { CanonicalModel = "7 Pro" } },

            // Arrive
            { "PC93100", new CanonicalPhoneName() { CanonicalModel = "Arrive", Comments = "Sprint" } },
            { "T7575", new CanonicalPhoneName() { CanonicalModel = "Arrive", Comments = "Sprint" } },

            // HD2
            { "HD2", new CanonicalPhoneName() { CanonicalModel = "HD2" } },
            { "HD2 LEO", new CanonicalPhoneName() { CanonicalModel = "HD2" } },
            { "LEO", new CanonicalPhoneName() { CanonicalModel = "HD2" } },

            // HD7
            { "7 SCHUBERT T9292", new CanonicalPhoneName() { CanonicalModel = "HD7" } },
            { "GOLD", new CanonicalPhoneName() { CanonicalModel = "HD7" } },
            { "HD7", new CanonicalPhoneName() { CanonicalModel = "HD7" } },
            { "HD7 T9292", new CanonicalPhoneName() { CanonicalModel = "HD7" } },
            { "MONDRIAN", new CanonicalPhoneName() { CanonicalModel = "HD7" } },
            { "SCHUBERT", new CanonicalPhoneName() { CanonicalModel = "HD7" } },
            { "Schubert T9292", new CanonicalPhoneName() { CanonicalModel = "HD7" } },
            { "T9296", new CanonicalPhoneName() { CanonicalModel = "HD7", Comments = "Telstra, AU" } },
            { "TOUCH-IT HD7", new CanonicalPhoneName() { CanonicalModel = "HD7" } },

            // HD7S
            { "T9295", new CanonicalPhoneName() { CanonicalModel = "HD7S" } },

            // Trophy
            { "7 TROPHY", new CanonicalPhoneName() { CanonicalModel = "Trophy" } },
            { "7 TROPHY T8686", new CanonicalPhoneName() { CanonicalModel = "Trophy" } },
            { "PC40100", new CanonicalPhoneName() { CanonicalModel = "Trophy", Comments = "Verizon" } },
            { "SPARK", new CanonicalPhoneName() { CanonicalModel = "Trophy" } },
            { "TOUCH-IT TROPHY", new CanonicalPhoneName() { CanonicalModel = "Trophy" } },
            { "MWP6985", new CanonicalPhoneName() { CanonicalModel = "Trophy" } },

            // 8S
            { "A620", new CanonicalPhoneName() { CanonicalModel = "8S" } },
            { "WINDOWS PHONE 8S BY HTC", new CanonicalPhoneName() { CanonicalModel = "8S" } },

            // 8X
            { "C620", new CanonicalPhoneName() { CanonicalModel = "8X" } },
            { "C625", new CanonicalPhoneName() { CanonicalModel = "8X" } },
            { "HTC6990LVW", new CanonicalPhoneName() { CanonicalModel = "8X", Comments="Verizon" } },
            { "PM23300", new CanonicalPhoneName() { CanonicalModel = "8X", Comments="AT&T" } },
            { "WINDOWS PHONE 8X BY HTC", new CanonicalPhoneName() { CanonicalModel = "8X" } },

            // 8XT
            { "HTCPO881", new CanonicalPhoneName() { CanonicalModel = "8XT", Comments="Sprint" } },
            { "HTCPO881 SPRINT", new CanonicalPhoneName() { CanonicalModel = "8XT", Comments="Sprint" } },
            { "HTCPO881 HTC", new CanonicalPhoneName() { CanonicalModel = "8XT", Comments="Sprint" } },

            // Titan
            { "ETERNITY", new CanonicalPhoneName() { CanonicalModel = "Titan", Comments = "China" } },
            { "PI39100", new CanonicalPhoneName() { CanonicalModel = "Titan", Comments = "AT&T" } },
            { "TITAN X310E", new CanonicalPhoneName() { CanonicalModel = "Titan" } },
            { "ULTIMATE", new CanonicalPhoneName() { CanonicalModel = "Titan" } },
            { "X310E", new CanonicalPhoneName() { CanonicalModel = "Titan" } },
            { "X310E TITAN", new CanonicalPhoneName() { CanonicalModel = "Titan" } },
            
            // Titan II
            { "PI86100", new CanonicalPhoneName() { CanonicalModel = "Titan II", Comments = "AT&T" } },
            { "RADIANT", new CanonicalPhoneName() { CanonicalModel = "Titan II" } },

            // Radar
            { "RADAR", new CanonicalPhoneName() { CanonicalModel = "Radar" } },
            { "RADAR 4G", new CanonicalPhoneName() { CanonicalModel = "Radar", Comments = "T-Mobile USA" } },
            { "RADAR C110E", new CanonicalPhoneName() { CanonicalModel = "Radar" } },
            
            // One M8
            { "HTC6995LVW", new CanonicalPhoneName() { CanonicalModel = "One (M8)", Comments="Verizon" } },
            { "0P6B180", new CanonicalPhoneName() { CanonicalModel = "One (M8)", Comments="AT&T" } },
            { "0P6B140", new CanonicalPhoneName() { CanonicalModel = "One (M8)", Comments="Dual SIM?" } },
};

        private static Dictionary<string, CanonicalPhoneName> nokiaLookupTable = new Dictionary<string, CanonicalPhoneName>()
        {
            // Lumia 505
            { "LUMIA 505", new CanonicalPhoneName() { CanonicalModel = "Lumia 505" } },
            // Lumia 510
            { "LUMIA 510", new CanonicalPhoneName() { CanonicalModel = "Lumia 510" } },
            { "NOKIA 510", new CanonicalPhoneName() { CanonicalModel = "Lumia 510" } },
            // Lumia 610
            { "LUMIA 610", new CanonicalPhoneName() { CanonicalModel = "Lumia 610" } },
            { "LUMIA 610 NFC", new CanonicalPhoneName() { CanonicalModel = "Lumia 610", Comments = "NFC" } },
            { "NOKIA 610", new CanonicalPhoneName() { CanonicalModel = "Lumia 610" } },
            { "NOKIA 610C", new CanonicalPhoneName() { CanonicalModel = "Lumia 610" } },
            // Lumia 620
            { "LUMIA 620", new CanonicalPhoneName() { CanonicalModel = "Lumia 620" } },
            { "RM-846", new CanonicalPhoneName() { CanonicalModel = "Lumia 620" } },
            // Lumia 710
            { "LUMIA 710", new CanonicalPhoneName() { CanonicalModel = "Lumia 710" } },
            { "NOKIA 710", new CanonicalPhoneName() { CanonicalModel = "Lumia 710" } },
            // Lumia 800
            { "LUMIA 800", new CanonicalPhoneName() { CanonicalModel = "Lumia 800" } },
            { "LUMIA 800C", new CanonicalPhoneName() { CanonicalModel = "Lumia 800" } },
            { "NOKIA 800", new CanonicalPhoneName() { CanonicalModel = "Lumia 800" } },
            { "NOKIA 800C", new CanonicalPhoneName() { CanonicalModel = "Lumia 800", Comments = "China" } },
            // Lumia 810
            { "RM-878", new CanonicalPhoneName() { CanonicalModel = "Lumia 810" } },
            // Lumia 820
            { "RM-824", new CanonicalPhoneName() { CanonicalModel = "Lumia 820" } },
            { "RM-825", new CanonicalPhoneName() { CanonicalModel = "Lumia 820" } },
            { "RM-826", new CanonicalPhoneName() { CanonicalModel = "Lumia 820" } },
            // Lumia 822
            { "RM-845", new CanonicalPhoneName() { CanonicalModel = "Lumia 822", Comments = "Verizon" } },
            // Lumia 900
            { "LUMIA 900", new CanonicalPhoneName() { CanonicalModel = "Lumia 900" } },
            { "NOKIA 900", new CanonicalPhoneName() { CanonicalModel = "Lumia 900" } },
            // Lumia 920
            { "RM-820", new CanonicalPhoneName() { CanonicalModel = "Lumia 920" } },
            { "RM-821", new CanonicalPhoneName() { CanonicalModel = "Lumia 920" } },
            { "RM-822", new CanonicalPhoneName() { CanonicalModel = "Lumia 920" } },
            { "RM-867", new CanonicalPhoneName() { CanonicalModel = "Lumia 920", Comments = "920T" } },
            { "NOKIA 920", new CanonicalPhoneName() { CanonicalModel = "Lumia 920" } },
            { "LUMIA 920", new CanonicalPhoneName() { CanonicalModel = "Lumia 920" } },
            // Lumia 520
            { "RM-914", new CanonicalPhoneName() { CanonicalModel = "Lumia 520" } },
            { "RM-915", new CanonicalPhoneName() { CanonicalModel = "Lumia 520" } },
            { "RM-913", new CanonicalPhoneName() { CanonicalModel = "Lumia 520", Comments="520T" } },
            // Lumia 521?
            { "RM-917", new CanonicalPhoneName() { CanonicalModel = "Lumia 521", Comments="T-Mobile 520" } },
            // Lumia 720
            { "RM-885", new CanonicalPhoneName() { CanonicalModel = "Lumia 720" } },
            { "RM-887", new CanonicalPhoneName() { CanonicalModel = "Lumia 720", Comments="China 720T" } },
            // Lumia 928
            { "RM-860", new CanonicalPhoneName() { CanonicalModel = "Lumia 928" } },
            // Lumia 925
            { "RM-892", new CanonicalPhoneName() { CanonicalModel = "Lumia 925" } },
            { "RM-893", new CanonicalPhoneName() { CanonicalModel = "Lumia 925" } },
            { "RM-910", new CanonicalPhoneName() { CanonicalModel = "Lumia 925" } },
            { "RM-955", new CanonicalPhoneName() { CanonicalModel = "Lumia 925", Comments="China 925T" } },
            // Lumia 1020
            { "RM-875", new CanonicalPhoneName() { CanonicalModel = "Lumia 1020" } },
            { "RM-876", new CanonicalPhoneName() { CanonicalModel = "Lumia 1020" } },
            { "RM-877", new CanonicalPhoneName() { CanonicalModel = "Lumia 1020" } },
            // Lumia 625
            { "RM-941", new CanonicalPhoneName() { CanonicalModel = "Lumia 625" } },
            { "RM-942", new CanonicalPhoneName() { CanonicalModel = "Lumia 625" } },
            { "RM-943", new CanonicalPhoneName() { CanonicalModel = "Lumia 625" } },
            // Lumia 1520
            { "RM-937", new CanonicalPhoneName() { CanonicalModel = "Lumia 1520" } },
            { "RM-938", new CanonicalPhoneName() { CanonicalModel = "Lumia 1520", Comments="AT&T" } },
            { "RM-939", new CanonicalPhoneName() { CanonicalModel = "Lumia 1520" } },
            { "RM-940", new CanonicalPhoneName() { CanonicalModel = "Lumia 1520", Comments="AT&T" } },
            // Lumia 525
            { "RM-998", new CanonicalPhoneName() { CanonicalModel = "Lumia 525" } },
            // Lumia 1320
            { "RM-994", new CanonicalPhoneName() { CanonicalModel = "Lumia 1320" } },
            { "RM-995", new CanonicalPhoneName() { CanonicalModel = "Lumia 1320" } },
            { "RM-996", new CanonicalPhoneName() { CanonicalModel = "Lumia 1320" } },
            // Lumia Icon
            { "RM-927", new CanonicalPhoneName() { CanonicalModel = "Lumia Icon", Comments="Verizon" } },
            // Lumia 630
            { "RM-976", new CanonicalPhoneName() { CanonicalModel = "Lumia 630" } },
            { "RM-977", new CanonicalPhoneName() { CanonicalModel = "Lumia 630" } },
            { "RM-978", new CanonicalPhoneName() { CanonicalModel = "Lumia 630" } },
            { "RM-979", new CanonicalPhoneName() { CanonicalModel = "Lumia 630" } },
            // Lumia 635
            { "RM-974", new CanonicalPhoneName() { CanonicalModel = "Lumia 635" } },
            { "RM-975", new CanonicalPhoneName() { CanonicalModel = "Lumia 635" } },
            { "RM-1078", new CanonicalPhoneName() { CanonicalModel = "Lumia 635", Comments="Sprint" } },
            // Lumia 526
            { "RM-997", new CanonicalPhoneName() { CanonicalModel = "Lumia 526", Comments="China Mobile" } },
            // Lumia 930
            { "RM-1045", new CanonicalPhoneName() { CanonicalModel = "Lumia 930" } },
            { "RM-1087", new CanonicalPhoneName() { CanonicalModel = "Lumia 930" } },
            // Lumia 636
            { "RM-1027", new CanonicalPhoneName() { CanonicalModel = "Lumia 636", Comments="China" } },
            // Lumia 638
            { "RM-1010", new CanonicalPhoneName() { CanonicalModel = "Lumia 638", Comments="China" } },
            // Lumia 530
            { "RM-1017", new CanonicalPhoneName() { CanonicalModel = "Lumia 530", Comments="Single SIM" } },
            { "RM-1018", new CanonicalPhoneName() { CanonicalModel = "Lumia 530", Comments="Single SIM" } },
            { "RM-1019", new CanonicalPhoneName() { CanonicalModel = "Lumia 530", Comments="Dual SIM" } },
            { "RM-1020", new CanonicalPhoneName() { CanonicalModel = "Lumia 530", Comments="Dual SIM" } },
            // Lumia 730
            { "RM-1040", new CanonicalPhoneName() { CanonicalModel = "Lumia 730", Comments="Dual SIM" } },
            // Lumia 735
            { "RM-1038", new CanonicalPhoneName() { CanonicalModel = "Lumia 735" } },
            { "RM-1039", new CanonicalPhoneName() { CanonicalModel = "Lumia 735" } },
            { "RM-1041", new CanonicalPhoneName() { CanonicalModel = "Lumia 735", Comments="Verizon" } },
            // Lumia 830
            { "RM-983", new CanonicalPhoneName() { CanonicalModel = "Lumia 830" } },
            { "RM-984", new CanonicalPhoneName() { CanonicalModel = "Lumia 830" } },
            { "RM-985", new CanonicalPhoneName() { CanonicalModel = "Lumia 830" } },
            { "RM-1049", new CanonicalPhoneName() { CanonicalModel = "Lumia 830" } },
            // Lumia 535
            { "RM-1089", new CanonicalPhoneName() { CanonicalManufacturer="MICROSOFT", CanonicalModel = "Lumia 535" } },
            { "RM-1090", new CanonicalPhoneName() { CanonicalManufacturer="MICROSOFT", CanonicalModel = "Lumia 535" } },
            { "RM-1091", new CanonicalPhoneName() { CanonicalManufacturer="MICROSOFT", CanonicalModel = "Lumia 535" } },
            { "RM-1092", new CanonicalPhoneName() { CanonicalManufacturer="MICROSOFT", CanonicalModel = "Lumia 535" } },
            // Lumia 435
            { "RM-1068", new CanonicalPhoneName() { CanonicalManufacturer="MICROSOFT", CanonicalModel = "Lumia 435", Comments="DS" } },
            { "RM-1069", new CanonicalPhoneName() { CanonicalManufacturer="MICROSOFT", CanonicalModel = "Lumia 435", Comments="DS" } },
            { "RM-1070", new CanonicalPhoneName() { CanonicalManufacturer="MICROSOFT", CanonicalModel = "Lumia 435", Comments="DS" } },
            { "RM-1071", new CanonicalPhoneName() { CanonicalManufacturer="MICROSOFT", CanonicalModel = "Lumia 435", Comments="DS" } },
            { "RM-1114", new CanonicalPhoneName() { CanonicalManufacturer="MICROSOFT", CanonicalModel = "Lumia 435", Comments="DS" } },
            // Lumia 532
            { "RM-1031", new CanonicalPhoneName() { CanonicalManufacturer="MICROSOFT", CanonicalModel = "Lumia 532", Comments="DS" } },
            { "RM-1032", new CanonicalPhoneName() { CanonicalManufacturer="MICROSOFT", CanonicalModel = "Lumia 532", Comments="DS" } },
            { "RM-1034", new CanonicalPhoneName() { CanonicalManufacturer="MICROSOFT", CanonicalModel = "Lumia 532", Comments="DS" } },
            { "RM-1115", new CanonicalPhoneName() { CanonicalManufacturer="MICROSOFT", CanonicalModel = "Lumia 532", Comments="DS" } },
            // Lumia 640
            { "RM-1072", new CanonicalPhoneName() { CanonicalManufacturer="MICROSOFT", CanonicalModel = "Lumia 640" } },
            { "RM-1073", new CanonicalPhoneName() { CanonicalManufacturer="MICROSOFT", CanonicalModel = "Lumia 640" } },
            { "RM-1074", new CanonicalPhoneName() { CanonicalManufacturer="MICROSOFT", CanonicalModel = "Lumia 640" } },
            { "RM-1075", new CanonicalPhoneName() { CanonicalManufacturer="MICROSOFT", CanonicalModel = "Lumia 640" } },
            { "RM-1077", new CanonicalPhoneName() { CanonicalManufacturer="MICROSOFT", CanonicalModel = "Lumia 640" } },
            { "RM-1109", new CanonicalPhoneName() { CanonicalManufacturer="MICROSOFT", CanonicalModel = "Lumia 640" } },
            { "RM-1113", new CanonicalPhoneName() { CanonicalManufacturer="MICROSOFT", CanonicalModel = "Lumia 640" } },
            // Lumia 640XL
            { "RM-1062", new CanonicalPhoneName() { CanonicalManufacturer="MICROSOFT", CanonicalModel = "Lumia 640 XL" } },
            { "RM-1063", new CanonicalPhoneName() { CanonicalManufacturer="MICROSOFT", CanonicalModel = "Lumia 640 XL" } },
            { "RM-1064", new CanonicalPhoneName() { CanonicalManufacturer="MICROSOFT", CanonicalModel = "Lumia 640 XL" } },
            { "RM-1065", new CanonicalPhoneName() { CanonicalManufacturer="MICROSOFT", CanonicalModel = "Lumia 640 XL" } },
            { "RM-1066", new CanonicalPhoneName() { CanonicalManufacturer="MICROSOFT", CanonicalModel = "Lumia 640 XL" } },
            { "RM-1067", new CanonicalPhoneName() { CanonicalManufacturer="MICROSOFT", CanonicalModel = "Lumia 640 XL" } },
            { "RM-1096", new CanonicalPhoneName() { CanonicalManufacturer="MICROSOFT", CanonicalModel = "Lumia 640 XL" } },
            // Lumia 540
            { "RM-1140", new CanonicalPhoneName() { CanonicalManufacturer="MICROSOFT", CanonicalModel = "Lumia 540" } },
            { "RM-1141", new CanonicalPhoneName() { CanonicalManufacturer="MICROSOFT", CanonicalModel = "Lumia 540" } },
            // Lumia 430 
            { "RM-1099", new CanonicalPhoneName() { CanonicalManufacturer="MICROSOFT", CanonicalModel = "Lumia 430", Comments="DS" } },
            // Lumia 950
            { "RM-1104", new CanonicalPhoneName() { CanonicalManufacturer="MICROSOFT", CanonicalModel = "Lumia 950", Comments="DS" } },
            { "RM-1105", new CanonicalPhoneName() { CanonicalManufacturer="MICROSOFT", CanonicalModel = "Lumia 950", Comments="DS" } },
            { "RM-1118", new CanonicalPhoneName() { CanonicalManufacturer="MICROSOFT", CanonicalModel = "Lumia 950", Comments="DS" } },
            // Lumia 950 XL
            { "RM-1085", new CanonicalPhoneName() { CanonicalManufacturer="MICROSOFT", CanonicalModel = "Lumia 950 XL" } },
            { "RM-1116", new CanonicalPhoneName() { CanonicalManufacturer="MICROSOFT", CanonicalModel = "Lumia 950 XL", Comments="DS" } },
            // Lumia 550
            { "RM-1127", new CanonicalPhoneName() { CanonicalManufacturer="MICROSOFT", CanonicalModel = "Lumia 550" } },
            { "RM-1128", new CanonicalPhoneName() { CanonicalManufacturer="MICROSOFT", CanonicalModel = "Lumia 550" } },
            // Lumia 650
            { "RM-1152", new CanonicalPhoneName() { CanonicalManufacturer="MICROSOFT", CanonicalModel = "Lumia 650" } },
            { "RM-1154", new CanonicalPhoneName() { CanonicalManufacturer="MICROSOFT", CanonicalModel = "Lumia 650", Comments="DS" } },
        };
    }

    public sealed class CanonicalPhoneName
    {
        public string ReportedManufacturer { get; set; }
        public string ReportedModel { get; set; }
        public string CanonicalManufacturer { get; set; }
        public string CanonicalModel { get; set; }
        public string Comments { get; set; }
        public bool IsResolved { get; set; }

        public string FullCanonicalName
        {
            get { return CanonicalManufacturer + " " + CanonicalModel; }
        }
    }
}
