Manatee Works Barcode Scanner SDK Plugin

PhoneGap implementation (see below for ionic and ionic2)
--------------------------------

Guide on how to add the Manatee Works Barcode Scanner SDK PhoneGap plugin to your project(s)

*For more info, visit our website at [www.manateeworks.com/phonegap-plugin](https://manateeworks.com/phonegap-plugin)*

1. Install using CLI interface (Phonegap >6.0 and above).          

    *First make sure you have the latest software required to run phoneGap apps. This means nodejs and git should be on your system. For more info about that, visit: http://docs.phonegap.com/getting-started/1-install-phonegap/cli/*

 - Install PhoneGap:

            sudo npm install -g phonegap@latest

 - Create your app by using CLI interface:
 
			phonegap create my-mw-app 

            //or use bundle identifiers, we bind our license with the bundle identifier!

            phonegap create my-mw-app --id "org.mwscanner.sampleapp" --name "mwbScanner"

- Previous step will create a folder named *my-mw-app*, navigate to your newly created folder and add the platforms you want to build with:

            cd my-mw-app
			phonegap build android 	//if you are developing an android app
		    phonegap build ios    //if you are developing an ios app

* Add our plugin to the project with:

		 phonegap plugin add manateeworks-barcodescanner

	or   

	    phonegap plugin add https://github.com/manateeworks/phonegap-mwbarcodescanner.git
	or   

	    phonegap plugin add LOCAL_PATH_TO_THE_FOLDER_WITH_PLUGIN (if you are adding from local folder)   
	or  install using plugman: (your platform should be already built)
    
	    plugman install --platform ios|android --project platforms/ios|platforms/android --plugin com.manateeworks.barcodescanner --plugins_dir plugins/ --www www/ 
    
    
   Once you do that, you can set your license key directly in the **plugin.xml** file that's found in the **manateeworks-barcodescanner** folder in the plugins folder see:
    
      APP_PATH/plugins/manateeworks-barcodescanner/plugin.xml
      
    edit this file and set your keys, for android look for:
    
            <meta-data
          android:name="MW_LICENSE_KEY"
          android:value="" /> 
    
    for ios look for:
    
        <config-file target="*-Info.plist" parent="MW_LICENSE_KEY">
            <string>PUT YOUR KEY HERE</string>
        </config-file>
        

    
    If you do that, these params will automatically be created for you for each platform repsectively.
    You can leave these fields empty and build the app, the scanner will stlil work, but the results will be masked. You can set them/change them in the .plist file on ios or the android manifest file on android.
    

* Perform initial build for each platform.   
  You can get errors building, on iOS it can complain about the signing profile.   
    Android could complain that it can't find the right gradle version.

        phonegap build ios
        phonegap build android
        phonegap build wp8

* Add a button to index.html which will handle the call to the scanning function

  ```html
  <button onClick="mwbScanner.startScanning();" style="width:80%;margin:15%;height:180px">scan</button>
  ```

    The scanner is initialized with default settings.   
    You can change these settings with the **loadSettings** method.  
         
* For phoneGap apps we include a **MWBConfig.js** where this can be handled. It needs to be included with a script tag in the index.html file.
    
        <script type="text/javascript" src="cordova.js"></script>
        <script type="text/javascript" src="js/index.js"></script>
        <script type="text/javascript" src="js/MWBConfig.js"></script>  <---add it here!!
        <script type="text/javascript">
            app.initialize();
        </script>   
    
    Here you can do a few things...    
    If you skipped adding your license key to your plugin.xml file, you can set your key with **setKey()**
    
     ```html
       return mwbScanner.setKey('input-your-key-here').then(function(response){
             //response of the setKey action
       });
     ```
    This method returns a promise that resolves to a boolean value which is true if the key was valid, and false in all other cases (invalid appname, invalid key etc).
    
    To configure your scanner with the desired settings you should use **loadSettings()**.   
    Expects an array of key/value objects used to set preferences for the scanner, where the key is the name of the method used and the value is the parameters expected by that method.   
    A list of all possible configuration methods is shown bellow.
    
    ```html
        var cc =  mwbScanner.getConstants(),settings;
        settings = [
        {'method': 'MWBsetActiveCodes', 'value' : [cc.MWB_CODE_MASK_25 | cc.MWB_CODE_MASK_39 | cc.MWB_CODE_MASK_93 | cc.MWB_CODE_MASK_128 | cc.MWB_CODE_MASK_AZTEC | cc.MWB_CODE_MASK_DM | cc.MWB_CODE_MASK_EANUPC | cc.MWB_CODE_MASK_PDF | cc.MWB_CODE_MASK_QR | cc.MWB_CODE_MASK_CODABAR | cc.MWB_CODE_MASK_11 | cc.MWB_CODE_MASK_MSI | cc.MWB_CODE_MASK_RSS | cc.MWB_CODE_MASK_MAXICODE | cc.MWB_CODE_MASK_POSTAL] }
      ];
      //load your settings with
       return mwbScanner.loadSettings(settings).catch(function(reason){console.log(reason)});
    ```

###How to build online with bulid.phonegap.com:

* Copy confing.xml from project’s dir to /www
* Add  this line in www/confing.xml:
    
        <gap:plugin name="manateeworks-barcodescanner" source="npm"/>

* Add this code in www/index.html:

```html
	<form style="width: 100%; text-align: center;">
		<input type="button" value="Scan Barcode" onclick="mwbScanner.startScanning()" style="font-size: 40px; width: 300px; height: 50px; margin-top: 100px;"/>
	</form>
```
* Compress /www folder
* Upload www.zip to build.phonegap.com 
* Build


##How to scan an image

* Instead of mwbScanner.startScanning() use:

        mwbScanner.scanImage(URI);
        
        
    or with custom init and callback:
    
        mwbScanner.scanImage(URI,function(result){//custom callback function});
        
* Params:   
        
        URI                     - the path to the image
        callback                - custom callback function
        
        

##How to scan in partial screen view

* Instead of mwbScanner.startScanning() use:

        mwbScanner.startScanning(x, y, width, height);
        
        
    or with custom init and callback:
    
        mwbScanner.startScanning(function(result){//custom callback}, x, y, width, height);
        

        
* Params:   
        
        x, y, width, height     - rectangle of the view in percentages relative to the screen size
        callback  - result callback

        TODO: ADD HOW TO SET PARAMS WITH SETTINGS CALLS

* Example:   

        Scan fullscreen  -  scanner.startScanning()
        Scan in view     -  scanner.startScanning(0,4,100,50)
        Pause/Resume     -  scanner.togglePauseResume()
        Close            -  scanner.closeScanner()
        Flash            -  scanner.toggleFlash()
        Zoom             -  scanner.toggleZoom()


IONIC Implementation
-------------------

Manateeworks barcode scanner can be loaded as an ionic plugin, which uses cordova, so every phonegap plugin could run as an ionic plugin with little to none change, most of the changes are in the configuration files, due to how the projects are organized in ionic (or even ionic2)

Just like phonegap, 


1. First we ensure we have nodejs installed and latest ionic and cordova
see (http://ionicframework.com/getting-started/) for more
      
        npm install -g cordova ionic
        //depending on your OS, this could very well be:
        sudo npm install -g cordova ionic
    
   if it's already installed, updating is done with:

         npm update -g cordova ionic
        //or 
        sudo npm update -g cordova ionic


2.  Then we create an ionic app (similar to the phonegap process, where we *created* an app, here we *start* one). 
Currently ionic is using v2, and ionic v1 may be completely retired, but for the purpose of clarity we provide these steps to getting an ionic v1 app running.

        ionic start myMwApp blank
        cd myMwApp
        ionic plugin add manateeworks-barcodescanner

     or   

        ionic plugin add https://github.com/manateeworks/phonegap-mwbarcodescanner.git

     or   

        ionic plugin add LOCAL_PATH_TO_THE_FOLDER_WITH_PLUGIN (if you are adding from local folder)  


3. In the platforms www folder (for example if you are developing for ios)  
    
        $PATH_TO_FOLDER/mMwApp/platforms/ios/platforms/ios/www/examples/ionic
  
 
   there are example files named exactly like the files you need to change to get the app started. 

    *app.js* should be replaced with the one in *js/app.js* and *index.html* should be replaced with the *index.html* of your app, to get you started.
    
Ionic 2
=======

Step 1 is the same as ionic, so:

        ionic start myMwApp blank --v2
        cd myMwApp
        ionic plugin add manateeworks-barcodescanner
        
which will create an ionic 2 blank app. This will create a slightly different project structure than phonegap and ionic 1.

App development in ionic2 is a little different, due to the fact that angularJS2 is used, so project structure is quite different than regular phonegap, or ionic1 (which uses angularJS).

If you are building for ionic 2 this is not news to you so we will presume that you are familiarized with the structure of your app.
We will modify the files in the /src folder, if you followed the instructions you should have 

    /src/pages/home folder 

We want to modify the home.html file, and add a button and a text input (which will show the result) and the home.ts file which will handle that control of the scanner. These files have been copied in the examples folder that we provide with the plugin. Copy the files to your /src/pages/home folder.

Important thing to notice is the addition of the 

     declare var mwbScanner:any; 
this is done, to stop ionic2 from complaining about scanner missing. The scanner variable becomes available after the cordova plugins load, so in order to "import" it to our component controller, we define it as a variable that can receive any value.

Since manateeworks-barcodescanner plugin is essentially a phonegap plugin, naturally we use a callback to show results. But we also return a promise, which is more an ionic2 way. To disable the default callback we do:

    mwbScanner.setCallback(function(result){});

Then to show results from a scan we simply do

    mwbScanner.startScanning().then(function(response){
        console.log('show the result here');
        console.log(response);
        //actual example in home.ts is different
    });

Build for your platform

    ionic build ios
    

And run your app.


Changelog
---------

##Important changes in 3.1
Promises are introduced
Methods for easy configuration introduced
setCallback
setKey

##Important change in 2.1.2
        
* UPC/EAN last digit missing fix


##Important change in 2.0
        
* The registration call functions have been completely revamped. License credentials issued prior to v. 3.0 will no longer work with the new and future releases.


##Important change in 1.9

New feature: Parsers. Users can now parse the scanned result. 
    
     mwbs['MWBsetActiveParser'](constants.MWP_PARSER_MASK_ISBT);
        Available options:
               MWP_PARSER_MASK_NONE
               MWP_PARSER_MASK_AUTO 
               MWP_PARSER_MASK_GS1 
               MWP_PARSER_MASK_IUID
               MWP_PARSER_MASK_ISBT
               MWP_PARSER_MASK_AAMVA
               MWP_PARSER_MASK_HIBC
               
##Important change in 1.8.8

Support for android app permissions requires using Cordova-Android 5.0.0+. In order to use our plugin on Cordova-Android <5.0.0 consider downgrading to 1.8.7

##Important change in 1.5

This library is now thread safe and multithreading is enabled. Users have the option to set the maximum number of threads (CPUs) the scanner can use by adding this line in the decoder initialization:

     mwbs['MWBsetMaxThreads'](NUM_OF_MAX_THREADS)
###Important change in 1.4

* Add a button to index.html which will call the scanner:

Users now can put decoder initialization and callback in separate Javascript file, so that they don't lose their changes when they update the plugin. Sample file is *js/MWBConfig.js*.

To use the custom script, user need to include it in the index file like so:

    <script type="text/javascript" src="js/MWBConfig.js"></script>

and start the scanner with:

    scanner.startScanning(MWBSInitSpace.init,MWBSInitSpace.callback);
    
All init and callback function can still be declared inside MWBScanner.js file, but will be overwritten on plugin update.

* Upon license purchase, replace the username/key pairs for the corresponding barcode types in the file 'MWBScanner.js';


&nbsp;


**WP8 Note**

It's seems there's a bug in Phonegap 3.0 so you have to add ```html '<script type="text/javascript" src="cordova.js"></script>' ``` in index.html (or other html files) manually



##Manual Install (Phonegap 2.x or 3.0)

###Android:
&nbsp;


* Create a Phonegap Android app;

* Copy the folder 'src/android/com/manateeworks' to your project's 'src/com/' folder;

* Copy the file 'src/android/res/layout/scanner.xml' to your project's 'res/layout' folder;

* Copy the file 'src/android/res/drawable/overlay_mw.png' to your project's 'res/drawable' folder. Do the same for the file in 'drawable-hdpi' folder;

* Copy the files 'src/android/libs/armeabi/libBarcodeScannerLib.so' and 'Android/libs/armeabi-v7a/libBarcodeScannerLib.so' to your project's 'libs/' folder, all the while preserving the same folder structure 

* Copy the file 'www/MSBScanner.js' to the 'assets/www/js' folder;
 
* Insert the Scanner activity definition into AndroidManifest.xml:
```
 	<activity android:name="com.manateeworks.ScannerActivity"
		android:screenOrientation="landscape" android:configChanges="orientation|keyboardHidden"
		android:theme="@android:style/Theme.NoTitleBar.Fullscreen">
	</activity>
```

* Insert the MWBScanner.js script into index.html:
```
	<script type="text/javascript" src="js/MWBScanner.js"></script> 
```
* Add a test button for calling the scanner to index.html:
```
 	<form style="width: 100%; text-align: center;">
        	    <input type="button" value="Scan Barcode" onclick="startScanning()" style="font-size: 20px; width: 300px; height: 30px; margin-top: 50px;"/>
       </form>
```


* Add the plugin to 'res/xml/config.xml':

For Phonegap 2.x 
```
	<plugins>
		...	
		<plugin name="MWBarcodeScanner" value="com.manateeworks.BarcodeScannerPlugin"/>
        ...
	</plugins>
```

For Phonegap 3 *
```
	<feature name="MWBarcodeScanner">
       		 <param name="android-package" value="com.manateeworks.BarcodeScannerPlugin" />
   	</feature>
```

* Import .R file of your project (import YOUR_APP_PACKAGE_NAME.R;) to the 'src/com/manateeworks/ScannerActivity.java';

* Upon license purchase, replace the username/key pairs for the corresponding barcode types in the file 'src/com/manateeworks/BarcodeScannerPlugin.java';

* Run the app and test the scanner by pressing the previously added button;

* (Optional): You can also replace our default overlay_mw.png for the camera screen with your own customized image;

* (For Phonegap 3) If notification plugin is not present in project, add it by following instructions from this url:

<!-- -->
	http://docs.phonegap.com/en/3.0.0/cordova_notification_notification.md.html

* If not present already, add camera permission to the AndroidManifest.xml:

<!-- -->
	<uses-permission android:name="android.permission.CAMERA" />

*  (For Phonegap 2.x) In BarcodeScannerPlugin.java replace org.apache.cordova reference to org.apache.cordova.api :

	Instead:	

		import org.apache.cordova.CallbackContext;
		import org.apache.cordova.CordovaPlugin;

	Use this:

 		import org.apache.cordova.api.CallbackContext;
		import org.apache.cordova.api.CordovaPlugin;

	
	
&nbsp;
###iOS:
&nbsp;


* Create a Phonegap iOS app;

* Copy all files from our 'src/ios' folder to your project's 'Plugins' folder and add them to the project;

* Copy the file 'www/MSBScanner.js' to the folder 'www/js' . NOTE: You cannot drag & drop directly into the Xcode project... use Finder instead;

* Insert MWBScanner.js script into index.html:
```
	<script type="text/javascript" src="js/MWBScanner.js"></script> 
```
* Add a test button for calling the scanner to index.html:
```
 	<form style="width: 100%; text-align: center;">
        	    <input type="button" value="Scan Barcode" onclick="startScanning()" style="font-size: 20px; width: 300px; height: 30px; margin-top: 50px;"/>
        </form>
```

* Add the plugin to config.xml (from project root, not the one from www folder):

For Phonegap 2.x 
```
	<plugins>
    
		...
		<plugin name="MWBarcodeScanner" value="CDVMWBarcodeScanner"/>
    
		...
	</plugins>
```
For Phonegap 3
```
	<feature name="MWBarcodeScanner">
        	<param name="ios-package" value="CDVMWBarcodeScanner" />
	</feature>
```



* Confirm you have the following frameworks added to your project: CoreVideo, CoreGraphics;

* Upon license purchase, replace the username/key pairs for the corresponding barcode types in the file Plugins/MWBarcodeScanner/MWScannerViewController.m;


* Run the app and test the scanner by pressing the previously added button;


* (Optional): You can replace our default overlay_mw.png and close_button.png for the camera screen with your own customized image;



&nbsp;
###Windows Phone 8:
&nbsp;

* Add (drag & drop) MWBarcodeScanner folder into the project folder named 'plugins'. If needed, create Plugins folder in project previously;

* Copy (this time from Windows Explorer, not by way of drag & drop) to the project BarcodeLib.winmd and BarcodeLib.dll to project root;

* Add (drag & drop) www/MWBScanner.js to www/js/ project folder;

* Insert MWBScanner.js script into index.html:
```
	<script type="text/javascript" src="js/MWBScanner.js"></script> 
```
* Add a button for calling the scanner to index.html:
```
 	<form style="width: 100%; text-align: center;">
 
	 	<input type="button" value="Scan Barcode" onclick="scanner.startScanning()" style="font-size: 40px; width: 300px;height: 50px; margin-top: 100px;"/>
 
	</form>
```
* Add BarcodeLib.winmd to project references: right click on 'References', 'Add Reference', 'Browse' and choose the file;

* Add the plugin to config.xml:

For Phonegap 2.x
```
	<plugins>
    
		...
		<plugin name="MWBarcodeScanner" value="MWBarcodeScanner"/>
    
		...
	</plugins>
```
For Phonegap 3
```
	<feature name="MWBarcodeScanner">
        	<param name="wp-package" value="MWBarcodeScanner" />
	</feature>
```

Add a notification plugin (if not already present):
```
	 <plugin name="Notification" value="Notification"/>
``` 

* (For Phonegap 2.9) Sometimes a bug occurs in Phonegap 2.9.0 with notification dialogs, making them crash on closing. It may be necessary to make a change in the Plugins/Notification.cs file:

	inside function: void btnOK_Click

	replace the following block:

		  NotifBoxData notifBoxData = notifBoxParent.Tag as NotifBoxData;
                  notifyBox = notifBoxData.previous as NotificationBox;
                  callbackId = notifBoxData.callbackId as string;

                  if (notifyBox == null)
                  {
                      page.BackKeyPress -= page_BackKeyPress;
                  }

	with the one below:

		NotifBoxData notifBoxData = notifBoxParent.Tag as NotifBoxData;
                if (notifBoxData != null)
                    {
                        notifyBox = notifBoxData.previous as NotificationBox;
                        callbackId = notifBoxData.callbackId as string;
                        if (notifyBox == null)
                        {
                            page.BackKeyPress -= page_BackKeyPress;
                        }
                    }

* Add ID_CAP_ISV_CAMERA capability into WMAppManifest.xml


* Upon license purchase, replace the username/key pairs for the corresponding barcode types in file Plugins/com.manateeworks.barcodescanner/BarcodeHelper.cs;


* Run the app and test the scanner by pressing the previously added button;


* (Optional): You can replace our default overlay_mw.png for the camera screen with your own customized image;

&nbsp;
###Changes in 2.1:
&nbsp;
- Pause mode
- Resize option
- Bug fixes
- Decoder updated to 3.1.0:
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

&nbsp;
###Changes in 2.0:
&nbsp;
- Decoding library updated to 3.0
- The registration functions have been revamped. License credentials issued prior to version 3.0 will no longer work with this and future releases.
- UPC/EAN decoder options now support a flag to disable add-on scanning
- Barcode location support has been implemented for 1D barcodes (Codabar, Code 25, Code 39, Code 93, Code 128, EAN & UPC) - not enabled by default, can be activated by using mwbs['MWBsetFlags'](0, constants.MWB_CFG_GLOBAL_CALCULATE_1D_LOCATION);
- PDF417 decoding has improved damage resistance making it easier to scan damaged codes
- Greatly improved the recognition of dotted Data Matrix 
- Rectangular Data Matrix codes with DMRE extension now supported
- Better recognition of Code 39 stop pattern
- Other bugfixes and performance improvements

&nbsp;
###Changes in 1.9:
&nbsp;
- Added Parsers: GS1, IUID, ISBT, AAMVA, HIBC
- Bug fixes

&nbsp;
###Changes in 1.8.8:
&nbsp;
- Added support for android API 23 app permissions:
- Bug fixes

&nbsp;
###Changes in 1.8.6:
&nbsp;
- Added option for using the front facing camera:

        mwbs['MWBuseFrontCamera'](true);
        
- Bug fixes

&nbsp;
###Changes in 1.8.2:
&nbsp;
- Added option to set scanning rectangle for partial view scanning. To use it just add the following line to the scanner configuration:

        mwbs['MWBuseAutoRect'](false);
        
- Bug fixes

&nbsp;
###Changes in 1.8:
&nbsp;
- Added new feature that makes possible scanning in view:

        scanner.startScanning(x, y, width, height); 
        //all parameters represent percentages relative to the screen size
        
- Other methods for partial screen scanning control:

        scanner.togglePauseResume() - toggle pause resume scanning
        scanner.closeScanner()       - stop and remove scanner view
        scanner.toggleFlash()       - toggle flash on/off
        scanner.toggleZoom()        - toggle zoom in/out


&nbsp;
###Changes in 1.7.1:
&nbsp;
- Added flags for including symbology identifiers in results


&nbsp;
###Changes in 1.7:
&nbsp;
- Added scanImage(URI) which can be used for image scanning. Optionally, the method can be used with custom init and callback  - scanImage(MWBSInitSpace.init,MWBSInitSpace.callback,URI);

        URI                     - the path to the image
        MWBSInitSpace.init      - scanner initialisation
        MWBSInitSpace.callback  - result callback


&nbsp;
###Changes in 1.6:
&nbsp;
- Added continuous scanning functionality:

        mwbs['MWBcloseScannerOnDecode'](false)  - to enable continuous scanning
        scanner.resumeScanning()                - for resuming after successful scan
        scanner.closeScanner()                   - to finish with continuous scanning
    
- Added support for 64bit android devices.
- Camera overlay bug fixes.

&nbsp;
###Changes in 1.5:
&nbsp;
- Added support for multithreading. The user can set the maximum number of threads by adding this line in the decoder initialization:
    
        mwbs['MWBsetMaxThreads'](NUM_OF_MAX_THREADS)
  
- Added MWBsetMinLength: function - allows user to set the minimum length of the code for weak protected code types (like: Code 25, MSI, Code 39, Codabar, Code 11...) to avoid false detection of short barcode fragments. This method can be used by adding this line in the decoder intialization:

        mwbs['MWBsetMinLength'](constants.MWB_CODE_MASK, MIN_LENGTH);

- Plugin is now plugman compatible
- Added IATA Code 25 support
- Improved detection of Databar Expanded barcode type

&nbsp;

###Changes in 1.4:
&nbsp;
- Added support for custom init and callback functions. All init and callback function can still be declared here, but users can now use an outside Javascript file that they can maintain during updates, so that they don't lose their changes when they update.
    To use the custom script, they only need to include it in the index file like so:

        <script type="text/javascript" src="js/MWBConfig.js"></script>
    To call the scanner with the custom init and callback you use    
        scanner.startScanning(MWBSInitSpace.init,MWBSInitSpace.callback);
- Added MWBsetCustomParam: function - allows user to put some custom key/value pair which can be used later from native code
- Added ITF-14 support
- Added Code 11 support
- Added MSI Plessey support
- Added GS1 support

&nbsp;
###Changes in 1.3:
&nbsp;

* Zoom feature added for iOS and Android. It's not supported on WP8 due to API limitation.
 
* Added function to turn Flash ON by default

* Fixed 'frameworks was not added to the references' on WP8
 
* Fixed freezing if missing org.apache.cordova.device plugin

* Added x86 lib for Android

* CameraManager.java rework 

 It now contains complete camera handling functionality, other files from camera folder are not necessary


&nbsp;
###Changes in 1.2:
&nbsp;

* Registering calls moved from native code to MWBScanner.js
 
 You can now enter your licensing info without changing the native code of plugin;

* Import package_name.R manually after adding Android plugin is not necessary anymore
 
* Decoding library updated to 2.9.31


 
&nbsp;
###Changes in 1.1:
&nbsp;

* Advanced Overlay (MWBsetOverlayMode: function(overlayMode)
 
 You can now choose between Simple Image Overlay and MW Dynamic Overlay, which shows the actual 
 viewfinder, depending on selected barcode types and their respective scanning rectangles;
 
* Orientation parameter (MWBsetInterfaceOrientation: function(interfaceOrientation))
 
 Now there's only a single function for supplying orientation parameters which makes tweaking the 
 controller for changing scanner orientation no longer needed; 
 
* Enable or disable high resolution scanning (MWBenableHiRes: function(enableHiRes))
 
 Added option to choose between high or normal resolution scanning to better match user 
 application requirements;
 
* Flash handling (MWBenableFlash: function(enableFlash))

 Added option to enable or disable the flash toggle button;
