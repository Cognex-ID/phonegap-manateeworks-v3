//
//  CDVMWBarcodeScanner.m
//  CameraDemo
//
//  Created by vladimir zivkovic on 5/8/13.
//  Modified by @zikam on 2/3/17
//
//

#import "CDVMWBarcodeScanner.h"
#import "BarcodeScanner.h"
#import "MWScannerViewController.h"
#import <Cordova/CDV.h>
#import "MWOverlay.h"
#import "MWParser.h"

#define MWBackgroundQueue "MWBackgroundQueue"

@implementation CDVMWBarcodeScanner


NSString *callbackId;
NSMutableDictionary *customParams = nil;
MWScannerViewController *scannerViewController;
BOOL hasCameraPermission = NO;

- (void)initDecoder:(CDVInvokedUrlCommand*)command
{
    static dispatch_once_t onceToken;
    dispatch_once(&onceToken, ^{
        [MWScannerViewController initDecoder];
        
        CDVPluginResult* pluginResult = [CDVPluginResult resultWithStatus:CDVCommandStatus_OK];
        [self.commandDelegate sendPluginResult:pluginResult callbackId:command.callbackId];
    });
}

float leftP = 0;
float topP = 0;
float widthP = 10;
float heightP = 10;
BOOL scanInView = NO;
AVCaptureVideoPreviewLayer *scannerPreviewLayer;
UIInterfaceOrientation currentOrientation;
UIImageView *overlayImage;
BOOL useAutoRect = true;
BOOL useFCamera = false;

NSMutableDictionary *recgtVals;

- (void)registerCode:(CDVInvokedUrlCommand *)command
{
}
-(void)usePartialScanner:(CDVInvokedUrlCommand*) command{
    scanInView = [[command.arguments objectAtIndex:0] boolValue];
}

- (void)startScannerView:(CDVInvokedUrlCommand*)command
{
    
    
    if (![self.viewController.view viewWithTag:9158436]) {
        recgtVals = nil;
        [MWOverlay setPaused:NO];
        
        dispatch_async(dispatch_queue_create(MWBackgroundQueue, nil), ^{
            UIView *view = [[UIView alloc] init];
            [view setTag:9158436];
            
            //                if (!scannerViewController) {
            scannerViewController = [[MWScannerViewController alloc] initWithNibName:@"MWScannerViewController" bundle:nil] ;
            //                }
            
            dispatch_async(dispatch_get_main_queue(), ^{
                currentOrientation = [[UIApplication sharedApplication]statusBarOrientation];
                scannerViewController.delegate = self;
                [MWScannerViewController setUseFrontCamera:useFCamera];
                scannerViewController.customParams = customParams;
                [[NSNotificationCenter defaultCenter] addObserver: self selector:@selector(decodeNotification:) name: @"DecoderResultNotification" object: nil];
                [[NSNotificationCenter defaultCenter] addObserver:self
                                                         selector:@selector(didRotate:)
                                                             name:@"UIDeviceOrientationDidChangeNotification" object:nil];
                
                [self.viewController.view addSubview:view];
                
                
                [self resizePartialScanner:(command.arguments.count > 3)?command:nil];
                
                [scannerViewController startScanning];
//                scannerViewController.state = LAUNCHING_CAMERA;
//                [scannerViewController.captureSession startRunning];
//                scannerViewController.state = CAMERA;
                
#if !__has_feature(objc_arc)
                callbackId= [command.callbackId retain];
#else
                callbackId= command.callbackId;
#endif
            });
        });
    }else{
        //        [CDVMWBarcodeScanner setAutoRect:scannerPreviewLayer];
        [self resizePartialScanner:(command.arguments.count > 3)?command:nil];
    }
    
}

- (void)getDeviceID:(CDVInvokedUrlCommand*)command
{
        CDVPluginResult* pluginResult = [CDVPluginResult resultWithStatus:CDVCommandStatus_OK messageAsString:[NSString stringWithFormat:@"%s", MWB_getDeviceID()]];
        [self.commandDelegate sendPluginResult:pluginResult callbackId:command.callbackId];
}

- (void)resizePartialScanner:(CDVInvokedUrlCommand*)command
{
    if (command != nil) {
        leftP   =[[command.arguments objectAtIndex:0]floatValue];
        topP    =[[command.arguments objectAtIndex:1]floatValue];
        widthP  =[[command.arguments objectAtIndex:2]floatValue];
        heightP =[[command.arguments objectAtIndex:3]floatValue];
    }
    
    if ([self.viewController.view viewWithTag:9158436]) {
        float x =  leftP /100 * [[UIScreen mainScreen] bounds].size.width;
        float y =  topP /100 * [[UIScreen mainScreen] bounds].size.height;
        
        float width = widthP /100 *[[UIScreen mainScreen] bounds].size.width;
        float height =heightP /100 *[[UIScreen mainScreen] bounds].size.height;
        
        UIView *view = [self.viewController.view viewWithTag:9158436];
        [view setFrame:CGRectMake(x,y,width,height)];
        
        @autoreleasepool {
            if (!scannerPreviewLayer) {
                scannerPreviewLayer = [scannerViewController generateLayerWithRect:CGPointMake(width, height)];
                [view.layer addSublayer:scannerPreviewLayer];
            }else{
                [scannerPreviewLayer setFrame:CGRectMake(0, 0, width, height)];
            }
        }
        
        
        if (leftP == 0 && topP == 0 && widthP == 1 && heightP == 1) {
            [view setHidden:YES];
        } else {
            [view setHidden:NO];
        }
        
        dispatch_after(dispatch_time(DISPATCH_TIME_NOW, (int64_t)(0.01 * NSEC_PER_SEC)), dispatch_get_main_queue(), ^(void){
            [CDVMWBarcodeScanner setAutoRect:scannerPreviewLayer];
            if ([MWScannerViewController getOverlayMode] == 1) {
                [MWOverlay removeFromPreviewLayer];
                [MWOverlay addToPreviewLayer:scannerPreviewLayer];
            }else if([MWScannerViewController getOverlayMode] == 2){
                if (!overlayImage) {
                    overlayImage = [[UIImageView alloc]initWithFrame:CGRectMake(0, 0, width, height)];
                    overlayImage.contentMode = UIViewContentModeScaleToFill;
                    overlayImage.image = [UIImage imageNamed:@"overlay_mw.png"];
                }
                if(![view viewWithTag:1111]){
                    [overlayImage setTag:1111];
                    [view addSubview:overlayImage];
                }
                [overlayImage setFrame:CGRectMake(0, 0, width, height)];
                [overlayImage setHidden:NO];
            }
        });
        
        
        if ([MWScannerViewController isFlashEnabled] && [scannerViewController.device isTorchModeSupported:AVCaptureTorchModeOn]) {
            if (!scannerViewController.flashButton) {
                scannerViewController.flashButton = [[UIButton alloc]init];
                [scannerViewController.flashButton setImage:[UIImage imageNamed:@"flashbuttonoff.png"] forState:UIControlStateNormal];
                [scannerViewController.flashButton setImage:[UIImage imageNamed:@"flashbuttonon.png"] forState:UIControlStateSelected];
                [scannerViewController.flashButton setBackgroundImage:nil forState:UIControlStateSelected];
                [scannerViewController.flashButton setBackgroundImage:nil forState:UIControlStateNormal];
                [scannerViewController.flashButton addTarget:scannerViewController action:@selector(doFlashToggle:) forControlEvents:UIControlEventTouchUpInside];
                [scannerViewController.flashButton setSelected:NO];
                [scannerViewController.flashButton setHidden:NO];
                [view addSubview:scannerViewController.flashButton];
            }
            
            [scannerViewController.flashButton setFrame:CGRectMake(view.frame.size.width-10-35, 10, 35, 35)];
        }
        
        if ([MWScannerViewController isZoomEnabled]) {
            if (!scannerViewController.zoomButton) {
                scannerViewController.zoomButton = [[UIButton alloc]init];
                [scannerViewController.zoomButton setImage:[UIImage imageNamed:@"zoom.png"] forState:UIControlStateNormal];
                [scannerViewController.zoomButton setHidden:NO];
                [scannerViewController.zoomButton setBackgroundImage:nil forState:UIControlStateNormal];
                [scannerViewController.zoomButton addTarget:scannerViewController action:@selector(doZoomToggle:) forControlEvents:UIControlEventTouchUpInside];
                [view addSubview:scannerViewController.zoomButton];
            }
            [scannerViewController.zoomButton setFrame:CGRectMake(10, 10, 35, 35)];
        }
    }
}

- (void)startScanner:(CDVInvokedUrlCommand*)command
{
    if (hasCameraPermission) {
        [self loadStartScanner:command];
    }else{
        dispatch_async(dispatch_queue_create(MWBackgroundQueue, nil), ^{
            if ([AVCaptureDevice authorizationStatusForMediaType:AVMediaTypeVideo] == AVAuthorizationStatusAuthorized) {
                dispatch_async(dispatch_get_global_queue(DISPATCH_QUEUE_PRIORITY_DEFAULT, 0), ^{
                    hasCameraPermission = YES;
                    [self loadStartScanner:command];
                });
            }else{
                [AVCaptureDevice requestAccessForMediaType:AVMediaTypeVideo completionHandler:^(BOOL granted) {
                    if (granted) {
                        dispatch_async(dispatch_get_global_queue(DISPATCH_QUEUE_PRIORITY_DEFAULT, 0), ^{
                            hasCameraPermission = YES;
                            [self loadStartScanner:command];
                        });
                    }else{
                        dispatch_async(dispatch_get_main_queue(), ^{
#if !__has_feature(objc_arc)
                            callbackId= [command.callbackId retain];
#else
                            callbackId= command.callbackId;
#endif
                            [self noPermissionErrorCallback];
                            hasCameraPermission = NO;
                        });
                    }
                }];
            }
        });
    }
}

-(void)noPermissionErrorCallback {
    [self closeScanner:nil];
    
    [self scanningFinished:@"No Camera Permission" withType:@"Error" isGS1:NO andRawResult:[[NSData alloc] init] locationPoints:nil imageWidth:0 imageHeight:0];
//    [self.commandDelegate sendPluginResult:[CDVPluginResult resultWithStatus:CDVCommandStatus_ERROR messageAsString:@"User declined Camera Permission"] callbackId:callbackId];
}

-(void) loadStartScanner:(CDVInvokedUrlCommand*)command
{
    if (scanInView) {
        [self startScannerView:command];
    }else{
        dispatch_async(dispatch_get_main_queue(), ^{
            [self stopScanner:command];
            
            dispatch_async(dispatch_queue_create(MWBackgroundQueue, nil), ^{

                scannerViewController = [[MWScannerViewController alloc] initWithNibName:@"MWScannerViewController" bundle:nil];
                
                dispatch_async(dispatch_get_main_queue(), ^{
                    scannerViewController.delegate = self;
                    [MWScannerViewController setUseFrontCamera:useFCamera];
                    scannerViewController.customParams = customParams;
                    
                    [self.viewController presentViewController:scannerViewController animated:YES completion:^{
                        scannerViewController.state = CAMERA;
                    }];
#if !__has_feature(objc_arc)
                    callbackId= [command.callbackId retain];
#else
                    callbackId= command.callbackId;
#endif
                });
            });
            
        });
    }
}

-(void)setUseAutorect:(CDVInvokedUrlCommand*)command
{
    useAutoRect = [[command.arguments objectAtIndex:0]boolValue];
}

- (void)stopScanner:(CDVInvokedUrlCommand*)command
{
    if (scannerViewController) {
        scannerViewController.state = NORMAL;
    }
    
    if ([self.viewController.view viewWithTag:9158436]) {
        [scannerViewController stopScanning];
        [scannerViewController removeFromParentViewController];
        [[self.viewController.view viewWithTag:9158436]removeFromSuperview];
        
        if (scannerPreviewLayer && scannerPreviewLayer.superlayer) {
            [scannerPreviewLayer removeFromSuperlayer];
            scannerPreviewLayer = nil;
        }
        [[NSNotificationCenter defaultCenter] removeObserver:self name:@"DecoderResultNotification" object:nil];
        [[NSNotificationCenter defaultCenter] removeObserver:self name:@"UIDeviceOrientationDidChangeNotification" object:nil];
        
        [scannerViewController unload];
        scannerViewController = nil;
    }
}

- (void)duplicateCodeDelay:(CDVInvokedUrlCommand*)command
{
    MWB_setDuplicatesTimeout([[command.arguments objectAtIndex:0] intValue]);
}

- (void)scanningFinished:(NSString *)result withType:(NSString *)lastFormat isGS1: (bool) isGS1 andRawResult: (NSData *) rawResult locationPoints:(MWLocation *)locationPoints imageWidth:(int)imageWidth imageHeight:(int)imageHeight
{
    dispatch_async(dispatch_queue_create(MWBackgroundQueue, nil), ^{
        
        
        CDVPluginResult* pluginResult = nil;
        
        NSMutableArray *bytesArray = [[NSMutableArray alloc] init];
        unsigned char *bytes = (unsigned char *) [rawResult bytes];
        for (int i = 0; i < rawResult.length; i++){
            [bytesArray addObject:[NSNumber numberWithInt: bytes[i]]];
        }
        NSMutableDictionary *resultDict;
        if (locationPoints) {
            NSArray *xyArray = [NSArray arrayWithObjects:@"x",@"y", nil];
            
            NSDictionary *p1 = [NSDictionary dictionaryWithObjects:[NSArray arrayWithObjects:[NSNumber numberWithFloat:locationPoints.p1.x],[NSNumber numberWithFloat:locationPoints.p1.y], nil]
                                                           forKeys:xyArray];
            NSDictionary *p2 = [NSDictionary dictionaryWithObjects:[NSArray arrayWithObjects:[NSNumber numberWithFloat:locationPoints.p2.x],[NSNumber numberWithFloat:locationPoints.p2.y], nil]
                                                           forKeys:xyArray];
            NSDictionary *p3 = [NSDictionary dictionaryWithObjects:[NSArray arrayWithObjects:[NSNumber numberWithFloat:locationPoints.p3.x],[NSNumber numberWithFloat:locationPoints.p3.y], nil]
                                                           forKeys:xyArray];
            NSDictionary *p4 = [NSDictionary dictionaryWithObjects:[NSArray arrayWithObjects:[NSNumber numberWithFloat:locationPoints.p4.x],[NSNumber numberWithFloat:locationPoints.p4.y], nil]
                                                           forKeys:xyArray];
            
            NSDictionary *location =[NSDictionary dictionaryWithObjects:[NSArray arrayWithObjects:p1,p2,p3,p4 ,nil]
                                                                forKeys:[NSArray arrayWithObjects:@"p1",@"p2",@"p3",@"p4",nil]];
            resultDict = [[NSMutableDictionary alloc] initWithObjects:[NSArray arrayWithObjects:result, lastFormat, bytesArray, [NSNumber numberWithBool:isGS1], location, [NSNumber numberWithInt:imageWidth],[NSNumber numberWithInt:imageHeight],nil]
                                                              forKeys:[NSArray arrayWithObjects:@"code", @"type",@"bytes", @"isGS1",@"location",@"imageWidth",@"imageHeight", nil]];
            
        }else{
            resultDict = [[NSMutableDictionary alloc] initWithObjects:[NSArray arrayWithObjects:result, lastFormat, bytesArray, [NSNumber numberWithBool:isGS1], [NSNumber numberWithBool:NO], [NSNumber numberWithInt:imageWidth],[NSNumber numberWithInt:imageHeight],nil]
                                                              forKeys:[NSArray arrayWithObjects:@"code", @"type",@"bytes", @"isGS1",@"location",@"imageWidth",@"imageHeight", nil]];
        }
                
        pluginResult = [CDVPluginResult resultWithStatus:CDVCommandStatus_OK messageAsDictionary:resultDict];
        
        if(![MWScannerViewController getCloseScannerOnDecode]){
            [pluginResult setKeepCallback:[NSNumber numberWithBool:YES]];
        }
        
        [self.commandDelegate sendPluginResult:pluginResult callbackId:callbackId];
        
    });
    
}
- (void)decodeNotification: (NSNotification *)notification {
    
    if ([notification.object isKindOfClass:[DecoderResult class]])
    {
        
        DecoderResult *obj = (DecoderResult*)notification.object;
        if (obj.succeeded)
        {
            if ([MWScannerViewController getCloseScannerOnDecode]) {
                [self performSelector:@selector(stopScanner:) withObject:nil afterDelay:[MWScannerViewController getCloseDelay]];
//                [self stopScanner:nil];
            }
            
            [self scanningFinished:obj.result.text withType: obj.result.typeName isGS1:obj.result.isGS1  andRawResult: [[NSData alloc] initWithBytes: obj.result.bytes length: obj.result.bytesLength] locationPoints:obj.result.locationPoints imageWidth:obj.result.imageWidth imageHeight:obj.result.imageHeight];
        }
    }
}

+ (void) setAutoRect:(AVCaptureVideoPreviewLayer *)layer{
    CGPoint p1 = [layer captureDevicePointOfInterestForPoint:CGPointMake(0,0)];
    CGPoint p2 = [layer captureDevicePointOfInterestForPoint:CGPointMake(layer.frame.size.width,layer.frame.size.height)];
    
    if (p1.x > p2.x){
        float tmp = p1.x;
        p1.x = p2.x;
        p2.x = tmp;
    }
    if (p1.y > p2.y){
        float tmp = p1.y;
        p1.y = p2.y;
        p2.y = tmp;
    }
    
    int masks[16] = {
        MWB_CODE_MASK_25,
        MWB_CODE_MASK_39,
        MWB_CODE_MASK_93,
        MWB_CODE_MASK_128,
        MWB_CODE_MASK_AZTEC,
        MWB_CODE_MASK_DM,
        MWB_CODE_MASK_EANUPC,
        MWB_CODE_MASK_PDF,
        MWB_CODE_MASK_QR,
        MWB_CODE_MASK_RSS,
        MWB_CODE_MASK_CODABAR,
        MWB_CODE_MASK_DOTCODE,
        MWB_CODE_MASK_11,
        MWB_CODE_MASK_MSI,
        MWB_CODE_MASK_MAXICODE,
        MWB_CODE_MASK_POSTAL
    };
    
    
    if (useAutoRect) {
        
        p1.x += 0.02;
        p1.y += 0.02;
        p2.x -= 0.02;
        p2.y -= 0.02;
        
        for (int i = 0; i<16; i++) {
            MWB_setScanningRect(masks[i], p1.x  *100, p1.y * 100, (p2.x - p1.x) * 100, (p2.y - p1.y) * 100);
        }
        
    }else{
        
        if (!recgtVals) {
            recgtVals = [[NSMutableDictionary alloc]init];
            
            for (int i =0; i<16; i++) {
                
                float left,top,width,height;
                MWB_getScanningRect(masks[i], &left, &top, &width, &height);
                [recgtVals setObject:[NSArray arrayWithObjects:[NSNumber numberWithInt:left],[NSNumber numberWithInt:top],[NSNumber numberWithInt:width],[NSNumber numberWithInt:height], nil] forKey:[NSNumber numberWithInt:masks[i]]];
                
            }
            
        }else{
            
            for (int i = 0 ; i<16; i++) {
                
                NSArray *rectVals = [[NSArray alloc] initWithArray:[recgtVals objectForKey:[NSNumber numberWithInt:masks[i]]]];
                MWB_setScanningRect(masks[i],[[rectVals objectAtIndex:0]intValue], [[rectVals objectAtIndex:1]intValue], [[rectVals objectAtIndex:2]intValue], [[rectVals objectAtIndex:3]intValue]);
            }
            
        }
        for (int i = 0 ; i<16; i++) {
            
            float left,top,width,height;
            MWB_getScanningRect(masks[i], &left, &top, &width, &height);
            MWB_setScanningRect(masks[i],    (p1.x+ (1- p1.x*2)*(left/100))  *100, (p1.y+ (1-p1.y*2)*(top/100)) * 100, (p2.x - p1.x) * (width/100) * 100, (p2.y - p1.y)*(height/100) * 100);
            
        }
    }
}


- (void) didRotate:(NSNotification *)notification{
    
    if ([self.viewController.view viewWithTag:9158436] && currentOrientation != [[UIApplication sharedApplication]statusBarOrientation] &&[[UIDevice currentDevice]orientation]<=4 && (int)[[UIDevice currentDevice]orientation] == (int)[UIApplication sharedApplication].statusBarOrientation
        ) {
        currentOrientation =[[UIApplication sharedApplication]statusBarOrientation];
        
        UIView *scannerView = [self.viewController.view viewWithTag:9158436];
        
        float x =  leftP /100 * [[UIScreen mainScreen] bounds].size.width;
        float y =  topP /100 * [[UIScreen mainScreen] bounds].size.height;
        
        float width = widthP /100 *[[UIScreen mainScreen] bounds].size.width;
        float height =heightP /100 *[[UIScreen mainScreen] bounds].size.height;
        
        scannerView.frame =CGRectMake(x,y,width,height);
        [scannerPreviewLayer setFrame:CGRectMake(0,0,width,height)];
        
        if(currentOrientation == UIDeviceOrientationLandscapeLeft){
            [scannerPreviewLayer.connection setVideoOrientation:AVCaptureVideoOrientationLandscapeRight];
        } else if (currentOrientation == UIDeviceOrientationLandscapeRight){
            scannerPreviewLayer.connection.videoOrientation = AVCaptureVideoOrientationLandscapeLeft;
        } else if (currentOrientation == UIDeviceOrientationPortrait){
            scannerPreviewLayer.connection.videoOrientation = AVCaptureVideoOrientationPortrait;
        } else if (currentOrientation == UIDeviceOrientationPortraitUpsideDown){
            scannerPreviewLayer.connection.videoOrientation = AVCaptureVideoOrientationPortraitUpsideDown;
        }
        [CDVMWBarcodeScanner setAutoRect:scannerPreviewLayer];
        
        if ([MWScannerViewController getOverlayMode] == 1) {
            [MWOverlay removeFromPreviewLayer];
            [MWOverlay addToPreviewLayer:scannerPreviewLayer];
        }else if([MWScannerViewController getOverlayMode] == 2){
            [overlayImage setFrame:scannerPreviewLayer.frame];
        }
        if ([MWScannerViewController isFlashEnabled] && scannerViewController.flashButton) {
            scannerViewController.flashButton.frame = CGRectMake(scannerView.frame.size.width-10-35, 10, 35, 35);
        }
        
        
    }
    
}


- (void)registerSDK:(CDVInvokedUrlCommand*)command
{
    NSString *license_key = [[NSBundle mainBundle] objectForInfoDictionaryKey:@"MW_LICENSE_KEY"];
    const char * key = (char *) [[command.arguments objectAtIndex:0] UTF8String];
    
    int registrationResult;
    
    if(key != '\0' && strlen(key) > 5){
        registrationResult = MWB_registerSDK(key);
    }
    else{
        key=[license_key UTF8String];
        registrationResult = MWB_registerSDK(key);
    }
    
    //    NSLog(@"Value of license_key = %@", key);
    
    
    CDVPluginResult* pluginResult = [CDVPluginResult resultWithStatus:CDVCommandStatus_OK messageAsString:[NSString stringWithFormat:@"%d",registrationResult]];
    
    [self.commandDelegate sendPluginResult:pluginResult callbackId:command.callbackId];
    
}



- (void)setActiveCodes:(CDVInvokedUrlCommand*)command
{
    int codeMask = [[command.arguments objectAtIndex:0] intValue];
    MWB_setActiveCodes(codeMask);
}

- (void)useFrontCamera:(CDVInvokedUrlCommand*)command
{
    useFCamera = [[command.arguments objectAtIndex:0] boolValue];
}

- (void)setActiveSubcodes:(CDVInvokedUrlCommand*)command
{
    int codeMask = [[command.arguments objectAtIndex:0] intValue];
    int subCodeMask = [[command.arguments objectAtIndex:1] intValue];
    MWB_setActiveSubcodes(codeMask, subCodeMask);
}

- (int)getLastType:(CDVInvokedUrlCommand*)command
{
    return MWB_getLastType();
}

- (void)setFlags:(CDVInvokedUrlCommand*)command
{
    int codeMask = [[command.arguments objectAtIndex:0] intValue];
    int flags = [[command.arguments objectAtIndex:1] intValue];
    MWB_setFlags(codeMask, flags);
}

- (void)setMinLength:(CDVInvokedUrlCommand*)command
{
    int codeMask = [[command.arguments objectAtIndex:0] intValue];
    int minLength = [[command.arguments objectAtIndex:1] intValue];
    MWB_setMinLength(codeMask, minLength);
}

- (void)setDirection:(CDVInvokedUrlCommand*)command
{
    int direction = [[command.arguments objectAtIndex:0] intValue];
    MWB_setDirection(direction);
}

- (void)setScanningRect:(CDVInvokedUrlCommand*)command
{
    int codeMask = [[command.arguments objectAtIndex:0] intValue];
    int left = [[command.arguments objectAtIndex:1] intValue];
    int top = [[command.arguments objectAtIndex:2] intValue];
    int width = [[command.arguments objectAtIndex:3] intValue];
    int height = [[command.arguments objectAtIndex:4] intValue];
    
    MWB_setScanningRect(codeMask, left, top, width, height);
}

- (void)setLevel:(CDVInvokedUrlCommand*)command
{
    int level = [[command.arguments objectAtIndex:0] intValue];
    MWB_setLevel(level);
}

- (void)setInterfaceOrientation:(CDVInvokedUrlCommand*)command
{
    NSString *orientation = [command.arguments objectAtIndex:0];
    UIInterfaceOrientationMask interfaceOrientation = UIInterfaceOrientationMaskLandscapeLeft;
    
    if ([orientation isEqualToString:@"Portrait"]){
        interfaceOrientation = UIInterfaceOrientationMaskPortrait;
    }
    if ([orientation isEqualToString:@"LandscapeLeft"]){
        interfaceOrientation = UIInterfaceOrientationMaskLandscapeLeft;
    }
    if ([orientation isEqualToString:@"LandscapeRight"]){
        interfaceOrientation = UIInterfaceOrientationMaskLandscapeRight;
    }
    if ([orientation isEqualToString:@"All"]){
        interfaceOrientation = UIInterfaceOrientationMaskAll;
    }
    
    [MWScannerViewController setInterfaceOrientation:interfaceOrientation];
    
}

-(void)setBlinkingLineVisible:(CDVInvokedUrlCommand*)command
{
    BOOL visible = [[command.arguments objectAtIndex:0] boolValue];
    [MWOverlay setBlinkingLineVisible:visible];
}

- (void)setOverlayMode:(CDVInvokedUrlCommand*)command{
    int overlayModeTmp = [[command.arguments objectAtIndex:0] intValue];
    if ([MWScannerViewController getOverlayMode] != overlayModeTmp) {
        
        [MWScannerViewController setOverlayMode:overlayModeTmp];
        if ([self.viewController.view viewWithTag:9158436]) {
            [MWOverlay removeFromPreviewLayer];
            
            if([MWScannerViewController getOverlayMode] == 1){
                [MWOverlay addToPreviewLayer:scannerPreviewLayer];
            }
            
            if (overlayImage) {
                
                if([MWScannerViewController getOverlayMode] == 2){
                    [overlayImage setHidden:NO];
                }else{
                    [overlayImage setHidden:YES];
                }
            }
        }else{
            
        }
        
    }
    
}

- (void)setCloseDelay:(CDVInvokedUrlCommand*)command
{
    @try {
        float delay = [[command.arguments objectAtIndex:0] floatValue];
        [MWScannerViewController setCloseDelay:delay];
    } @catch (NSException *exception) {
        
    } @finally {
    }
}

- (void)enableHiRes:(CDVInvokedUrlCommand*)command
{
    bool hiRes = [[command.arguments objectAtIndex:0] boolValue];
    [MWScannerViewController enableHiRes:hiRes];
}

- (void)enableFlash:(CDVInvokedUrlCommand*)command
{
    bool flash = [[command.arguments objectAtIndex:0] boolValue];
    [MWScannerViewController enableFlash:flash];
}

- (void)enableZoom:(CDVInvokedUrlCommand*)command
{
    bool zoom = [[command.arguments objectAtIndex:0] boolValue];
    [MWScannerViewController enableZoom:zoom];
}

- (void)closeScannerOnDecode:(CDVInvokedUrlCommand*)command
{
    BOOL shouldClose =[[command.arguments objectAtIndex:0] boolValue];
    [MWScannerViewController closeScannerOnDecode:shouldClose];
}


- (void)turnFlashOn:(CDVInvokedUrlCommand*)command
{
    bool flash = [[command.arguments objectAtIndex:0] boolValue];
    [MWScannerViewController turnFlashOn:flash];
}
- (void)toggleFlash:(CDVInvokedUrlCommand*)command
{
    [scannerViewController toggleTorch];
}
- (void)toggleZoom:(CDVInvokedUrlCommand*)command
{
    [scannerViewController doZoomToggle:nil];
}

- (void)setZoomLevels:(CDVInvokedUrlCommand*)command
{
    [MWScannerViewController setZoomLevels:[[command.arguments objectAtIndex:0] intValue] zoomLevel2:[[command.arguments objectAtIndex:1] intValue] initialZoomLevel:[[command.arguments objectAtIndex:2] intValue]];
}

- (void)setMaxThreads:(CDVInvokedUrlCommand*)command
{
    [MWScannerViewController setMaxThreads:[[command.arguments objectAtIndex:0] intValue]];
}

- (void)setCustomParam:(CDVInvokedUrlCommand*)command
{
    NSString *key = [command.arguments objectAtIndex:0];
    NSObject *value = [command.arguments objectAtIndex:1];
    
    if (customParams == nil){
        customParams = [[NSMutableDictionary alloc] init];
    }
    
    [customParams setObject:value forKey:key];
    
}
- (void)setParam:(CDVInvokedUrlCommand*)command
{
    MWB_setParam([[command.arguments objectAtIndex:0] intValue], [[command.arguments objectAtIndex:1] intValue], [[command.arguments objectAtIndex:2] intValue]);
}
- (void)setActiveParser:(CDVInvokedUrlCommand*)command
{
    [MWScannerViewController setActiveParser:[[command.arguments objectAtIndex:0] intValue]];
}
- (void)resumeScanning:(CDVInvokedUrlCommand*)command
{
    scannerViewController.state = CAMERA;
}
- (void)use60fps:(CDVInvokedUrlCommand*)command
{
    [MWScannerViewController use60fps:[[command.arguments objectAtIndex:0] boolValue]];;
}
- (void)closeScanner:(CDVInvokedUrlCommand*)command
{
    if ([self.viewController.view viewWithTag:9158436]) {
        dispatch_async(dispatch_queue_create(MWBackgroundQueue, nil), ^{
            [scannerViewController stopScanning];
            [[NSNotificationCenter defaultCenter] removeObserver:self name:@"DecoderResultNotification" object:nil];
            [[NSNotificationCenter defaultCenter] removeObserver:self name:@"UIDeviceOrientationDidChangeNotification" object:nil];
            dispatch_async(dispatch_get_main_queue(), ^{
                [[self.viewController.view viewWithTag:9158436] removeFromSuperview];
                if (scannerPreviewLayer && scannerPreviewLayer.superlayer) {
                    [scannerPreviewLayer removeFromSuperlayer];
                    scannerPreviewLayer = nil;
                }
                
                [scannerViewController unload];
                scannerViewController = nil;
            });
        });
        
        
    }
    else if (scannerViewController) {
        [scannerViewController dismissViewControllerAnimated:YES completion:^{
            [scannerViewController unload];
            scannerViewController = nil;
        }];
    }
}
- (void)togglePauseResume:(CDVInvokedUrlCommand*)command
{
    if (scannerViewController.state != NORMAL) {
        scannerViewController.state = NORMAL;
        
        if ([MWScannerViewController getOverlayMode] == 1) {
            [MWOverlay setPaused:YES];
        }
    }else{
        scannerViewController.state = CAMERA;
        if ([MWScannerViewController getOverlayMode] == 1) {
            [MWOverlay setPaused:NO];
        }
    }
}
-(void) setPauseMode:(CDVInvokedUrlCommand*)command
{
    int pauseMode = [[command.arguments objectAtIndex:0] intValue];
    switch (pauseMode) {
        case 0:
            [MWOverlay setPauseMode:PM_NONE];
            break;
        case 1:
            [MWOverlay setPauseMode:PM_PAUSE];
            break;
        case 2:
            [MWOverlay setPauseMode:PM_STOP_BLINKING];
            break;
        default:
            break;
    }
}
- (void)scanImage:(CDVInvokedUrlCommand*)command
{
    callbackId = command.callbackId;
    
    NSString *prefixToRemove = @"file://";
    
    NSString *filePath = [command.arguments objectAtIndex:0];
    
    
    if ([filePath hasPrefix:prefixToRemove])
        
        filePath = [filePath substringFromIndex:[prefixToRemove length]];
    
    UIImage * image = [UIImage imageWithContentsOfFile:filePath];
    
    if (image!=nil) {
        
        int newWidth;
        int newHeight;
        
        uint8_t *bytes = [CDVMWBarcodeScanner UIImageToGrayscaleByteArray:image newWidth: &newWidth newHeight: &newHeight];
        
        unsigned char *pResult=NULL;
        
        if (bytes) {
            
            int resLength = MWB_scanGrayscaleImage(bytes, newWidth, newHeight, &pResult);
            
            free(bytes);
            
            MWResults *mwResults = nil;
            MWResult *mwResult = nil;
            
            if (resLength > 0){
                
                mwResults = [[MWResults alloc] initWithBuffer:pResult];
                if (mwResults && mwResults.count > 0){
                    mwResult = [mwResults resultAtIntex:0];
                }
                free(pResult);
                
            }
            if (mwResult)
            {
                [self scanningFinished:mwResult.text withType: mwResult.typeName isGS1:mwResult.isGS1  andRawResult: [[NSData alloc] initWithBytes: mwResult.bytes length: mwResult.bytesLength] locationPoints:mwResult.locationPoints imageWidth:mwResult.imageWidth imageHeight:mwResult.imageHeight];
                
            }else{
                [self scanningFinished:@"" withType: @"NoResult" isGS1:NO  andRawResult: nil locationPoints:nil imageWidth:0 imageHeight:0];
            }
        }
        
    }
    
}



#define MAX_IMAGE_SIZE 1280

+ (unsigned char*)UIImageToGrayscaleByteArray:(UIImage*)image newWidth: (int*)newWidth newHeight: (int*)newHeight; {
    
    int targetWidth = image.size.width;
    int targetHeight = image.size.height;
    float scale = 1.0;
    
    if (targetWidth > MAX_IMAGE_SIZE || targetHeight > MAX_IMAGE_SIZE){
        targetWidth /= 2;
        targetHeight /= 2;
        scale *= 2;
        
    }
    
    *newWidth = targetWidth;
    
    *newHeight = targetHeight;
    
    unsigned char *imageData = (unsigned char*)(malloc( targetWidth*targetHeight));
    
    CGColorSpaceRef colorSpace = CGColorSpaceCreateDeviceGray();
    
    CGImageRef imageRef = [image CGImage];
    CGContextRef bitmap = CGBitmapContextCreate( imageData,
                                                targetWidth,
                                                targetHeight,
                                                8,
                                                targetWidth,
                                                colorSpace,
                                                0);
    
    CGContextDrawImage( bitmap, CGRectMake(0, 0, targetWidth, targetHeight), imageRef);
    
    CGContextRelease( bitmap);
    
    CGColorSpaceRelease( colorSpace);
    
    return imageData;
    
}




@end
