using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.ComponentModel;
using BarcodeLib;



namespace BarcodeScanners
{
    public class ScannerResult
    {
        public string code { get; set; }
        public string type { get; set; }
        public byte[] bytes { get; set; }
        public bool isGS1 { get; set; }
        public Object location { get; set; }
        public int imageWidth { get; set; }
        public int imageHeight { get; set; }
    }

        class PointF
        {
            public float x;
            public float y;
        }

        class MWLocation
        {
            public PointF p1;
            public PointF p2;
            public PointF p3;
            public PointF p4;

            public PointF[] points;

            public MWLocation(float[] _points)
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

        class MWResult
        {
            public String text;
            public byte[] bytes;
           public byte[] encryptedResult;
           public int bytesLength;
            public int type;
            public int subtype;
            public int imageWidth;
            public int imageHeight;
            public Boolean isGS1;
            public MWLocation locationPoints;

            public MWResult()
            {
                text = null;
                bytes = null;
                bytesLength = 0;
                type = 0;
                subtype = 0;
                isGS1 = false;
                locationPoints = null;
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
                                                result.locationPoints = new MWLocation(locations);
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
                                                     }else if(fieldType == Scanner.MWB_RESULT_FT_PARSER_BYTES)
                                                         {
                                                            result.encryptedResult = new byte[fieldContentLength + 1];
                                                            result.encryptedResult[fieldContentLength] = 0;
                                                            Buffer.BlockCopy(buffer, contentPos, result.encryptedResult, 0, fieldContentLength);
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

        class BarcodeHelper
        {

            // public static String resultCode;
            // public static String resultType;
            //  public static byte[] resultBytes;
            public static ScannerResult scannerResult;
            public static bool resultAvailable = false;

            public static Boolean PDF_OPTIMIZED = false;

            public static Windows.Foundation.Rect RECT_LANDSCAPE_1D = new Windows.Foundation.Rect(6, 20, 88, 60);
            public static Windows.Foundation.Rect RECT_LANDSCAPE_2D = new Windows.Foundation.Rect(20, 6, 60, 88);
            public static Windows.Foundation.Rect RECT_PORTRAIT_1D = new Windows.Foundation.Rect(20, 6, 60, 88);
            public static Windows.Foundation.Rect RECT_PORTRAIT_2D = new Windows.Foundation.Rect(20, 6, 60, 88);
            public static Windows.Foundation.Rect RECT_FULL_1D = new Windows.Foundation.Rect(6, 6, 88, 88);
            public static Windows.Foundation.Rect RECT_FULL_2D = new Windows.Foundation.Rect(20, 6, 60, 88);
            public static Windows.Foundation.Rect RECT_DOTCODE = new Windows.Foundation.Rect(30, 20, 40, 60);

            public static void initDecoder()
            {

                // You can now register codes from MWBScanner.js!
                /*  Scanner.MWBregisterCode(Scanner.MWB_CODE_MASK_25, "username", "key");
                  Scanner.MWBregisterCode(Scanner.MWB_CODE_MASK_39, "username", "key");
                  Scanner.MWBregisterCode(Scanner.MWB_CODE_MASK_93, "username", "key");
                  Scanner.MWBregisterCode(Scanner.MWB_CODE_MASK_128, "username", "key");
                  Scanner.MWBregisterCode(Scanner.MWB_CODE_MASK_AZTEC, "username", "key");
                  Scanner.MWBregisterCode(Scanner.MWB_CODE_MASK_DM, "username", "key");
                  Scanner.MWBregisterCode(Scanner.MWB_CODE_MASK_EANUPC, "username", "key");
                  Scanner.MWBregisterCode(Scanner.MWB_CODE_MASK_PDF, "username", "key");
                  Scanner.MWBregisterCode(Scanner.MWB_CODE_MASK_QR, "username", "key");
                  Scanner.MWBregisterCode(Scanner.MWB_CODE_MASK_RSS, "username", "key");
                  Scanner.MWBregisterCode(Scanner.MWB_CODE_MASK_CODABAR, "username", "key");
                Scanner.MWBregisterCode(Scanner.MWB_CODE_MASK_DOTCODE, "username", "key");
                Scanner.MWBregisterCode(Scanner.MWB_CODE_MASK_11, "username", "key");
                Scanner.MWBregisterCode(Scanner.MWB_CODE_MASK_MSI, "username", "key");*/


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
                                              Scanner.MWB_CODE_MASK_QR |
                                              Scanner.MWB_CODE_MASK_CODABAR |
                        //Scanner.MWB_CODE_MASK_DOTCODE |
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

                Scanner.MWBsetResultType(Scanner.MWB_RESULT_TYPE_MW);
            }

            public static void MWBsetScanningRect(int codeMask, Windows.Foundation.Rect rect)
            {
                Scanner.MWBsetScanningRect(codeMask, (float)rect.Left, (float)rect.Top, (float)rect.Width, (float)rect.Height);
            }

            public static Windows.Foundation.Rect MWBgetScanningRect(int codeMask)
            {
                float left, top, width, height;
                Scanner.MWBgetScanningRect(codeMask, out left, out top, out width, out height);

                return new Windows.Foundation.Rect(left, top, width, height);
            }

            public static String getBarcodeName(MWResult result)
            {
                String typeName = "Unknown";
                if (result.type == Scanner.FOUND_128) typeName = "Code 128";
                if (result.type == Scanner.FOUND_39) typeName = "Code 39";
                if (result.type == Scanner.FOUND_DM) typeName = "Datamatrix";
                if (result.type == Scanner.FOUND_EAN_13) typeName = "EAN 13";
                if (result.type == Scanner.FOUND_EAN_8) typeName = "EAN 8";
                if (result.type == Scanner.FOUND_NONE) typeName = "None";
                if (result.type == Scanner.FOUND_RSS_14) typeName = "Databar 14";
                if (result.type == Scanner.FOUND_RSS_14_STACK) typeName = "Databar 14 Stacked";
                if (result.type == Scanner.FOUND_RSS_EXP) typeName = "Databar Expanded";
                if (result.type == Scanner.FOUND_RSS_LIM) typeName = "Databar Limited";
                if (result.type == Scanner.FOUND_UPC_A) typeName = "UPC A";
                if (result.type == Scanner.FOUND_UPC_E) typeName = "UPC E";
                if (result.type == Scanner.FOUND_PDF) typeName = "PDF417";
                if (result.type == Scanner.FOUND_QR) typeName = "QR";
                if (result.type == Scanner.FOUND_AZTEC) typeName = "Aztec";
                if (result.type == Scanner.FOUND_25_INTERLEAVED) typeName = "Code 25 Interleaved";
                if (result.type == Scanner.FOUND_25_STANDARD) typeName = "Code 25 Standard";
                if (result.type == Scanner.FOUND_93) typeName = "Code 93";
                if (result.type == Scanner.FOUND_CODABAR) typeName = "Codabar";
                if (result.type == Scanner.FOUND_DOTCODE) typeName = "Dotcode";
                if (result.type == Scanner.FOUND_128_GS1) typeName = "Code 128 GS1";
                if (result.type == Scanner.FOUND_ITF14) typeName = "ITF 14";
                if (result.type == Scanner.FOUND_11) typeName = "Code 11";
                if (result.type == Scanner.FOUND_MSI) typeName = "MSI Plessey";
			    if (result.type == Scanner.FOUND_25_IATA) typeName = "IATA Code 25";
                if(result.type == Scanner.FOUND_25_MATRIX) typeName = "Code 2/5 Matrix";
                if (result.type == Scanner.FOUND_25_COOP) typeName = "Code 2/5 COOP";
                if (result.type == Scanner.FOUND_25_INVERTED) typeName = "Code 2/5 Inverted";
                if (result.type == Scanner.FOUND_QR_MICRO) typeName = "QR Micro";
                if (result.type == Scanner.FOUND_MAXICODE) typeName = "Maxicode";
                if (result.type == Scanner.FOUND_POSTNET) typeName = "Postnet";
                if (result.type == Scanner.FOUND_PLANET) typeName = "Planet";
                if (result.type == Scanner.FOUND_IMB) typeName = "Intelligent mail";
                if (result.type == Scanner.FOUND_ROYALMAIL) typeName = "Royal mail";


            return typeName;
            }

            static public Byte[] BufferFromImage(BitmapImage imageSource)
            {
                WriteableBitmap wb = new WriteableBitmap(imageSource);

                int px = wb.PixelWidth;
                int py = wb.PixelHeight;

                Byte[] res = new Byte[px * py];

                for (int y = 0; y < py; y++)
                {
                    for (int x = 0; x < px; x++)
                    {
                        int color = wb.Pixels[y * px + x];
                        res[y * px + x] = (byte)color;
                    }
                }

                return res;


            }

            private static void bw_DoWork(object sender, DoWorkEventArgs e)
            {

                BackgroundWorker worker = sender as BackgroundWorker;

                while (1 == 1)
                {
                    if ((worker.CancellationPending == true))
                    {
                        e.Cancel = true;
                        break;
                    }
                    int count = 0;
                    for (int i = 1; i < 1000; i++)
                    {
                        float f1 = (float)((i + 13) * 323);
                        float f2 = (i + 1.12f) / 2.34f;
                        float res = f1 / f2;
                        if (res < 100)
                        {
                            count++;
                        }
                        else
                        {
                            count--;
                        }
                    }

                    threadsCounter++;

                }

            }

            public static int threadsCounter;

            public static int getCPUCores()
            {
                BackgroundWorker bw1 = new BackgroundWorker();
                bw1.WorkerSupportsCancellation = true;

                threadsCounter = 0;

                bw1.DoWork += new DoWorkEventHandler(bw_DoWork);
                bw1.RunWorkerAsync();

                System.Threading.Thread.Sleep(200);

                int singleThreadCount = threadsCounter;

                bw1.CancelAsync();

                threadsCounter = 0;

                BackgroundWorker[] bwm = new BackgroundWorker[8];
                for (int i = 0; i < 8; i++)
                {
                    bwm[i] = new BackgroundWorker();
                    bwm[i].WorkerSupportsCancellation = true;
                    bwm[i].DoWork += new DoWorkEventHandler(bw_DoWork);
                    bwm[i].RunWorkerAsync();
                }


                System.Threading.Thread.Sleep(200);

                int eightThreadsCount = threadsCounter;

                for (int i = 0; i < 8; i++)
                {
                    bwm[i].CancelAsync();
                }

                float performanceRatio = (float)eightThreadsCount / singleThreadCount;

                int cpuCores = 1;

                if (performanceRatio > 7)
                {
                    cpuCores = 8;
                }
                else
                    if (performanceRatio > 3.5)
                    {
                        cpuCores = 4;
                    }
                    else

                        if (performanceRatio > 1.7)
                        {
                            cpuCores = 2;
                        }


                return cpuCores;

            }


        }




    }

