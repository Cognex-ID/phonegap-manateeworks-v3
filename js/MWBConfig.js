/*
*
*  USAGE: INCLUDE WITH  <script type="text/javascript" src="js/MWBConfig.js"></script> in your index.html
*  You should call this function in your device ready event (when all the cordova plugins are loaded). Our scanner gets exposed as scanner
*  NOTE: You don't have to use exactly this file, it's here as an example how to set different scanner parameters. If you are developing for ionic you will probably do this in a controller, or for ionic2 in a component ts file
*
*/
scannerConfig = function(){

    /* phonegap 3.* possible callback 
    *  - here we have a straight forwards callback one that just alerts the value. When scannerConfig is called, it will set this callback as default and scanner.startScanner can be called without inline callbacks
    *    however users still have the option to not even user setCallback, and set a callback function directly passed as parameter to the scanner.startScanner()
    *
    */
    mwbScanner.setCallback(function(result){
      if(result && result.code){
        alert('type: ' + result.type + 'result: ' + result.code);
      }
      else
        console.log('No Result');
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

    var cc =  mwbScanner.getConstants(),settings;

    settings = [
      // {'method': 'MWBsetActiveCodes', 'value' : [cc.MWB_CODE_MASK_25 | cc.MWB_CODE_MASK_39 | cc.MWB_CODE_MASK_93 | cc.MWB_CODE_MASK_128 | cc.MWB_CODE_MASK_AZTEC | cc.MWB_CODE_MASK_DM | cc.MWB_CODE_MASK_EANUPC | cc.MWB_CODE_MASK_PDF | cc.MWB_CODE_MASK_QR | cc.MWB_CODE_MASK_CODABAR | cc.MWB_CODE_MASK_11 | cc.MWB_CODE_MASK_MSI | cc.MWB_CODE_MASK_RSS | cc.MWB_CODE_MASK_MAXICODE | cc.MWB_CODE_MASK_POSTAL] }
      // {'method': 'MWBsetActiveCodes', 'value' : [cc.MWB_CODE_MASK_25]}
    ];

    // mwbScanner.setKey('you can set a key here'); //set a key to use instead of the one in the manifest/plist files

    //load your settings
    return mwbScanner.loadSettings(settings).catch(function(reason){console.log(reason)});

}
