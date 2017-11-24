//
//  CDVMWBarcodeScanner.h
//  CameraDemo
//
//  Created by vladimir zivkovic on 5/8/13.
//  Modified by @zikam on 2/3/17
//
//

#import "MWScannerViewController.h"
#import <Cordova/CDV.h>
#import "MWResult.h"

@interface CDVMWBarcodeScanner : CDVPlugin <ScanningFinishedDelegate>

- (void)initDecoder:(CDVInvokedUrlCommand*)command;
- (void)usePartialScanner:(CDVInvokedUrlCommand*) command;
- (void)resizePartialScanner:(CDVInvokedUrlCommand*)command;
- (void)startScanner:(CDVInvokedUrlCommand*)command;
- (void)setActiveCodes:(CDVInvokedUrlCommand*)command;
- (void)setActiveSubcodes:(CDVInvokedUrlCommand*)command;
- (void)setFlags:(CDVInvokedUrlCommand*)command;
- (void)setDirection:(CDVInvokedUrlCommand*)command;
- (void)setScanningRect:(CDVInvokedUrlCommand*)command;
- (void)setLevel:(CDVInvokedUrlCommand*)command;
- (void)registerCode:(CDVInvokedUrlCommand*)command;
- (int)getLastType:(CDVInvokedUrlCommand*)command;
- (void)getDeviceID:(CDVInvokedUrlCommand*)command;

- (void)setInterfaceOrientation:(CDVInvokedUrlCommand*)command;
- (void)setOverlayMode:(CDVInvokedUrlCommand*)command;
- (void)enableHiRes:(CDVInvokedUrlCommand*)command;
- (void)enableFlash:(CDVInvokedUrlCommand*)command;
- (void)enableZoom:(CDVInvokedUrlCommand*)command;
- (void)turnFlashOn:(CDVInvokedUrlCommand*)command;
- (void)setZoomLevels:(CDVInvokedUrlCommand*)command;
- (void)setMaxThreads:(CDVInvokedUrlCommand*)command;
- (void)setCustomParam:(CDVInvokedUrlCommand*)command;
- (void)scanImage:(CDVInvokedUrlCommand*)command;
- (void)setParam:(CDVInvokedUrlCommand*)command;
- (void)setActiveParser:(CDVInvokedUrlCommand*)command;
- (void)setCloseDelay:(CDVInvokedUrlCommand*)command;
@end

