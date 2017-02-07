/*
 * Copyright (C) 2012  Manatee Works, Inc.
 *
 */

#import "MWScannerViewController.h"
#import "BarcodeScanner.h"
#import "MWOverlay.h"
#include "MWParser.h"
#include <mach/mach_host.h>

// !!! Rects are in format: x, y, width, height !!!
#define RECT_LANDSCAPE_1D       6, 20, 88, 60
#define RECT_LANDSCAPE_2D       20, 6, 60, 88
#define RECT_PORTRAIT_1D        20, 6, 60, 88
#define RECT_PORTRAIT_2D        20, 6, 60, 88
#define RECT_FULL_1D            6, 6, 88, 88
#define RECT_FULL_2D            20, 6, 60, 88
#define RECT_DOTCODE            30, 20, 40, 60



UIInterfaceOrientationMask param_Orientation = UIInterfaceOrientationMaskPortrait;
int param_activeParser = MWP_PARSER_MASK_NONE;

BOOL param_EnableHiRes = YES;
BOOL param_EnableFlash = YES;
BOOL param_EnableZoom = YES;
BOOL param_closeOnSuccess = YES;


static BOOL param_use60fps = NO;

BOOL param_defaultFlashOn = NO;
static int param_OverlayMode = OM_MW;
int param_ZoomLevel1 = 0;
int param_ZoomLevel2 = 0;
int zoomLevel = 0;
int param_maxThreads = 4;
int activeThreads = 0;
int availableThreads = 0;
BOOL useFrontCamera = NO;


static NSString *DecoderResultNotification = @"DecoderResultNotification";

@implementation MWScannerViewController {
    AVCaptureSession *_captureSession;
    AVCaptureDevice *_device;
    UIImageView *_imageView;
    CALayer *_customLayer;
    AVCaptureVideoPreviewLayer *_prevLayer;
    bool running;
    NSString * lastFormat;
    
    MainScreenState state;
    
    CGImageRef	decodeImage;
    NSString *	decodeResult;
    int width;
    int height;
    int bytesPerRow;
    unsigned char *baseAddress;
    NSTimer *focusTimer;
    
    BOOL statusBarHidden;
    
    
    
}

@synthesize captureSession = _captureSession;
@synthesize prevLayer = _prevLayer;
@synthesize device = _device;
@synthesize state;
@synthesize focusTimer;
@synthesize flashButton;
@synthesize zoomButton;
@synthesize customParams;

#pragma mark -
#pragma mark Initialization


+ (void) initDecoder {
    
    // choose code type or types you want to search for
    
    // Our sample app is configured by default to search all supported barcodes...
    MWB_setActiveCodes(MWB_CODE_MASK_25     |
                       MWB_CODE_MASK_39     |
                       MWB_CODE_MASK_93     |
                       MWB_CODE_MASK_128    |
                       MWB_CODE_MASK_AZTEC  |
                       MWB_CODE_MASK_DM     |
                       MWB_CODE_MASK_EANUPC |
                       MWB_CODE_MASK_PDF    |
                       MWB_CODE_MASK_QR     |
                       MWB_CODE_MASK_CODABAR|
                       MWB_CODE_MASK_11     |
                       MWB_CODE_MASK_MSI    |
                       MWB_CODE_MASK_RSS    |
                       MWB_CODE_MASK_MAXICODE|
                       MWB_CODE_MASK_POSTAL);
    
    
    
    // Our sample app is configured by default to search both directions...
    MWB_setDirection(MWB_SCANDIRECTION_HORIZONTAL | MWB_SCANDIRECTION_VERTICAL);
    // set the scanning rectangle based on scan direction(format in pct: x, y, width, height)
    MWB_setScanningRect(MWB_CODE_MASK_25,     RECT_FULL_1D);
    MWB_setScanningRect(MWB_CODE_MASK_39,     RECT_FULL_1D);
    MWB_setScanningRect(MWB_CODE_MASK_93,     RECT_FULL_1D);
    MWB_setScanningRect(MWB_CODE_MASK_128,    RECT_FULL_1D);
    MWB_setScanningRect(MWB_CODE_MASK_AZTEC,  RECT_FULL_2D);
    MWB_setScanningRect(MWB_CODE_MASK_DM,     RECT_FULL_2D);
    MWB_setScanningRect(MWB_CODE_MASK_EANUPC, RECT_FULL_1D);
    MWB_setScanningRect(MWB_CODE_MASK_PDF,    RECT_FULL_1D);
    MWB_setScanningRect(MWB_CODE_MASK_QR,     RECT_FULL_2D);
    MWB_setScanningRect(MWB_CODE_MASK_RSS,    RECT_FULL_1D);
    MWB_setScanningRect(MWB_CODE_MASK_CODABAR,RECT_FULL_1D);
    MWB_setScanningRect(MWB_CODE_MASK_DOTCODE,RECT_DOTCODE);
    MWB_setScanningRect(MWB_CODE_MASK_11,     RECT_FULL_1D);
    MWB_setScanningRect(MWB_CODE_MASK_MSI,    RECT_FULL_1D);
    MWB_setScanningRect(MWB_CODE_MASK_MAXICODE,RECT_FULL_2D);
    MWB_setScanningRect(MWB_CODE_MASK_POSTAL, RECT_FULL_1D);

    // set decoder effort level (1 - 5)
    // for live scanning scenarios, a setting between 1 to 3 will suffice
    // levels 4 and 5 are typically reserved for batch scanning
    MWB_setLevel(2);
    
    //Set minimum result length for low-protected barcode types
    MWB_setMinLength(MWB_CODE_MASK_25, 5);
    MWB_setMinLength(MWB_CODE_MASK_MSI, 5);
    MWB_setMinLength(MWB_CODE_MASK_39, 5);
    MWB_setMinLength(MWB_CODE_MASK_CODABAR, 5);
    MWB_setMinLength(MWB_CODE_MASK_11, 5);
    
    //Use MWResult class instead of barcode raw byte array as result
    MWB_setResultType(MWB_RESULT_TYPE_MW);
    
    //get and print Library version
    int ver = MWB_getLibVersion();
    int v1 = (ver >> 16);
    int v2 = (ver >> 8) & 0xff;
    int v3 = (ver & 0xff);
    NSString *libVersion = [NSString stringWithFormat:@"%d.%d.%d", v1, v2, v3];
    NSLog(@"Lib version: %@", libVersion);
}

+ (void) setInterfaceOrientation: (UIInterfaceOrientationMask) interfaceOrientation {
    
    param_Orientation = interfaceOrientation;
    
}

+ (void) enableHiRes: (BOOL) hiRes {
    
    param_EnableHiRes = hiRes;
    
}

+ (void) enableFlash: (BOOL) flash {
    
    param_EnableFlash = flash;
    
}
+ (BOOL) isFlashEnabled {
    
    return param_EnableFlash;
    
}
+ (BOOL) isZoomEnabled {
    
    return param_EnableZoom;
    
}

+ (void) turnFlashOn: (BOOL) flashOn {
    
    param_defaultFlashOn = flashOn;
    
}

+ (void) setOverlayMode: (int) overlayMode {
    
    param_OverlayMode = overlayMode;
    
}
+ (int) getOverlayMode
{
    
   return param_OverlayMode;
    
}

+ (void) enableZoom: (BOOL) zoom {
    
    param_EnableZoom = zoom;
    
}
+ (void) closeScannerOnDecode: (BOOL) close {
    param_closeOnSuccess = close;
}
+ (BOOL) getCloseScannerOnDecode {
    return param_closeOnSuccess;
}
+ (void) use60fps: (BOOL) use {
    param_use60fps = use;
}

+ (void) setActiveParser: (int) parserType {
    param_activeParser = parserType;
}

+ (void) setMaxThreads: (int) maxThreads {
    
    if (availableThreads == 0){
        host_basic_info_data_t hostInfo;
        mach_msg_type_number_t infoCount;
        infoCount = HOST_BASIC_INFO_COUNT;
        host_info( mach_host_self(), HOST_BASIC_INFO, (host_info_t)&hostInfo, &infoCount ) ;
        availableThreads = hostInfo.max_cpus;
    }
    
    
    param_maxThreads = maxThreads;
    if (param_maxThreads > availableThreads){
        param_maxThreads = availableThreads;
    }
    
    
    
}
+ (void) setUseFrontCamera: (BOOL) use
{
    useFrontCamera = use;
}
    
    -(void)refreshOverlay
    {
        dispatch_async(dispatch_get_main_queue(), ^(void) {
            
            [MWOverlay removeFromPreviewLayer];
            
            if(param_OverlayMode == 1){
                [MWOverlay addToPreviewLayer:self.prevLayer];
            }
            
            if (cameraOverlay) {
                
                if([MWScannerViewController getOverlayMode] == 2){
                    [cameraOverlay setHidden:NO];
                }else{
                    [cameraOverlay setHidden:YES];
                }
            }
            
        });
    }

+ (void) setZoomLevels: (int) zoomLevel1 zoomLevel2: (int) zoomLevel2 initialZoomLevel: (int) initialZoomLevel {
    
    param_ZoomLevel1 = zoomLevel1;
    param_ZoomLevel2 = zoomLevel2;
    zoomLevel = initialZoomLevel;
    if (zoomLevel > 2){
        zoomLevel = 2;
    }
    if (zoomLevel < 0){
        zoomLevel = 0;
    }
    
}



- (void)viewWillAppear:(BOOL)animated {
    [super viewWillAppear:animated];
    statusBarHidden =  [[UIApplication sharedApplication] isStatusBarHidden];
    [[UIApplication sharedApplication] setStatusBarHidden:YES];
    
    flashButton.hidden = !param_EnableFlash;
    zoomButton.hidden = !param_EnableZoom;
    
#if TARGET_IPHONE_SIMULATOR
    NSLog(@"On iOS simulator camera is not Supported");
#else
    [self initCapture];
#endif
    
    if (param_OverlayMode & OM_MW){
        [MWOverlay addToPreviewLayer:self.prevLayer];
    }
    
    cameraOverlay.hidden = !(param_OverlayMode & OM_IMAGE);
    
    
    if (param_EnableFlash && [self.device isTorchModeSupported:AVCaptureTorchModeOn] && param_defaultFlashOn){
        if ([self.device lockForConfiguration:NULL]) {
            if ([self.device torchMode] == AVCaptureTorchModeOff){
                [self.device setTorchMode:AVCaptureTorchModeOn];
                flashButton.selected = YES;
            }
        }
    }
    
    [self updateTorch];
    
    
}

- (void)viewWillDisappear:(BOOL) animated {
    [super viewWillDisappear:animated];
    if (param_EnableFlash && [self.device isTorchModeSupported:AVCaptureTorchModeOn] && [self.device lockForConfiguration:NULL]) {
        if ([self.device torchMode] == AVCaptureTorchModeOn){
            [self.device setTorchMode:AVCaptureTorchModeOff];
            flashButton.selected = NO;
        }
    }
    [self stopScanning];
    [self deinitCapture];
    flashButton.selected = NO;
}

- (void)viewDidAppear:(BOOL)animated {
    [super viewDidAppear:animated];
    [self startScanning];
}

- (void)viewDidLoad {
    [super viewDidLoad];
    
    [self.view setBackgroundColor:[UIColor blackColor]];
    self.prevLayer = nil;
    [[NSNotificationCenter defaultCenter] addObserver: self selector:@selector(decodeResultNotification:) name: DecoderResultNotification object: nil];
    

    
}

// IOS 7 statusbar hide
- (BOOL)prefersStatusBarHidden
{
    return YES;
}


-(void) reFocus {
    //NSLog(@"refocus");
    
    NSError *error;
    if ([self.device lockForConfiguration:&error]) {
        
        if ([self.device isFocusPointOfInterestSupported]){
            [self.device setFocusPointOfInterest:CGPointMake(0.49,0.49)];
            [self.device setFocusMode:AVCaptureFocusModeAutoFocus];
        }
        [self.device unlockForConfiguration];
        
    }
}

- (void) updateTorch {
    
    if (param_EnableFlash && [self.device isTorchModeSupported:AVCaptureTorchModeOn]) {
        
        flashButton.hidden = NO;
        
    } else {
        flashButton.hidden = YES;
    }
    
}

- (void)toggleTorch
{
    if ([self.device isTorchModeSupported:AVCaptureTorchModeOn]) {
        NSError *error;
        
        if ([self.device lockForConfiguration:&error]) {
            if ([self.device torchMode] == AVCaptureTorchModeOn){
                [self.device setTorchMode:AVCaptureTorchModeOff];
                flashButton.selected = NO;
            }
            else {
                [self.device setTorchMode:AVCaptureTorchModeOn];
                flashButton.selected = YES;
            }
            
            if([self.device isFocusModeSupported: AVCaptureFocusModeContinuousAutoFocus])
            self.device.focusMode = AVCaptureFocusModeContinuousAutoFocus;
            
            [self.device unlockForConfiguration];
        } else {
            
        }
    }
}

- (void) updateDigitalZoom {
    
    if (videoZoomSupported){
        
        [self.device lockForConfiguration:nil];
        
        switch (zoomLevel) {
            case 0:
            [self.device setVideoZoomFactor:1 /*rampToVideoZoomFactor:1 withRate:4*/];
            break;
            case 1:
            [self.device setVideoZoomFactor:firstZoom /*rampToVideoZoomFactor:firstZooom withRate:4*/];
            break;
            case 2:
            [self.device setVideoZoomFactor:secondZoom /*rampToVideoZoomFactor:secondZoom withRate:4*/];
            break;
            
            default:
            break;
        }
        [self.device unlockForConfiguration];
        
        zoomButton.hidden = !param_EnableZoom;
    } else {
        zoomButton.hidden = true;
    }
}

- (void) deinitCapture {
    if (self.captureSession != nil){
        [focusTimer invalidate];
        
        if (param_OverlayMode & OM_MW){
            [MWOverlay removeFromPreviewLayer];
        }
        
#if !__has_feature(objc_arc)
        [self.captureSession release];
#endif
        self.captureSession=nil;
        
        [self.prevLayer removeFromSuperlayer];
        self.prevLayer = nil;
        
    }
}

- (void)initCapture
{
    /*We setup the input*/
    if (useFrontCamera) {
        NSArray *devices = [AVCaptureDevice devicesWithMediaType:AVMediaTypeVideo];
        for (AVCaptureDevice *device in devices) {
            if ([device position] == AVCaptureDevicePositionFront) {
                self.device = device;
                break;
            }
        }
    }else{
        self.device = [AVCaptureDevice defaultDeviceWithMediaType:AVMediaTypeVideo];
    }
    
    AVCaptureDeviceInput *captureInput = [AVCaptureDeviceInput deviceInputWithDevice:self.device error:nil];
    
    if (captureInput == nil){
        NSString *appName = [[[NSBundle mainBundle] infoDictionary] objectForKey:(NSString*)kCFBundleNameKey];
        [[[UIAlertView alloc] initWithTitle:@"Camera Unavailable" message:[NSString stringWithFormat:@"The %@ has not been given a permission to your camera. Please check the Privacy Settings: Settings -> %@ -> Privacy -> Camera", appName, appName] delegate:nil cancelButtonTitle:@"OK" otherButtonTitles: nil] show];
        
        return;
    }
    
    /*We setupt the output*/
    AVCaptureVideoDataOutput *captureOutput = [[AVCaptureVideoDataOutput alloc] init];
    captureOutput.alwaysDiscardsLateVideoFrames = YES;
    //captureOutput.minFrameDuration = CMTimeMake(1, 10); Uncomment it to specify a minimum duration for each video frame
    [captureOutput setSampleBufferDelegate:self queue:dispatch_get_main_queue()];
    // Set the video output to store frame in BGRA (It is supposed to be faster)
    
    NSString* key = (NSString*)kCVPixelBufferPixelFormatTypeKey;
    // Set the video output to store frame in 422YpCbCr8(It is supposed to be faster)
    
    //************************Note this line
    NSNumber* value = [NSNumber numberWithUnsignedInt:kCVPixelFormatType_420YpCbCr8BiPlanarVideoRange];
    
    NSDictionary* videoSettings = [NSDictionary dictionaryWithObject:value forKey:key];
    [captureOutput setVideoSettings:videoSettings];
    
    //And we create a capture session
    self.captureSession = [[AVCaptureSession alloc] init];
    //We add input and output
    [self.captureSession addInput:captureInput];
    [self.captureSession addOutput:captureOutput];
    
    
    
    float resX = 640;
    float resY = 480;
    
    if (param_EnableHiRes && [self.captureSession canSetSessionPreset:AVCaptureSessionPreset1280x720])
    {
        NSLog(@"Set preview port to 1280X720");
        resX = 1280;
        resY = 720;
        self.captureSession.sessionPreset = AVCaptureSessionPreset1280x720;
    } else
        //set to 640x480 if 1280x720 not supported on device
        if ([self.captureSession canSetSessionPreset:AVCaptureSessionPreset640x480])
        {
            NSLog(@"Set preview port to 640X480");
            self.captureSession.sessionPreset = AVCaptureSessionPreset640x480;
        }
    
    // Limit camera FPS to 15 for single core devices (iPhone 4 and older) so more CPU power is available for decoder
    host_basic_info_data_t hostInfo;
    mach_msg_type_number_t infoCount;
    infoCount = HOST_BASIC_INFO_COUNT;
    host_info( mach_host_self(), HOST_BASIC_INFO, (host_info_t)&hostInfo, &infoCount ) ;
    
    if (hostInfo.max_cpus < 2){
        if ([self.device respondsToSelector:@selector(setActiveVideoMinFrameDuration:)]){
            [self.device lockForConfiguration:nil];
            [self.device setActiveVideoMinFrameDuration:CMTimeMake(1, 15)];
            [self.device unlockForConfiguration];
        } else {
            AVCaptureConnection *conn = [captureOutput connectionWithMediaType:AVMediaTypeVideo];
            [conn setVideoMinFrameDuration:CMTimeMake(1, 15)];
        }
    }else if (param_use60fps) {
        for(AVCaptureDeviceFormat *vFormat in [self.device formats] )
        {
            CMFormatDescriptionRef description= vFormat.formatDescription;
            float maxrate=((AVFrameRateRange*)[vFormat.videoSupportedFrameRateRanges objectAtIndex:0]).maxFrameRate;
            float minrate=((AVFrameRateRange*)[vFormat.videoSupportedFrameRateRanges objectAtIndex:0]).minFrameRate;
            CMVideoDimensions dimension = CMVideoFormatDescriptionGetDimensions(description);
            
            if(maxrate>59 && CMFormatDescriptionGetMediaSubType(description)==kCVPixelFormatType_420YpCbCr8BiPlanarVideoRange &&
               dimension.width == resX && dimension.height == resY)
            {
                if ( YES == [self.device lockForConfiguration:NULL] )
                {
                    self.device.activeFormat = vFormat;
                    [self.device setActiveVideoMinFrameDuration:CMTimeMake(10,minrate * 10)];
                    [self.device setActiveVideoMaxFrameDuration:CMTimeMake(10,600)];
                    [self.device unlockForConfiguration];
                    
                    NSLog(@"formats  %@ %@ %@",vFormat.mediaType,vFormat.formatDescription,vFormat.videoSupportedFrameRateRanges);
                    //break;
                }
            }
        }
    }
    
    if (availableThreads == 0){
        availableThreads = hostInfo.max_cpus;
    }
    
    if (param_maxThreads > availableThreads){
        param_maxThreads = availableThreads;
    }
    
    /*We add the preview layer*/
    
    self.prevLayer = [AVCaptureVideoPreviewLayer layerWithSession: self.captureSession];
    
    CGRect screenRect = [[UIScreen mainScreen] bounds];
    float screenWidth = screenRect.size.width;
    float screenHeight = screenRect.size.height;
    
    if (self.interfaceOrientation == UIInterfaceOrientationLandscapeLeft){
        self.prevLayer.connection.videoOrientation = AVCaptureVideoOrientationLandscapeLeft;
        self.prevLayer.frame = CGRectMake(0, 0, MAX(screenWidth, screenHeight), MIN(screenWidth, screenHeight));
    }
    if (self.interfaceOrientation == UIInterfaceOrientationLandscapeRight){
        self.prevLayer.connection.videoOrientation = AVCaptureVideoOrientationLandscapeRight;
        self.prevLayer.frame = CGRectMake(0, 0, MAX(screenWidth, screenHeight), MIN(screenWidth, screenHeight));
    }
    
    
    if (self.interfaceOrientation == UIInterfaceOrientationPortrait) {
        self.prevLayer.connection.videoOrientation = AVCaptureVideoOrientationPortrait;
        self.prevLayer.frame = CGRectMake(0, 0, MIN(screenWidth, screenHeight), MAX(screenWidth, screenHeight));
    }
    if (self.interfaceOrientation == UIInterfaceOrientationPortraitUpsideDown) {
        self.prevLayer.connection.videoOrientation = AVCaptureVideoOrientationPortraitUpsideDown;
        self.prevLayer.frame = CGRectMake(0, 0, MIN(screenWidth, screenHeight), MAX(screenWidth, screenHeight));
    }
    
    self.prevLayer.videoGravity = AVLayerVideoGravityResizeAspectFill;
    
    [self.view.layer addSublayer: self.prevLayer];
    
    if (![self.device isTorchModeSupported:AVCaptureTorchModeOn]) {
        flashButton.hidden = YES;
        
    }
    
    videoZoomSupported = false;
    
    if ([self.device respondsToSelector:@selector(setActiveFormat:)] &&
        [self.device.activeFormat respondsToSelector:@selector(videoMaxZoomFactor)] &&
        [self.device respondsToSelector:@selector(setVideoZoomFactor:)]){
        
        float maxZoom = 0;
        if ([self.device.activeFormat respondsToSelector:@selector(videoZoomFactorUpscaleThreshold)]){
            maxZoom = self.device.activeFormat.videoZoomFactorUpscaleThreshold;
        } else {
            maxZoom = self.device.activeFormat.videoMaxZoomFactor;
        }
        
        float maxZoomTotal = self.device.activeFormat.videoMaxZoomFactor;
        
        if ([self.device respondsToSelector:@selector(setVideoZoomFactor:)] && maxZoomTotal > 1.1){
            videoZoomSupported = true;
            
            
            
            if (param_ZoomLevel1 != 0 && param_ZoomLevel2 != 0){
                
                if (param_ZoomLevel1 > maxZoomTotal * 100){
                    param_ZoomLevel1 = (int)(maxZoomTotal * 100);
                }
                if (param_ZoomLevel2 > maxZoomTotal * 100){
                    param_ZoomLevel2 = (int)(maxZoomTotal * 100);
                }
                
                firstZoom = 0.01 * param_ZoomLevel1;
                secondZoom = 0.01 * param_ZoomLevel2;
                
                
            } else {
                
                if (maxZoomTotal > 2){
                    
                    if (maxZoom > 1.0 && maxZoom <= 2.0){
                        firstZoom = maxZoom;
                        secondZoom = maxZoom * 2;
                    } else
                        if (maxZoom > 2.0){
                            firstZoom = 2.0;
                            secondZoom = 4.0;
                        }
                    
                }
            }
            
            
        } else {
            
        }
        
        
        
        
    }
    
    if (!videoZoomSupported){
        zoomButton.hidden = true;
    } else {
        [self updateDigitalZoom];
    }
    
    [self.view bringSubviewToFront:cameraOverlay];
    [self.view bringSubviewToFront:closeButton];
    [self.view bringSubviewToFront:flashButton];
    [self.view bringSubviewToFront:zoomButton];
    
    
    
    self.focusTimer = [NSTimer scheduledTimerWithTimeInterval:2.5 target:self selector:@selector(reFocus) userInfo:nil repeats:YES];
    
    activeThreads = 0;
    
}


- (AVCaptureVideoPreviewLayer *)generateLayerWithRect:(CGPoint)bottomRightPoint
{
    /*We setup the input*/
    if (useFrontCamera) {
        NSArray *devices = [AVCaptureDevice devicesWithMediaType:AVMediaTypeVideo];
        for (AVCaptureDevice *device in devices) {
            if ([device position] == AVCaptureDevicePositionFront) {
                self.device = device;
                break;
            }
        }
    }else{
        self.device = [AVCaptureDevice defaultDeviceWithMediaType:AVMediaTypeVideo];
    }
    
    AVCaptureDeviceInput *captureInput = [AVCaptureDeviceInput deviceInputWithDevice:self.device error:nil];
    
    if (captureInput == nil){
        NSString *appName = [[[NSBundle mainBundle] infoDictionary] objectForKey:(NSString*)kCFBundleNameKey];
        [[[UIAlertView alloc] initWithTitle:@"Camera Unavailable" message:[NSString stringWithFormat:@"The %@ has not been given a permission to your camera. Please check the Privacy Settings: Settings -> %@ -> Privacy -> Camera", appName, appName] delegate:nil cancelButtonTitle:@"OK" otherButtonTitles: nil] show];
        
        return nil;
    }
    
    /*We setupt the output*/
    AVCaptureVideoDataOutput *captureOutput = [[AVCaptureVideoDataOutput alloc] init];
    captureOutput.alwaysDiscardsLateVideoFrames = YES;
    //captureOutput.minFrameDuration = CMTimeMake(1, 10); Uncomment it to specify a minimum duration for each video frame
    [captureOutput setSampleBufferDelegate:self queue:dispatch_get_main_queue()];
    // Set the video output to store frame in BGRA (It is supposed to be faster)
    
    NSString* key = (NSString*)kCVPixelBufferPixelFormatTypeKey;
    // Set the video output to store frame in 422YpCbCr8(It is supposed to be faster)
    
    //************************Note this line
    NSNumber* value = [NSNumber numberWithUnsignedInt:kCVPixelFormatType_420YpCbCr8BiPlanarVideoRange];
    
    NSDictionary* videoSettings = [NSDictionary dictionaryWithObject:value forKey:key];
    [captureOutput setVideoSettings:videoSettings];
    
    //And we create a capture session
    self.captureSession = [[AVCaptureSession alloc] init];
    //We add input and output
    [self.captureSession addInput:captureInput];
    [self.captureSession addOutput:captureOutput];
    
    
    
    float resX = 640;
    float resY = 480;
    
    if (param_EnableHiRes && [self.captureSession canSetSessionPreset:AVCaptureSessionPreset1280x720])
    {
        NSLog(@"Set preview port to 1280X720");
        resX = 1280;
        resY = 720;
        self.captureSession.sessionPreset = AVCaptureSessionPreset1280x720;
    } else
    //set to 640x480 if 1280x720 not supported on device
    if ([self.captureSession canSetSessionPreset:AVCaptureSessionPreset640x480])
    {
        NSLog(@"Set preview port to 640X480");
        self.captureSession.sessionPreset = AVCaptureSessionPreset640x480;
    }
    
    // Limit camera FPS to 15 for single core devices (iPhone 4 and older) so more CPU power is available for decoder
    host_basic_info_data_t hostInfo;
    mach_msg_type_number_t infoCount;
    infoCount = HOST_BASIC_INFO_COUNT;
    host_info( mach_host_self(), HOST_BASIC_INFO, (host_info_t)&hostInfo, &infoCount ) ;
    
    if (hostInfo.max_cpus < 2){
        if ([self.device respondsToSelector:@selector(setActiveVideoMinFrameDuration:)]){
            [self.device lockForConfiguration:nil];
            [self.device setActiveVideoMinFrameDuration:CMTimeMake(1, 15)];
            [self.device unlockForConfiguration];
        } else {
            AVCaptureConnection *conn = [captureOutput connectionWithMediaType:AVMediaTypeVideo];
            [conn setVideoMinFrameDuration:CMTimeMake(1, 15)];
        }
    }else if (param_use60fps) {
        for(AVCaptureDeviceFormat *vFormat in [self.device formats] )
        {
            CMFormatDescriptionRef description= vFormat.formatDescription;
            float maxrate=((AVFrameRateRange*)[vFormat.videoSupportedFrameRateRanges objectAtIndex:0]).maxFrameRate;
            float minrate=((AVFrameRateRange*)[vFormat.videoSupportedFrameRateRanges objectAtIndex:0]).minFrameRate;
            CMVideoDimensions dimension = CMVideoFormatDescriptionGetDimensions(description);
            
            if(maxrate>59 && CMFormatDescriptionGetMediaSubType(description)==kCVPixelFormatType_420YpCbCr8BiPlanarVideoRange &&
               dimension.width == resX && dimension.height == resY)
            {
                if ( YES == [self.device lockForConfiguration:NULL] )
                {
                    self.device.activeFormat = vFormat;
                    [self.device setActiveVideoMinFrameDuration:CMTimeMake(10,minrate * 10)];
                    [self.device setActiveVideoMaxFrameDuration:CMTimeMake(10,600)];
                    [self.device unlockForConfiguration];
                    
                    NSLog(@"formats  %@ %@ %@",vFormat.mediaType,vFormat.formatDescription,vFormat.videoSupportedFrameRateRanges);
                    //break;
                }
            }
        }
    }
    
    if (availableThreads == 0){
        availableThreads = hostInfo.max_cpus;
    }
    
    if (param_maxThreads > availableThreads){
        param_maxThreads = availableThreads;
    }
    
    /*We add the preview layer*/
    AVCaptureVideoPreviewLayer *theLayer = [AVCaptureVideoPreviewLayer layerWithSession: self.captureSession];

    if (self.interfaceOrientation == UIInterfaceOrientationLandscapeLeft){
        theLayer.connection.videoOrientation = AVCaptureVideoOrientationLandscapeLeft;
    }
    if (self.interfaceOrientation == UIInterfaceOrientationLandscapeRight){
        theLayer.connection.videoOrientation = AVCaptureVideoOrientationLandscapeRight;
    }
    
    
    if (self.interfaceOrientation == UIInterfaceOrientationPortrait) {
        theLayer.connection.videoOrientation = AVCaptureVideoOrientationPortrait;
    }
    if (self.interfaceOrientation == UIInterfaceOrientationPortraitUpsideDown) {
        theLayer.connection.videoOrientation = AVCaptureVideoOrientationPortraitUpsideDown;
    }
    [theLayer setFrame:CGRectMake(0, 0, bottomRightPoint.x, bottomRightPoint.y)];
    theLayer.videoGravity = AVLayerVideoGravityResizeAspectFill;
    
    self.focusTimer = [NSTimer scheduledTimerWithTimeInterval:2.5 target:self selector:@selector(reFocus) userInfo:nil repeats:YES];
    activeThreads = 0;
    
    if ([self.device respondsToSelector:@selector(setActiveFormat:)] &&
        [self.device.activeFormat respondsToSelector:@selector(videoMaxZoomFactor)] &&
        [self.device respondsToSelector:@selector(setVideoZoomFactor:)]){
        
        float maxZoom = 0;
        if ([self.device.activeFormat respondsToSelector:@selector(videoZoomFactorUpscaleThreshold)]){
            maxZoom = self.device.activeFormat.videoZoomFactorUpscaleThreshold;
        } else {
            maxZoom = self.device.activeFormat.videoMaxZoomFactor;
        }
        
        float maxZoomTotal = self.device.activeFormat.videoMaxZoomFactor;
        
        if ([self.device respondsToSelector:@selector(setVideoZoomFactor:)] && maxZoomTotal > 1.1){
            videoZoomSupported = true;
            
            
            
            if (param_ZoomLevel1 != 0 && param_ZoomLevel2 != 0){
                
                if (param_ZoomLevel1 > maxZoomTotal * 100){
                    param_ZoomLevel1 = (int)(maxZoomTotal * 100);
                }
                if (param_ZoomLevel2 > maxZoomTotal * 100){
                    param_ZoomLevel2 = (int)(maxZoomTotal * 100);
                }
                
                firstZoom = 0.01 * param_ZoomLevel1;
                secondZoom = 0.01 * param_ZoomLevel2;
                
                
            } else {
                
                if (maxZoomTotal > 2){
                    
                    if (maxZoom > 1.0 && maxZoom <= 2.0){
                        firstZoom = maxZoom;
                        secondZoom = maxZoom * 2;
                    } else
                        if (maxZoom > 2.0){
                            firstZoom = 2.0;
                            secondZoom = 4.0;
                        }
                    
                }
            }
            
            
        }
        
    }
    return theLayer;
    
}

- (void) onVideoStart: (NSNotification*) note
{
    if(running)
    return;
    running = YES;
    
    // lock device and set focus mode
    NSError *error = nil;
    if([self.device lockForConfiguration: &error])
    {
        if([self.device isFocusModeSupported: AVCaptureFocusModeContinuousAutoFocus])
        self.device.focusMode = AVCaptureFocusModeContinuousAutoFocus;
    }
}

- (void) onVideoStop: (NSNotification*) note
{
    if(!running)
    return;
    [self.device unlockForConfiguration];
    running = NO;
}

#pragma mark -
#pragma mark AVCaptureSession delegate

- (void)captureOutput:(AVCaptureOutput *)captureOutput
didOutputSampleBuffer:(CMSampleBufferRef)sampleBuffer
       fromConnection:(AVCaptureConnection *)connection
{
    if (state != CAMERA && state != CAMERA_DECODING) {
        return;
    }
    
    if (activeThreads >= param_maxThreads){
        return;
    }
    
    
    if (self.state != CAMERA_DECODING)
    {
        self.state = CAMERA_DECODING;
    }
    
    activeThreads++;
    
    CVImageBufferRef imageBuffer = CMSampleBufferGetImageBuffer(sampleBuffer);
    //Lock the image buffer
    CVPixelBufferLockBaseAddress(imageBuffer,0);
    //Get information about the image
    baseAddress = (uint8_t *)CVPixelBufferGetBaseAddressOfPlane(imageBuffer,0);
    int pixelFormat = CVPixelBufferGetPixelFormatType(imageBuffer);
    switch (pixelFormat) {
        case kCVPixelFormatType_420YpCbCr8BiPlanarVideoRange:
        //NSLog(@"Capture pixel format=NV12");
        bytesPerRow = CVPixelBufferGetBytesPerRowOfPlane(imageBuffer,0);
        width = bytesPerRow;//CVPixelBufferGetWidthOfPlane(imageBuffer,0);
        height = CVPixelBufferGetHeightOfPlane(imageBuffer,0);
        break;
        case kCVPixelFormatType_422YpCbCr8:
        //NSLog(@"Capture pixel format=UYUY422");
        bytesPerRow = CVPixelBufferGetBytesPerRowOfPlane(imageBuffer,0);
        width = CVPixelBufferGetWidth(imageBuffer);
        height = CVPixelBufferGetHeight(imageBuffer);
        int len = width*height;
        int dstpos=1;
        for (int i=0;i<len;i++){
            baseAddress[i]=baseAddress[dstpos];
            dstpos+=2;
        }
        
        break;
        default:
        //	NSLog(@"Capture pixel format=RGB32");
        break;
    }
    
    unsigned char *frameBuffer = malloc(width * height);
    memcpy(frameBuffer, baseAddress, width * height);
    CVPixelBufferUnlockBaseAddress(imageBuffer,0);
    dispatch_async(dispatch_get_global_queue(DISPATCH_QUEUE_PRIORITY_DEFAULT, 0), ^{
        
        unsigned char *pResult=NULL;
        
        int resLength = MWB_scanGrayscaleImage(frameBuffer,width,height, &pResult);
        free(frameBuffer);
//        NSLog(@"Frame decoded. Active threads: %d", activeThreads);
        MWResults *mwResults = nil;
        MWResult *mwResult = nil;
        if (resLength > 0){
            
            if (self.state == NORMAL){
                resLength = 0;
                free(pResult);
                
            } else {
                mwResults = [[MWResults alloc] initWithBuffer:pResult];
                if (mwResults && mwResults.count > 0){
                    mwResult = [mwResults resultAtIntex:0];
                }
                
                free(pResult);
            }
        }
        
        if (mwResult)
        {
            MWB_setDuplicate(mwResult.bytes, mwResult.bytesLength);

            self.state = NORMAL;
            [MWOverlay setPaused:YES];
            
        
            if(param_activeParser != MWP_PARSER_MASK_NONE && !(param_activeParser == MWP_PARSER_MASK_GS1 && !mwResult.isGS1)){
                
                
                unsigned char * parserResult = NULL;
                double parserRes = -1;
                NSString *parserMask;
                
                
                
                //USE THIS CODE FOR JSONFORMATTED RESULT
                
                parserRes = MWP_getJSON(param_activeParser, mwResult.encryptedResult, mwResult.bytesLength, &parserResult);
                
                
                //use jsonString to get the JSON formatted result
                if (parserRes >= 0){
                    mwResult.text = [NSString stringWithCString:parserResult encoding:NSUTF8StringEncoding];
                }
                
                //
                
                /*
                 //USE THIS CODE FOR TEXT FORMATTED RESULT
                 
                 parserRes = MWP_getFormattedText(MWPARSER_MASK, obj.result.encryptedResult, obj.result.bytesLength, &parserResult);
                 if (parserRes >= 0){
                 decodeResult = [NSString stringWithCString:parserResult encoding:NSUTF8StringEncoding];
                 }
                 */
                //
                
                
                
                NSLog(@"%f",parserRes);
                if (parserRes >= 0){
                    
                    switch (param_activeParser) {
                        case MWP_PARSER_MASK_GS1:
                            parserMask = @"GS1";
                            break;
                        case MWP_PARSER_MASK_IUID:
                            parserMask = @"IUID";
                            break;
                        case MWP_PARSER_MASK_ISBT:
                            parserMask = @"ISBT";
                            break;
                        case MWP_PARSER_MASK_AAMVA:
                            parserMask = @"AAMVA";
                            break;
                        case MWP_PARSER_MASK_HIBC:
                            parserMask = @"HIBC";
                            break;
                        case MWP_PARSER_MASK_SCM:
                            parserMask = @"SCM";
                            break;
                        default:
                            parserMask = @"Unknown";
                            break;
                    }
                    
                    mwResult.typeName = [NSString stringWithFormat:@"%@ (%@)", mwResult.typeName, parserMask];
                    
                }
            }
        
            
            NSNotificationCenter *center = [NSNotificationCenter defaultCenter];
            DecoderResult *notificationResult = [DecoderResult createSuccess:mwResult];
            
            dispatch_async(dispatch_get_main_queue(), ^(void) {
                
                if (param_closeOnSuccess) {
                    [self.captureSession stopRunning];
                }
                if (mwResult.locationPoints) {
                    [MWOverlay showLocation:mwResult.locationPoints.points imageWidth:mwResult.imageWidth imageHeight:mwResult.imageHeight];
                }
                
                [center postNotificationName:DecoderResultNotification object: notificationResult];
                NSLog(@"SCANNED RESULT: %@",mwResult.text);
                
            });
            
        }
        else if (self.state!=NORMAL)
        {
            
            self.state = CAMERA;
            [MWOverlay setPaused:NO];
        }
        activeThreads --;
    });
}


- (IBAction)doClose:(id)sender {
    [self dismissViewControllerAnimated:YES completion:^{}];
    [self.delegate scanningFinished:@"" withType:@"Cancel" isGS1:NO andRawResult:[[NSData alloc] init] locationPoints:nil imageWidth:0 imageHeight:0];
}

- (IBAction)doFlashToggle:(id)sender {
    
    [self toggleTorch];
    
}

- (IBAction)doZoomToggle:(id)sender {
    
    zoomLevel++;
    if (zoomLevel > 2){
        zoomLevel = 0;
    }
    
    [self updateDigitalZoom];
    
}

#pragma mark -
#pragma mark Memory management

- (void)viewDidUnload
{
    [self stopScanning];
    
    self.prevLayer = nil;
    [super viewDidUnload];
}

- (void)dealloc {
#if !__has_feature(objc_arc)
    [super dealloc];
#endif
    
    [[NSNotificationCenter defaultCenter] removeObserver:self];
}

- (void) startScanning {
    self.state = LAUNCHING_CAMERA;
    [self.captureSession startRunning];
    self.prevLayer.hidden = NO;
    self.state = CAMERA;
    [MWOverlay setPaused:NO];
}

- (void)stopScanning {
    [self.captureSession stopRunning];
    self.state = NORMAL;
    [MWOverlay setPaused:YES];
}

- (void)revertToNormal {
    
    [self.captureSession stopRunning];
    self.state = NORMAL;
    [MWOverlay setPaused:YES];
}

- (void)decodeResultNotification: (NSNotification *)notification {
    
    if ([notification.object isKindOfClass:[DecoderResult class]])
    {
        DecoderResult *obj = (DecoderResult*)notification.object;
        if (obj.succeeded)
        {
            
            NSString *typeName = obj.result.typeName;
            
            if (param_closeOnSuccess) {
                [self dismissViewControllerAnimated:YES completion:^{}];
            }
    
            [self.delegate scanningFinished:obj.result.text withType: typeName isGS1:obj.result.isGS1  andRawResult: [[NSData alloc] initWithBytes: obj.result.bytes length: obj.result.bytesLength] locationPoints:obj.result.locationPoints imageWidth:obj.result.imageWidth imageHeight:obj.result.imageHeight];
            
        }
    }
}

- (void)alertView:(UIAlertView *)alertView didDismissWithButtonIndex:(NSInteger)buttonIndex {
    if (buttonIndex == 0) {
        [self startScanning];
    }
}

- (UIInterfaceOrientationMask)supportedInterfaceOrientations {
    
    return param_Orientation;
}

- (BOOL) shouldAutorotate {
    
    return (param_Orientation & (1 << self.interfaceOrientation)) != 0;
    
}

- (BOOL)shouldAutorotateToInterfaceOrientation:(UIInterfaceOrientation)interfaceOrientation {
    return (param_Orientation & (1 << interfaceOrientation)) != 0;
}

- (void) didRotateFromInterfaceOrientation:(UIInterfaceOrientation)fromInterfaceOrientation {

    if (param_OverlayMode == OM_MW) {
        [MWOverlay addToPreviewLayer:self.prevLayer];
    }

    
}

- (void)willRotateToInterfaceOrientation:(UIInterfaceOrientation)toInterfaceOrientation duration:(NSTimeInterval)duration {
    
    if (param_OverlayMode == OM_MW) {
        [MWOverlay removeFromPreviewLayer];
    }
    
    
    
    if (toInterfaceOrientation == UIInterfaceOrientationLandscapeLeft){
        self.prevLayer.connection.videoOrientation = AVCaptureVideoOrientationLandscapeLeft;
        self.prevLayer.frame = CGRectMake(0, 0, MAX(self.view.frame.size.width,self.view.frame.size.height), MIN(self.view.frame.size.width,self.view.frame.size.height));
    }
    if (toInterfaceOrientation == UIInterfaceOrientationLandscapeRight){
        self.prevLayer.connection.videoOrientation = AVCaptureVideoOrientationLandscapeRight;
        self.prevLayer.frame = CGRectMake(0, 0, MAX(self.view.frame.size.width,self.view.frame.size.height), MIN(self.view.frame.size.width,self.view.frame.size.height));
    }
    
    
    if (toInterfaceOrientation == UIInterfaceOrientationPortrait) {
        self.prevLayer.connection.videoOrientation = AVCaptureVideoOrientationPortrait;
        self.prevLayer.frame = CGRectMake(0, 0, MIN(self.view.frame.size.width,self.view.frame.size.height), MAX(self.view.frame.size.width,self.view.frame.size.height));
    }
    if (toInterfaceOrientation == UIInterfaceOrientationPortraitUpsideDown) {
        self.prevLayer.connection.videoOrientation = AVCaptureVideoOrientationPortraitUpsideDown;
        self.prevLayer.frame = CGRectMake(0, 0, MIN(self.view.frame.size.width,self.view.frame.size.height), MAX(self.view.frame.size.width,self.view.frame.size.height));
    }
    
    
    
}





@end

/*
 *  Implementation of the object that returns decoder results (via the notification
 *	process)
 */

@implementation DecoderResult

@synthesize succeeded;
@synthesize result;

+(DecoderResult *)createSuccess:(MWResult *)result {
    DecoderResult *obj = [[DecoderResult alloc] init];
    if (obj != nil) {
        obj.succeeded = YES;
        obj.result = result;
    }
    return obj;
}

+(DecoderResult *)createFailure {
    DecoderResult *obj = [[DecoderResult alloc] init];
    if (obj != nil) {
        obj.succeeded = NO;
        obj.result = nil;
    }
    return obj;
    
}

- (void)dealloc {
#if !__has_feature(objc_arc)
    [super dealloc];
#endif
    
    self.result = nil;
}

@end
