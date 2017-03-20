# PhoneGap implementation (see below for ionic and ionic2)


Guide on how to add the Manatee Works Barcode Scanner SDK PhoneGap plugin to your project(s)

**For more info, visit our website at [www.manateeworks.com/phonegap-plugin](https://manateeworks.com/phonegap-plugin)**

##Install using CLI interface (Phonegap >6.0 and above).          

*First make sure you have the latest software required to run phoneGap apps. This means nodejs and git should be on your system.
For more information about that, visit: [phonegap's getting started.](http://docs.phonegap.com/getting-started/1-install-phonegap/cli/)*

Install PhoneGap:

```ssh
$ sudo npm install -g phonegap@latest
```

Create your app by using CLI interface:
 
```ssh
phonegap create my-mw-app 
```

or use bundle identifiers, we bind our license with the bundle identifier!

```ssh
phonegap create my-mw-app --id "org.mwscanner.sampleapp" --name "mwbScanner"
```
	
Previous step will create a folder named *my-mw-app*, navigate to your newly created folder and add the platforms you want to build with:
	
```ssh
cd my-mw-app
phonegap build android 	//if you are developing an android app
phonegap build ios    //if you are developing an ios app
```
Add our plugin to the project with:

```ssh
phonegap plugin add manateeworks-barcodescanner-v3
```

or   

```ssh
phonegap plugin add https://github.com/manateeworks/phonegap-manateeworks-v3.git
```
or   

```ssh
phonegap plugin add LOCAL_PATH_TO_THE_FOLDER_WITH_PLUGIN (if you are adding from local folder)   
```
    
Perform initial build for each platform.   

```ssh
phonegap build ios
phonegap build android
phonegap build windows
```

### License

Manateeworks Scanner requires a license to work properly. You can get one here:  
[Manateeworks Evaluation License](https://manateeworks.com/lpr?type=evaluation)

There are two ways to set it up:

1.The MW_LICENSE_KEY variable can be added in an xml file respectively for each platform

 - For iOS you can add a row in your *.plist file, with the following format MW_LICENSE_KEY : THE_KEY_FROM_MWDN
![ios-plist](https://cloud.githubusercontent.com/assets/5564013/23270653/da3fda0c-f9f4-11e6-9944-fd3bb246ce59.png)
 - For Android you can use the AndroidManifest.xml file and the meta-data element named MW_LICENSE_KEY and place your key as value instead of KEY_FROM_MWDN
![android-manifest](https://cloud.githubusercontent.com/assets/5564013/23270678/e9836010-f9f4-11e6-9e4e-c780b7af6d94.png)
 - For Windows (UWP) under the WindowsComponnent project you can use the Strings\en-US\Resources.resw file and the element named MW_LICENSE_KEY, where you can set your license in place of YOUR_LICENSE_KEY in the value section
![windows_resw](https://cloud.githubusercontent.com/assets/5564013/23270749/33a2168c-f9f5-11e6-947a-25041ecf5376.png)

2.We also provide setting the key via a JavaScript call, more on that, in the **Setting up your app** section.

### Setting up your app

  Add a button to index.html which will handle the call to the scanning function

```html
<button onClick="mwbScanner.startScanning();" style="width:80%;margin:15%;height:180px">scan</button>
```

The scanner is initialized with default settings.   You can change these settings with the **loadSettings()** method.


For phoneGap apps we include a **MWBConfig.js** where this can be handled. Obviously for your use case you don't need to use this file, and you can provide your own, and you probably will handle it differently, but for the needs of this document let's include it in our **index.html**. It needs to be included with a script tag like:
    
```html
  <script type="text/javascript" src="cordova.js"></script>
  <script type="text/javascript" src="js/index.js"></script>
  <script type="text/javascript" src="js/MWBConfig.js"></script><---add it here!! ->
  <script type="text/javascript">
      app.initialize();
  </script>
```
 
First thing we need to do is setup a valid license with: 
    
```javaScript
  return mwbScanner.setKey('input-your-key-here').then(function(response){
        //response of the setKey action
  });
```
    
   This method returns a promise that resolves to a boolean value which is true if the key was valid, and false in all other cases (invalid appname, invalid key etc).
    
Next we configure the scanner with the desired settings using **loadSettings()**.   
  
```javascript
var mw_c =  mwbScanner.getConstants(),settings;
   settings = [{'method': 'MWBsetActiveCodes', 'value' : [mw_c.MWB_CODE_MASK_25 | mw_c.MWB_CODE_MASK_39 | mw_c.MWB_CODE_MASK_93 | mw_c.MWB_CODE_MASK_128 | mw_c.MWB_CODE_MASK_AZTEC | mw_c.MWB_CODE_MASK_DM | mw_c.MWB_CODE_MASK_EANUPC | mw_c.MWB_CODE_MASK_PDF | mw_c.MWB_CODE_MASK_QR | mw_c.MWB_CODE_MASK_CODABAR | mw_c.MWB_CODE_MASK_11 | mw_c.MWB_CODE_MASK_MSI | mw_c.MWB_CODE_MASK_RSS | mw_c.MWB_CODE_MASK_MAXICODE | mw_c.MWB_CODE_MASK_POSTAL]}];
```
The function  **expects an array of key/value objects** used to set preferences for the scanner. The **key** is the name of the **method** and the **value** is the **parameters** (passed as an array) expected by that method.  

```javascript
return mwbScanner.loadSettings(settings).then(function(response){
			console.log(response);
		}).catch(function(reason){
			console.log(reason);
		});
```

#### How to scan an image

Instead of mwbScanner.startScanning() use:

```javascript
mwbScanner.scanImage(URI);
```
        
or with custom init and callback:

```javascript
mwbScanner.scanImage(URI,function(result){
	//custom callback 
});
```
        
Params:   
```javascript        
URI                     - the path to the image
callback                - custom callback function
```        
        
#### How to scan in partial screen view

Instead of mwbScanner.startScanning() use:

```javascript
mwbScanner.startScanning(x, y, width, height);
```
        
        
or with a custom init and callback:

```javascript    
mwbScanner.startScanning(function(result){
//custom callback
}, x, y, width, height);
```        
        
Params:   
        
```
x, y, width, height     - rectangle of the view in percentages relative to the screen size
callback  - result callback

TODO: ADD HOW TO SET PARAMS WITH SETTINGS CALLS

```

Example:   

     Scan fullscreen  -  mwbScanner.startScanning()
     Scan in view     -  mwbScanner.startScanning(0,4,100,50)
     Pause/Resume     -  mwbScanner.togglePauseResume()
     Close            -  mwbScanner.closeScanner()
     Flash            -  mwbScanner.toggleFlash()
     Zoom             -  mwbScanner.toggleZoom()


## Configuration parameters
    

```javascript
@name "MWBsetActiveCodes"  Sets active or inactive status of decoder types     
@param[in]	activeCodes   ORed bit flags (MWB_CODE_MASK_...) of decoder types to be activated.

  @n       MWB_CODE_MASK_NONE
  @n       MWB_CODE_MASK_QR
  @n       MWB_CODE_MASK_DM
  @n       MWB_CODE_MASK_RSS
  @n       MWB_CODE_MASK_39
  @n       MWB_CODE_MASK_EANUPC
  @n       MWB_CODE_MASK_128
  @n       MWB_CODE_MASK_PDF
  @n       MWB_CODE_MASK_AZTEC
  @n       MWB_CODE_MASK_25
  @n       MWB_CODE_MASK_93
  @n       MWB_CODE_MASK_CODABAR
  @n       MWB_CODE_MASK_DOTCODE
  @n       MWB_CODE_MASK_11
  @n       MWB_CODE_MASK_MSI
  @n       MWB_CODE_MASK_MAXICODE
  @n       MWB_CODE_MASK_POSTAL
  @n       MWB_CODE_MASK_ALL   

CodeMask constants are available for all codeMask variables

@name "MWBsetActiveSubcodes"  Set active subcodes for given code group flag.  Subcodes under some decoder type are all activated by default.

@param[in]  codeMask    Single decoder type/group (MWB_CODE_MASK_...)
@param[in]  subMask     ORed bit flags of requested decoder subtypes (MWB_SUBC_MASK_)

@name "MWBsetFlags"   Sets active or inactive status of decoder types    
@param[in]   codeMask   Single decoder type (MWB_CODE_MASK_...)
@param[in]   flags      ORed bit mask of selected decoder type options (MWB_FLAG_...)

@name "MWBsetMinLength" configures minimum result length for decoder type specified in codeMask.
@param[in]   codeMask   Single decoder type (MWB_CODE_MASK_...)
@param[in]   minLength  Minimum result length for selected decoder type

@name "MWBsetDirection" 
@param[in]   direction   ORed bit mask of direction modes given with MWB_SCANDIRECTION_... bit-masks
@n     MWB_SCANDIRECTION_HORIZONTAL - horizontal lines
@n     MWB_SCANDIRECTION_VERTICAL - vertical lines
@n     MWB_SCANDIRECTION_OMNI - omnidirectional lines
@n     MWB_SCANDIRECTION_AUTODETECT - enables BarcodeScanners autodetection of barcode direction

@name "MWBsetScanningRect"
Sets the scanning rectangle
Parameters are interpreted as percentage of image dimensions, i.e. ranges are 0 - 100 for all parameters.
@param[in]   codeMask    Single decoder type selector (MWB_CODE_MASK_...)
@param[in]   left        X coordinate of left edge (percentage)
@param[in]   top         Y coordinate of top edge (percentage)
@param[in]   width       Rectangle witdh (x axis) (percentage)
@param[in]   height      Rectangle height (y axis) (percentage)

@name "MWBsetLevel"
Effort level of the scanner values can be 
@param[in]   level     1,2,3,4 and 5
example : [{"method":"setLevel" : "value" : [3]}]    

@name "MWBsetInterfaceOrientation"
Sets prefered User Interface orientation of scanner screen
@param[in]   orientation
@n     OrientationPortrait    
@n     OrientationLandscapeLeft
@n     OrientationLandscapeRight
default is OrientationPortrait

@name "MWBsetOverlayMode"
@param[in]    OverlayMode
@n  OverlayModeNone     No overlay is displayed
@n  OverlayModeMW       Use MW Dynamic Viewfinder with blinking line
@n  OverlayModeImage    Show image on top of camera preview
example : [{"method":"MWBsetOverlayMode" : "value" : [mw_c.OverlayModeImage]}]    

@name "MWBresizePartialScanner"
Resizes partial scanner dimensions. If usePartialScanner is true the scanner will open in a window with these dimensions
@param[in]   left      X coordinate of left edge (percentage)
@param[in]   top       Y coordinate of top edge (percentage)
@param[in]   width     Rectangle witdh (x axis) (percentage)
@param[in]   height    Rectangle height (y axis) (percentage)
example : [{"method":"MWBresizePartialScanner" : "value" : [0,0,50,50]}]    

@name "MWBusePartialScanner"
Boolean value that opens a partial scanner if set true
@param[in]   bool               true/false
example : [{"method":"MWBusePartialScanner" : "value" : [true]}]

@name "MWBsetActiveParser"
Set active parser types
@param[in]    ActiveParser    ORed values
@n      MWP_PARSER_MASK_NONE
@n      MWP_PARSER_MASK_AUTO
@n      MWP_PARSER_MASK_GS1
@n      MWP_PARSER_MASK_IUID
@n      MWP_PARSER_MASK_ISBT
@n      MWP_PARSER_MASK_AAMVA
@n      MWP_PARSER_MASK_HIBC
@n      MWP_PARSER_MASK_SCM    
example : [{"method":"MWBsetActiveParser" : "value" : [mw_c.MWP_PARSER_MASK_GS1 | mw_c.MWP_PARSER_MASK_IUID]}]

//additional settings:

@name "MWBsetBlinkingLineVisible"
Set blinking line visible
Default value is true
@param[in]	visible
example : [{"method" : "MWBsetBlinkingLineVisible" : "value" : [true]}]

@name "MWBsetPauseMode"
What happens when the scanner is paused
Default value is PM_PAUSE
@param[in]	pauseMode
@n	PM_NONE             - Nothing happens
@n	PM_PAUSE            - Blinking lines are replaced with a pause view
@n	PM_STOP_BLINKING    - Blinking lines stop blinking
example : [{"method" : "MWBsetPauseMode" : "value" : [mw_c.PM_STOP_BLINKING]}]

@name "MWBenableHiRes"
Enable or disable high resolution scanning. It is recommended to enable it when target barcodes
are of high density or small footprint. If device does not support high resolution param will be ignored
Default value is true (enabled)
@param[in]	enableHiRes
example : [{"method" : "MWBenableHiRes" : "value" : [true]}]

@name "MWBenableFlash"
Enable or disable flash toggle button on scanning screen. If device does not support flash mode
button will be hidden regardles of param
Default value is true (enabled)
@param[in]	enableFlash
example : [{"method" : "MWBenableFlash" : "value" : [true]}]

@name "MWBturnFlashOn"
Set default state of flash (torch) when scanner activity is started
Default value is false (disabled)
@param[in]	flashOn
example : [{"method" : "MWBturnFlashOn" : "value" : [false]}]

@name "MWBtoggleFlash"
Toggle on/off flash state
example : [{"method" : "MWBtoggleFlash" : "value" : []}]

@name "MWBenableZoom"
Enable or disable zoom button on scanning screen. If device does not support zoom,
button will be hidden regardles of param. Zoom is not supported on Windows Phone 8
as there is no zooming api available!
Default value is true (enabled)
@param[in]	enableZoom
example : [{"method" : "MWBenableZoom" : "value" : [true]}]

@name "MWBsetZoomLevels"
Set two desired zoom levels in percentage and initial level. Set first two params to zero for default
levels. On iOS, first zoom level is set to maximum non-interpolated level available on device, and
second is double of first level. On Android, default first zoom is 150% and second is 300%. Zoom is
not supported on Windows Phone 8 as there is no zooming api available! On Windows 10 UWP phone devices,
default values are half the maximum device supported zoom level for the first zoom and maxinum device
supported zoom level for the second zoom. Zoom levels are scalars, so values are expected to be in 
the [1, max] range, for example 1.0 (no zoom) and 4.0 (400% zoom).
Initial zoom level can be 0 (100% - non zoomed), 1 (zoomLevel1) or 2 (zoomLevel2). Default is 0.
@param[in]	zoomLevel1
@param[in]	zoomLevel2
@param[in]	initialZoomLevel
example : [{"method" : "MWBsetZoomLevels" : "value" : [150, 300, 0]}] //android
example : [{"method" : "MWBsetZoomLevels" : "value" : [2.0, 4.0, 0]}] //windows uwp

@name "MWBtoggleZoom"
Toggle on/off zoom state
example : [{"method" : "MWBtoggleZoom" : "value" : []}]

@name "MWBsetMaxThreads"
Set maximum threads to be used for decoding. Value will be limited to maximum available CPU cores.
Default is 4 (will trim to max available value). Set to 1 to disable multi-threaded decoding
@param[in]	maxThreads
example : [{"method" : "MWBsetMaxThreads" : "value" : [2]}]

@name "MWBcloseScannerOnDecode"
Enable/disable continuous scanning. If 'shouldClose' is 'false', result callback will be performed and
scanner will be paused. The User can call 'resumeScanning' to continue scanning, or 'closeScanner'
for closing the scanner. Default is 'true'.
Function is not available on WP8 and UWP due to the technical limitations.
@param[in]	shouldClose
example : [{"method" : "MWBcloseScannerOnDecode" : "value" : [true]}]

@name "MWBresumeScanning"
Resume scanning. Use this method if already using MWBcloseScannerOnDecode(false).
Function is not available on WP8 and UWP due to the technical limitations.
example : [{"method" : "MWBresumeScanning" : "value" : []}]

@name "MWBcloseScanner"
Close scanner. Use this method if already using MWBcloseScannerOnDecode(false).
Function is not available on WP8 due to the technical limitations.
example : [{"method" : "MWBcloseScanner" : "value" : []}]

@name "MWBuse60fps"
Use 60 fps when available.
Function is only available on iOS.
Default value is false
@param[in]	use
example : [{"method" : "MWBuse60fps" : "value" : [false]}]

@name "MWBscanImage"
Scan image.
imageURI - path to the image to be scanned.
@param[in]	imageURI
example : [{"method" : "MWBscanImage" : "value" : ['image.jpg']}]

@name "MWBsetParam"
Set custom decoder param id / value pair for decoder type specified in a codeMask.
@param[in]	codeMask                Single decoder type (MWB_CODE_MASK_...)
@param[in]	paramId                 ID of param
@param[in]	paramValue              Integer value of param
example : [{"method" : "MWBsetParam" : "value" : [codeMask, paramId, paramValue]}]

@name "MWBtogglePauseResume"
Pause/unpause scanner view
example : [{"method" : "MWBtogglePauseResume" : "value" : []}]

@name "MWBduplicateCodeDelay"
Ignore result if scanned the same code in continuous scanning mode
@param[in]	delay         Time interval between 2 scan results with the same result.code in milliseconds
example : [{"method" : "MWBduplicateCodeDelay" : "value" : [1000]}]

@name "MWBuseAutoRect"
Use auto generated full screen scanning rectangle, or use user defined scanning rectangles
@param[in]	useAutoRect   Whether or not to use auto generated full screen scanning rectangle
Default value is true
example : [{"method" : "MWBuseAutoRect" : "value" : [true]}]

@name "MWBuseFrontCamera"
Use front facing camera
@param[in]	useFrontCamera   Whether or not to use front facing camera
Default value is false
example : [{"method" : "MWBuseFrontCamera" : "value" : [false]}]

```

## Windows (UWP) specifics

### Requirements

 - Visual Studio with Universal Windows App Development Tools
 - MWBarcodeLibUniversal SDK W10 extension
(You can download it from https://manateeworks.com/files/download_latest/mobiScan-Windows, and then install 10.0\MWBCameraDemo\MWBarcodeLibUniversalSDK.vsix)

### Post-build settings (required)
 - In the solution explorer, set the project **CordovaApp.Windows10 (Universal Windows)** as startup project.
 - In this project’s directory, open the file **package.windows10.appxmanifest** and in the capabilities tab check **Webcam** from the list.
 - On some devices, there might be a problem with deployment and starting the scanner which can be fixed by
removing the following line from the **index.html** file.
```html
<meta http-equiv="Content-Security-Policy" content="default-src * 'unsafe-inline'; style-src 'self' 'unsafe-inline'; media-src *" />
```

### Functionalities

 - The function **scanImage()** requires image files to be placed in the www folder.
 - The usage of front camera is currently not available.

### Performance
The camera capture and preview are implemented in JavaScript on the web side. Windows doesn't seem to support non-native implementation well, and this results in lower frame rate than hardware available. This can be improved by using a lower camera resolution (480p instead of the default 720p), which can be set with the method **MWBenableHiRes** and **false** as value. Some devices with slower processors could further improve their frame rate by reducing the number of cores the decoder uses (default is all), by using the method **MWBsetMaxThreads** with a value **lower than all CPU cores but not lower than 1**.

# IONIC 1 Implementation


Manateeworks barcode scanner can be loaded as an ionic plugin, which uses cordova, so every phonegap plugin could run as an ionic plugin with little to none change. Most of the changes are in the configuration files, due to how the projects are organized in ionic (or even ionic2)

Just like phoneGap, 

First we ensure we have nodejs installed and latest ionic and cordova see (http://ionicframework.com/getting-started/) for more:

```ssh
npm install -g cordova ionic
//depending on your OS, this could very well be:
sudo npm install -g cordova ionic
```   
if it's already installed, updating is done with:
```ssh
npm update -g cordova ionic
```
or
```ssh
sudo npm update -g cordova ionic
```

Then we create an ionic app (similar to the phonegap process, where we *created* an app, here we *start* one). 
Currently ionic is using v2, and ionic v1 may be completely retired, but for the purpose of clarity we provide these steps to getting an ionic v1 app running.

```ssh
ionic start -a "Manateeworks Barcode Scanner Ionic App" mwbScanner blank -i com.ionic.manateeworks --v1
cd mwbScanner
ionic plugin add manateeworks-barcodescanner-v3 --variable MW_LICENSE_KEY=YOUR_LICENSE_KEY
```

<center>or</center>

```ssh
ionic plugin add https://github.com/manateeworks/phonegap-manateeworks-v3.git --variable MW_LICENSE_KEY=YOUR_LICENSE_KEY        
```

<center>or</center>

```ssh
ionic plugin add LOCAL_PATH_TO_THE_FOLDER_WITH_PLUGIN --variable MW_LICENSE_KEY=YOUR_LICENSE_KEY (if you are adding from local folder)  
```

In the platforms www folder (url is an example if you are developing for ios)  
    
```ssh
$PATH_TO_FOLDER/mMwApp/platforms/ios/platforms/ios/www/examples/ionic
```
there are example files named exactly like the files you need to change to get the app started. 
*app.js* should be replaced with the one in *js/app.js* and *index.html* should be replaced with the *index.html* of your app.

Demo app NOW available [HERE!](https://github.com/manateeworks/manateeworks-barcodescanner-starter)
    
# Ionic 2

Step 1 is the same as ionic, so:

```ssh
ionic start myMwApp blank --v2
cd myMwApp
ionic plugin add manateeworks-barcodescanner-v3 --variable MW_LICENSE_KEY=YOUR_LICENSE_KEY
```
which will create an ionic 2 blank app. This will create a slightly different project structure than phonegap or ionic 1.

App development in ionic2 is a little different, due to the fact that angularJS2 is used, so project structure is quite different than regular phonegap, or even ionic 1.

If you are building for ionic 2 this is not news to you so we will presume that you are familiarized with the structure of your app.
To build for ionic 2 you need to modify the files in your **src** folder. Ionic 2 pushes typescript usage, so whatever you code needs to be compiled to be used as normal javaScript. 

You should have:

```ssh
/src/pages/home folder 
```

We want to modify the **home.html** file, and add a button and a text input (which will show the result) and the home.ts file which will handle the control of the scanner. These files have been copied in the examples folder when you added our plugin. 
Copy the files to your ```ssh /src/pages/home folder```.

Important thing to notice is the addition of the 

```javascript
declare var mwbScanner:any; 
```

this is done, to stop ionic2 from complaining about mwbScanner missing. 
The mwbScanner variable becomes available after the cordova plugins load, so in order to "import" it to our component controller, we define it as a variable that can receive any value.

Since manateeworks-barcodescanner plugin is essentially a phonegap plugin, naturally we use a callback to show results. But we also return a promise, which is more an ionic2 way. To disable the default callback we need to do:

```javascript
mwbScanner.setCallback(function(result){});
```
Then to show results from a scan we simply do

```javascript
mwbScanner.startScanning().then(function(response){
	console.log('show the result here');
	console.log(response);
	//actual example in home.ts is different
});
```

Build for your platform
```ssh
ionic build ios
```
    
And run your app.

Demo app with a more proper ionic solution is NOW available [HERE.](https://github.com/manateeworks/manateeworks-barcodescanner-ionic2-starter) In this solution we wrap the cordova plugin in an ionic provider.


#EXAMPLES

We've added a minimum set of files that you need to replace, to help you setup your app as soon as possible (FOR TESTING PURPOSES).

[examples.zip](https://github.com/manateeworks/phonegap-manateeworks-v3/files/797274/examples.zip)



