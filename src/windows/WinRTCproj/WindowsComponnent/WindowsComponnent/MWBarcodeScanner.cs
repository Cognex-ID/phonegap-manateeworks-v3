using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BarcodeLib;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Media.Capture;
using Windows.ApplicationModel.Resources;

//using  MWBarcodeRuntimeComponent;

namespace WindowsComponnent
{
    public sealed class MWBarcodeScanner
    {
        public static string getLibVersionString()
        {
            int version = BarcodeLib.Scanner.MWBgetLibVersion();

            //char* result = (char *)malloc(20 * sizeof(char));
            string result;

            int v1 = (version >> 16);
            int v2 = (version >> 8) & 0xff;
            int v3 = (version & 0xff);

            //sprintf(result, "%d.%d.%d", v1, v2, v3);
            /*result = std::to_string(v1);
            result += ".";
            result += std::to_string(v2);
            result += ".";
            result + std::to_string(v3);

            wchar_t* wcstring = new wchar_t[result.length() + 1];
            size_t convertedChars = 0;

            mbstowcs_s(&convertedChars, wcstring, result.length(), result.c_str(), _TRUNCATE);
                */
            string str = null;


            return str;
        }

        public static int getLibVersion()
        {
            return Scanner.MWBgetLibVersion();
        }

        public static int getSupportedCodes()
        {
            return Scanner.MWBgetSupportedCodes();
        }

        public static int setActiveCodes(int codeMask)
        {
            return Scanner.MWBsetActiveCodes(codeMask);
        }

        public static int getActiveCodes()
        {
            return Scanner.MWBgetActiveCodes();
        }

        public static int setActiveSubcodes(int codeMask, int subcodeMask)
        {
            return Scanner.MWBsetActiveSubcodes(codeMask, subcodeMask);
        }

        public static int setFlags(int codeMask, int flags)
        {
            return Scanner.MWBsetFlags(codeMask, flags);
        }

        public static int setMinLength(int codeMask, int minLength)
        {
            return Scanner.MWBsetMinLength(codeMask, minLength);
        }

        public static int registerSDK(string key)
        {
            var licenceKey = "YOUR_LICENSE_KEY";

            if (key.Length > 5) licenceKey = key;
            else
            {
                try
                {
                    var loader = ResourceLoader.GetForCurrentView("WindowsComponnent/Resources"); //this namespace path is very crucial
                    licenceKey = loader.GetString("MW_LICENSE_KEY");
                }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex.Message); }
            }

            return Scanner.MWBregisterSDK(licenceKey);
        }

        public static int setLevel(int level)
        {
            return Scanner.MWBsetLevel(level);
        }

        public static int setDirection(uint direction)
        {
            return Scanner.MWBsetDirection(direction);
        }

        public static int getDirection()
        {
            return Scanner.MWBgetDirection();
        }

        public static int setScanningRect(int codeMask, float left, float top, float width, float height)
        {
            return Scanner.MWBsetScanningRect(codeMask, left, top, width, height);
        }

        public static int getScanningRect(int codeMask, out float left, out float top, out float width, out float height)
        {
            return Scanner.MWBgetScanningRect(codeMask, out left, out top, out width, out height);
        }

        public static int getResultType()
        {
            return Scanner.MWBgetResultType();
        }

        public static int setResultType(int resultType)
        {
            return Scanner.MWBsetResultType(resultType);
        }

        public static int getLastType()
        {
            return Scanner.MWBgetLastType();
        }

        public static int isLastGS1()
        {
            return Scanner.MWBisLastGS1();
        }

        public static int scanGrayscaleImage([ReadOnlyArray()]byte[] pp_image, int lenX, int lenY, [WriteOnlyArray()] byte[] result)
        {
            return Scanner.MWBscanGrayscaleImage(pp_image, lenX, lenY, result);
        }

        /*custom cs functions*/
		
        public static void setMaxThreads(int maxThreads)
        {
            int nThreads = ScannerPage.getHardwareThreads();
            if (nThreads < maxThreads || maxThreads < 1) maxThreads = nThreads;
            ScannerPage.maxThreads = maxThreads;
        }

        public static void enableParser(bool enableParser)
        {
            ScannerPage.USE_MWPARSER = enableParser;
        }

        public static void setActiveParser(int activeParser)
        {
            ScannerPage.MWPARSER_MASK = activeParser;
        }

        public static void togglePauseResume()
        {
            ScannerPage.pauseDecoder = !ScannerPage.pauseDecoder;
        }

        public static void setParam(int codeMask, int paramId, int paramValue)
        {
            Scanner.MWBsetParam(codeMask, paramId, paramValue);
        }
    }

    public sealed class MWBarcodeParser
    {

        public static string getLibVersionString()
        {
            uint version = (uint) Scanner.MWBgetLibVersion();

            //char* result = (char *)malloc(20 * sizeof(char));
            string result;

            uint v1 = (version >> 16);
            uint v2 = (version >> 8) & 0xff;
            uint v3 = (version & 0xff);

            //sprintf(result, "%d.%d.%d", v1, v2, v3);
            /*    result = std::to_string(v1);
                result += ".";
                result += std::to_string(v2);
                result += ".";
                result + std::to_string(v3);

                wchar_t* wcstring = new wchar_t[result.length() + 1];
                size_t convertedChars = 0;

                mbstowcs_s(&convertedChars, wcstring, result.length(), result.c_str(), _TRUNCATE);
        */
            string str = null; //ref new Platform::String(wcstring);
           
            return str;
        }


        public static uint getLibVersion()
        {
            return (uint)Scanner.MWPgetLibVersion();
        }

        public static uint getSupportedParsers()
        {
            return (uint)Scanner.MWPgetSupportedParsers();
        }

        /*public static int registerParser(uint parserMask, string userName, string key)
        {
            return Scanner.MWBregisterSDK( key );
        }*/

        public static double getFormattedText(int parser_type, [ReadOnlyArray()] byte[] p_input, int inputLength, [WriteOnlyArray()] byte[]  pp_output)
        {
            return Scanner.MWPgetFormattedText(parser_type, p_input, pp_output);
        }

        public static double getJSON(int parser_type, [ReadOnlyArray()] byte[] p_input, int inputLength, [WriteOnlyArray()]byte[]  pp_output)
        {
            return Scanner.MWPgetJSON(parser_type, p_input, pp_output);
        }

    }
}
