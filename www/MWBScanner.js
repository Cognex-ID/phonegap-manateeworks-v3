
/*
Version 3.0.49
  - iOS restored missing params in MWResult.h that were removed in 3.0.48

Version 3.0.48
  - Android and iOS libs updated to v3.3.1

Version 3.0.47
  - iOS Flash button will not have a fixed frame, instead it will use the MWScannerViewController.xib file

  Version 3.0.46
  - Added australian postal subcode
	 - Decoder updated to 3.3.0
	 
  Version 3.0.45
  - Added Code 32 support
  - Added UPCE don't expand flag
	 - Decoder updated to 3.2.3
  
  Version 3.0.44
  - iOS Fixed scanning rects losing their values
  - iOS Fixed partial view to use XIB layout
  - iOS Fixed partial view initial zoom

  Version 3.0.43
  -Android fixed null pointer when not setting orientation in MWBConfig.js

  Version 3.0.41
  -MWBsetInterfaceOrientation can take two arguments for Landscape (OrientationLandscapeLeft and OrientationLandscapeRight). 
    ex: // {"method" : 'MWBsetInterfaceOrientation', "value" : [mw_c.OrientationLandscapeLeft,mw_c.OrientationLandscapeRight]} used when both landscape orientation should be available for your app, so your scanner can start in the correct orientation

  Version 3.0.39
  
  -Native libs QR fix
  -Fixed orientation on startup on iOS
  
  Version 3.0.38
  
  -Android OS 8.0 Oreo compatibility fix
  
  Version 3.0.37
  
  -Bug fixes
     - Decoder updated to 3.2.1
  -Large Dotcode support
  -Dotcode location fix
  -Code128 trialing Group Separtor fix
  
  Version 3.0.36
  
  -Bug fixes
  -Fixed continuous scanning
  -Removed es6-promise-plugin dependency
     - Decoder updated to 3.2.0
  -Added Micro PDF417 subcode

  Version 3.0.33
  
  -Bug fixes
     - Decoder updated to 3.1.5
  -Added basic scan count statistics
  -Added location/verification support for Code 11 and MSI Plessey
  -Added option to strip GS1 prefixes from GS1 Databar barcodes
  -Added option to turn verification on/off by barcode type
  -Enhanced Code 25 algorithms for fewer false reads
  -Fixed SDK state information tracking on Android
  -Image pre-processing (for Illumination equalization) can now be disabled
  -All SDK licensing features now implemented for Windows
  
  Version 3.0.26

  - Added getDeviceID : returns a promise that contains the deviceID
  usage:
    mwbScanner.getDeviceID()
        .then(function(deviceID){
            console.log("Device ID is: "+deviceID);
        });
  - Decoder updated to 3.1.4

  Version 3.0.0

    Re-vamp of the whole plugin
  - added promises to the function calls, we no longer expect callbacks except for the callback from the scanner result, because that one is convinient
  - you can now set your license key wihout changing this file. It's not important if the key is for android,ios or anyother platform, it uses the same function
  - you can now attach a callback function for results setCallback
  - now able to attach a custom setup function (not a callback function but acts like one). It's called with the context of the BarcodeScanner object, so wherver it is defined it uses the this keyword so all setting calls such as mwbs['MWBuseAutoRect'](false); are changed to this['MWBuseAutoRect'](false); inside this function. This function can be used with initConfig(fn)
  - decoder is initialized with initDecoder (no longer needed to pass the init anonymous function everytime we want to use the scanner)
  - license keys can be set from info.plist and android manifest files


 Version 2.1.0

    - Pause mode
 - Resize option
 - Bug fixes
    - Decoder updated to 3.1.0
 - Added support for three additional Code 25 variants: Matrix, COOP, and Inverted
 - Added support for MaxiCode symbology
 - Added support for Micro QR symbology
 - Added support for POSTNET, PLANET, IMB, and Royal Mail postal codes
 - Added support for Kanji for both Standard and Micro QR codes
 - Added support for GS1 DotCode
 - Added Structured Carrier Message (MaxiCode) to Parser Plugin.
 - Greatly improved decoding performance for low-light or unevenly lit barcodes
 - Improved Direct Part Marking (DPM) detection for Data Matrix barcodes
 - Improved performance of the PDF417 decoder
 - Improved performance of the Data Matrix decoder
 - Improved performance for Code 25 detection

    Version 2.0.2

 - Decoding library updated to 3.0
 - The registration functions have been revamped. License credentials issued prior to version 3.0 will no longer work with this and future releases.
 - UPC/EAN decoder options now support a flag to disable add-on scanning
 - Barcode location support has been implemented for 1D barcodes (Codabar, Code 25, Code 39, Code 93, Code 128, EAN & UPC) - not enabled by default, can be activated by using mwbs['MWBsetFlags'](0, constants.MWB_CFG_GLOBAL_CALCULATE_1D_LOCATION);
 - PDF417 decoding has improved damage resistance making it easier to scan damaged codes
 - Greatly improved the recognition of dotted Data Matrix
 - Rectangular Data Matrix codes with DMRE extension now supported
 - Better recognition of Code 39 stop pattern
 - Other bugfixes and performance improvements
 Version 1.9

 - Parsers

 Version 1.8.8

 - Added support for android API 23 app permissions
 - Bugfixes

 Version 1.8.7

 - Added option to set scanning rectangle for partial view scanning. To use it just add the following line to the scanner configuration:

 mwbs['MWBuseAutoRect'](false);

 - Added new feature that makes possible scanning in view:

 scanner.startScanning(x, y, width, height);
 //all parameters represent percentages relative to the screen size

 - Other methods for partial screen scanning control:

 scanner.togglePauseResume() - toggle pause resume scanning
 scanner.closeScanner()       - stop and remove scanner view
 scanner.toggleFlash()       - toggle flash on/off
 scanner.toggleZoom()        - toggle zoom in/out


 Version 1.7

 - Added scanImage(URI) which can be used for image scanning. Optionally, the method can be used with custom init and callback - scanImage(MWBSInitSpace.init,MWBSInitSpace.callback,URI);

 URI                     - the path to the image
 MWBSInitSpace.init      - scanner initialisation
 MWBSInitSpace.callback  - result callback


 Version 1.6

 - Added continuous scanning functionality:

 - Added support for 64bit android devices.
 - Camera overlay bug fixes.
 Decoder updates:
 - DM non-centric scanning upgrade (still with fixed number of testing locations)
 - GS1 updates:
 - Code 128 FNC1 not included in result, only GS1 compliance is set
 - DM - proper handling of FNC1 and ECI support added
 - Fixed non-symetric viewfinder (for all orientations) in ios/android native and phonegap
 - Fixed PDF trialing characters on all platforms for specific samples


 Version 1.5

 - Added multi-threading support. By default decoder will use all available CPU cores on device. To limit the number
 of used threads, use new function:  MWBsetMaxThreads: function (maxThreads)

 Version 1.4.1

 - Structure of plugin reorganized to fit plugman specifications

 Version 1.4

 - Added support for custom init and callback functions. All init and callback function can still be declared here,
 but users can now use an outside Javascript file that they can maintain during updates, so that they don't lose
 their changes when they update.
 To use the custom script, they only need to include it in the index file like so:
 <script type="text/javascript" src="js/MWBConfig.js"></script>
 To call the scanner with the custom init and callback you use scanner.startScanning(MWBSInitSpace.init,MWBSInitSpace.callback);

 - Added MWBsetCustomParam: function - allows user to put some custom key/value pair which can be used later from native code

 - Added ITF-14 support

 - Added Code 11 support

 - Added MSI Plessey support

 - Added GS1 support


 Version 1.3
 Copyright (c) 2014 Manatee Works. All rights reserved.

 Changes in 1.3:

 - Zoom feature added for iOS and Android. It's not supported on WP8 due to API limitation.

 - Added function to turn Flash ON by default

 - Fixed 'frameworks was not added to the references' on WP8

 - Fixed freezing if missing org.apache.cordova.device plugin

 - Added x86 lib for Android



 Changes in 1.2:

 - Registering calls moved from native code to MWBScanner.js

 You can now enter your licensing info without changing the native code of plugin;

 - Import package_name.R manually after adding Android plugin is not necessary anymore

 - Decoding library updated to 2.9.31



 Changes in 1.1:

 - Advanced Overlay (MWBsetOverlayMode: function(overlayMode)

 You can now choose between Simple Image Overlay and MW Dynamic Overlay, which shows the actual
 viewfinder, depending on selected barcode types and their respective scanning rectangles;

 - Orientation parameter (MWBsetInterfaceOrientation: function(interfaceOrientation))

 Now there's only a single function for supplying orientation parameters which makes tweaking the
 controller for changing scanner orientation no longer needed;

 - Enable or disable high resolution scanning (MWBenableHiRes: function(enableHiRes))

 Added option to choose between high or normal resolution scanning to better match user
 application requirements;

 - Flash handling (MWBenableFlash: function(enableFlash))

 Added option to enable or disable the flash toggle button;


 */

 var CONSTANTS = {
        /**
        * @name Basic return values for API functions
        * @{
        */
        MWB_RT_OK :                     0,
        MWB_RT_FAIL :                  -1,
        MWB_RT_NOT_SUPPORTED :         -2,
        MWB_RT_BAD_PARAM :             -3,



        /** @brief  Code39 decoder flags value: require checksum check
        */
        MWB_CFG_CODE39_REQUIRE_CHECKSUM :  0x2,
        /**/

        /** @brief  Code39 decoder flags value: don't require stop symbol - can lead to false results
        */
        MWB_CFG_CODE39_DONT_REQUIRE_STOP : 0x4,
        /**/

        /** @brief  Code39 decoder flags value: decode full ASCII
        */
        MWB_CFG_CODE39_EXTENDED_MODE :      0x8,
        /**/
		
		/** @brief  Code39 decoder flags value: Try decoding result to CODE32. if failed, Code39 will return
		*/
		MWB_CFG_CODE39_CODE32_ENABLED :      0x10,
		/**/

		/** @brief  Code39 decoder flags value: ADD 'A' prefix to Code32 result
		*/
		MWB_CFG_CODE39_CODE32_PREFIX :      0x20,
		/**/

        /** @brief  Code93 decoder flags value: decode full ASCII
        */
        MWB_CFG_CODE93_EXTENDED_MODE :      0x8,
        /**/


        /** @brief  Code25 decoder flags value: require checksum check
        */
        MWB_CFG_CODE25_REQ_CHKSUM :        0x1,
        /**/

        /** @brief  Code11 decoder flags value: require checksum check
        *  MWB_CFG_CODE11_REQ_SINGLE_CHKSUM is set by default
        */
        MWB_CFG_CODE11_REQ_SINGLE_CHKSUM:         0x1,
        MWB_CFG_CODE11_REQ_DOUBLE_CHKSUM:         0x2,
        /**/

        /** @brief  MSI Plessey decoder flags value: require checksum check
        *  MWB_CFG_MSI_REQ_10_CHKSUM is set by default
        */
        MWB_CFG_MSI_REQ_10_CHKSUM :                 0x01,
        MWB_CFG_MSI_REQ_1010_CHKSUM :               0x02,
        MWB_CFG_MSI_REQ_11_IBM_CHKSUM :             0x04,
        MWB_CFG_MSI_REQ_11_NCR_CHKSUM :             0x08,
        MWB_CFG_MSI_REQ_1110_IBM_CHKSUM :           0x10,
        MWB_CFG_MSI_REQ_1110_NCR_CHKSUM :           0x20,
        /**/

        /** @brief  Codabar decoder flags value: include start/stop symbols in result
        */
        MWB_CFG_CODABAR_INCLUDE_STARTSTOP :        0x1,
        /**/



        /** @brief  Barcode decoder param types
        */
        MWB_PAR_ID_RESULT_PREFIX :       0x10,
        MWB_PAR_ID_ECI_MODE :            0x08,
        /**/

        /** @brief  Barcode param values
        */

        MWB_PAR_VALUE_ECI_DISABLED :    0x00, //default
        MWB_PAR_VALUE_ECI_ENABLED :     0x01,

        MWB_PAR_VALUE_RESULT_PREFIX_NEVER :   0x00, // default
        MWB_PAR_VALUE_RESULT_PREFIX_ALWAYS :  0x01,
        MWB_PAR_VALUE_RESULT_PREFIX_DEFAULT : 0x02,
		
		MWB_PAR_VALUE_VERIFY_LOCATION_OFF :   0x00,
        MWB_PAR_VALUE_VERIFY_LOCATION_ON :  0x01,
        /**/


        /** @brief  UPC/EAN decoder disable addons detection
        */
        MWB_CFG_EANUPC_DISABLE_ADDON :  0x1,
		MWB_CFG_EANUPC_DONT_EXPAND_UPCE :   0x2,
        /**/

        /** @brief  Global decoder flags value: apply sharpening on input image
        */
        MWB_CFG_GLOBAL_HORIZONTAL_SHARPENING :          0x01,
        MWB_CFG_GLOBAL_VERTICAL_SHARPENING :            0x02,
        MWB_CFG_GLOBAL_SHARPENING :                     0x03,

        /** @brief  Global decoder flags value: apply rotation on input image
        */
        MWB_CFG_GLOBAL_ROTATE90 :                       0x04,
        MWB_CFG_GLOBAL_ROTATE180:                       0x08,



        /** @brief  Global decoder flags value: calculate location for 1D barcodeTypes (Code128, Code93, Code39 supported)
        */
        MWB_CFG_GLOBAL_CALCULATE_1D_LOCATION   :  0x10,

        /** @brief  Global decoder flags value: fail 1D decode if result is not confirmed by location expanding (Code128, Code93, Code39 supported)
        */
        MWB_CFG_GLOBAL_VERIFY_1D_LOCATION    :   0x20,
        /** @brief  Global decoder flags value: fail decode if result is not touching the center of viewfinder (2D + Code128, Code93, Code39 supported)
        * 1D locaiton flags will be enabled automatically with this one
        */
        MWB_CFG_GLOBAL_USE_CENTRIC_SCANNING  :   0x40,

        /** @brief  Code39 decoder flags value: require checksum check
        */
        MWB_CFG_CODE39_REQ_CHKSUM            :    0x2,
        /**/


        /**
        * @name Bit mask identifiers for supported decoder types
        * @{ */
        MWB_CODE_MASK_NONE :             0x00000000,
        MWB_CODE_MASK_QR :               0x00000001,
        MWB_CODE_MASK_DM :               0x00000002,
        MWB_CODE_MASK_RSS :              0x00000004,
        MWB_CODE_MASK_39 :               0x00000008,
        MWB_CODE_MASK_EANUPC :           0x00000010,
        MWB_CODE_MASK_128 :              0x00000020,
        MWB_CODE_MASK_PDF :              0x00000040,
        MWB_CODE_MASK_AZTEC :            0x00000080,
        MWB_CODE_MASK_25 :               0x00000100,
        MWB_CODE_MASK_93 :               0x00000200,
        MWB_CODE_MASK_CODABAR :          0x00000400,
        MWB_CODE_MASK_DOTCODE :          0x00000800,
        MWB_CODE_MASK_11 :               0x00001000,
        MWB_CODE_MASK_MSI :              0x00002000,
        MWB_CODE_MASK_MAXICODE:          0x00004000,
        MWB_CODE_MASK_POSTAL:            0x00008000,
        MWB_CODE_MASK_ALL :              0xffffffff,
        /** @} */

        /**
        * @name Basic return values for RegisterSDK function
        * @{
        */
        MWB_RTREG_OK                  : 0,
        MWB_RTREG_INVALID_KEY         : -1,
        MWB_RTREG_INVALID_CHECKSUM    : -2,
        MWB_RTREG_INVALID_APPLICATION : -3,
        MWB_RTREG_INVALID_SDK_VERSION : -4,
        MWB_RTREG_INVALID_KEY_VERSION : -5,
        MWB_RTREG_INVALID_PLATFORM    : -6,
        MWB_RTREG_KEY_EXPIRED         : -7,
        /** @} */

        /**
        * Messages for the return codes
        *
        */
        REGMESSAGES : {
        "0"  : 'Registration OK',
        "-1" : 'Registration Invalid Key',
        "-2" : 'Registration Invalid Checksum',
        "-3" : 'Registration Invalid Application',
        "-4" : 'Registration Invalid SDK Version',
        "-5" : 'Registration Invalid Key Version',
        "-6" : 'Registration Invalid Platform',
        "-7" : 'Registration Key Expired',

        },
        /**
        * @name Bit mask identifiers for RSS decoder types
        * @{ */
        MWB_SUBC_MASK_RSS_14 :           0x00000001,
        MWB_SUBC_MASK_RSS_14_STACK :     0x00000002,
        MWB_SUBC_MASK_RSS_LIM :          0x00000004,
        MWB_SUBC_MASK_RSS_EXP :          0x00000008,
        /** @} */

        /**
        * @name Bit mask identifiers for QR decoder types
        * @{ */
        MWB_SUBC_MASK_QR_STANDARD :      0x00000001,
        MWB_SUBC_MASK_QR_MICRO    :      0x00000002,
        /** @} */
		
		/**
        * @name Bit mask identifiers for PDF decoder types
        * @{ */
        MWB_SUBC_MASK_PDF_STANDARD :      0x00000001,
        MWB_SUBC_MASK_PDF_MICRO    :      0x00000002,
        /** @} */


        /**
        * @name Bit mask identifiers for Code 2 of 5 decoder types
        * @{ */
        MWB_SUBC_MASK_C25_INTERLEAVED :  0x00000001,
        MWB_SUBC_MASK_C25_STANDARD :     0x00000002,
        MWB_SUBC_MASK_C25_ITF14 :        0x00000004,
        MWB_SUBC_MASK_C25_IATA  :        0x00000008,
        MWB_SUBC_MASK_C25_MATRIX :       0x00000010,
        MWB_SUBC_MASK_C25_COOP   :       0x00000020,
        MWB_SUBC_MASK_C25_INVERTED:      0x00000040,

        /** @} */

        /**
        * @name Bit mask identifiers for POSTAL decoder types
        * @{ */
        MWB_SUBC_MASK_POSTAL_POSTNET :   0x00000001,
        MWB_SUBC_MASK_POSTAL_PLANET  :   0x00000002,
        MWB_SUBC_MASK_POSTAL_IM      :   0x00000004,
        MWB_SUBC_MASK_POSTAL_ROYAL   :   0x00000008,
        MWB_SUBC_MASK_POSTAL_AUSTRALIAN :0x00000008,

        /** @} */


        /**
        * @name Bit mask identifiers for UPC/EAN decoder types
        * @{ */
        MWB_SUBC_MASK_EANUPC_EAN_13 :    0x00000001,
        MWB_SUBC_MASK_EANUPC_EAN_8 :     0x00000002,
        MWB_SUBC_MASK_EANUPC_UPC_A :     0x00000004,
        MWB_SUBC_MASK_EANUPC_UPC_E :     0x00000008,
        MWB_SUBC_MASK_EANUPC_UPC_E1 :    0x00000010,
        /** @} */

        /**
        * @name Bit mask identifiers for 1D scanning direction
        * @{ */
        MWB_SCANDIRECTION_HORIZONTAL :   0x00000001,
        MWB_SCANDIRECTION_VERTICAL :     0x00000002,
        MWB_SCANDIRECTION_OMNI :         0x00000004,
        MWB_SCANDIRECTION_AUTODETECT :   0x00000010,
        /** @} */

        FOUND_NONE          :   0,
        FOUND_DM            :   1,
        FOUND_39            :   2,
        FOUND_RSS_14        :   3,
        FOUND_RSS_14_STACK  :   4,
        FOUND_RSS_LIM       :   5,
        FOUND_RSS_EXP       :   6,
        FOUND_EAN_13        :   7,
        FOUND_EAN_8         :   8,
        FOUND_UPC_A         :   9,
        FOUND_UPC_E         :   10,
        FOUND_128           :   11,
        FOUND_PDF           :   12,
        FOUND_QR            :   13,
        FOUND_AZTEC         :   14,
        FOUND_25_INTERLEAVED:   15,
        FOUND_25_STANDARD   :   16,
        FOUND_93            :   17,
        FOUND_CODABAR       :   18,
        FOUND_DOTCODE       :   19,
        FOUND_128_GS1       :   20,
        FOUND_ITF14         :   21,
        FOUND_11            :   22,
        FOUND_MSI           :   23,
        FOUND_25_IATA       :   24,
        FOUND_25_MATRIX     :   25,
        FOUND_25_COOP       :   26,
        FOUND_25_INVERTED   :   27,
        FOUND_QR_MICRO      :   28,
        FOUND_MAXICODE      :   29,
        FOUND_POSTNET       :   30,
        FOUND_PLANET        :   31,
        FOUND_IMB           :   32,
        FOUND_ROYALMAIL     :   33,
        FOUND_MICRO_PDF     :   34,
        FOUND_32	        :   35,
        FOUND_AUSTRALIAN    :   36,



        OrientationPortrait :       'Portrait',
        OrientationLandscapeLeft :  'LandscapeLeft',
        OrientationLandscapeRight : 'LandscapeRight',
        OrientationAll :            'All',
        OverlayModeNone :           0,
        OverlayModeMW :             1,
        OverlayModeImage :          2,
        PM_NONE :                   0,
        PM_PAUSE :                  1,
        PM_STOP_BLINKING :          2,



        /**
        * @name Bit mask identifiers for supported decoder types
        * @{ */
        MWP_PARSER_MASK_NONE :               0x00000000,
        MWP_PARSER_MASK_GS1  :               0x00000001,
        MWP_PARSER_MASK_IUID :               0x00000002,
        MWP_PARSER_MASK_ISBT :               0x00000004,
        MWP_PARSER_MASK_AAMVA:               0x00000008,
        MWP_PARSER_MASK_HIBC :               0x00000010,
        MWP_PARSER_MASK_SCM  :               0x00000020,
        MWP_PARSER_MASK_AUTO :               0x0fffffff,

        /** @} */


        /**
        * @name Partial View Constants, in case we usePartialScanner but don't supply position parameters
        * @{ */

        XPOINT : 0,
        YPOINT : 0,
        WIDTH  : 50,
        HEIGHT : 50
        /** @} */


   };


var BarcodeScanner = {

  /**
  * Init decoder with default params.
  */
  MWBinitDecoder: function(callback) {
    cordova.exec(callback, function(){}, "MWBarcodeScanner", "initDecoder", []);
  },

  /**
  * result.code - string representation of barcode result
  * result.type - type of barcode detected or 'Cancel' if scanning is canceled
  * result.bytes - bytes array of raw barcode result
  * result.isGS1 - (boolean) barcode is GS1 compliant
  * result.location - contains rectangle points p1,p2,p3,p4 with the corresponding x,y
  * result.imageWidth - Width of the scanned image
  * result.imageHeight - Height of the scanned image
  */
  MWBstartScanner: function(callback) {
       cordova.exec(callback, function(err) {callback('Error: ' + err); }, "MWBarcodeScanner", "startScanner", []);
  },

  /**
  * Registers licensing information for all SDK functionality.
  * It should be called once on app startup.
  *
  * @param[in]   key                     License key string
  * @param[in]   callback                Result callback
  *
  */
  MWBregisterSDK: function(key, callback) {
   cordova.exec(callback, function(){}, "MWBarcodeScanner", "registerSDK", [key]);
  },

  /**
  *
  *
  *
  */

  MWBgetDeviceID : function(callback){

    cordova.exec(callback, function(){}, "MWBarcodeScanner", "getDeviceID", []);

  },

  /**
  * Sets active or inactive status of decoder types
  *
  * @param[in]       activeCodes             ORed bit flags (MWB_CODE_MASK_...) of decoder types
  *                                          to be activated.
  */
  MWBsetActiveCodes: function(activeCodes) {
    cordova.exec(function(){}, function(){}, "MWBarcodeScanner", "setActiveCodes", [activeCodes]);
  },

  /**
  * Set active subcodes for given code group flag.
  * Subcodes under some decoder type are all activated by default.
  *
  * @param[in]       codeMask                Single decoder type/group (MWB_CODE_MASK_...)
  * @param[in]       subMask                 ORed bit flags of requested decoder subtypes (MWB_SUBC_MASK_)
  */
  MWBsetActiveSubcodes: function(codeMask, activeSubcodes) {
   cordova.exec(function(){}, function(){}, "MWBarcodeScanner", "setActiveSubcodes", [codeMask, activeSubcodes]);
 },

  /**
  * MWBsetFlags configures options (if any) for decoder type specified in codeMask.
  * Options are given in  flags as bitwise OR of option bits. Available options depend on selected decoder type.
  *
  * @param[in]   codeMask                Single decoder type (MWB_CODE_MASK_...)
  * @param[in]   flags                   ORed bit mask of selected decoder type options (MWB_FLAG_...)
  */
  MWBsetFlags: function(codeMask, flags) {
    cordova.exec(function(){}, function(){}, "MWBarcodeScanner", "setFlags", [codeMask, flags]);
  },

  /**
  * MWBsetMinLength configures minimum result length for decoder type specified in codeMask.
  *
  * @param[in]   codeMask                Single decoder type (MWB_CODE_MASK_...)
  * @param[in]   minLength               Minimum result length for selected decoder type
  */
  MWBsetMinLength: function(codeMask, minLength) {
    cordova.exec(function(){}, function(){}, "MWBarcodeScanner", "setMinLength", [codeMask, minLength]);
  },

  /**
  * This function enables some control over scanning lines choice for 1D barcodes. By ORing
  * available bit-masks user can add one or more direction options to scanning lines set.
  * @n           - MWB_SCANDIRECTION_HORIZONTAL - horizontal lines
  * @n           - MWB_SCANDIRECTION_VERTICAL - vertical lines
  * @n           - MWB_SCANDIRECTION_OMNI - omnidirectional lines
  * @n           - MWB_SCANDIRECTION_AUTODETECT - enables BarcodeScanner's
  *                autodetection of barcode direction
  *
  * @param[in]   direction               ORed bit mask of direction modes given with
  *                                      MWB_SCANDIRECTION_... bit-masks
  */
  MWBsetDirection: function(direction) {
    cordova.exec(function(){}, function(){}, "MWBarcodeScanner", "setDirection", [direction]);
 },

  /**
  * Sets rectangular area for barcode scanning with selected single decoder type.
  * After MWBsetScanningRect() call, all subseqent scans will be restricted
  * to this region. If rectangle is not set, whole image is scanned.
  * Also, if width or height is zero, whole image is scanned.
  *
  * Parameters are interpreted as percentage of image dimensions, i.e. ranges are
  * 0 - 100 for all parameters.
  *
  * @param[in]   codeMask            Single decoder type selector (MWB_CODE_MASK_...)
  * @param[in]   left                X coordinate of left edge (percentage)
  * @param[in]   top                 Y coordinate of top edge (percentage)
  * @param[in]   width               Rectangle witdh (x axis) (percentage)
  * @param[in]   height              Rectangle height (y axis) (percentage)
  */
  MWBsetScanningRect: function(codeMask, left, top, width, height) {
    cordova.exec(function(){}, function(){}, "MWBarcodeScanner", "setScanningRect", [codeMask, left, top, width, height]);
  },

  /**
  * Barcode detector relies on image processing and geometry inerpolation for
  * extracting optimal data for decoding. Higher effort level involves more processing
  * and intermediate parameter values, thus increasing probability of successful
  * detection with low quality images, but also consuming more CPU time.
  *
  * @param[in]   level                   Effort level - available values are 1, 2, 3, 4 and 5.
  *                                      Levels greater than 3 are not suitable fro real-time decoding
  */
  MWBsetLevel: function(level) {
    cordova.exec(function(){}, function(){}, "MWBarcodeScanner", "setLevel", [level]);
  },

  /**
  * Sets prefered User Interface orientation of scanner screen
  * Choose one fo the available values:
  * OrientationPortrait
  * OrientationLandscapeLeft
  * OrientationLandscapeRight
  *
  * Default value is OrientationPortrait
  */
  MWBsetInterfaceOrientation: function(interfaceOrientation, interfaceOrientationOptional) {
    if (typeof interfaceOrientationOptional != "undefined"){
        cordova.exec(function(){}, function(){}, "MWBarcodeScanner", "setInterfaceOrientation", [interfaceOrientation, interfaceOrientationOptional]);
    }else{
        cordova.exec(function(){}, function(){}, "MWBarcodeScanner", "setInterfaceOrientation", [interfaceOrientation]);
    }
  },

  /**
  * Choose overlay graphics type for scanning screen:
  * OverlayModeNone   - No overlay is displayed
  * OverlayModeMW     - Use MW Dynamic Viewfinder with blinking line (you can customize display options
  *                     in native class by changing defaults)
  * OverlayModeImage  - Show image on top of camera preview
  *
  * Default value is OverlayModeMW
  */
  MWBsetOverlayMode: function(overlayMode) {
    cordova.exec(function(){}, function(){}, "MWBarcodeScanner", "setOverlayMode", [overlayMode]);
  },
  /**
  * Set blinking line visible
  * Default value is true
  */
  MWBsetBlinkingLineVisible: function(visible) {
    cordova.exec(function(){}, function(){}, "MWBarcodeScanner", "setBlinkingLineVisible", [visible]);
  },
  /**
  * Set pause mode
  *
  * What happens when the scanner is paused:
  *
  *   PM_NONE             - Nothing happens
  *   PM_PAUSE            - Blinking lines are replaced with a pause view
  *   PM_STOP_BLINKING    - Blinking lines stop blinking
  *
  *   Default value is PM_PAUSE
  */
  MWBsetPauseMode: function(pauseMode) {
    cordova.exec(function(){}, function(){}, "MWBarcodeScanner", "setPauseMode", [pauseMode]);
  },
  /**
  * Enable or disable high resolution scanning. It's recommended to enable it when target barcodes
  * are of high density or small footprint. If device doesn't support high resolution param will be ignored
  *
  * Default value is true (enabled)
  */
  MWBenableHiRes: function(enableHiRes) {
    cordova.exec(function(){}, function(){}, "MWBarcodeScanner", "enableHiRes", [enableHiRes]);
  },

  /**
  * Enable or disable flash toggle button on scanning screen. If device doesn't support flash mode
  * button will be hidden regardles of param
  *
  * Default value is true (enabled)
  */
  MWBenableFlash: function(enableFlash) {
    cordova.exec(function(){}, function(){}, "MWBarcodeScanner", "enableFlash", [enableFlash]);
  },

  /**
  * Set default state of flash (torch) when scanner activity is started
  *
  * Default value is false (disabled)
  */
  MWBturnFlashOn: function(flashOn) {
    cordova.exec(function(){}, function(){}, "MWBarcodeScanner", "turnFlashOn", [flashOn]);
  },
  /**
  * Toggle on/off flash state
  *
  */
  MWBtoggleFlash: function() {
    cordova.exec(function(){}, function(){}, "MWBarcodeScanner", "toggleFlash", []);
  },

  /**
  * Enable or disable zoom button on scanning screen. If device doesn't support zoom,
  * button will be hidden regardles of param. Zoom is not supported on Windows Phone 8
  * as there's no zooming api available!
  *
  * Default value is true (enabled)
  */
  MWBenableZoom: function(enableZoom) {
    cordova.exec(function(){}, function(){}, "MWBarcodeScanner", "enableZoom", [enableZoom]);
  },

  /**
  * Set two desired zoom levels in percentage and initial level. Set first two params to zero for default
  * levels. On iOS, first zoom level is set to maximum non-interpolated level available on device, and
  * second is double of first level. On Android, default first zoom is 150% and second is 300%. Zoom is
  * not supported on Windows Phone 8 as there's no zooming api available!
  * Initial zoom level can be 0 (100% - non zoomed), 1 (zoomLevel1) or 2 (zoomLevel2). Default is 0.
  *
  */
  MWBsetZoomLevels: function(zoomLevel1, zoomLevel2, initialZoomLevel) {
    cordova.exec(function(){}, function(){}, "MWBarcodeScanner", "setZoomLevels", [zoomLevel1, zoomLevel2, initialZoomLevel]);
  },
  /**
   * Toggle on/off zoom state
   *
   */
   MWBtoggleZoom: function() {
     cordova.exec(function(){}, function(){}, "MWBarcodeScanner", "toggleZoom", []);
   },

  /**
  * Set maximum threads to be used for decoding. Value will be limited to maximum available CPU cores.
  * Default is 4 (will trim to max available value). Set to 1 to disable multi-threaded decoding
  */
  MWBsetMaxThreads: function (maxThreads) {
    cordova.exec(function () { }, function () { }, "MWBarcodeScanner", "setMaxThreads", [maxThreads]);
  },


  /**
  * Set custom key:value pair which is accesible from native code.
  */
  MWBsetCustomParam: function(key, value) {
    cordova.exec(function(){}, function(){}, "MWBarcodeScanner", "setCustomParam", [key, value]);
  },
  /**
  * Enable/disable continuous scanning. If 'shouldClose' is 'false', result callback will be performed and
  * scanner will be paused. The User can call 'resumeScanning' to continue scanning, or 'closeScanner'
  * for closing the scanner. Default is 'true'.
  * Function is not available on WP8 due to the technical limitations.
  */
  MWBcloseScannerOnDecode: function(shouldClose) {
    cordova.exec(function(){}, function(){}, "MWBarcodeScanner", "closeScannerOnDecode", [shouldClose]);
  },
  /**
  * Resume scanning. Use this method if already using MWBcloseScannerOnDecode(false).
  * Function is not available on WP8 due to the technical limitations.
  */
  MWBresumeScanning: function() {
    cordova.exec(function(){}, function(){}, "MWBarcodeScanner", "resumeScanning", []);
  },
  /**
  * Close scanner. Use this method if already using MWBcloseScannerOnDecode(false).
  * Function is not available on WP8 due to the technical limitations.
  */
  MWBcloseScanner: function() {
    cordova.exec(function(){}, function(){}, "MWBarcodeScanner", "closeScanner", []);
  },
  /**
  * Use 60 fps when available.
  * Function is only available on iOS.
  * Default is 'false'
  */
  MWBuse60fps: function(use) {
    cordova.exec(function(){}, function(){}, "MWBarcodeScanner", "use60fps", [use]);
  },
  /**
  * Scan image.
  * imageURI - path to the image to be scanned.
  */
  MWBscanImage: function(imageURI, callback) {
    cordova.exec(callback, function(){}, "MWBarcodeScanner", "scanImage", [imageURI]);
  },
  /**
  * Set custom decoder param.
  * MWB_setParam set custom decoder param id/value pair for decoder type specified in \a codeMask.
  * codeMask                Single decoder type (MWB_CODE_MASK_...)
  * paramId                 ID of param
  * paramValue              Integer value of param
  */
  MWBsetParam: function(codeMask, paramId, paramValue) {
    cordova.exec(function(){}, function(){}, "MWBarcodeScanner", "setParam", [codeMask, paramId, paramValue]);
  },
  /**
  * Pause scanner view
  */
  MWBtogglePauseResume: function() {
    cordova.exec(function(){}, function(){}, "MWBarcodeScanner", "togglePauseResume", []);
  },
  /**
  *  Ignore result if scanned the same code in continuous scanning mode
  *
  *  delay         Time interval between 2 scan results with the same result.code in milliseconds
  */
  MWBduplicateCodeDelay: function(delay) {
    cordova.exec(function(){}, function(){}, "MWBarcodeScanner", "duplicateCodeDelay", [delay]);
  },
  /**
  *  Use auto generated full screen scanning rectangle, or use user defined scanning rectangles
  *
  *  useAutoRect   Whether or not to use auto generated full screen scanning rectangle, or use user defined scanning rectangles [true, false]; default: true
  */
  MWBuseAutoRect: function(useAutoRect) {
    cordova.exec(function(){}, function(){}, "MWBarcodeScanner", "setUseAutorect", [useAutoRect]);
  },
  /**
  *  Use front facing camera
  *
  *  useFrontCamera   Whether or not to use front facing camera [true, false]; default: false
  */
  MWBuseFrontCamera: function(useFrontCamera) {
    cordova.exec(function(){}, function(){}, "MWBarcodeScanner", "useFrontCamera", [useFrontCamera]);
  },
  /**
  *  Set active parser type
  *
  *  activeParser  Available options:
  *                   MWP_PARSER_MASK_NONE
  *                   MWP_PARSER_MASK_AUTO
  *                   MWP_PARSER_MASK_GS1
  *                   MWP_PARSER_MASK_IUID
  *                   MWP_PARSER_MASK_ISBT
  *                   MWP_PARSER_MASK_AAMVA
  *                   MWP_PARSER_MASK_HIBC
  *                   MWP_PARSER_MASK_SCM
  *
  */
  MWBsetActiveParser: function(activeParser) {
    cordova.exec(function(){}, function(){}, "MWBarcodeScanner", "setActiveParser", [activeParser]);
  },
  /**
  *  Resize partial scanner view
  *
  *  x, y, width, height (percentage of screen size)
  */
  MWBresizePartialScanner: function(x, y, width, height) {
    cordova.exec(function(){}, function(){}, "MWBarcodeScanner", "resizePartialScanner", [x, y, width, height]);
  },

  /**
  *  @name usePartialScanner
  *
  *  @description
  *    introduced in 3.1 expects a boolean, if true the startScanner will use a partial scanner, default should be false
  *   @zikam
  */
  MWBusePartialScanner: function(b) {
    cordova.exec(function(){}, function(){}, "MWBarcodeScanner", "usePartialScanner", [b]);
  }

};


  /*
  *  private
  *   @name startScanner
  *   @params callback
  *   @description call the bridge function to the native side
  *
  **/
  var startScanner = function(callback) {

    var callback = (typeof callback === 'function') ? callback : this.dflt_clb;

      return new window.Promise(function(resolve,reject){
        BarcodeScanner.MWBstartScanner(function(result){
            callback.call(null,result); //call the callback for backward compatability
            resolve(result); //return a promise
        });
      });
  }

  /**
  * Scanner Defaults to be loaded in the constructor of Scanner
  * TODO: maybe set the callback function to an empty anonymous function
  **/
  var defaults = {
    init_decoder : false,
    valid_key : false,
    dflt_clb : function(result){
        /**
        * result.code - string representation of barcode result
        * result.type - type of barcode detected or 'Cancel' if scanning is canceled
        * result.bytes - bytes array of raw barcode result
        * result.isGS1 - (boolean) barcode is GS1 compliant
        * result.location - contains rectangle points p1,p2,p3,p4 with the corresponding x,y
        * result.imageWidth - Width of the scanned image
        * result.imageHeight - Height of the scanned image
        */
        if (result.type == 'NoResult'){
          //Perform some action on scanning canceled if needed
        }
        else if (result && result.code){
          navigator.notification.alert(result.code, function(){}, result.type + (result.isGS1?" (GS1)":""), 'Close');
        }
    }
  }

/*
* @name
*   Scanner
* @description
*   constructor of the scanner object
* @params
*   key   optional license key
*
*/
var Scanner = function(){

    var self = this;
    //load defaults
    for (var key in defaults) {
        if (defaults.hasOwnProperty(key)) {
          self[key] = defaults[key];
        }
    }

}

  /**
  *  @name initDecoder
  *  @description setups a default decoder
  *
  * @return promise
  */
  Scanner.prototype.initDecoder = function(){
    var that = this;
    return new window.Promise(function(resolve,reject){
      if(!that.init_decoder)
        return BarcodeScanner.MWBinitDecoder(function(){resolve('FRESH_INIT'); that.init_decoder = true; });
      else
        resolve('VETERAN_INIT');
    });
  }

  /**

  *   @name
  *     setKey
  *   @description
  *     sets the license key to be used with our decoder
  *   @params
  *     key : the license key
  *   @return promise that resolves to a boolean value. True if key was valid; false in every other case
  *
  *
  */
  Scanner.prototype.setKey = function(key){
    var that = this;
    if(typeof key === 'undefined' || !key)
        key = '';

    if( this.valid_key ){
      return new window.Promise(function(resolve,reject){
        resolve(that.valid_key);
      })
    }
    else{
      return new window.Promise(function(resolve,reject){
        return BarcodeScanner.MWBregisterSDK(key,function(result){
          console.log(CONSTANTS.REGMESSAGES[parseInt(result)]);
          if( parseInt(result) === 0 )
              that.valid_key = true;

          resolve(that.valid_key);
        })
      });
    }
  }

  /**
  *   @name
  *     setCallback
  *   @description
  *     set a custom callback function that's called once the scan is performed. Should be called in the configuration stage
  *   @params
  *     callback - a callback function with result parameter
  *
  *
  */
 Scanner.prototype.setCallback = function(callback){
    this.dflt_clb = callback;
 }

  /**
  * @name
  *   loadSettings
  * @params
  *   settings - an array of settings objects with the following structure, use this to loadSettings
  *     {"method" : "MWBmethod" , "value" : "array of params that the method expects"}
  *     example:
  *       [
  *         {'method': 'MWBsetActiveCodes', 'value' : [cc.MWB_CODE_MASK_128]},
  *         {'method': 'MWBsetFlags', 'value' : [cc.MWB_CODE_MASK_39, cc.MWB_CFG_CODE39_EXTENDED_MODE]},
  *         {'method': 'MWBsetDirection', 'value' : [cc.MWB_SCANDIRECTION_VERTICAL | cc.MWB_SCANDIRECTION_HORIZONTAL]},
  *         {'method': 'MWBsetScanningRect', 'value' : [cc.MWB_CODE_MASK_39, 20,20,60,60]},
  *         {'method': 'MWBsetMinLength', 'value' : [cc.MWB_CODE_MASK_39, 4]},
  *         {'method': 'MWBsetParam', 'value' : [cc.MWB_CODE_MASK_DM, cc.MWB_PAR_ID_RESULT_PREFIX, cc.MWB_PAR_VALUE_RESULT_PREFIX_ALWAYS]},
  *         {'method': 'MWBsetActiveParser', 'value' : [cc.MWP_PARSER_MASK_ISBT]}
  *       ]
  * @return
  *   promise that resolves with the loaded settings
  *
  */

  Scanner.prototype.loadSettings =function(settings){
    var that = this;
    return new window.Promise(function(resolve,reject){
        if(Array.isArray(settings)){
          for (var i = settings.length - 1; i >= 0; i--) {
            var expression = settings[i];
            BarcodeScanner[expression.method].apply(null,expression.value);
          }
        }
        resolve(settings);
    });
  }
  /**
  *  @name getConstants
  *  @description : exposes the contants of the scanner so we can use them when calling configuration functions
  **/
  Scanner.prototype.getConstants = function(){
    return CONSTANTS;
  }

  /**
  *  @name getDeviceID
  *  @description get the DeviceID that was generated for this device
  *
  **/
  Scanner.prototype.getDeviceID = function(){
      return new window.Promise(function(resolve,reject){
            BarcodeScanner.MWBgetDeviceID(function(result){
                // console.log(result);
                resolve(result);
            });
      });
  }

  /**
    @name scanImage
    @params
        imageUri *required
        callback *optional will get replaced by a default callback if it's missing

    @description scan an image from an URI

    @callback results
    * result.code - string representation of barcode result
    * result.type - type of barcode detected or 'Cancel' if scanning is canceled
    * result.bytes - bytes array of raw barcode result
    * result.isGS1 - (boolean) barcode is GS1 compliant
    * result.location - contains rectangle points p1,p2,p3,p4 with the corresponding x,y
    * result.imageWidth - Width of the scanned image
    * result.imageHeight - Height of the scanned image

  */
  Scanner.prototype.scanImage = function(){

    var args = Array.prototype.slice.call(arguments)
        ,callback = this.dflt_clb
        ,that = this
        ,imageUri = args[0];


      if(args.length == 2)
        if(typeof args[1] === 'function')
            callback = args[1];

    BarcodeScanner.MWBscanImage(imageUri,callback);
  }


  /**
  *   @name startScanning
  *   @description proxy method for starting the scanner with different params (in view/ fullscreen).
        This should be called from outside, usually from an UI element that triggers a click/tap event
  *
  */
  Scanner.prototype.startScanning = function(){

      var settings,
          self = this,
          args = Array.prototype.slice.call(arguments),
          callback = false,
          x = CONSTANTS.XPOINT,
          y = CONSTANTS.YPOINT,
          w = CONSTANTS.WIDTH,
          h = CONSTANTS.HEIGHT;



      if(args.length == 1 || args.length == 2 || args.length == 3){
        if (typeof args[0] !== 'function')
          callback = false;
        else
          callback = args[0];
      }

      if(args.length == 4){
        if(typeof args[0] === 'function'){
          callback = args[0];
        }
        else{
          callback = false;
          x = (typeof args[0] != 'undefined')?args[0]:CONSTANTS.XPOINT;
          y = (typeof args[1] != 'undefined')?args[1]:CONSTANTS.YPOINT;
          w = (typeof args[2] != 'undefined')?args[2]:CONSTANTS.WIDTH;
          h = (typeof args[3] != 'undefined')?args[3]:CONSTANTS.HEIGHT;
        }
      }
      else if(args.length == 5){
        callback = (args[0])?args[0]:false;
        x = (typeof args[1] != 'undefined')?args[1]:CONSTANTS.XPOINT;
        y = (typeof args[2] != 'undefined')?args[2]:CONSTANTS.YPOINT;
        w = (typeof args[3] != 'undefined')?args[3]:CONSTANTS.WIDTH;
        h = (typeof args[4] != 'undefined')?args[4]:CONSTANTS.HEIGHT;

      }

      if(args.length < 4){
        settings = [
          {'method': 'MWBusePartialScanner', 'value' : [false]},
          {'method': 'MWBresizePartialScanner', 'value' : [x,y,w,h]}
        ];
        return this.loadSettings(settings).then(function(response){
            return startScanner.call(self,callback);
        }).catch(function(reason){
          console.log(reason);
        });
      }
      else{
        settings = [
          {'method': 'MWBusePartialScanner', 'value' : [true]},
          {'method': 'MWBresizePartialScanner', 'value' : [x,y,w,h]}
        ];
        return this.loadSettings(settings).then(function(response){
           return startScanner.call(self,callback);
        }).catch(function(reason){
          console.log(reason)
        });

      }
  };
  /*
  * @name closeScanner
  * @description exposes the closeScanner native function, when called it closes the scanner window
  **/
  Scanner.prototype.closeScanner = function(){
    BarcodeScanner.MWBcloseScanner();
  };
  /*
  * @name togglePauseResume
  * @description exposes the togglePauseResume native function, when called it toggles pause/resume of scanning
  **/
  Scanner.prototype.togglePauseResume =function(){
    BarcodeScanner.MWBtogglePauseResume();
  };
  /*
  * @name toggleFlash
  * @description exposes the toggleFlash native function, when called it toggles the flash function of the camera
  **/
  Scanner.prototype.toggleFlash = function(){
    BarcodeScanner.MWBtoggleFlash();
  };
  /*
  * @name toggleZoom
  * @description exposes the toggleZoom native function, toggles the zoom
  **/
  Scanner.prototype.toggleZoom = function(){
    BarcodeScanner.MWBtoggleZoom();
  };
  /*
  * @name resumeScanning
  * @description exposes the resumeScanning native function, resumes scanning after it was paused
  **/
  Scanner.prototype.resumeScanning = function(){
    BarcodeScanner.MWBresumeScanning();
  };
  /*
  * @name setScannerOverlayMode
  * @description exposes the setScannerOverlayMode native function, see MWBsetOverlayMode for more
  **/
  Scanner.prototype.setScannerOverlayMode = function(overlayMode){
    BarcodeScanner.MWBsetOverlayMode(overlayMode);
  };
  /*
  * @name setBlinkingLineVisible
  * @description exposes the setBlinkingLineVisible native function
  **/
  Scanner.prototype.setBlinkingLineVisible = function(visible){
    BarcodeScanner.MWBsetBlinkingLineVisible(visible);
  };
  /*
  * @name resizePartialScanner
  * @description exposes the resizePartialScanner native function. It sets the size of the partial scanner
  **/
  Scanner.prototype.resizePartialScanner = function(x,y,width,height){
    BarcodeScanner.MWBresizePartialScanner(x,y,width,height);
  };

  var scanner = new Scanner();

  document.addEventListener("deviceready", function(result){

        //initialize decoder and get it ready for scanning
        scanner.initDecoder().then(function(response){
            //scanner object initialized
            console.log('MWBScanner Ready!');
        });

  }, false);

  module.exports = scanner;