/*
*
*  USAGE: INCLUDE WITH  <script type="text/javascript" src="js/MWBConfig.js"></script> in your index.html
*  You should call this function in your device ready event (when all the cordova plugins are loaded). Our scanner gets exposed as scanner
*  NOTE: You don't have to use exactly this file, it's here as an example how to set different scanner parameters. If you are developing for ionic you will probably do this in a controller, or for ionic2 in a component ts file
*
*/
scannerConfig = function(){

    /* phonegap/cordova 3.* possible callback
    *  - here we have a straight forwards callback one that just alerts the value. When scannerConfig is called, it will set this callback as default and scanner.startScanner can be called without inline callbacks
    *    however users still have the option to not even user setCallback, and set a callback function directly passed as parameter to the scanner.startScanner()
    *
    * In case of successful scan result object is json array of items with following structure
    * result.code - string representation of barcode result
    * result.type - type of barcode detected or 'Cancel' if scanning is canceled
    * result.bytes - bytes array of raw barcode result
    * result.isGS1 - (boolean) barcode is GS1 compliant
    * result.location - contains rectangle points p1,p2,p3,p4 with the corresponding x,y
    * result.imageWidth - Width of the scanned image
    * result.imageHeight - Height of the scanned image
    * result.barcodeWidth;
    * result.barcodeHeight;
    * result.pdfRowsCount;
    * result.pdfColumnsCount;
    * result.pdfECLevel;
    * result.pdfIsTruncated;
    * result.pdfCodewords;
    */
    mwbScanner.setCallback(function(result){
      if (result.type == "Error"){
          navigator.notification.alert(result.code, function(){}, "Error", 'Close');
      }
      else if (result.type == "Cancel" || result.type == "NoResult"){
          alert("No Read" + result.code);
          //Perform some action on scanning canceled if needed
      }
      else
      {
          var resultText = "";

          result.forEach(function (item, index){
              resultText += item.type + (item.isGS1 ? " (GS1)" : "") + ": " + item.code + "\n";
          });

          navigator.notification.alert(resultText, function(){}, "Result", 'Close');
      }
    });

    /* ionic 1 possible callback,
    *   - here we have an angularJS controller with an input field to which an ng-model="barcoderesult" is attached
    *   - upon successful scan we update that model and the result is shown in the input field
    *
    *
    */
    // mwbScanner.setCallback(function(result){
    //   if(result && result.code){
    //     //you need to wrap into apply so that view will update the result immediately
    //     $scope.$apply(function(){
    //       $scope.barcoderesult = result.code;
    //     });
    //   }
    //   else
    //     console.log('No Result');
    // });


    // Some predefined settings, uncomment out the ones you want to use it; Scanner gets initialized with every symbology enabled
    var mw_c =  mwbScanner.getConstants()
        , settings = [
//            {'method': 'MWBsetActiveCodes', 'value' : [mw_c.MWB_CODE_MASK_DM | mw_c.MWB_CODE_MASK_39 | mw_c.MWB_CODE_MASK_93 | mw_c.MWB_CODE_MASK_QR | mw_c.MWB_CODE_MASK_128 | mw_c.MWB_CODE_MASK_PDF]},
//            {"method" : 'MWBenableZoom', "value" : [true]},
//            {"method" : 'MWBsetZoomLevels', "value" : [200, 400, 1]},
//            // {"method" : 'MWBsetInterfaceOrientation', "value" : [mw_c.OrientationLandscapeLeft]},
//            {"method" : 'MWBsetOverlayMode', "value" : [mw_c.OverlayModeImage]},
//            {"method" : 'MWBsetLevel', "value" : [3]}, //3 will try to scan harder than the default which is 2
//            {"method" : 'MWBenableHiRes','value' : [true]}, //possible setting
//            {"method" : 'MWBenableFlash','value' : [true]}, //possible setting
//            {"method" : 'MWBuse60fps','value' : [true]}, //possible
//            {"method" : "MWBsetScanningRect", "value" : [mw_c.MWB_CODE_MASK_25, 2, 2, 96, 96]},
//            {"method" : "MWBsetScanningRect", "value" : [mw_c.MWB_CODE_MASK_39, 2, 2, 96, 96]},
//            {"method" : "MWBsetScanningRect", "value" : [mw_c.MWB_CODE_MASK_93, 2, 2, 96, 96]},
//            {"method" : "MWBsetScanningRect", "value" : [mw_c.MWB_CODE_MASK_128, 2, 2, 96, 96]},
//            {"method" : "MWBsetScanningRect", "value" : [mw_c.MWB_CODE_MASK_AZTEC, 20, 2, 60, 96]},
//            {"method" : "MWBsetScanningRect", "value" : [mw_c.MWB_CODE_MASK_DM, 20, 2, 60, 96]},
//            {"method" : "MWBsetScanningRect", "value" : [mw_c.MWB_CODE_MASK_EANUPC, 2, 2, 96, 96]},
//            {"method" : "MWBsetScanningRect", "value" : [mw_c.MWB_CODE_MASK_PDF, 2, 2, 96, 96]},
//            {"method" : "MWBsetScanningRect", "value" : [mw_c.MWB_CODE_MASK_QR, 20, 2, 60, 96]},
//            {"method" : "MWBsetScanningRect", "value" : [mw_c.MWB_CODE_MASK_RSS, 2, 2, 96, 96]},
//            {"method" : "MWBsetScanningRect", "value" : [mw_c.MWB_CODE_MASK_CODABAR, 2, 2, 96, 96]},
//            {"method" : "MWBsetScanningRect", "value" : [mw_c.MWB_CODE_MASK_DOTCODE, 30, 20, 40, 60]},
//            {"method" : "MWBsetScanningRect", "value" : [mw_c.MWB_CODE_MASK_11, 2, 2, 96, 96]},
//            {"method" : "MWBsetScanningRect", "value" : [mw_c.MWB_CODE_MASK_MSI, 2, 2, 96, 96]},
//            {"method" : "MWBsetScanningRect", "value" : [mw_c.MWB_CODE_MASK_MAXICODE, 20, 2, 60, 96]},
//            {"method" : "MWBsetScanningRect", "value" : [mw_c.MWB_CODE_MASK_POSTAL, 2, 2, 96, 96]},
//            {"method" : "MWBsetMinLength", "value" : [mw_c.MWB_CODE_MASK_25, 5]},
//            {"method" : "MWBsetMinLength", "value" : [mw_c.MWB_CODE_MASK_MSI, 5]},
//            {"method" : "MWBsetMinLength", "value" : [mw_c.MWB_CODE_MASK_39, 5]},
//            {"method" : "MWBsetMinLength", "value" : [mw_c.MWB_CODE_MASK_CODABAR, 5]},
//            {"method" : "MWBsetMinLength", "value" : [mw_c.MWB_CODE_MASK_11, 5]},
//            {"method" : 'MWBsetDirection', "value" : [mw_c.MWB_SCANDIRECTION_VERTICAL | mw_c.MWB_SCANDIRECTION_HORIZONTAL]}
//            {"method" : "MWBsetFlags", "value" : [0, mw_c.MWB_CFG_GLOBAL_ENABLE_MULTI]} //With this flag multicode mode is enabled
        ];

        //multi-platform keys object
        var keys = {
            'Android'   : "VALID_ANDROID_KEY",
            'iOS'       : "VALID_IOS_KEY",
            'Win32NT'   : "VALID_WIN_WP8_KEY",
            'windows'   : "VALID_WIN_10_UWP_KEY"
        };
        //resolve the key for this platform
        var key = (keys[device.platform])?keys[device.platform]:'';

        //sets your key and loads your settings
        return mwbScanner.setKey(key).then(function(response){
            if(response)
                console.log('VALID KEY');
            else
                console.log('INVALID KEY');

            return mwbScanner.loadSettings(settings)
                        .then(function(response){
                            //console.log(response); //the response is the settings array
                        })
                        .catch(function(reason){
                            console.log(reason)
                        });
        });


}