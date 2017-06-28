/*
    * Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
    *
    * Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at
    *
    * http://www.apache.org/licenses/LICENSE-2.0
    *
    * Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License.
    */

var urlutil = require('cordova/urlutil');


var CAMERA_STREAM_STATE_CHECK_RETRY_TIMEOUT = 200; // milliseconds
var OPERATION_IS_IN_PROGRESS = -2147024567;
var INITIAL_FOCUS_DELAY = 200; // milliseconds
var CHECK_PLAYING_TIMEOUT = 100; // milliseconds

/**
    * List of supported barcode formats from ManateeWorks Barcode library. Used to return format
    *   name instead of number code as per plugin spec.
    *
    * @enum {String}
    */
/*var BARCODE_FORMAT = {
    1: 'AZTEC',
    2: 'CODABAR',
    4: 'CODE_39',
    8: 'CODE_93',
    16: 'CODE_128',
    32: 'DATA_MATRIX',
    64: 'EAN_8',
    128: 'EAN_13',
    256: 'ITF',
    512: 'MAXICODE',
    1024: 'PDF_417',
    2048: 'QR_CODE',
    4096: 'RSS_14',
    8192: 'RSS_EXPANDED',
    16384: 'UPC_A',
    32768: 'UPC_E',
    61918: 'All_1D',
    65536: 'UPC_EAN_EXTENSION',
    131072: 'MSI',
    262144: 'PLESSEY'
};*/

/**
    * Detects the first appropriate camera located at the back panel of device. If
    *   there is no back cameras, returns the first available.
    *
    * @returns {Promise<String>} Camera id
    */

var fps = 0;

var widthIndex = 0;
var heightIndex = 1;
var supportedResolutions = [
    [1920, 1080],
    [1280, 720],
    [640, 480]
];

var Full_HD = 0;
var HD      = 1;
var Normal  = 2;

/* Set the scanning resolution of the camera, aspect ratio may vary
    * available options:
    *   Full_HD 1920x1080
    *   HD      1280x720
    *   Normal  640x480
    */
var USE_CAMERA_RESOLUTION = supportedResolutions[HD];
var HARDWARE_CAMERA_RESOLUTION = { width: 0, height: 0 }; //ini

var USE_FPS = 30;

var print_hw_available_camera_res = false;

var _useFrontCamera = false;

var numberOfSupporedCodes = 16;

var codeMasksArray = [];
var codeMasksArrayInitialized = false;
var untouchedScanningRectsArray = [];
var untouchedScanningRectsUnion;

var jsSettingsScanningRects = [];
jsSettingsScanningRects[0x00000001] = { x: 0, y: 0, width: 100, height: 100, set: false };
jsSettingsScanningRects[0x00000002] = { x: 0, y: 0, width: 100, height: 100, set: false };
jsSettingsScanningRects[0x00000004] = { x: 0, y: 0, width: 100, height: 100, set: false };
jsSettingsScanningRects[0x00000008] = { x: 0, y: 0, width: 100, height: 100, set: false };
jsSettingsScanningRects[0x00000010] = { x: 0, y: 0, width: 100, height: 100, set: false };
jsSettingsScanningRects[0x00000020] = { x: 0, y: 0, width: 100, height: 100, set: false };
jsSettingsScanningRects[0x00000040] = { x: 0, y: 0, width: 100, height: 100, set: false };
jsSettingsScanningRects[0x00000080] = { x: 0, y: 0, width: 100, height: 100, set: false };
jsSettingsScanningRects[0x00000100] = { x: 0, y: 0, width: 100, height: 100, set: false };
jsSettingsScanningRects[0x00000200] = { x: 0, y: 0, width: 100, height: 100, set: false };
jsSettingsScanningRects[0x00000400] = { x: 0, y: 0, width: 100, height: 100, set: false };
jsSettingsScanningRects[0x00000800] = { x: 0, y: 0, width: 100, height: 100, set: false };
jsSettingsScanningRects[0x00001000] = { x: 0, y: 0, width: 100, height: 100, set: false };
jsSettingsScanningRects[0x00002000] = { x: 0, y: 0, width: 100, height: 100, set: false };
jsSettingsScanningRects[0x00004000] = { x: 0, y: 0, width: 100, height: 100, set: false };
jsSettingsScanningRects[0x00008000] = { x: 0, y: 0, width: 100, height: 100, set: false };
var jsSettingsScanningRectsAltered = false;

var assetsPath = "assets/";

var mwOverlayProperties = { mode: 1, pauseMode: 1, lineColor: "rgba(255, 0, 0, 1.0)", locationColor: "rgba(0, 255, 0, 1.0)", borderWidth: 2, linesWidth: 1, blinkingRate: 500, locationShowTime: 200, locationAllPointsDraw: true, imageSrc: assetsPath + "overlay_mw.png" };
var mwBlinkingLines = { v: null, h: null };

var hideDuringUpdate = true;

var partialView = { x: 5, y: 5, width: 90, height: 54.73, orientation: 0 };
var _usePartialScanner = false;

var resizePartialScannerView = null;
var DOM_complete = false;

var fullscreenButtons = {
    flashReference: null,
	flashControlRef: null,					  
	flashControlRef_enabled: false,							   
    flashState: 0,
    zoomReference: null,
	zoomControlRef: null,					 
    zoomState: 0,
    zoom_lvl_ini: 0,
    zoomLevels: [1, 2, 4],
    flash0: assetsPath + "flashbuttonoff.png",
    flash1: assetsPath + "flashbuttonon.png",
    flash9: assetsPath + "flashbuttonnotsupported.png",
    zoom0: assetsPath + "zoom.png",
    zoom9: assetsPath + "zoomnotsupported.png",
    hideFlash: false,
    hideZoom: false
};

var anyReader = null;
var anyScannerStarted = false;

var cancelPartial = null;

var debug_print = false;

cordova.commandProxy.add("MWBarcodeScanner", {
    initDecoder: function (successCallback, errorCallback, args) {
        MWBarcodeScanner.initDecoder(successCallback, errorCallback);
    },

    startScanner: function (successCallback, errorCallback, args) {

        if (!_usePartialScanner)    MWBarcodeScanner.startScanner(successCallback, errorCallback, args);
        else                        MWBarcodeScanner.startScannerView(successCallback, errorCallback, args);
    },

    startScannerView: function (successCallback, errorCallback, args) {
        MWBarcodeScanner.startScannerView(successCallback, errorCallback, args);
    },

    registerSDK: function (successCallback, errorCallback, args) {
        MWBarcodeScanner.registerSDK(successCallback, errorCallback, args);
    },

    setActiveCodes: function (successCallback, errorCallback, args) {
        MWBarcodeScanner.setActiveCodes(successCallback, errorCallback, args);
    },

    setActiveSubcodes: function (successCallback, errorCallback, args) {
        MWBarcodeScanner.setActiveSubcodes(successCallback, errorCallback, args);
    },

    setFlags: function (successCallback, errorCallback, args) {
        MWBarcodeScanner.setFlags(successCallback, errorCallback, args);
    },

    setMinLength: function (successCallback, errorCallback, args) {
        MWBarcodeScanner.setMinLength(successCallback, errorCallback, args);
    },

    setDirection: function (successCallback, errorCallback, args) {
        MWBarcodeScanner.setDirection(successCallback, errorCallback, args);
    },

    setScanningRect: function (successCallback, errorCallback, args) {
        MWBarcodeScanner.setScanningRect(successCallback, errorCallback, args);
    },

    setLevel: function (successCallback, errorCallback, args) {
        MWBarcodeScanner.setLevel(successCallback, errorCallback, args);
    },

    setInterfaceOrientation: function (successCallback, errorCallback, args) {
        // N/A
    },

    setOverlayMode: function (successCallback, errorCallback, args) {
        MWBarcodeScanner.setOverlayMode(successCallback, errorCallback, args);
    },

    setBlinkingLineVisible: function (successCallback, errorCallback, args) {
        MWBarcodeScanner.setBlinkingLineVisible(successCallback, errorCallback, args);
    },

    setPauseMode: function (successCallback, errorCallback, args) {
        MWBarcodeScanner.setPauseMode(successCallback, errorCallback, args);
    },

    enableHiRes: function (successCallback, errorCallback, args) {
        MWBarcodeScanner.enableHiRes(successCallback, errorCallback, args);
    },

    enableFlash: function (successCallback, errorCallback, args) {
        MWBarcodeScanner.enableFlash(successCallback, errorCallback, args);
    },
    turnFlashOn: function (successCallback, errorCallback, args) {
        MWBarcodeScanner.turnFlashOn(successCallback, errorCallback, args);
    },

    toggleFlash: function (successCallback, errorCallback) {
        MWBarcodeScanner.toggleFlash(successCallback, errorCallback);
    },

    enableZoom: function (successCallback, errorCallback, args) {
        MWBarcodeScanner.enableZoom(successCallback, errorCallback, args);
    },

    setZoomLevels: function (successCallback, errorCallback, args) {
        MWBarcodeScanner.setZoomLevels(successCallback, errorCallback, args);
    },

    toggleZoom: function (successCallback, errorCallback, args) {
        MWBarcodeScanner.toggleZoom(successCallback, errorCallback);
    },

    setMaxThreads: function (successCallback, errorCallback, args) {
        MWBarcodeScanner.setMaxThreads(successCallback, errorCallback, args)
    },

    setCustomParam: function (successCallback, errorCallback, args) {
        // TO BE REMOVED
    },

    closeScannerOnDecode: function (successCallback, errorCallback, args) {
        // N/A
    },

    resumeScanning: function (successCallback, errorCallback, args) {
        // N/A
    },

    closeScanner: function (successCallback, errorCallback, args) {
        MWBarcodeScanner.closeScanner(successCallback, errorCallback);
    },

    use60fps: function (successCallback, errorCallback, args) {
        // N/A
    },

    scanImage: function (successCallback, errorCallback, args) {
        MWBarcodeScanner.scanImage(successCallback, errorCallback, args);
    },

    setParam: function (successCallback, errorCallback, args) {
        MWBarcodeScanner.setParam(successCallback, errorCallback, args);
    },

    togglePauseResume: function (successCallback, errorCallback, args) {
        MWBarcodeScanner.togglePauseResume(successCallback, errorCallback);
    },

    duplicateCodeDelay: function (successCallback, errorCallback, args) {
        // N/A
    },

    setUseAutoRect: function (successCallback, errorCallback, args) {
        // N/A
    },

    useFrontCamera: function (successCallback, errorCallback, args) {
        MWBarcodeScanner.useFrontCamera(successCallback, errorCallback, args);
    },

    enableParser: function (successCallback, errorCallback, args) {
        MWBarcodeScanner.enableParser(successCallback, errorCallback, args);
    },

    setActiveParser: function (successCallback, errorCallback, args) {
        MWBarcodeScanner.setActiveParser(successCallback, errorCallback, args);
    },

    resizePartialScanner: function (successCallback, errorCallback, args) {
        MWBarcodeScanner.resizePartialScanner(successCallback, errorCallback, args);
    },

    usePartialScanner: function (successCallback, errorCallback, args) {
        MWBarcodeScanner.usePartialScanner(successCallback, errorCallback, args);
    }
});


function findCamera() {
    var Devices = Windows.Devices.Enumeration;

    // Enumerate cameras and add them to the list
    return Devices.DeviceInformation.findAllAsync(Devices.DeviceClass.videoCapture)
    .then(function (cameras) {

        if (!cameras || cameras.length === 0) {
            throw new Error("No cameras found");
        }

        var backCameras = cameras.filter(function (camera) {
            return camera.enclosureLocation && camera.enclosureLocation.panel === Devices.Panel.back;
        });

        //console.log('available cameras: ' + cameras.length + ' of which back cameras: ' + backCameras.length);

        // If there is back cameras, return the id of the first,
        // otherwise take the first available device's id
        return (backCameras[0] || cameras[0]).id;
    });
}

/**
 * @param {Windows.Graphics.Display.DisplayOrientations} displayOrientation
 * @return {Number}
 */
function videoPreviewRotationLookup(displayOrientation, isMirrored) {
    var degreesToRotate;

    switch (displayOrientation) {
        case Windows.Graphics.Display.DisplayOrientations.landscape:
            degreesToRotate = 0;
            break;
        case Windows.Graphics.Display.DisplayOrientations.portrait:
            if (isMirrored) {
                degreesToRotate = 270;
            } else {
                degreesToRotate = 90;
            }
            break;
        case Windows.Graphics.Display.DisplayOrientations.landscapeFlipped:
            degreesToRotate = 180;
            break;
        case Windows.Graphics.Display.DisplayOrientations.portraitFlipped:
            if (isMirrored) {
                degreesToRotate = 90;
            } else {
                degreesToRotate = 270;
            }
            break;
        default:
            degreesToRotate = 0;
            break;
    }

    return degreesToRotate;
}

/**
 * The pure JS implementation of barcode reader from WinRTBarcodeReader.winmd.
 *   Works only on Windows 10 devices and more efficient than original one.
 *
 * @class {BarcodeReader}
 */
function BarcodeReader () {
    this._promise = null;
    this._cancelled = false;
}

/**
 * Returns an instance of Barcode reader, depending on capabilities of Media
 *   Capture API
 *
 * @static
 * @constructs {BarcodeReader}
 *
 * @param   {MediaCapture}   mediaCaptureInstance  Instance of
 *   Windows.Media.Capture.MediaCapture class
 *
 * @return  {BarcodeReader}  BarcodeReader instance that could be used for
 *   scanning
 */
BarcodeReader.get = function (mediaCaptureInstance) {
    if (mediaCaptureInstance.getPreviewFrameAsync ) {
        return new BarcodeReader();
    }

    // If there is no corresponding API (Win8/8.1/Phone8.1) use old approach with WinMD library
   // return new WinRTBarcodeReader.Reader();

};

/**
 * Initializes instance of reader.
 *
 * @param   {MediaCapture}  capture  Instance of
 *   Windows.Media.Capture.MediaCapture class, used for acquiring images/ video
 *   stream for barcode scanner.
 * @param   {Number}  width    Video/image frame width
 * @param   {Number}  height   Video/image frame height
 */
BarcodeReader.prototype.init = function (capture, width, height) {
    this._capture = capture;
    this._width = width;
    this._height = height;
};

/**
 * Starts barcode search routines asyncronously.
 *
 * @return  {Promise<ScanResult>}  barcode scan result or null if search
 *   cancelled.
 */
BarcodeReader.prototype.readCode = function () {

    /**
     * Grabs a frame from preview stream uning Win10-only API and tries to
     *   get a barcode using reader provided. If there is no barcode
     *   found, returns null.
     */
    function scanBarcodeAsync(mediaCapture, frameWidth, frameHeight) {
        // Shortcuts for namespaces
        var Imaging = Windows.Graphics.Imaging;
        var Streams = Windows.Storage.Streams;

        var frame = new Windows.Media.VideoFrame(Imaging.BitmapPixelFormat.bgra8, frameWidth, frameHeight);
        return mediaCapture.getPreviewFrameAsync(frame)
        .then(function (capturedFrame) {
            // PAUSE/RESUME feature
            if (WindowsComponnent.ScannerPage.pauseDecoder) { capturedFrame.close(); return null; }
            // IMPORTANT: check active threads here if none available don't call convert/decode
            if (WindowsComponnent.ScannerPage.getActiveThreads() == WindowsComponnent.ScannerPage.maxThreads) { capturedFrame.close(); return null; }
            if (debug_print) console.log('frameCount: ' + (++fps));
            // Copy captured frame to buffer for further deserialization
            var bitmap = capturedFrame.softwareBitmap;

            var width = 0;
            var height = 0;
			
            var retArray = WindowsComponnent.ScannerPage.convertToGrayscale(bitmap, width, height);
            
            capturedFrame.close();

            if ((retArray.value.length == 1) && (retArray.value[0] == 0)) return null;
            else
            return WindowsComponnent.ScannerPage.decodeFrame(retArray.value, frameWidth, frameHeight);
        });
    }

    var self = this;
    return scanBarcodeAsync(this._capture,  this._width, this._height)
    .then(function (result) {
		
        if (self._cancelled)
            return null;

        return result || (self._promise = self.readCode());
    });
};

/**
 * Stops barcode search
 */
BarcodeReader.prototype.stop = function () {
    this._cancelled = true;
};

var MWBarcodeScanner = {

    /**
     * Scans image via device camera and retieves barcode from it.
     * @param  {function} success Success callback
     * @param  {function} fail    Error callback
     * @param  {array} args       Arguments array
     */
    startScanner: function (success, fail, args) {
        var capturePreview,
            capturePreviewAlignmentMark,
            captureCancelButton,
            navigationButtonsDiv,
            previewMirroring,
            closeButton,
            capture,
            reader;

        var canvasOverlay;
        var canvasBlinkingLineV, canvasBlinkingLineH;

        var is_portrait;
        var viewfinderOnScreen = { orientation: 0, x: 0, y: 0, width: 0, height: 0 };

        // check if already scanning
        if (anyScannerStarted) return;
        else anyScannerStarted = true;

        // disable b1 b2, tho since its full screen theres no need, but still
		// DEPENDENCY: buttons will need to be named that way in the index
        //document.getElementById("b1").disabled = true;
        //document.getElementById("b2").disabled = true;

        // clear needs to be done for every scan
        WindowsComponnent.ScannerPage.iniClear();

        // and for all scanningRects in the sdk //hereX
        WindowsComponnent.BarcodeHelper.resetScanningRects();

        if (jsSettingsScanningRectsAltered) {
            var _codeMask;
            for (var _i = 0; _i < numberOfSupporedCodes; _i++) {
                _codeMask = Math.pow(2, _i);
                if (jsSettingsScanningRects[_codeMask].set) {
                    WindowsComponnent.BarcodeHelper.mwBsetScanningRect(
                    Math.pow(2, _i),
                    WindowsComponnent.BarcodeHelper.createRect(
                        jsSettingsScanningRects[_codeMask].x,
                        jsSettingsScanningRects[_codeMask].y,
                        jsSettingsScanningRects[_codeMask].width,
                        jsSettingsScanningRects[_codeMask].height
                    ));
                }
            }
        }

        var torchLight;
        var zoomControl;

        // obtain a ref
        anyReader = reader;

        // get device type
        var easClientDeviceInformation = new Windows.Security.ExchangeActiveSyncProvisioning.EasClientDeviceInformation();
        var operatingSystem = easClientDeviceInformation.operatingSystem;

        var firstTimeUpdate = true;

        function updatePreviewForRotation(evt) {
            if (!capture) {
                return;
            }

            var ROTATION_KEY = "C380465D-2271-428C-9B83-ECEA3B4A85C1";

            var displayInformation = (evt && evt.target) || Windows.Graphics.Display.DisplayInformation.getForCurrentView();
            var currentOrientation = displayInformation.currentOrientation;

            previewMirroring = capture.getPreviewMirroring();

            // Lookup up the rotation degrees.
            var rotDegree = videoPreviewRotationLookup(currentOrientation, previewMirroring);

            // rotate the preview video
            var videoEncodingProperties = capture.videoDeviceController.getMediaStreamProperties(Windows.Media.Capture.MediaStreamType.videoPreview);
            videoEncodingProperties.properties.insert(ROTATION_KEY, rotDegree);

            if (debug_print) console.log('\nON ORIENTATION CHANGE ');
            
            // update style depending on orientation
            switch (currentOrientation) {
                case Windows.Graphics.Display.DisplayOrientations.landscape:
                    is_portrait = false;
                    viewfinderOnScreen.orientation = 0;
                    break;
                case Windows.Graphics.Display.DisplayOrientations.portrait:
                    is_portrait = true;
                    viewfinderOnScreen.orientation = 1;
                    break;
                case Windows.Graphics.Display.DisplayOrientations.landscapeFlipped:
                    is_portrait = false;
                    viewfinderOnScreen.orientation = 2;
                    break;
                case Windows.Graphics.Display.DisplayOrientations.portraitFlipped: // might need future handling
                    is_portrait = true;
                    viewfinderOnScreen.orientation = 3;
                    break;
                default:
                    /*none*/
            }

            // PREVIEW UPDATE SHOULD BE DONE AFTER UI CHANGE WHICH MAY TAKE SOME TIME TO COMPLETE RENDERING AFTER ORIENTATION CHANGE | HANDLED BY window.resizeEvent->resizeCanvas
            if (firstTimeUpdate) { resizeCanvas(); firstTimeUpdate = false; }
            

            if (operatingSystem == 'WindowsPhone')
                return capture.setEncodingPropertiesAsync(Windows.Media.Capture.MediaStreamType.videoPreview, videoEncodingProperties, null);
            else if (operatingSystem == 'WINDOWS')
            return capture.videoDeviceController.setMediaStreamPropertiesAsync(Windows.Media.Capture.MediaStreamType.videoPreview, videoEncodingProperties);
        }

        /**
         * Draws overlay lines inside the canvasOverlay area.
		 * @param  {float} x1 canvasOverlay Left
		 * @param  {float} y1 canvasOverlay Top
		 * @param  {float} w1 canvasOverlay Width
		 * @param  {float} h1 canvasOverlay Left
		 * @param  {float} lineThickness CanvasBlinkingLine lineThickness
         */
        function drawOverlayLines(x1, y1, w1, h1, lineThickness) {

            var startLeft = x1;
            var startTop = y1;
            canvasBlinkingLineV.style.left = canvasBlinkingLineH.style.left = (startLeft - 0) + "px";
            canvasBlinkingLineV.style.top = canvasBlinkingLineH.style.top = (startTop - 0) + "px";

            canvasBlinkingLineV.width = canvasBlinkingLineH.width = w1;
            canvasBlinkingLineV.height = canvasBlinkingLineH.height = h1;


            canvasBlinkingLineV.width = lineThickness;
            canvasBlinkingLineV.style.left = (startLeft + (w1 / 2) - (canvasBlinkingLineV.width / 2) - 0) + "px";

            canvasBlinkingLineH.height = lineThickness;
            canvasBlinkingLineH.style.top = (startTop + (h1 / 2) - (canvasBlinkingLineH.height / 2) - 0) + "px";

            canvasBlinkingLineV.style.backgroundColor = canvasBlinkingLineH.style.backgroundColor = mwOverlayProperties.lineColor;
            canvasBlinkingLineV.style.animation = canvasBlinkingLineH.style.animation = "fadeColor " + mwOverlayProperties.blinkingRate + "ms infinite";
        }

        /**
         * Resizes the canvasOverlay to fill browser window dynamically (no need for capturePreviewFrame since its fullscreen).
         */
        function resizeCanvas() {
            // get viewfinder (landscape)
            var viewfinderUnionRect = WindowsComponnent.BarcodeHelper.mwBgetScanningRect(0);
			
            if (debug_print) console.log('resizeCanvas | window size ' + window.innerWidth + ' ' + window.innerHeight);

            // improve UIX during update | users don't need to see the underlying changes only the final result
            if (hideDuringUpdate) {
                capturePreview.style.display = "none";
                //canvasOverlay.style.display = "none";
                canvasBlinkingLineV.style.display = "none";
                canvasBlinkingLineH.style.display = "none";
            }

            // set viewfinder in pixels
            viewfinderOnScreen.x = window.innerWidth * (viewfinderUnionRect.x / 100);
            viewfinderOnScreen.y = window.innerHeight * (viewfinderUnionRect.y / 100);
            viewfinderOnScreen.width = window.innerWidth * (viewfinderUnionRect.width / 100);
            viewfinderOnScreen.height = window.innerHeight * (viewfinderUnionRect.height / 100);

            // if orientation is not landscape swap values to reflect viewfinder in decoder
            if (viewfinderOnScreen.orientation == 1) // portrait
            {
                viewfinderOnScreen.x = window.innerWidth * ((100 - viewfinderUnionRect.y - viewfinderUnionRect.height) / 100);
                viewfinderOnScreen.y = window.innerHeight * (viewfinderUnionRect.x / 100);

                viewfinderOnScreen.width = window.innerWidth * (viewfinderUnionRect.height / 100);
                viewfinderOnScreen.height = window.innerHeight * (viewfinderUnionRect.width / 100);
            }
            else if (viewfinderOnScreen.orientation == 2) // flipped landscape
            {
                viewfinderOnScreen.x = window.innerWidth * ((100 - viewfinderUnionRect.x - viewfinderUnionRect.width) / 100);
                viewfinderOnScreen.y = window.innerHeight * ((100 - viewfinderUnionRect.y - viewfinderUnionRect.height) / 100);

                viewfinderOnScreen.width = window.innerWidth * (viewfinderUnionRect.width / 100);
                viewfinderOnScreen.height = window.innerHeight * (viewfinderUnionRect.height / 100);
            }

            //canvasOverlay.style.left = "0px";
            //canvasOverlay.style.top = "0px";
            canvasOverlay.width = window.innerWidth;
            canvasOverlay.height = window.innerHeight;

            /**
             * Your drawings need to be inside this function otherwise they will be reset when 
             * you resize the browser window and the canvas will be cleared.
             */

            if (mwOverlayProperties.mode == 0) return;

            var ctx = canvasOverlay.getContext("2d");

            if (mwOverlayProperties.mode == 1)
            {
                // draw fullcanvas shadow and clear the viewfinder area
                ctx.fillStyle = "rgba(0, 0, 0, 0.5)";
                ctx.fillRect(0, 0, canvasOverlay.width, canvasOverlay.height);
                ctx.clearRect(viewfinderOnScreen.x, viewfinderOnScreen.y, viewfinderOnScreen.width, viewfinderOnScreen.height);

                // draw red viewfinder border
                ctx.lineWidth = mwOverlayProperties.borderWidth;
                ctx.strokeStyle = mwOverlayProperties.lineColor;
                ctx.strokeRect(viewfinderOnScreen.x, viewfinderOnScreen.y, viewfinderOnScreen.width, viewfinderOnScreen.height);

                // draw red lines
                drawOverlayLines(viewfinderOnScreen.x, viewfinderOnScreen.y, viewfinderOnScreen.width, viewfinderOnScreen.height, mwOverlayProperties.linesWidth);
            }
            else if (mwOverlayProperties.mode == 2)
            {
                //if (document.getElementById("canvas-line-v") != null) document.getElementById("canvas-line-v").style.visibility = "hidden"; // handled in createPreview
                //if (document.getElementById("canvas-line-h") != null) document.getElementById("canvas-line-h").style.visibility = "hidden";

                var imageOverlay = document.createElement("img");
                imageOverlay.src = mwOverlayProperties.imageSrc;

                imageOverlay.onload = function () {
                    ctx.drawImage(imageOverlay, 0, 0, imageOverlay.width, imageOverlay.height,      // source rectangle
                                                0, 0, canvasOverlay.width, canvasOverlay.height);   // destination rectangle
                }
            }

            // reappear video preview
            setTimeout(function () {
                if (hideDuringUpdate) {
                    capturePreview.style.display = "initial";
                    //canvasOverlay.style.display = "initial";
                    canvasBlinkingLineV.style.display = "initial";
                    canvasBlinkingLineH.style.display = "initial";
                }
            }, 1000);
            
        }

        function onResume() {
            // Handle the resume event
        }

        function onPause() {
            // Handle the pause event
            reader && reader.stop();

            anyScannerStarted = false;
            anyReader = null;
        }

        // *** IMAGE BUTTONS ***

        /**
         * Handles the click event of the flash button.
         */
        function clickedFlash() {
            //flash
            if (debug_print) console.log('clicked flash');

            if (fullscreenButtons.flashState == -1) return;
            else
            {
                //torchLight.powerPercent = 100;
                if (fullscreenButtons.flashState == 0) {
                    torchLight.enabled = true;
                    fullscreenButtons.flashState = 1;
                    fullscreenButtons.flashReference.getElementsByTagName("img")[0].src = fullscreenButtons.flash1;
                }
                else {
                    torchLight.enabled = false;
                    fullscreenButtons.flashState = 0;
                    fullscreenButtons.flashReference.getElementsByTagName("img")[0].src = fullscreenButtons.flash0;
                }
            }
        }

        /**
         * Handles the click event of the zoom button.
         */
        function clickedZoom() {
            //zoom
            if (debug_print) console.log('clicked zoom');

            if (fullscreenButtons.zoomState == -1) return;

            var zoomSettings = new Windows.Media.Devices.ZoomSettings();

            // toggle zoom levels 1 - 2 - 4
            /*if (zoomControl.value == zoomControl.min) zoomSettings.value = zoomControl.max / 2;
            else if (zoomControl.value == (zoomControl.max / 2)) zoomSettings.value = zoomControl.max;
            else if (zoomControl.value == zoomControl.max) zoomSettings.value = zoomControl.min;*/

            // new way to handle things courtesy of setZoomLevels
            fullscreenButtons.zoom_lvl_ini = ((++fullscreenButtons.zoom_lvl_ini) % 3);
            zoomSettings.value = fullscreenButtons.zoomLevels[fullscreenButtons.zoom_lvl_ini];

            zoomSettings.mode = zoomControl.supportedModes.first();
            zoomControl.configure(zoomSettings);
        }

        /**
         * Creates a preview frame and necessary objects.
         */
        function createPreview() {

            // Create fullscreen preview
            var capturePreviewFrameStyle = document.createElement('link');
            capturePreviewFrameStyle.rel = "stylesheet";
            capturePreviewFrameStyle.type = "text/css";
            capturePreviewFrameStyle.href = urlutil.makeAbsolute("/www/css/plugin-barcodeScanner.css");

            document.head.appendChild(capturePreviewFrameStyle);

            capturePreviewFrame = document.createElement('div');
            capturePreviewFrame.className = "barcode-scanner-wrap";

            capturePreview = document.createElement("video");
            capturePreview.className = "barcode-scanner-preview";
            capturePreview.addEventListener('click', function () {
                focus();
            });

            capturePreviewAlignmentMark = document.createElement('div');
            capturePreviewAlignmentMark.className = "barcode-scanner-mark";

            navigationButtonsDiv = document.createElement("div");
            navigationButtonsDiv.className = "barcode-scanner-app-bar";
            navigationButtonsDiv.onclick = function (e) {
                e.cancelBubble = true;
            };

            // create canvas for Overlay
            canvasOverlay = document.createElement("canvas");
            canvasOverlay.id = "canvas-overlay";
            canvasOverlay.className = "shadow-overlay";

            // create canvas for line
            canvasBlinkingLineV = document.createElement("canvas");
            canvasBlinkingLineV.id = "canvas-line-v";
            canvasBlinkingLineV.className = "blinking-line";

            // create canvas for line
            canvasBlinkingLineH = document.createElement("canvas");
            canvasBlinkingLineH.id = "canvas-line-h";
            canvasBlinkingLineH.className = "blinking-line";

            // obtain a ref
            mwBlinkingLines.v = canvasBlinkingLineV;
            mwBlinkingLines.h = canvasBlinkingLineH;

            // if mwOverlay mode is set to image hide lines
            if (mwOverlayProperties.mode == 2) {
                mwBlinkingLines.v.style.visibility = "hidden";
                mwBlinkingLines.h.style.visibility = "hidden";
            }

            // create image buttons for flash
            fullscreenButtons.flashReference = document.createElement("div");
            fullscreenButtons.flashReference.id = "flash-button";

            var divImage = document.createElement("img");
            divImage.id = "flash-image";
            divImage.src = fullscreenButtons.flash0;

            // if not enabled hide the button as if it doesn't exist at all
            if (fullscreenButtons.hideFlash) fullscreenButtons.flashReference.style.display = "none";

            fullscreenButtons.flashReference.appendChild(divImage);

            // and zoom
            fullscreenButtons.zoomReference = document.createElement("div");
            fullscreenButtons.zoomReference.id = "zoom-button";

            var divImage2 = document.createElement("img");
            divImage2.id = "zoom-image";
            divImage2.src = fullscreenButtons.zoom0;

            // if not enabled hide the button as if it doesn't exist at all
            if (fullscreenButtons.hideZoom) fullscreenButtons.zoomReference.style.display = "none";

            fullscreenButtons.zoomReference.appendChild(divImage2);

            fullscreenButtons.flashReference.addEventListener('click', clickedFlash, false);
            fullscreenButtons.zoomReference.addEventListener('click', clickedZoom, false);

            resizeCanvas();

            // register an event listener to be notified and execute resizeCanvas upon window resize for desktop only AND orientation->UI change for phone/tablet
            window.addEventListener('resize', resizeCanvas, false);

            /*closeButton = document.createElement("div");
            closeButton.innerText = "close";
            closeButton.className = "app-bar-action action-close";
            navigationButtonsDiv.appendChild(closeButton);

            closeButton.addEventListener("click", cancelPreview, false);*/
            document.addEventListener('backbutton', cancelPreview, false);
            document.addEventListener("pause", onPause, false);
            document.addEventListener("resume", onResume, false);

            [capturePreview, capturePreviewAlignmentMark/*, navigationButtonsDiv*/].forEach(function (element) {
                capturePreviewFrame.appendChild(element);
            });
        }

        function focus(controller) {

            var result = WinJS.Promise.wrap();

            if (!capturePreview || capturePreview.paused) {
                // If the preview is not yet playing, there is no sense in running focus
                return result;
            }

            if (!controller) {
                try {
                    controller = capture && capture.videoDeviceController;
                } catch (err) {
                    console.log('Failed to access focus control for current camera: ' + err);
                    return result;
                }
            }

            if (!controller.focusControl || !controller.focusControl.supported) {
                console.log('Focus control for current camera is not supported');
                return result;
            }

            // Multiple calls to focusAsync leads to internal focusing hang on some Windows Phone 8.1 devices
            if (controller.focusControl.focusState === Windows.Media.Devices.MediaCaptureFocusState.searching) {
                return result;
            }

            // The delay prevents focus hang on slow devices
            return WinJS.Promise.timeout(INITIAL_FOCUS_DELAY)
            .then(function () {
                try {
                    return controller.focusControl.focusAsync().then(function () {
                        return result;
                    }, function (e) {
                        // This happens on mutliple taps
                        if (e.number !== OPERATION_IS_IN_PROGRESS) {
                            console.error('focusAsync failed: ' + e);
                            return WinJS.Promise.wrapError(e);
                        }
                        return result;
                    });
                } catch (e) {
                    // This happens on mutliple taps
                    if (e.number !== OPERATION_IS_IN_PROGRESS) {
                        console.error('focusAsync failed: ' + e);
                        return WinJS.Promise.wrapError(e);
                    }
                    return result;
                }
            });
        }

        function setupFocus(focusControl) {

            function supportsFocusMode(mode) {
                return focusControl.supportedFocusModes.indexOf(mode).returnValue;
            }

            if (!focusControl || !focusControl.supported || !focusControl.configure) {
                return WinJS.Promise.wrap();
            }

            var FocusMode = Windows.Media.Devices.FocusMode;
            var focusConfig = new Windows.Media.Devices.FocusSettings();
            focusConfig.autoFocusRange = Windows.Media.Devices.AutoFocusRange.normal;

            // Determine a focus position if the focus search fails:
            focusConfig.disableDriverFallback = false;

            if (supportsFocusMode(FocusMode.continuous)) {
                console.log("Device supports continuous focus mode");
                focusConfig.mode = FocusMode.continuous;
            } else if (supportsFocusMode(FocusMode.auto)) {
                console.log("Device doesn\'t support continuous focus mode, switching to autofocus mode");
                focusConfig.mode = FocusMode.auto;
            }

            focusControl.configure(focusConfig);

            // Continuous focus should start only after preview has started. See 'Remarks' at 
            // https://msdn.microsoft.com/en-us/library/windows/apps/windows.media.devices.focuscontrol.configure.aspx
            function waitForIsPlaying() {
                var isPlaying = !capturePreview.paused && !capturePreview.ended && capturePreview.readyState > 2;

                if (!isPlaying) {
                    return WinJS.Promise.timeout(CHECK_PLAYING_TIMEOUT)
                    .then(function () {
                        return waitForIsPlaying();
                    });
                }

                return focus();
            }

            return waitForIsPlaying();
        }

        /**
         * Starts stream transmission to preview frame and then run barcode search.
         */
        function startPreview() {
            return findCamera()
            .then(function (id) {
                var captureSettings = new Windows.Media.Capture.MediaCaptureInitializationSettings();
                captureSettings.streamingCaptureMode = Windows.Media.Capture.StreamingCaptureMode.video;
                captureSettings.photoCaptureSource = Windows.Media.Capture.PhotoCaptureSource.videoPreview;
                captureSettings.videoDeviceId = id;

                capture = new Windows.Media.Capture.MediaCapture();
                return capture.initializeAsync(captureSettings);
            })
            .then(function () {

                var controller = capture.videoDeviceController;
                var deviceProps = controller.getAvailableMediaStreamProperties(Windows.Media.Capture.MediaStreamType.videoRecord);

                // *** IMAGE BUTTONS LOGIC ***
                torchLight = controller.torchControl;
                if (torchLight.supported) {
                    console.log('Torch is supported.'); // torch / flash / light
                    //torchLight.powerPercent = 100;
                    setTimeout(function () {
                        if (fullscreenButtons.flashControlRef_enabled) {
                            torchLight.enabled = true;
                            fullscreenButtons.flashState = 1;
                            fullscreenButtons.flashReference.getElementsByTagName("img")[0].src = fullscreenButtons.flash1;
                            console.log('Torch is turned on.');
                        }
                        else {
                            torchLight.enabled = false;
                            fullscreenButtons.flashState = 0;
                            fullscreenButtons.flashReference.getElementsByTagName("img")[0].src = fullscreenButtons.flash0;
                        }
                    }, 100); //950XL: 39, 640: 50
                }
                else {
                    console.log('Torch is NOT supported.'); //torch / flash / light
                    //torchLight.powerPercent = 0;
                    //torchLight.enabled = false;
                    fullscreenButtons.flashReference.getElementsByTagName("img")[0].src = fullscreenButtons.flash9;
                    fullscreenButtons.flashReference.style.display = "none";
                    fullscreenButtons.flashState = -1;
                }

                zoomControl = controller.zoomControl;
                if (zoomControl.supported) {
                    console.log('Zoom is supported.');
                    console.log('Zoom levels ' + zoomControl.min + '-' + zoomControl.max); // 1-4
                    /*var zoomSettings = new Windows.Media.Devices.ZoomSettings();
                    zoomSettings.value = zoomControl.max;
                    zoomSettings.mode = zoomControl.supportedModes.first();
                    zoomControl.configure(zoomSettings);*/

                    // handle custom zoom levels | if custom values are out of bounds set defaults
                    if (fullscreenButtons.zoomLevels[1] < zoomControl.min || fullscreenButtons.zoomLevels[1] > zoomControl.max)
                        fullscreenButtons.zoomLevels[1] = zoomControl.max / 2;

                    if (fullscreenButtons.zoomLevels[2] < zoomControl.min || fullscreenButtons.zoomLevels[2] > zoomControl.max)
                        fullscreenButtons.zoomLevels[2] = zoomControl.max;

                    if (!(fullscreenButtons.zoom_lvl_ini == 0 || fullscreenButtons.zoom_lvl_ini == 1 || fullscreenButtons.zoom_lvl_ini == 2))
                        fullscreenButtons.zoom_lvl_ini = 0;
					// apply ini zoomLevel
                    var zoomSettings = new Windows.Media.Devices.ZoomSettings();

                    zoomSettings.value = fullscreenButtons.zoomLevels[fullscreenButtons.zoom_lvl_ini];

                    zoomSettings.mode = zoomControl.supportedModes.first();
                    zoomControl.configure(zoomSettings);					  

                    fullscreenButtons.zoomReference.getElementsByTagName("img")[0].src = fullscreenButtons.zoom0;
                }
                else {
                    console.log('Zoom is NOT supported.');
                    //fullscreenButtons.zoomReference.getElementsByTagName("img")[0].src = fullscreenButtons.zoom9;
                    fullscreenButtons.zoomReference.style.display = "none";
                    fullscreenButtons.zoomState = -1;
                }

                deviceProps = Array.prototype.slice.call(deviceProps);
                deviceProps = deviceProps.filter(function (prop) {
                    // filter out streams with "unknown" subtype - causes errors on some devices
                    return prop.subtype !== "Unknown";
                }).sort(function (propA, propB) {
                    // sort properties by resolution
                    return propB.width - propA.width;
                });
				
				// find a resolution as USE_CAMERA_RESOLUTION or the next lower available 
                var resolutionListIndex = 0;
                var resolutionListLength = deviceProps.length;

                if (debug_print && print_hw_available_camera_res) {
                    console.log('[available camera video properties]');
                    for (; resolutionListIndex < resolutionListLength; resolutionListIndex++) {
                        console.log(deviceProps[resolutionListIndex].width + ' ' + deviceProps[resolutionListIndex].height + ' ' + deviceProps[resolutionListIndex].frameRate.numerator + '/' + deviceProps[resolutionListIndex].frameRate.denominator);
                    } resolutionListIndex = 0;
                }

                do {
                    if (deviceProps[resolutionListIndex].height < USE_CAMERA_RESOLUTION[heightIndex]) break;
                    else if (deviceProps[resolutionListIndex].height == USE_CAMERA_RESOLUTION[heightIndex]) break;
                    resolutionListIndex++;
                } while (resolutionListIndex < resolutionListLength);

                var maxResProps = deviceProps[resolutionListIndex];

                if (maxResProps.frameRate.numerator != USE_FPS)
                do {
                    if (deviceProps[resolutionListIndex].width == maxResProps.width && deviceProps[resolutionListIndex].height == maxResProps.height) {
                        if (deviceProps[resolutionListIndex].frameRate.numerator == USE_FPS) { maxResProps = deviceProps[resolutionListIndex]; break; }
                        resolutionListIndex++;
                    }
                    else break;
                } while (resolutionListIndex < resolutionListLength);

                HARDWARE_CAMERA_RESOLUTION.width = maxResProps.width;
                HARDWARE_CAMERA_RESOLUTION.height = maxResProps.height;

                return controller.setMediaStreamPropertiesAsync(Windows.Media.Capture.MediaStreamType.videoRecord, maxResProps)
                .then(function () {
                    return {
                        capture: capture,
                        width: maxResProps.width,
                        height: maxResProps.height
                    };
                });
            })
            .then(function (captureSettings) {

                capturePreview.msZoom = true;
                capturePreview.src = URL.createObjectURL(capture);
                capturePreview.play();

                // Insert preview frame and controls into page
                document.body.appendChild(capturePreviewFrame);
                document.body.appendChild(canvasOverlay);
                document.body.appendChild(canvasBlinkingLineV);
                document.body.appendChild(canvasBlinkingLineH);
                document.body.appendChild(fullscreenButtons.flashReference);
                document.body.appendChild(fullscreenButtons.zoomReference);

                return setupFocus(captureSettings.capture.videoDeviceController.focusControl)
                .then(function () {
                    Windows.Graphics.Display.DisplayInformation.getForCurrentView().addEventListener("orientationchanged", updatePreviewForRotation, false);
                    return updatePreviewForRotation();
                })
                .then(function () {
                    return captureSettings;
                });
            });
        }

        /**
         * Removes preview frame and corresponding objects from window.
         */
        function destroyPreview() {

            Windows.Graphics.Display.DisplayInformation.getForCurrentView().removeEventListener("orientationchanged", updatePreviewForRotation, false);
            document.removeEventListener('backbutton', cancelPreview);

            window.removeEventListener('resize', resizeCanvas, false);
            window.removeEventListener("pause", onPause, false);
            window.removeEventListener("resume", onResume, false);

            fullscreenButtons.flashReference.removeEventListener('click', clickedFlash, false);
            fullscreenButtons.zoomReference.removeEventListener('click', clickedZoom, false);

            capturePreview.pause();
            capturePreview.src = null;

            if (capturePreviewFrame) {
                document.body.removeChild(capturePreviewFrame);
                document.body.removeChild(canvasOverlay);
                document.body.removeChild(canvasBlinkingLineV);
                document.body.removeChild(canvasBlinkingLineH);
                document.body.removeChild(fullscreenButtons.flashReference);
                document.body.removeChild(fullscreenButtons.zoomReference);
            }

            reader && reader.stop();
            reader = null;

            capture && capture.stopRecordAsync();
            capture = null;
        }

        /**
         * Stops preview and then call success callback with cancelled=true.
         * See https://github.com/phonegap-build/BarcodeScanner#using-the-plugin
         */
        function cancelPreview() {
            reader && reader.stop();
			
			anyScannerStarted = false;
			anyReader = null;
			//document.getElementById("b1").disabled = false;
            //document.getElementById("b2").disabled = false;

			success({
			    code: "",
			    type: "Cancel" /*BARCODE_FORMAT[result.barcodeFormat]*/,
			    isGS1: null,
			    bytes: "",
			    location: null,
			    imageWidth: null,
			    imageHeight: null,
			    cancelled: true
			});
        }

        WinJS.Promise.wrap(createPreview())
        .then(function () {
            return startPreview();
        })
        .then(function (captureSettings) {
            anyReader = reader = BarcodeReader.get(captureSettings.capture); // obtain ref
            captureSettings.capture;
            reader.init(captureSettings.capture, /*1280*/captureSettings.width, /*720*/captureSettings.height); // ok

            // Add a small timeout before capturing first frame otherwise
            // we would get an 'Invalid state' error from 'getPreviewFrameAsync'
            return WinJS.Promise.timeout(200)
            .then(function () {
                return reader.readCode();
            });
        })
        .then(function (result) {
			if (result.locationPoints != null)
            {
                capturePreview.pause();
                //MWOverlay location points green box draw
                var ctx = canvasOverlay.getContext("2d");

                if (debug_print) console.log("loc_points:");
                if (debug_print) console.log(result);

                ctx.lineWidth = mwOverlayProperties.borderWidth * 2;
                ctx.strokeStyle = mwOverlayProperties.locationColor;

                var navigationBarHeight = (window.innerWidth > window.innerHeight) ? ((window.innerHeight * (16 / 9)) - window.innerWidth) : ((window.innerWidth * (16 / 9)) - window.innerHeight);

                //debug_print = true;
                if (debug_print) console.log('navigationBarHeight:' + navigationBarHeight);

                var window_actualWidth = window.innerWidth;
                var window_actualHeight = window.innerHeight;

			    //when the navigation bar is present, the image is pushed/translated by the size of the navigation bar
                if (viewfinderOnScreen.orientation == 0 || viewfinderOnScreen.orientation == 2) //landscape or flipped landscape
                {
                    window_actualWidth += navigationBarHeight;
                }
                else //portrait
                {
                    window_actualHeight += navigationBarHeight;
                }

                var scale_x = window_actualWidth / result.imageWidth;
                var scale_y = window_actualHeight / result.imageHeight;

                if (debug_print) console.log('screen w h : ' + window.innerWidth + ' ' + window.innerHeight + ' image w h : ' + result.imageWidth + ' ' + result.imageHeight + ' scales x y : ' + scale_x + ' ' + scale_y);



                if (viewfinderOnScreen.orientation == 1) //swap scales operands to account for the rotation
                {
                    scale_x = window_actualWidth / result.imageHeight;
                    scale_y = window_actualHeight / result.imageWidth;
                }

                //viewfinderOnScreen.orientation 1 port 2 flipped land
                var coordSysOrigin_x_Axis_toCenter = 0;
                var coordSysOrigin_y_Axis_toCenter = 0;

                if (viewfinderOnScreen.orientation != 0) //if not landscape, do some transformations to match the position on the current orientation
                {
                    coordSysOrigin_x_Axis_toCenter = canvasOverlay.width / 2; //this /2 is correct
                    coordSysOrigin_y_Axis_toCenter = canvasOverlay.height / 2;

                    ctx.translate(coordSysOrigin_x_Axis_toCenter, coordSysOrigin_y_Axis_toCenter);
                    ctx.rotate(viewfinderOnScreen.orientation * 90 * Math.PI / 180);
                }

                ctx.translate(-navigationBarHeight / 2, 0); //this needs to be /2 no matter what the scale_x value is

                if (!mwOverlayProperties.locationAllPointsDraw) //draw box from p1 and p3
                {
                    var x = result.locationPoints.p1.x;
                    var y = result.locationPoints.p1.y;

                    var w = result.locationPoints.p3.x - x;
                    var h = result.locationPoints.p3.y - y;

                    x *= scale_x;
                    y *= scale_y;
                    w *= scale_x;
                    h *= scale_y;

                    //draw green location border polygon | swap translation back operands to account for the different width and height for portrait, otherwise just normal translation back
                    if (viewfinderOnScreen.orientation == 1)
                    {
                        ctx.strokeRect(x - coordSysOrigin_y_Axis_toCenter, y - coordSysOrigin_x_Axis_toCenter, w, h);
                    }
                    else ctx.strokeRect(x - coordSysOrigin_x_Axis_toCenter, y - coordSysOrigin_y_Axis_toCenter, w, h);
                }
                else
                {
                    var x1 = result.locationPoints.p1.x;
                    var y1 = result.locationPoints.p1.y;
                    var x2 = result.locationPoints.p2.x;
                    var y2 = result.locationPoints.p2.y;
                    var x3 = result.locationPoints.p3.x;
                    var y3 = result.locationPoints.p3.y;
                    var x4 = result.locationPoints.p4.x;
                    var y4 = result.locationPoints.p4.y;

                    x1 *= scale_x;
                    y1 *= scale_y;
                    x2 *= scale_x;
                    y2 *= scale_y;
                    x3 *= scale_x;
                    y3 *= scale_y;
                    x4 *= scale_x;
                    y4 *= scale_y;

                    if (debug_print) ctx.strokeStyle = "rgba(255, 225, 0, 1.0)";

                    //draw green location border lines | swap translation back operands to account for the different width and height for portrait, otherwise just normal translation back
                    if (viewfinderOnScreen.orientation == 1)
                    {
                        ctx.beginPath();
                        ctx.moveTo(x1 - coordSysOrigin_y_Axis_toCenter, y1 - coordSysOrigin_x_Axis_toCenter);
                        ctx.lineTo(x2 - coordSysOrigin_y_Axis_toCenter, y2 - coordSysOrigin_x_Axis_toCenter);
                        ctx.lineTo(x3 - coordSysOrigin_y_Axis_toCenter, y3 - coordSysOrigin_x_Axis_toCenter);
                        ctx.lineTo(x4 - coordSysOrigin_y_Axis_toCenter, y4 - coordSysOrigin_x_Axis_toCenter);
                        ctx.lineTo(x1 - coordSysOrigin_y_Axis_toCenter, y1 - coordSysOrigin_x_Axis_toCenter);

                        ctx.stroke();
                    }
                    else
                    {
                        ctx.beginPath();
                        ctx.moveTo(x1 - coordSysOrigin_x_Axis_toCenter, y1 - coordSysOrigin_y_Axis_toCenter);
                        ctx.lineTo(x2 - coordSysOrigin_x_Axis_toCenter, y2 - coordSysOrigin_y_Axis_toCenter);
                        ctx.lineTo(x3 - coordSysOrigin_x_Axis_toCenter, y3 - coordSysOrigin_y_Axis_toCenter);
                        ctx.lineTo(x4 - coordSysOrigin_x_Axis_toCenter, y4 - coordSysOrigin_y_Axis_toCenter);
                        ctx.lineTo(x1 - coordSysOrigin_x_Axis_toCenter, y1 - coordSysOrigin_y_Axis_toCenter);

                        ctx.stroke();
                    }
                    
                    //ctx.beginPath();
                    //ctx.moveTo(x1, y1);
                    //ctx.lineTo(x2, y2);
                    //ctx.lineTo(x3, y3);
                    //ctx.lineTo(x4, y4);
                    //ctx.lineTo(x1, y1);

                    //ctx.stroke();
                }
            }

            return WinJS.Promise.timeout(mwOverlayProperties.locationShowTime) //mwOverlay showtime
            .then(function () {
                return result;
            });
        })								 
        .done(function (result) {
            destroyPreview();
			
			anyScannerStarted = false;
			anyReader = null;
			//document.getElementById("b1").disabled = false;
			//document.getElementById("b2").disabled = false;
			
            /**
               * result.code - string representation of barcode result
               * result.type - type of barcode detected or 'Cancel' if scanning is canceled
               * result.bytes - bytes array of raw barcode result
               * result.isGS1 - (boolean) barcode is GS1 compliant
               * result.location - contains rectangle points p1,p2,p3,p4 with the corresponding x,y
               * result.imageWidth - Width of the scanned image
               * result.imageHeight - Height of the scanned image
               */
            success({
                code: result && result.text,
                type: result && WindowsComponnent.BarcodeHelper.getBarcodeName(result.type) /*BARCODE_FORMAT[result.barcodeFormat]*/,
                isGS1: result && result.isGS1,
                bytes: result && result.bytes,
                location: result && result.location,
                imageWidth: result && result.imageWidth,
                imageHeight: result && result.imageHeight,
                cancelled: !result
            });
        }, function (error) {
            destroyPreview();
            fail(error);
        });
    },
	
	/**
     * Scans partial image via device camera and retieves barcode from it.
     * @param  {function} success Success callback
     * @param  {function} fail    Error callback
     * @param  {array} args       Arguments array
     */
    startScannerView: function (success, fail, args) {
        var capturePreview,
            capturePreviewAlignmentMark,
            captureCancelButton,
            navigationButtonsDiv,
            previewMirroring,
            closeButton,
            capture,
            reader; if (debug_print) console.log('startScannerView');

            var capturePreviewFrame; // needed here because unlike fullscreen it's properties will be altered
            var proxyWrapCapturePreview;
		
		var canvasOverlay;
        var canvasBlinkingLineV, canvasBlinkingLineH;

        var is_portrait;
        var viewfinderOnScreenView = { orientation: 0, x: 0, y: 0, width: 0, height: 0 };

        // check if already scanning
        if (anyScannerStarted) return;
        else
        {
            DOM_complete = false;
            anyScannerStarted = true;
        }

        // disable b1 b2, tho since its full screen theres no need, but still
		// DEPENDENCY: buttons will need to be named that way in the index
        //document.getElementById("b1").disabled = true;
        //document.getElementById("b2").disabled = true;

        // copy args
        /*partialView.x = args[0];
        partialView.y = args[1];
        partialView.width = args[2];
        partialView.height = args[3];*/

        // clear needs to be done for every scan
        WindowsComponnent.ScannerPage.iniClear();

        // obtain a ref
        anyReader = reader;

        // get device type
        var easClientDeviceInformation = new Windows.Security.ExchangeActiveSyncProvisioning.EasClientDeviceInformation();
        var operatingSystem = easClientDeviceInformation.operatingSystem;

        var torchLight;
        var zoomControl;

        //var TimeN = new Date();
        var firstTimeUpdate = true;

        function updatePreviewForRotation(evt) {
            if (!capture) {
                return;
            }

            var ROTATION_KEY = "C380465D-2271-428C-9B83-ECEA3B4A85C1";

            var displayInformation = (evt && evt.target) || Windows.Graphics.Display.DisplayInformation.getForCurrentView();
            var currentOrientation = displayInformation.currentOrientation;

            previewMirroring = capture.getPreviewMirroring();

            // Lookup up the rotation degrees.
            var rotDegree = videoPreviewRotationLookup(currentOrientation, previewMirroring);

            // rotate the preview video
            var videoEncodingProperties = capture.videoDeviceController.getMediaStreamProperties(Windows.Media.Capture.MediaStreamType.videoPreview);
            videoEncodingProperties.properties.insert(ROTATION_KEY, rotDegree);
			
            if (debug_print) console.log('\nON ORIENTATION CHANGE ');
			
			if (debug_print)
			{
			    console.log('currentOrientation' + currentOrientation);
			    //console.log('time from previous orientation change ' + (new Date - TimeN)); TimeN = new Date();
			}

            // update style depending on orientation
            switch (currentOrientation) {
                case Windows.Graphics.Display.DisplayOrientations.landscape:
					is_portrait = false;
                    viewfinderOnScreenView.orientation = 0;
                    break;
                case Windows.Graphics.Display.DisplayOrientations.portrait:
					is_portrait = true;
                    viewfinderOnScreenView.orientation = 1;
                    break;
                case Windows.Graphics.Display.DisplayOrientations.landscapeFlipped:
					is_portrait = false;
                    viewfinderOnScreenView.orientation = 2;
                    break;
                case Windows.Graphics.Display.DisplayOrientations.portraitFlipped: //not handled for anchoring
					is_portrait = true;
                    viewfinderOnScreenView.orientation = 3;
                    break;
                default:
                    /*none*/
            }
			
            // PREVIEW UPDATE SHOULD BE DONE AFTER UI CHANGE WHICH MAY TAKE SOME TIME TO COMPLETE RENDERING AFTER ORIENTATION CHANGE | HANDLED BY window.resizeEvent->resizePartialScannerView
            if (firstTimeUpdate) { resizePartialScannerView(); firstTimeUpdate = false; }
			

            if (operatingSystem == 'WindowsPhone')
                return capture.setEncodingPropertiesAsync(Windows.Media.Capture.MediaStreamType.videoPreview, videoEncodingProperties, null);
            else if (operatingSystem == 'WINDOWS')
            return capture.videoDeviceController.setMediaStreamPropertiesAsync(Windows.Media.Capture.MediaStreamType.videoPreview, videoEncodingProperties);
        }

        /**
         * Resize partial scanning view.
         */
        resizePartialScannerView = function resizeView(/*x1, y1, w1, h1*/) {
				
			if (debug_print) console.log('resizePartialView | window size ' + window.innerWidth + ' ' + window.innerHeight);

            // USE THIS IF YOU WANT TO STORE THE VALUES OF THE RESIZE FOR THE NEXT SCAN
            /*partialView.x = x1;
            partialView.y = y1;
            partialView.width = w1;
            partialView.height = h1;*/

            // improve UIX during update | users don't need to see the underlying changes only the final result
			if (hideDuringUpdate) {
			    proxyWrapCapturePreview.style.visibility = "hidden";
			    //canvasOverlay.style.display = "none";
			    canvasBlinkingLineV.style.display = "none";
			    canvasBlinkingLineH.style.display = "none";
			}

            anchorView_toOrientation(partialView.x, partialView.y, partialView.width, partialView.height, partialView.orientation, viewfinderOnScreenView.orientation);
            //anchorView_toOrientation(x1, y1, w1, h1, partialView.orientation, viewfinderOnScreenView.orientation);
            calcPreview(is_portrait);
            resizeCanvas();

            // reappear video preview
            setTimeout(function () {
                if (hideDuringUpdate) {
                    proxyWrapCapturePreview.style.visibility = "visible";
                    //canvasOverlay.style.display = "initial";
                    canvasBlinkingLineV.style.display = "initial";
                    canvasBlinkingLineH.style.display = "initial";
                }
            }, 1000);
        }

        function onResume() {
            // Handle the resume event
        }

        function onPause() {
            // Handle the pause event
            reader && reader.stop();

            anyScannerStarted = false;
            anyReader = null;
        }

        /**
         * Processes preview behaviour.
         */
        function anchorView_toOrientation(x1, y1, w1, h1, anchor_to, current_orientation) {

            if (anchor_to < 0 || anchor_to > 3 || current_orientation < 0 || current_orientation > 2) return;

            if (anchor_to == 0) //anchor_free: percentages are applied "as is" | behaviour: preview transforms dynamically for different orientations
            {
                capturePreviewFrame.style.cssText = "left: " + x1 + "%; top: " + y1 + "%; width: " + w1 + "%; height: " + h1 + "%;";
            }
            else //anchor_to_orientation: percentages are applied wrt. orientation | behaviour: preview stays fixed/immutable wrt. orientation
            {
                //[anchor_to] x [current_orientation]
                var anchoringProperties = [
                    //landscape                                 portrait                                landscape flipped
                    [{ x: x1, y: y1, width: w1, height: h1 }, { x: (100 - y1 - h1), y: x1, width: h1, height: w1 }, { x: (100 - x1 - w1), y: (100 - y1 - h1), width: w1, height: h1 }], // landscape
                    [{ x: y1, y: (100 - x1 - w1), width: h1, height: w1 }, { x: x1, y: y1, width: w1, height: h1 }, { x: (100 - y1 - h1), y: (100 - x1 - w1), width: h1, height: w1 }], // portrait
                    [{ x: (100 - x1 - w1), y: (100 - y1 - h1), width: w1, height: h1 }, { x: y1, y: (100 - x1 - w1), width: h1, height: w1 }, { x: x1, y: y1, width: w1, height: h1 }]  // landscape flipped
                ];
                anchor_to--;
                capturePreviewFrame.style.cssText = "left: " + anchoringProperties[anchor_to][current_orientation].x +
                                                    "%; top: " + anchoringProperties[anchor_to][current_orientation].y +
                                                    "%; width: " + anchoringProperties[anchor_to][current_orientation].width +
                                                    "%; height: " + anchoringProperties[anchor_to][current_orientation].height + "%;";
            }
        }

        /**
	     * Transforms the viewfinder to reflect scanning area in decoder.
	     */
        function rotateLandscape_toOrientation(scanningRect1, to_orientation) {

            if (to_orientation < 0 || to_orientation > 2) return;

            var x1 = scanningRect1.x;
            var y1 = scanningRect1.y;
            var w1 = scanningRect1.width;
            var h1 = scanningRect1.height;

            var from_orientation = 0;
            //[to_orientation] x [from_orientation] //transpose it if you want [from][to]
            /*var orientationRotation = [
                //landscape                                 portrait                                landscape flipped
                [{ x: x1, y: y1, width: w1, height: h1 }, { x: (100 - y1 - h1), y: x1, width: h1, height: w1 }, { x: (100 - x1 - w1), y: (100 - y1 - h1), width: w1, height: h1 }], //landscape
                [{ x: y1, y: (100 - x1 - w1), width: h1, height: w1 }, { x: x1, y: y1, width: w1, height: h1 }, { x: (100 - y1 - h1), y: (100 - x1 - w1), width: h1, height: w1 }], //portrait
                [{ x: (100 - x1 - w1), y: (100 - y1 - h1), width: w1, height: h1 }, { x: y1, y: (100 - x1 - w1), width: h1, height: w1 }, { x: x1, y: y1, width: w1, height: h1 }]  //landscape flipped
            ];*/

            var orientationRotationT = [
                //landscape                                 portrait                                landscape flipped
                [{ x: x1, y: y1, width: w1, height: h1 }, { x: y1, y: (100 - x1 - w1), width: h1, height: w1 }, { x: (100 - x1 - w1), y: (100 - y1 - h1), width: w1, height: h1 }]//, //landscape
                //[{ x: (100 - y1 - h1), y: x1, width: h1, height: w1 }, { x: x1, y: y1, width: w1, height: h1 }, { x: y1, y: (100 - x1 - w1), width: h1, height: w1 }], //portrait
                //[{ x: (100 - x1 - w1), y: (100 - y1 - h1), width: w1, height: h1 }, { x: (100 - y1 - h1), y: (100 - x1 - w1), width: h1, height: w1 }, { x: x1, y: y1, width: w1, height: h1 }]  //landscape flipped
            ];

            return orientationRotationT[from_orientation][to_orientation];
        }

        /**
	     * Transforms the viewfinder to reflect scanning area in decoder 2.
	     */
        function scaleFull_toPartial(scanningRect1, partialScale, scaleHeight) {

            if (partialScale < 0.01 || partialScale > 1.0 || scaleHeight < 0 || scaleHeight > 1) return;

            var x1 = scanningRect1.x;
            var y1 = scanningRect1.y;
            var w1 = scanningRect1.width;
            var h1 = scanningRect1.height;

            var cropScaleP = (1 - partialScale) * 100; //on [0,100) scale
            //[scaleDirection]
            var scale_and_center = [
                //scale down and translate to justified position
                { x: ((cropScaleP / 2) + (x1 * partialScale)), y: y1, width: (w1 * partialScale), height: h1 }, //scaleWidth
                { x: x1, y: ((cropScaleP / 2) + (y1 * partialScale)), width: w1, height: (h1 * partialScale) }  //scaleHeight
            ];

            return scale_and_center[scaleHeight];
        }

        /**
         * Computes the int value of codeMask for all supported barcodes and places them in codeMasksArray.
         * Initializes the untouchedScanningRectsArray and untouchedScanningRectsUnion.
         */
        function initCodeMasksArray_and_untouchedScanningRectsArray_and_untouchedScanningRectsUnion(_numberOfSupporedCodes)
        {
            var _i = 0;
            for (; _i < _numberOfSupporedCodes; _i++) {
                codeMasksArray.push(Math.pow(2, _i));
                untouchedScanningRectsArray.push(WindowsComponnent.BarcodeHelper.mwBgetScanningRect(codeMasksArray[_i]));
            }

            untouchedScanningRectsUnion = WindowsComponnent.BarcodeHelper.mwBgetScanningRect(0);

            if (debug_print) console.log('-> one-time initCodeMasksArray_and_untouchedScanningRectsArray_and_untouchedScanningRectsUnion ');
        }

        /**
         * Calculates the scanningRects for all codes and sets them in the SDK.
         */
        function calcScanningRect(is_portrait, _isDivArHigher, _croppedCameraAreaScale)
        {
            if (debug_print) console.log('calcScanningRect(' + is_portrait + ' ' + _isDivArHigher + ' ' + _croppedCameraAreaScale + ') ');

            var viewfinderAreaScale = (1 - _croppedCameraAreaScale);
            
            // determine if cutting is done by width or height
            if ((!is_portrait && _isDivArHigher) || (is_portrait && !_isDivArHigher))
            {
                // it's done by height, rare                
                var codeMask, 
                    scanningRectTM;

                var _i = 0;
                for (; _i < numberOfSupporedCodes; _i++) {
                    
                    // copy needed primitive types BY VALUE | these functions create new structures and assign primitive types by value | no ref here
                    scanningRectTM = rotateLandscape_toOrientation(untouchedScanningRectsArray[_i], viewfinderOnScreenView.orientation);
                    scanningRectTM = scaleFull_toPartial(scanningRectTM, viewfinderAreaScale, heightIndex);

                    // create rect
                    var csharpScanningRectTM = WindowsComponnent.BarcodeHelper.createRect(scanningRectTM.x, scanningRectTM.y, scanningRectTM.width, scanningRectTM.height);
                    
                    // set in decoder
                    codeMask = codeMasksArray[_i];
                    WindowsComponnent.BarcodeHelper.mwBsetScanningRect(codeMask, csharpScanningRectTM);
                }
            }
            else
            {
                // it's by width, most common
                var codeMask,
                    scanningRectTM;

                var _i = 0;
                for (; _i < numberOfSupporedCodes; _i++) {
                    
                    // copy needed primitive types BY VALUE | these functions create new structures and assign primitive types by value | no ref here
                    scanningRectTM = rotateLandscape_toOrientation(untouchedScanningRectsArray[_i], viewfinderOnScreenView.orientation);
                    scanningRectTM = scaleFull_toPartial(scanningRectTM, viewfinderAreaScale, widthIndex);

                    // create rect
                    var csharpScanningRectTM = WindowsComponnent.BarcodeHelper.createRect(scanningRectTM.x, scanningRectTM.y, scanningRectTM.width, scanningRectTM.height);

                    // set in decoder
                    codeMask = codeMasksArray[_i];
                    WindowsComponnent.BarcodeHelper.mwBsetScanningRect(codeMask, csharpScanningRectTM);
                }
            }
			
            if (debug_print) {
				//get viewfinder
				var viewfnderUnionRect = WindowsComponnent.BarcodeHelper.mwBgetScanningRect(0);
				console.log('viewfinderUnion after TM ' + viewfnderUnionRect.x + ' ' + viewfnderUnionRect.y + ' ' + viewfnderUnionRect.width + ' ' + viewfnderUnionRect.height + ' ');
			}
        }

        /**
         * Calculates overlay coordinates for canvas and calls calcScanningRect.
         */
        function calcPreview(is_portrait) {
			
            if (debug_print) console.log('calcPreview(' + is_portrait + ') ');
			
            var windowWidth = window.innerWidth;
            var windowHeight = window.innerHeight;
            var window_AR = windowWidth / windowHeight;
			
            var rootDivInviewTop = document.getElementById("root-div-inview").offsetTop;
            var rootDivInviewLeft = document.getElementById("root-div-inview").offsetLeft;
			
            var rootDivInviewWidth = document.getElementById("root-div-inview").offsetWidth;
            var rootDivInviewHeigth = document.getElementById("root-div-inview").offsetHeight;
            var rootDivInview_AR = rootDivInviewWidth / rootDivInviewHeigth;
			
            var cameraWidth = HARDWARE_CAMERA_RESOLUTION.width;
            var cameraHeight = HARDWARE_CAMERA_RESOLUTION.height;
            var camera_AR = cameraWidth / cameraHeight;
			
            if (is_portrait) {
                cameraWidth = HARDWARE_CAMERA_RESOLUTION.height;
                cameraHeight = HARDWARE_CAMERA_RESOLUTION.width;
                camera_AR = cameraWidth / cameraHeight;
            }

            if (rootDivInview_AR == camera_AR) return;
            else
                if (rootDivInview_AR > camera_AR)
                {
                    // fill div by width, most likely portrait
                    var scalingFactor = rootDivInviewWidth / cameraWidth;
                    var new_cameraHeight = cameraHeight * scalingFactor;
                    var croppedCameraArea = new_cameraHeight - rootDivInviewHeigth;
					
					// get percentages:
					var croppedCameraAreaScale = croppedCameraArea / new_cameraHeight;
                    var translatedCameraTopP = -(croppedCameraAreaScale / 2) * 100;
					
                    capturePreview.style.cssText = "position: absolute; margin: auto; top: 0; bottom: 0; width: 100%; height: auto;";
					
                    calcScanningRect(is_portrait, true, croppedCameraAreaScale);
                }
                else
                    if (rootDivInview_AR < camera_AR) // default remaining
                    {
                        //fill div by height
                        var scalingFactor = rootDivInviewHeigth / cameraHeight;
                        var new_cameraWidth = cameraWidth * scalingFactor;
                        var croppedCameraArea = new_cameraWidth - rootDivInviewWidth;

                        // get percentages
                        var croppedCameraAreaScale = croppedCameraArea / new_cameraWidth;
                        var translateCameraLeftP = -(croppedCameraAreaScale / 2) * 100;

                        var croppedinDivAreaScale = croppedCameraArea / rootDivInviewWidth;
                        var translateinDivLeftP = -(croppedinDivAreaScale / 2) * 100;
						
                        capturePreview.style.cssText = "position: absolute; margin-left: " + translateinDivLeftP + "%; width: auto; height: 100%;";
						
                        calcScanningRect(is_portrait, false, croppedCameraAreaScale);
                    }
        }

        /**
         * Draws overlay lines inside the canvasOverlay area.
		 * @param  {float} x1 canvasOverlay Left
		 * @param  {float} y1 canvasOverlay Top
		 * @param  {float} w1 canvasOverlay Width
		 * @param  {float} h1 canvasOverlay Left
		 * @param  {float} lineThickness CanvasBlinkingLine lineThickness
         */
        function drawOverlayLines(x1, y1, w1, h1, lineThickness) {

            var startLeft = x1;
            var startTop = y1;
            canvasBlinkingLineV.style.left = canvasBlinkingLineH.style.left = (startLeft - 0) + "px";
            canvasBlinkingLineV.style.top = canvasBlinkingLineH.style.top = (startTop - 0) + "px";

            canvasBlinkingLineV.width = canvasBlinkingLineH.width = w1;
            canvasBlinkingLineV.height = canvasBlinkingLineH.height = h1;


            canvasBlinkingLineV.width = lineThickness;
            canvasBlinkingLineV.style.left = (startLeft + (w1 / 2) - (canvasBlinkingLineV.width / 2) - 0) + "px";

            canvasBlinkingLineH.height = lineThickness;
            canvasBlinkingLineH.style.top = (startTop + (h1 / 2) - (canvasBlinkingLineH.height / 2) - 0) + "px";
			
			// NOTE: at the time this is called the canvas lines have already been added to the html document and the animation has been started and can't be changed at this point
			// SOLUTION: execute these instructions in createPreview before canvas lines are added (because resize and subsequently this function is called after)
			//canvasBlinkingLineV.style.backgroundColor = canvasBlinkingLineH.style.backgroundColor = mwOverlayProperties.lineColor;
            //canvasBlinkingLineV.style.animation = canvasBlinkingLineH.style.animation = "fadeColor " + mwOverlayProperties.blinkingRate + "ms infinite";
        }

        /**
         * Resizes the canvas to fill browser window dynamically.
         */
        function resizeCanvas() {
            // get viewfinder (landscape)
            var viewfinderUnionRect = untouchedScanningRectsUnion; // it's a pointer, but it doesn't matter since no changes will be made
			
			if (debug_print) console.log('resizeCanvas ');

            // set canvas over preview
            canvasOverlay.style.top = capturePreviewFrame.style.top;
            canvasOverlay.style.left = capturePreviewFrame.style.left;
            canvasOverlay.width = capturePreviewFrame.offsetWidth; // capturePreviewFrame is a <div> element so it doesn't have width and height properties
            canvasOverlay.height = capturePreviewFrame.offsetHeight;

            // linking image buttons to position
            fullscreenButtons.flashReference.style.top = (canvasOverlay.offsetTop + 2) + "px";
            fullscreenButtons.flashReference.style.left = canvasOverlay.offsetLeft + "px";            

            fullscreenButtons.zoomReference.style.top = (canvasOverlay.offsetTop + 2) + "px";
            fullscreenButtons.zoomReference.style.left = ((canvasOverlay.offsetWidth + canvasOverlay.offsetLeft) - fullscreenButtons.zoomReference.offsetWidth) + "px";

            // set viewfinder in pixels
            viewfinderOnScreenView.x = canvasOverlay.width * (viewfinderUnionRect.x / 100);
            viewfinderOnScreenView.y = canvasOverlay.height * (viewfinderUnionRect.y / 100);
            viewfinderOnScreenView.width = canvasOverlay.width * (viewfinderUnionRect.width / 100);
            viewfinderOnScreenView.height = canvasOverlay.height * (viewfinderUnionRect.height / 100);

            /**
             * Your drawings need to be inside this function otherwise they will be reset when 
             * you resize the browser window and the canvas goes will be cleared.
             */

            // draw fullcanvas shadow and clear the viewfinder area
            var ctx = canvasOverlay.getContext("2d");
            ctx.fillStyle = "rgba(0, 0, 0, 0.5)";
            ctx.fillRect(0, 0, canvasOverlay.width, canvasOverlay.height);
            ctx.clearRect(viewfinderOnScreenView.x, viewfinderOnScreenView.y, viewfinderOnScreenView.width, viewfinderOnScreenView.height);

            // draw red viewfinder border
            ctx.lineWidth = mwOverlayProperties.borderWidth;
            ctx.strokeStyle = mwOverlayProperties.lineColor;
            ctx.strokeRect(viewfinderOnScreenView.x, viewfinderOnScreenView.y, viewfinderOnScreenView.width, viewfinderOnScreenView.height);

            // draw red lines
            drawOverlayLines(canvasOverlay.offsetLeft + viewfinderOnScreenView.x,
                            canvasOverlay.offsetTop + viewfinderOnScreenView.y,
                            viewfinderOnScreenView.width,
                            viewfinderOnScreenView.height,
                            mwOverlayProperties.linesWidth);
        }

        // *** IMAGE BUTTONS ***

        /**
         * Handles the click event of the flash button.
         */
        function clickedFlash() {
            //flash
            if (debug_print) console.log('clicked flash');

            if (fullscreenButtons.flashState == -1) return;
            else {
                //torchLight.powerPercent = 100;
                if (fullscreenButtons.flashState == 0) {
                    torchLight.enabled = true;
                    fullscreenButtons.flashState = 1;
                    fullscreenButtons.flashReference.getElementsByTagName("img")[0].src = fullscreenButtons.flash1;
                }
                else {
                    torchLight.enabled = false;
                    fullscreenButtons.flashState = 0;
                    fullscreenButtons.flashReference.getElementsByTagName("img")[0].src = fullscreenButtons.flash0;
                }
            }
        }

        /**
         * Handles the click event of the zoom button.
         */
        function clickedZoom() {
            //zoom
            if (debug_print) console.log('clicked zoom');

            if (fullscreenButtons.zoomState == -1) return;

            var zoomSettings = new Windows.Media.Devices.ZoomSettings();

            // toggle zoom levels 1 - 2 - 4
            /*if (zoomControl.value == zoomControl.min) zoomSettings.value = zoomControl.max / 2;
            else if (zoomControl.value == (zoomControl.max / 2)) zoomSettings.value = zoomControl.max;
            else if (zoomControl.value == zoomControl.max) zoomSettings.value = zoomControl.min;*/

            // new way to handle things courtesy of setZoomLevels
            fullscreenButtons.zoom_lvl_ini = ((++fullscreenButtons.zoom_lvl_ini) % 3);
            zoomSettings.value = fullscreenButtons.zoomLevels[fullscreenButtons.zoom_lvl_ini];

            zoomSettings.mode = zoomControl.supportedModes.first();
            zoomControl.configure(zoomSettings);
        }

        /**
         * Creates a preview frame and necessary objects.
         */
        function createPreview() {
			
            if (debug_print) console.log('createPreview ');

            // Create partial screen preview
            var capturePreviewFrameStyle = document.createElement('link');
            capturePreviewFrameStyle.rel = "stylesheet";
            capturePreviewFrameStyle.type = "text/css";
            capturePreviewFrameStyle.href = urlutil.makeAbsolute("/www/css/plugin-barcodeScanner.css");

            document.head.appendChild(capturePreviewFrameStyle);

            capturePreviewFrame = document.createElement('div');
            capturePreviewFrame.id = "root-div-inview";
            capturePreviewFrame.className = "barcode-scanner-wrap-inview";
            
			// FEATURE: can transform based on args[4] and sets capturePreviewFrame.style.cssText
            //anchorView_toOrientation(args[0], args[1], args[2], args[3], args[4], viewfinderOnScreenView.orientation); //TO BE REMOVED
            anchorView_toOrientation(partialView.x, partialView.y, partialView.width, partialView.height, partialView.orientation, viewfinderOnScreenView.orientation);
            
            proxyWrapCapturePreview = document.createElement('div');
            proxyWrapCapturePreview.className = "proxy-wrap-of-preview-inview";
            proxyWrapCapturePreview.style.cssText = "width: 100%; height: 100%;";
            
            capturePreview = document.createElement("video");
            capturePreview.id = "video-layer";
            capturePreview.className = "barcode-scanner-preview-inview";
            capturePreview.addEventListener('click', function () {
                focus();
            });

            proxyWrapCapturePreview.appendChild(capturePreview);

            // create canvas for Overlay
            canvasOverlay = document.createElement("canvas");
            canvasOverlay.id = "canvas-overlay";
            canvasOverlay.className = "shadow-overlay";

            // create canvas for line
            canvasBlinkingLineV = document.createElement("canvas");
            canvasBlinkingLineV.id = "canvas-line-v";
            canvasBlinkingLineV.className = "blinking-line";

            // create canvas for line
            canvasBlinkingLineH = document.createElement("canvas");
            canvasBlinkingLineH.id = "canvas-line-h";
            canvasBlinkingLineH.className = "blinking-line";

            // obtain a ref
            mwBlinkingLines.v = canvasBlinkingLineV;
            mwBlinkingLines.h = canvasBlinkingLineH;
			
            // SOLUTION: instead of calling resize and draw set just the style (turns out you can use animation-play-state)
			canvasBlinkingLineV.style.backgroundColor = canvasBlinkingLineH.style.backgroundColor = mwOverlayProperties.lineColor;
			canvasBlinkingLineV.style.animation = canvasBlinkingLineH.style.animation = "fadeColor " + mwOverlayProperties.blinkingRate + "ms infinite";
			
			// scanningRects need to be reset in the sdk //hereX
			WindowsComponnent.BarcodeHelper.resetScanningRects();

			if (jsSettingsScanningRectsAltered)
			{
			    var _codeMask;
			    for (var _i = 0; _i < numberOfSupporedCodes; _i++)
			    {
			        _codeMask = Math.pow(2, _i);
			        if (jsSettingsScanningRects[_codeMask].set)
			        {
			            WindowsComponnent.BarcodeHelper.mwBsetScanningRect(
                        Math.pow(2, _i),
                        WindowsComponnent.BarcodeHelper.createRect(
                            jsSettingsScanningRects[_codeMask].x,
                            jsSettingsScanningRects[_codeMask].y,
                            jsSettingsScanningRects[_codeMask].width,
                            jsSettingsScanningRects[_codeMask].height
                        ));
			        }
			    }
			}
			
            initCodeMasksArray_and_untouchedScanningRectsArray_and_untouchedScanningRectsUnion(numberOfSupporedCodes);

            // PartialView FLash and Zoom Images
            // create image buttons for flash
            fullscreenButtons.flashReference = document.createElement("div");
            fullscreenButtons.flashReference.id = "flash-button";

            var divImage = document.createElement("img");
            divImage.id = "flash-image";
            divImage.src = fullscreenButtons.flash0;

            // if not enabled hide the button as if it doesn't exist at all
            if (fullscreenButtons.hideFlash) fullscreenButtons.flashReference.style.display = "none";

            fullscreenButtons.flashReference.appendChild(divImage);

            // and zoom
            fullscreenButtons.zoomReference = document.createElement("div");
            fullscreenButtons.zoomReference.id = "zoom-button";

            var divImage2 = document.createElement("img");
            divImage2.id = "zoom-image";
            divImage2.src = fullscreenButtons.zoom0;

            // if not enabled hide the button as if it doesn't exist at all
            if (fullscreenButtons.hideZoom) fullscreenButtons.zoomReference.style.display = "none";

            fullscreenButtons.zoomReference.appendChild(divImage2);

            fullscreenButtons.flashReference.addEventListener('click', clickedFlash, false);
            fullscreenButtons.zoomReference.addEventListener('click', clickedZoom, false);

            // **style for position needs to be linked to canvasOverlay's position
            fullscreenButtons.flashReference.style.position = "absolute";
            fullscreenButtons.flashReference.style.cssFloat = "none";

            fullscreenButtons.zoomReference.style.position = "absolute";
            fullscreenButtons.zoomReference.style.cssFloat = "none";

            // register an event listener to be notified and execute resizeCanvas upon window resize for desktop only
            if (operatingSystem == 'WINDOWS')
            window.addEventListener('resize', resizeCanvas, false);

            // PROBLEM: on two consecutive orientation changes UI update lag causes unsynchronised preview update
            // SOLUTION: instead of updating preview on orientation change register an event listener for handling UI change which is reflected in changed window' width and height
            else if (operatingSystem == 'WindowsPhone')
            window.addEventListener('resize', resizePartialScannerView, false);

            capturePreviewAlignmentMark = document.createElement('div');
            capturePreviewAlignmentMark.className = "barcode-scanner-mark";

            navigationButtonsDiv = document.createElement("div");
            navigationButtonsDiv.className = "barcode-scanner-app-bar";
            navigationButtonsDiv.onclick = function (e) {
                e.cancelBubble = true;
            };

            /*closeButton = document.createElement("div");
            closeButton.innerText = "close";
            closeButton.className = "app-bar-action action-close";
            navigationButtonsDiv.appendChild(closeButton);

            closeButton.addEventListener("click", cancelPreview, false);*/
            //document.addEventListener('backbutton', cancelPartial, false);
            document.addEventListener("pause", onPause, false);
            document.addEventListener("resume", onResume, false);

            [proxyWrapCapturePreview, capturePreviewAlignmentMark/*, navigationButtonsDiv*/].forEach(function (element) {
                capturePreviewFrame.appendChild(element);
            });

            //DEBUG: USE THIS TO TEST RESIZE
            /*setTimeout(function () {
                //console.log(document.body.innerHTML); //doesn't print longer strings in console
                partialView.x = 10;
                partialView.y = 0;
                partialView.width = 80;
                partialView.height = 30;
                resizePartialScannerView();
            }, 10000);*/
        }

        function focus(controller) {

            var result = WinJS.Promise.wrap();

            if (!capturePreview || capturePreview.paused) {
                // If the preview is not yet playing, there is no sense in running focus
                return result;
            }

            if (!controller) {
                try {
                    controller = capture && capture.videoDeviceController;
                } catch (err) {
                    console.log('Failed to access focus control for current camera: ' + err);
                    return result;
                }
            }

            if (!controller.focusControl || !controller.focusControl.supported) {
                console.log('Focus control for current camera is not supported');
                return result;
            }

            // Multiple calls to focusAsync leads to internal focusing hang on some Windows Phone 8.1 devices
            if (controller.focusControl.focusState === Windows.Media.Devices.MediaCaptureFocusState.searching) {
                return result;
            }

            // The delay prevents focus hang on slow devices
            return WinJS.Promise.timeout(INITIAL_FOCUS_DELAY)
            .then(function () {
                try {
                    return controller.focusControl.focusAsync().then(function () {
                        return result;
                    }, function (e) {
                        // This happens on mutliple taps
                        if (e.number !== OPERATION_IS_IN_PROGRESS) {
                            console.error('focusAsync failed: ' + e);
                            return WinJS.Promise.wrapError(e);
                        }
                        return result;
                    });
                } catch (e) {
                    // This happens on mutliple taps
                    if (e.number !== OPERATION_IS_IN_PROGRESS) {
                        console.error('focusAsync failed: ' + e);
                        return WinJS.Promise.wrapError(e);
                    }
                    return result;
                }
            });
        }

        function setupFocus(focusControl) {

            function supportsFocusMode(mode) {
                return focusControl.supportedFocusModes.indexOf(mode).returnValue;
            }

            if (!focusControl || !focusControl.supported || !focusControl.configure) {
                return WinJS.Promise.wrap();
            }

            var FocusMode = Windows.Media.Devices.FocusMode;
            var focusConfig = new Windows.Media.Devices.FocusSettings();
            focusConfig.autoFocusRange = Windows.Media.Devices.AutoFocusRange.normal;

            // Determine a focus position if the focus search fails:
            focusConfig.disableDriverFallback = false;

            if (supportsFocusMode(FocusMode.continuous)) {
                console.log("Device supports continuous focus mode");
                focusConfig.mode = FocusMode.continuous;
            } else if (supportsFocusMode(FocusMode.auto)) {
                console.log("Device doesn\'t support continuous focus mode, switching to autofocus mode");
                focusConfig.mode = FocusMode.auto;
            }

            focusControl.configure(focusConfig);

            // Continuous focus should start only after preview has started. See 'Remarks' at 
            // https://msdn.microsoft.com/en-us/library/windows/apps/windows.media.devices.focuscontrol.configure.aspx
            function waitForIsPlaying() {
                var isPlaying = !capturePreview.paused && !capturePreview.ended && capturePreview.readyState > 2;

                if (!isPlaying) {
                    return WinJS.Promise.timeout(CHECK_PLAYING_TIMEOUT)
                    .then(function () {
                        return waitForIsPlaying();
                    });
                }

                return focus();
            }

            return waitForIsPlaying();
        }

        /**
         * Starts stream transmission to preview frame and then run barcode search.
         */
        function startPreview() {
            return findCamera()
            .then(function (id) {
                var captureSettings = new Windows.Media.Capture.MediaCaptureInitializationSettings();
                captureSettings.streamingCaptureMode = Windows.Media.Capture.StreamingCaptureMode.video;
                captureSettings.photoCaptureSource = Windows.Media.Capture.PhotoCaptureSource.videoPreview;
                captureSettings.videoDeviceId = id;

                capture = new Windows.Media.Capture.MediaCapture();
                return capture.initializeAsync(captureSettings);
            })
            .then(function () {

                var controller = capture.videoDeviceController;
                var deviceProps = controller.getAvailableMediaStreamProperties(Windows.Media.Capture.MediaStreamType.videoRecord);

                fullscreenButtons.flashControlRef = torchLight = controller.torchControl; //HERE_9
                if (torchLight.supported)
                {

                    console.log('Torch is supported.'); // torch / flash / light
                    //torchLight.powerPercent = 100;
                    setTimeout(function () {
                        if (fullscreenButtons.flashControlRef_enabled) {
                            fullscreenButtons.flashControlRef.enabled = torchLight.enabled = true;
                            fullscreenButtons.flashState = 1;
                            fullscreenButtons.flashReference.getElementsByTagName("img")[0].src = fullscreenButtons.flash1;
                            console.log('Torch is turned on.');
                        }
                        else {
                            fullscreenButtons.flashControlRef.enabled = torchLight.enabled = false;
                            fullscreenButtons.flashState = 0;
                            fullscreenButtons.flashReference.getElementsByTagName("img")[0].src = fullscreenButtons.flash0;
                        }
                    }, 100); //950XL: 39, 640: 50
                   
                    /**/
                    //fullscreenButtons.flashReference.getElementsByTagName("img")[0].src = fullscreenButtons.flash0
                }
                else
                {
                    console.log('Torch is NOT supported.'); //torch / flash / light
                    //torchLight.powerPercent = 0;
                    //torchLight.enabled = false;
                    //fullscreenButtons.flashReference.getElementsByTagName("img")[0].src = fullscreenButtons.flash9;
                    fullscreenButtons.flashReference.style.display = "none";
                    fullscreenButtons.flashState = -1;
                }

                fullscreenButtons.zoomControlRef = zoomControl = controller.zoomControl;
                if (zoomControl.supported)
                {
                    console.log('Zoom is supported.');
                    console.log('Zoom levels ' + zoomControl.min + '-' + zoomControl.max); // 1-4
                    /*var zoomSettings = new Windows.Media.Devices.ZoomSettings();
                    zoomSettings.value = zoomControl.max;
                    zoomSettings.mode = zoomControl.supportedModes.first();
                    zoomControl.configure(zoomSettings);*/

                    // handle custom zoom levels | if custom values are out of bounds set defaults
                    if (fullscreenButtons.zoomLevels[1] < zoomControl.min || fullscreenButtons.zoomLevels[1] > zoomControl.max)
                        fullscreenButtons.zoomLevels[1] = zoomControl.max / 2;

                    if (fullscreenButtons.zoomLevels[2] < zoomControl.min || fullscreenButtons.zoomLevels[2] > zoomControl.max)
                        fullscreenButtons.zoomLevels[2] = zoomControl.max;

                    if (!(fullscreenButtons.zoom_lvl_ini == 0 || fullscreenButtons.zoom_lvl_ini == 1 || fullscreenButtons.zoom_lvl_ini == 2))
                        fullscreenButtons.zoom_lvl_ini = 0;

					// apply ini zoomLevel
                    var zoomSettings = new Windows.Media.Devices.ZoomSettings();

                    var zoomControlRef = fullscreenButtons.zoomControlRef;
                    zoomSettings.value = fullscreenButtons.zoomLevels[fullscreenButtons.zoom_lvl_ini];

                    zoomSettings.mode = zoomControlRef.supportedModes.first();
                    zoomControlRef.configure(zoomSettings);														  
                    //fullscreenButtons.zoomReference.getElementsByTagName("img")[0].src = fullscreenButtons.zoom0;
                }
                else
                {
                    console.log('Zoom is NOT supported.');
                    //fullscreenButtons.zoomReference.getElementsByTagName("img")[0].src = fullscreenButtons.zoom9;
                    fullscreenButtons.zoomReference.style.display = "none";
                    fullscreenButtons.zoomState = -1;
                }

                deviceProps = Array.prototype.slice.call(deviceProps);
                deviceProps = deviceProps.filter(function (prop) {
                    // filter out streams with "unknown" subtype - causes errors on some devices
                    return prop.subtype !== "Unknown";
                }).sort(function (propA, propB) {
                    // sort properties by resolution
                    return propB.width - propA.width;
                });

                // find a resolution as USE_CAMERA_RESOLUTION or the next lower available
                var resolutionListIndex = 0;
                var resolutionListLength = deviceProps.length;
                do {
                    if (deviceProps[resolutionListIndex].height < USE_CAMERA_RESOLUTION[heightIndex]) break;
                    else if (deviceProps[resolutionListIndex].height == USE_CAMERA_RESOLUTION[heightIndex]) break;
                    resolutionListIndex++;
                } while (resolutionListIndex < resolutionListLength);

                var maxResProps = deviceProps[resolutionListIndex];
                HARDWARE_CAMERA_RESOLUTION.width = maxResProps.width;
                HARDWARE_CAMERA_RESOLUTION.height = maxResProps.height;

                return controller.setMediaStreamPropertiesAsync(Windows.Media.Capture.MediaStreamType.videoRecord, maxResProps)
                .then(function () {
                    return {
                        capture: capture,
                        width: maxResProps.width,
                        height: maxResProps.height
                    };
                });
            })
            .then(function (captureSettings) {
                capturePreview.msZoom = true;
                capturePreview.src = URL.createObjectURL(capture);
                capturePreview.play();


                // Insert preview frame and controls into page
                document.body.appendChild(capturePreviewFrame);
                document.body.appendChild(canvasOverlay);
                document.body.appendChild(canvasBlinkingLineV);
                document.body.appendChild(canvasBlinkingLineH);
                document.body.appendChild(fullscreenButtons.flashReference);
                document.body.appendChild(fullscreenButtons.zoomReference);

                DOM_complete = true;
				
                calcPreview(false);
				resizeCanvas();

                return setupFocus(captureSettings.capture.videoDeviceController.focusControl)
                .then(function () {
                    Windows.Graphics.Display.DisplayInformation.getForCurrentView().addEventListener("orientationchanged", updatePreviewForRotation, false);
                    return updatePreviewForRotation();
                })
                .then(function () {
                    return captureSettings;
                });
            });
        }

        /**
         * Removes preview frame and corresponding objects from window.
         */
        function destroyPreview() {

            Windows.Graphics.Display.DisplayInformation.getForCurrentView().removeEventListener("orientationchanged", updatePreviewForRotation, false);
            //document.removeEventListener('backbutton', cancelPartial); 
			cancelPartial = null;
			
            if (operatingSystem == 'WINDOWS')
            window.removeEventListener('resize', resizeCanvas, false);

            else if (operatingSystem == 'WindowsPhone')
            window.removeEventListener('resize', resizePartialScannerView, false);

            window.removeEventListener("pause", onPause, false);
            window.removeEventListener("resume", onResume, false);

            capturePreview.pause();
            capturePreview.src = null;

            if (capturePreviewFrame) {
                document.body.removeChild(capturePreviewFrame);
                document.body.removeChild(canvasOverlay);
                document.body.removeChild(canvasBlinkingLineV);
                document.body.removeChild(canvasBlinkingLineH);
                document.body.removeChild(fullscreenButtons.flashReference);
                document.body.removeChild(fullscreenButtons.zoomReference);
            }

            reader && reader.stop();
            reader = null;

            capture && capture.stopRecordAsync();
            capture = null;
        }

        /**
         * Stops preview and then call success callback with cancelled=true
         * See https://github.com/phonegap-build/BarcodeScanner#using-the-plugin
         */
        cancelPartial = function cancelPreview() {
            reader && reader.stop();
			
			anyScannerStarted = false;
			anyReader = null;
			//document.getElementById("b1").disabled = false;
            //document.getElementById("b2").disabled = false;

			success({
			    code: "",
			    type: "Cancel" /*BARCODE_FORMAT[result.barcodeFormat]*/,
			    isGS1: null,
			    bytes: "",
			    location: null,
			    imageWidth: null,
			    imageHeight: null,
			    cancelled: true
			});
        }

        WinJS.Promise.wrap(createPreview())
        .then(function () {
            return startPreview();
        })
        .then(function (captureSettings) {
            anyReader = reader = BarcodeReader.get(captureSettings.capture); // obtain ref (inview)
            captureSettings.capture;
            reader.init(captureSettings.capture, /*1280*/captureSettings.width, /*720*/captureSettings.height); // ok

            // Add a small timeout before capturing first frame otherwise
            // we would get an 'Invalid state' error from 'getPreviewFrameAsync'
            return WinJS.Promise.timeout(200)
            .then(function () {
                return reader.readCode();
            });
        })
        .done(function (result) {
            destroyPreview();
			
			anyScannerStarted = false;
			anyReader = null;
			//document.getElementById("b1").disabled = false;
			//document.getElementById("b2").disabled = false;
			
            /**
               * result.code - string representation of barcode result
               * result.type - type of barcode detected or 'Cancel' if scanning is canceled
               * result.bytes - bytes array of raw barcode result
               * result.isGS1 - (boolean) barcode is GS1 compliant
               * result.location - contains rectangle points p1,p2,p3,p4 with the corresponding x,y
               * result.imageWidth - Width of the scanned image
               * result.imageHeight - Height of the scanned image
               */
            success({
                code: result && result.text,
                type: result && WindowsComponnent.BarcodeHelper.getBarcodeName(result.type) /*BARCODE_FORMAT[result.barcodeFormat]*/,
                isGS1: result && result.isGS1,
                bytes: result && result.bytes,
                location: result && result.location,
                imageWidth: result && result.imageWidth,
                imageHeight: result && result.imageHeight,
                cancelled: !result
            });
        }, function (error) {
            destroyPreview();
            fail(error);
        });
    },

    // ADDITIONAL WRAPPER FUNCTIONS ----------------------------------------------------------------------------------------------------------------------------------

    /**
     * Initializes the decoder
     */
    initDecoder: function (success, fail) {
        WindowsComponnent.BarcodeHelper.initDecoder();
        success();
    },

    /**
     * Registers the SDK
     */
    registerSDK: function (success, fail, args) {
        var key = ((typeof args[0]) == 'string') ? args[0] : '';

        if (debug_print) console.log('REGISTER HAS ARRIVED');

        var retVal = -1;

        retVal = WindowsComponnent.MWBarcodeScanner.registerSDK(key);
        success(retVal);
    },

    /**
     * Sets the activeCodes
     */
    setActiveCodes: function (success, fail, args) {
        var activeCodes = ((typeof args[0]) == 'number') ? args[0] : 0;

        if (activeCodes != 0)
        WindowsComponnent.MWBarcodeScanner.setActiveCodes(activeCodes);
    },

    /**
     * Sets the activeSubcodes
     */
    setActiveSubcodes: function (success, fail, args) {
        var codeMask        = ((typeof args[0]) == 'number') ? args[0] : 0;
        var activeSubcodes  = ((typeof args[1]) == 'number') ? args[1] : 0;

        if ((codeMask + activeSubcodes) != 0)
        WindowsComponnent.MWBarcodeScanner.setActiveSubcodes(codeMask, activeSubcodes);
    },

    /**
     * Sets the flags
     */
    setFlags: function (success, fail, args) {
        var codeMask    = ((typeof args[0]) == 'number') ? args[0] : 0;
        var flags       = ((typeof args[1]) == 'number') ? args[1] : 0;

        if ((codeMask + flags) != 0)
        WindowsComponnent.MWBarcodeScanner.setFlags(codeMask, flags);
    },

    /**
     * Sets the minimumLength
     */
    setMinLength: function (success, fail, args) {
        var codeMask    = ((typeof args[0]) == 'number') ? args[0] : 0;
        var minLength   = ((typeof args[1]) == 'number') ? args[1] : 0;

        if ((codeMask + minLength) != 0)
        WindowsComponnent.MWBarcodeScanner.setMinLength(codeMask, minLength);
    },

    /**
     * Sets the direction
     */
    setDirection: function (success, fail, args) {
        var direction = ((typeof args[0]) == 'number') ? args[0] : 4;
        WindowsComponnent.MWBarcodeScanner.setDirection(direction);
    },

    /**
     * Sets the scanningRect area to be scanned by the decoder
     */
    setScanningRect: function (success, fail, args) {
        var codeMask    = ((typeof args[0]) == 'number') ? args[0] : 0;
        var left        = ((typeof args[1]) == 'number') ? args[1] : 0;
        var top         = ((typeof args[2]) == 'number') ? args[2] : 0;
        var width       = ((typeof args[3]) == 'number') ? args[3] : 100;
        var height      = ((typeof args[4]) == 'number') ? args[4] : 100;

        jsSettingsScanningRects[codeMask].x = left;
        jsSettingsScanningRects[codeMask].y = top;
        jsSettingsScanningRects[codeMask].width = width;
        jsSettingsScanningRects[codeMask].height = height;
        jsSettingsScanningRects[codeMask].set = true;
        jsSettingsScanningRectsAltered = true;

        if (codeMask != 0)
        WindowsComponnent.MWBarcodeScanner.setScanningRect(codeMask, left, top, width, height);
    },

    /**
     * Sets the decoder level
     */
    setLevel: function (success, fail, args) {
        var level = ((typeof args[0]) == 'number') ? args[0] : 2;
        WindowsComponnent.MWBarcodeScanner.setLevel(level);
    },

    /**
     * Sets the overlayMode
     */
    setOverlayMode: function (success, fail, args) {
        mwOverlayProperties.mode = ((typeof args[0]) == 'number') ? args[0] : 1;
    },

    /**
     * Sets the visibility of the blinking line
     */
    setBlinkingLineVisible: function (success, fail, args) {
        var visible = ((typeof args[0]) == 'boolean') ? args[0] : true;

        if (visible)    mwOverlayProperties.lineColor = "rgba(255, 0, 0, 1.0)"; // the color will be reset to red
        else            mwOverlayProperties.lineColor = "rgba(255, 0, 0, 0.0)"; // will affect the viewfinder border as well
    },

    /**
     * Sets the pauseMode
     */
    setPauseMode: function (success, fail, args) {

        /* What happens when the scanner is paused:
            *
            *   PM_NONE             - Nothing happens
            *   PM_PAUSE            - Blinking lines are replaced with a pause view
            *   PM_STOP_BLINKING    - Blinking lines stop blinking
            *
            *   Default value is PM_PAUSE
        */
        mwOverlayProperties.pauseMode = ((typeof args[0]) == 'number') ? args[0] : 1; // currently PM_STOP_BLINKING
    },

    /**
     * Enable or disable 720p camera resolution
     */
    enableHiRes: function (success, fail, args) {
        var enableHiRes = ((typeof args[0]) == 'boolean') ? args[0] : true;

        if (enableHiRes)    USE_CAMERA_RESOLUTION = supportedResolutions[HD];
        else                USE_CAMERA_RESOLUTION = supportedResolutions[Normal];
    },

    /**
     * Show image buttons or not in full screen
     */
    enableFlash: function (success, fail, args) {
        fullscreenButtons.hideFlash = ((typeof args[0]) == 'boolean') ? !args[0] : false;
    },

    /**
     * Turns the flash ON or OFF
     */
    turnFlashOn: function (success, fail, args) {
        var flashOn = ((typeof args[0]) == 'boolean') ? args[0] : false;
        fullscreenButtons.flashControlRef_enabled = flashOn;
    },

    /**
     * Implements toggleFlash
     */
    toggleFlash: function (success, fail) {
        if (anyScannerStarted) {
            //flash
            if (debug_print) console.log('clicked flash');

            if (fullscreenButtons.flashState == -1) return;
            else
            {
                //torchLight.powerPercent = 100;
                if (fullscreenButtons.flashState == 0) {
                    fullscreenButtons.flashControlRef.enabled = true;
                    fullscreenButtons.flashState = 1;
                    fullscreenButtons.flashReference.getElementsByTagName("img")[0].src = fullscreenButtons.flash1;
                }
                else {
                    fullscreenButtons.flashControlRef.enabled = false;
                    fullscreenButtons.flashState = 0;
                    fullscreenButtons.flashReference.getElementsByTagName("img")[0].src = fullscreenButtons.flash0;
                }
            }
        }
    },

    /**
     * Show image buttons or not in full screen
     */
    enableZoom: function (success, fail, args) {
        fullscreenButtons.hideZoom = ((typeof args[0]) == 'boolean') ? !args[0] : false;
    },

    /**
     * Sets the camera zoom levels
     */
    setZoomLevels: function (success, fail, args) {

        var zoomLevel1          = ((typeof args[0]) == 'number') ? args[0] : 2;
        var zoomLevel2          = ((typeof args[1]) == 'number') ? args[1] : 4;
        var initialZoomLevel    = ((typeof args[2]) == 'number') ? args[2] : 0;

            fullscreenButtons.zoom_lvl_ini = initialZoomLevel;
            fullscreenButtons.zoomLevels[1] = zoomLevel1;
            fullscreenButtons.zoomLevels[2] = zoomLevel2;
    },

    /**
     * Implements toggleZoom of the camera zoom levels
     */
    toggleZoom: function (success, fail) {
        if (anyScannerStarted) {
            if (debug_print) console.log('clicked zoom');

            if (fullscreenButtons.zoomState == -1) return;

            var zoomSettings = new Windows.Media.Devices.ZoomSettings();

            var zoomControlRef = fullscreenButtons.zoomControlRef;
            // toggle zoom levels 1 - 2 - 4
            /*if (zoomControlRef.value == zoomControlRef.min) zoomSettings.value = zoomControlRef.max / 2;
            else if (zoomControlRef.value == (zoomControlRef.max / 2)) zoomSettings.value = zoomControlRef.max;
            else if (zoomControlRef.value == zoomControlRef.max) zoomSettings.value = zoomControlRef.min;*/

            // new way to handle things because of setZoomLevels
            fullscreenButtons.zoom_lvl_ini = ((++fullscreenButtons.zoom_lvl_ini) % 3);
            zoomSettings.value = fullscreenButtons.zoomLevels[fullscreenButtons.zoom_lvl_ini];

            zoomSettings.mode = zoomControlRef.supportedModes.first();
            zoomControlRef.configure(zoomSettings);
        }
    },

    /**
     * Sets the number of maximum threads for the decoder to use
     */
    setMaxThreads: function (success, fail, args) {
        var maxThreads = ((typeof args[0]) == 'number') ? args[0] : 4;
        WindowsComponnent.MWBarcodeScanner.setMaxThreads(maxThreads); console.log(maxThreads);
    },

    /**
     * Implements the closing of the scanner previously created by calling startScanner
     */
    closeScanner: function (success, fail) {
        if (debug_print) console.log('about to close');
        if (anyReader != null) {
            anyReader && anyReader.stop();

            anyScannerStarted = false;
            anyReader = null;
            //document.getElementById("b1").disabled = false;
            //document.getElementById("b2").disabled = false;
        } // nice, finaly ref is useful

        cancelPartial();
    },

    /**
     * Scans an image
     */
    scanImage: function (success, fail, args) {
        var imageURI = ((typeof args[0]) == 'string') ? args[0] : '';

        // clear needs to be done for every scan
        WindowsComponnent.ScannerPage.iniClear();

        if (debug_print) console.log('about to scan image: ' + imageURI);

        // KNOCK EM OUT
        var canvasOverlay = document.createElement("canvas");

        var pad = 20;

        var imageOverlay = document.createElement("img");
        imageOverlay.src = imageURI;

        imageOverlay.onload = function () {
            canvasOverlay.width = imageOverlay.width + (pad * 2);
            canvasOverlay.height = imageOverlay.height + (pad * 2);

            var ctx = canvasOverlay.getContext("2d");

            // draw white background to pad image with white frame
            ctx.fillStyle = "rgba(0, 255, 255, 1.0)";
            ctx.fillRect(0, 0, canvasOverlay.width, canvasOverlay.height);

            ctx.drawImage(imageOverlay, 0, 0, imageOverlay.width, imageOverlay.height,      // source rectangle
                                        pad, pad, imageOverlay.width, imageOverlay.height);   // destination rectangle

            var imgData = ctx.getImageData(0, 0, canvasOverlay.width, canvasOverlay.height);

            var result = WindowsComponnent.ScannerPage.scanImage(imgData.data, imgData.width, imgData.height); // imgData.data is a byte array where each pixel is [RGBA]

            if (debug_print) console.log(result);

            /**
               * result.code - string representation of barcode result
               * result.type - type of barcode detected or 'Cancel' if scanning is canceled
               * result.bytes - bytes array of raw barcode result
               * result.isGS1 - (boolean) barcode is GS1 compliant
               * result.location - contains rectangle points p1,p2,p3,p4 with the corresponding x,y
               * result.imageWidth - Width of the scanned image
               * result.imageHeight - Height of the scanned image
               */
            if (result != null)
                success({
                    code: result && result.text,
                    type: result && WindowsComponnent.BarcodeHelper.getBarcodeName(result.type) /*BARCODE_FORMAT[result.barcodeFormat]*/,
                    isGS1: result && result.isGS1,
                    bytes: result && result.bytes,
                    location: result && result.location,
                    imageWidth: result && result.imageWidth,
                    imageHeight: result && result.imageHeight,
                    cancelled: !result
                });
            else
                fail('No barcode found.'); // PROPER WAY ?
        }
    },

    /**
     * Sets parameters in the SDK
     */
    setParam: function (success, fail, args) {

        var codeMask    = ((typeof args[0]) == 'number') ? args[0] : 0;
        var paramId     = ((typeof args[1]) == 'number') ? args[1] : 0;
        var paramValue  = ((typeof args[2]) == 'number') ? args[2] : 0;

        if ((codeMask | paramId | paramValue) != 0)
        WindowsComponnent.MWBarcodeScanner.setParam(codeMask, paramId, paramValue);
    },

    /**
     * Implements togglePauseResume of the scanner
     */
    togglePauseResume: function (success, fail) {
        if (anyScannerStarted) {
            if (debug_print) {
                if (!WindowsComponnent.ScannerPage.pauseDecoder) console.log('about to pause scanning');
                else console.log('about to unpause scanning');
            }

            WindowsComponnent.MWBarcodeScanner.togglePauseResume();

            if (!WindowsComponnent.ScannerPage.pauseDecoder) {
                mwBlinkingLines.v.style.animationPlayState = "running";
                mwBlinkingLines.h.style.animationPlayState = "running";
            }
            else {
                mwBlinkingLines.v.style.animationPlayState = "paused";
                mwBlinkingLines.h.style.animationPlayState = "paused";
            }
        }
    },

    /**
     * Enables or disables the use of the front camera if available
     */
    useFrontCamera: function (success, fail, args) {
        _useFrontCamera = ((typeof args[0]) == 'boolean') ? args[0] : false;
        // TO-DO | Needs GUID ROTATION_KEY
    },

    /**
     * Enables or disables the parser
     */
    enableParser: function (success, fail, args) {
        var enableParser = ((typeof args[0]) == 'boolean') ? args[0] : false;
        WindowsComponnent.MWBarcodeScanner.enableParser(enableParser);
    },

    /**
     * Sets the mask of the parser to be used
     */
    setActiveParser: function (success, fail, args) {
        var activeParser = ((typeof args[0]) == 'number') ? args[0] : 0;
        WindowsComponnent.MWBarcodeScanner.setActiveParser(activeParser); console.log(activeParser);
    },

    /**
     * Resizes the partialPreview
     */
    resizePartialScanner: function (success, fail, args) {
        if (true) { //instead of _usePartialScanner because this function is called before _usePartialScanner is set

            // IF this is called BEFORE startScannerView then only args should be set
            partialView.x       = ((typeof args[0]) == 'number') ? args[0] : 0;
            partialView.y       = ((typeof args[1]) == 'number') ? args[1] : 0;
            partialView.width   = ((typeof args[2]) == 'number') ? args[2] : 50;
            partialView.height = ((typeof args[3]) == 'number') ? args[3] : 50;

            // ONLY IF startScannerView has completed loading elements in DOM and is actively running should resizeView be called
            if (anyScannerStarted && DOM_complete) resizePartialScannerView();
        }
    },

    /**
     * Enables or disables the use of startScannerView
     */
    usePartialScanner: function (success, fail, args) {
        _usePartialScanner = ((typeof args[0]) == 'boolean') ? args[0] : false;
    },

    // --------------------------------------------------------------------------------------------------------------------------------------


    /**
     * Encodes specified data into barcode
     * @param  {function} success Success callback
     * @param  {function} fail    Error callback
     * @param  {array} args       Arguments array
     */
    encode: function (success, fail, args) {
        fail("Not implemented yet");
    }
};
