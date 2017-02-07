//
//  MWOverlay.m
//  mobiscan_ALL
//
//  Created by vladimir zivkovic on 12/12/13.
//  Copyright (c) 2013 Manatee Works. All rights reserved.
//

#import "MWOverlay.h"
#import <AVFoundation/AVFoundation.h>
#import <CoreGraphics/CoreGraphics.h>
#import "BarcodeScanner.h"

#define CHANGE_TRACKING_INTERVAL 0.1

@implementation MWOverlay

CALayer *viewportLayer;
CALayer *lineLayer;
CALayer *locationLayer;
AVCaptureVideoPreviewLayer * previewLayer;
BOOL isAttached = NO;
BOOL isViewportVisible = YES;
BOOL isBlinkingLineVisible = YES;

enum PauseMode pauseMode = PM_PAUSE;

MWOverlay *instance = nil;

float viewportLineWidth = 3.0;
float blinkingLineWidth = 1.0;
float locationLineWidth = 4.0;
float viewportAlpha = 0.5;
float viewportLineAlpha = 0.5;
float blinkingLineAlpha = 1.0;
float blinkingSpeed = 0.25;
int viewportLineColor = 0xff0000;
int blinkingLineColor = 0xff0000;
int locationLineColor = 0x00ff00;

int lastOrientation = -1;
int lastMask = -1;
float lastLeft = -1;
float lastTop = -1;
float lastWidth = -1;
float lastHeight = -1;

int imageWidth = 1;
int imageHeight = 1;

BOOL isPaused = NO;



+ (void) updatePreviewLayer {
    viewportLayer.frame = CGRectMake(0, 0, previewLayer.frame.size.width, previewLayer.frame.size.height);
    lineLayer.frame = CGRectMake(0, 0, previewLayer.frame.size.width, previewLayer.frame.size.height);
    locationLayer.frame = CGRectMake(0, 0, previewLayer.frame.size.width, previewLayer.frame.size.height);
    [MWOverlay updateOverlay];
}


+ (void) addToPreviewLayer:(AVCaptureVideoPreviewLayer *) videoPreviewLayer
{
    viewportLayer = [[CALayer alloc] init];
    viewportLayer.frame = CGRectMake(0, 0, videoPreviewLayer.frame.size.width, videoPreviewLayer.frame.size.height);
    
    lineLayer = [[CALayer alloc] init];
    lineLayer.frame = CGRectMake(0, 0, videoPreviewLayer.frame.size.width, videoPreviewLayer.frame.size.height);
    
    locationLayer = [[CALayer alloc] init];
    locationLayer.frame = CGRectMake(0, 0, videoPreviewLayer.frame.size.width, videoPreviewLayer.frame.size.height);
    
    [videoPreviewLayer addSublayer:viewportLayer];
    [videoPreviewLayer addSublayer:lineLayer];
    
    previewLayer = videoPreviewLayer;
    
    isAttached = YES;
    
    instance = [[MWOverlay alloc] init];
    
    
    [instance performSelector:@selector(checkForChanges) withObject:nil afterDelay:CHANGE_TRACKING_INTERVAL];
    [MWOverlay updateOverlay];
    
}

+ (void) removeFromPreviewLayer {
    
    if (!isAttached){
        return;
    }
    
    if (previewLayer){
        if (lineLayer){
            [lineLayer removeFromSuperlayer];
        }
        if (viewportLayer){
            [viewportLayer removeFromSuperlayer];
        }
        
        if (locationLayer){
            [locationLayer removeFromSuperlayer];
        }
    }
    
    isAttached = NO;
    
}

+ (void) setPaused: (BOOL) paused
{
    if (isPaused != paused) {
        isPaused = paused;
        if (instance != nil && previewLayer != nil && lineLayer != nil){
            [instance performSelectorOnMainThread:@selector(updateOverlayMainThread) withObject:nil waitUntilDone:NO];
        }
    }
}
+ (void) setPauseMode: (enum PauseMode) pMode
{
    pauseMode = pMode;
}
- (void) checkForChanges {
    
    if (isAttached){
        [instance performSelector:@selector(checkForChanges) withObject:nil afterDelay:CHANGE_TRACKING_INTERVAL];
    } else {
        return;
    }
    
    float left, top, width, height;
    int res = MWB_getScanningRect(0, &left, &top, &width, &height);
    
    if (res == 0){
        
        int orientation = MWB_getDirection();
        
        if (orientation != lastOrientation || left != lastLeft || top != lastTop || width != lastWidth || height != lastHeight) {
            
            [instance performSelectorOnMainThread:@selector(updateOverlayMainThread) withObject:nil waitUntilDone:NO];
            // NSLog(@"Change detected");
            
        }
        
        lastOrientation = orientation;
        lastLeft = left;
        lastTop = top;
        lastWidth = width;
        lastHeight = height;
        
    }
    
    
}

- (void) updateOverlayMainThread {
    
    [MWOverlay updateOverlay];
    
}

+ (void) updateOverlay{
    
    if (!isAttached || !previewLayer){
        return;
    }
    
    float yScale = 1.0;
    float yOffset = 0.0;
    float xScale = 1.0;
    float xOffset = 0.0;
    
    //aspect ratio correction available only on ios 6+
    if ([previewLayer respondsToSelector:@selector(captureDevicePointOfInterestForPoint:)]){
        CGPoint p1 = [previewLayer captureDevicePointOfInterestForPoint:CGPointMake(0,0)];
        yScale = -1.0/(1 + (p1.y - 1)*2);
        yOffset = (1.0 - yScale) / 2.0 * 100;
        xScale = -1.0/(1 + (p1.x - 1)*2);
        xOffset = (1.0 - xScale) / 2.0 * 100;
        if (previewLayer.connection.videoOrientation == AVCaptureVideoOrientationPortrait || previewLayer.connection.videoOrientation == AVCaptureVideoOrientationPortraitUpsideDown){
            yScale = -1.0/(1 + (p1.x - 1)*2);
            yOffset = (1.0 - yScale) / 2.0 * 100;
            xScale = -1.0/(1 + (p1.y - 1)*2);
            xOffset = (1.0 - xScale) / 2.0 * 100;
        }
    }
    
    viewportLayer.hidden = !isViewportVisible;
    lineLayer.hidden = !isBlinkingLineVisible;
    
    
    int overlayWidth = viewportLayer.frame.size.width;
    int overlayHeight = viewportLayer.frame.size.height;
    
    CGRect cgRect = viewportLayer.frame;
    
    
    UIGraphicsBeginImageContext(cgRect.size);
    CGContextRef context = UIGraphicsGetCurrentContext();
    UIGraphicsPushContext(context);
    
    
    
    float left, top, width, height;
    
    MWB_getScanningRect(0, &left, &top, &width, &height);
    
    if (previewLayer.connection.videoOrientation == AVCaptureVideoOrientationPortrait || previewLayer.connection.videoOrientation == AVCaptureVideoOrientationPortraitUpsideDown){
        
        float tmp = left;
        left = top;
        top = tmp;
        tmp = height;
        height = width;
        width = tmp;
        
    }
    
    if (previewLayer.connection.videoOrientation == AVCaptureVideoOrientationLandscapeLeft){
        left = 100 - width - left;
        top = 100 - height - top;
    } else
        if (previewLayer.connection.videoOrientation == AVCaptureVideoOrientationPortrait){
            left = 100 - width - left;
        }
        else
            if (previewLayer.connection.videoOrientation == AVCaptureVideoOrientationPortraitUpsideDown){
                top = 100 - height - top;
            }

    
    CGRect rect = CGRectMake(xOffset + left * xScale, yOffset + top * yScale, width * xScale, height * yScale);
    
    if (rect.size.width < 0){
        rect.size.width = - rect.size.width;
        rect.origin.x = 100 - rect.origin.x;
    }
    
    if (rect.size.height < 0){
        rect.size.height = - rect.size.height;
        rect.origin.y = 100 - rect.origin.y;
    }
    
    
    rect.origin.x *= overlayWidth;
    rect.origin.x /= 100.0;
    rect.origin.y *= overlayHeight;
    rect.origin.y /= 100.0;
    rect.size.width *= overlayWidth;
    rect.size.width /= 100.0;
    rect.size.height *= overlayHeight;
    rect.size.height /= 100.0;
    
    
   

    
    CGContextSetRGBFillColor(context, 0, 0, 0, viewportAlpha);
    CGContextFillRect(context, CGRectMake(0,0,overlayWidth,overlayHeight));
    CGContextClearRect(context, rect);
    
    float r = (viewportLineColor >> 16) / 255.0;
    float g = ((viewportLineColor & 0x00ff00) >> 8) / 255.0;
    float b = (viewportLineColor & 0x0000ff) / 255.0;
    
    
    CGContextSetRGBStrokeColor(context, r, g, b, 0.5);
    CGContextStrokeRectWithWidth(context, rect, viewportLineWidth);
    
    UIGraphicsPopContext();
    
    viewportLayer.contents = (id)[UIGraphicsGetImageFromCurrentImageContext() CGImage];
    
    
    
    CGContextClearRect(context, cgRect);
    
    r = (blinkingLineColor >> 16) / 255.0;
    g = ((blinkingLineColor & 0x00ff00) >> 8) / 255.0;
    b = (blinkingLineColor & 0x0000ff) / 255.0;
    
    CGContextSetRGBStrokeColor(context, r, g, b, 1);
    
    int orientation = MWB_getDirection();
    
    if (previewLayer.connection.videoOrientation == AVCaptureVideoOrientationPortrait || previewLayer.connection.videoOrientation == AVCaptureVideoOrientationPortraitUpsideDown){
        
        double pos1f = log(MWB_SCANDIRECTION_HORIZONTAL) / log(2);
        double pos2f = log(MWB_SCANDIRECTION_VERTICAL) / log(2);
        
        int pos1 = (int)(pos1f + 0.01);
        int pos2 = (int)(pos2f + 0.01);
        
        int bit1 = (orientation >> pos1) & 1;// bit at pos1
        int bit2 = (orientation >> pos2) & 1;// bit at pos2
        int mask = (bit2 << pos1) | (bit1 << pos2);
        orientation = orientation & 0xc;
        orientation = orientation | mask;
        
    }
    if (isPaused && pauseMode == PM_PAUSE){
        CGContextSetRGBFillColor(context, r, g, b, 1);
        float size = MIN(overlayHeight, overlayWidth) / 10;
        CGContextFillRect(context, CGRectMake(rect.origin.x + rect.size.width / 2 - size / 2, rect.origin.y + rect.size.height / 2 - size / 2, size / 3, size));
        
        CGContextFillRect(context, CGRectMake(rect.origin.x + rect.size.width / 2 + size / 6, rect.origin.y + rect.size.height / 2 - size / 2, size / 3, size));
        
        
    } else {
    
        if (orientation & MWB_SCANDIRECTION_HORIZONTAL || orientation & MWB_SCANDIRECTION_OMNI || orientation & MWB_SCANDIRECTION_AUTODETECT){
            CGContextSetLineWidth(context, blinkingLineWidth);
            CGContextMoveToPoint(context, rect.origin.x, rect.origin.y + rect.size.height / 2);
            CGContextAddLineToPoint(context, rect.origin.x + rect.size.width, rect.origin.y + rect.size.height / 2);
            CGContextStrokePath(context);
        }
        
        if (orientation & MWB_SCANDIRECTION_VERTICAL || orientation & MWB_SCANDIRECTION_OMNI || orientation & MWB_SCANDIRECTION_AUTODETECT){
            
            CGContextMoveToPoint(context, rect.origin.x + rect.size.width / 2, rect.origin.y);
            CGContextAddLineToPoint(context, rect.origin.x + rect.size.width / 2, rect.origin.y + rect.size.height);
            CGContextStrokePath(context);
        }
        
        if (orientation & MWB_SCANDIRECTION_OMNI || orientation & MWB_SCANDIRECTION_AUTODETECT){
            CGContextMoveToPoint(context, rect.origin.x , rect.origin.y);
            CGContextAddLineToPoint(context, rect.origin.x + rect.size.width , rect.origin.y + rect.size.height);
            CGContextStrokePath(context);
            
            CGContextMoveToPoint(context, rect.origin.x + rect.size.width, rect.origin.y);
            CGContextAddLineToPoint(context, rect.origin.x , rect.origin.y + rect.size.height);
            CGContextStrokePath(context);
        }
    }
    
    lineLayer.contents = (id)[UIGraphicsGetImageFromCurrentImageContext() CGImage];
    
    UIGraphicsEndImageContext();
    
    
    if(isPaused && pauseMode == PM_STOP_BLINKING){
        [lineLayer removeAllAnimations];
    }else{
        [MWOverlay startLineAnimation];
    }
    
}

+ (void) showLocation: (CGPoint *) points imageWidth:(int) width imageHeight: (int) height{
    
    imageWidth = width;
    imageHeight = height;
    
    if (points == NULL){
        return;
    }
    
    if (!isAttached || !previewLayer){
        return;
    }
    
    
    
//    dispatch_async(dispatch_get_main_queue(), ^(void) {
    
        [locationLayer removeAllAnimations];
        
        [previewLayer addSublayer:locationLayer];
        
        
        CGRect cgRect = locationLayer.frame;
        
        
        UIGraphicsBeginImageContext(cgRect.size);
        CGContextRef context = UIGraphicsGetCurrentContext();
        UIGraphicsPushContext(context);
        
        
        CGContextClearRect(context, cgRect);
        
        float r = (locationLineColor >> 16) / 255.0;
        float g = ((locationLineColor & 0x00ff00) >> 8) / 255.0;
        float b = (locationLineColor & 0x0000ff) / 255.0;
        
        CGContextSetRGBStrokeColor(context, r, g, b, 1);
        
        CGContextSetLineWidth(context, locationLineWidth);
        
             
        for (int i = 0; i < 4; i++){
            points[i].x/= imageWidth;
            points[i].y/= imageHeight;
            
            points[i] = [previewLayer pointForCaptureDevicePointOfInterest:points[i]];
            
        }
        
        
        CGContextMoveToPoint(context, points[0].x,points[0].y);
        for (int i = 1; i < 4; i++){
            CGContextAddLineToPoint(context, points[i].x,points[i].y);
        }
        CGContextAddLineToPoint(context, points[0].x,points[0].y);
        
        CGContextStrokePath(context);
        
        UIGraphicsPopContext();
        
        locationLayer.contents = (id)[UIGraphicsGetImageFromCurrentImageContext() CGImage];
        
        UIGraphicsEndImageContext();
        
        CABasicAnimation *animation = [CABasicAnimation animationWithKeyPath:@"opacity"];
        [animation setFromValue:[NSNumber numberWithFloat:1]];
        [animation setToValue:[NSNumber numberWithFloat:0.0]];
        [animation setDuration:0.5];
        [animation setTimingFunction:[CAMediaTimingFunction
                                      functionWithName:kCAMediaTimingFunctionEaseOut]];
        [animation setFillMode:kCAFillModeForwards];
        [animation setRemovedOnCompletion:NO];
        // [animation setAutoreverses:NO];
        //[animation setRepeatCount:0];
        [locationLayer addAnimation:animation forKey:@"opacity"];
        
        
//    });
    
}



+ (void) startLineAnimation {
    [lineLayer removeAllAnimations];
    CABasicAnimation *animation = [CABasicAnimation animationWithKeyPath:@"opacity"];
    [animation setFromValue:[NSNumber numberWithFloat:blinkingLineAlpha]];
    [animation setToValue:[NSNumber numberWithFloat:0.0]];
    [animation setDuration:blinkingSpeed];
    [animation setTimingFunction:[CAMediaTimingFunction
                                  functionWithName:kCAMediaTimingFunctionLinear]];
    [animation setAutoreverses:YES];
    [animation setRepeatCount:INFINITY];
    [lineLayer addAnimation:animation forKey:@"opacity"];
}

+ (void) setViewportVisible: (BOOL) value {
    
    isViewportVisible = value;
    
}

+ (void) setBlinkingLineVisible: (BOOL) value {
    
    isBlinkingLineVisible = value;
    [MWOverlay updateOverlay];
    
}

+ (void) setViewportLineWidth: (float) value {
    
    viewportLineWidth = value;
    [MWOverlay updateOverlay];
    
}

+ (void) setBlinkingLineWidth: (float) value {
    
    blinkingLineWidth = value;
    [MWOverlay updateOverlay];
    
}

+ (void) setViewportAlpha: (float) value {
    
    viewportAlpha = value;
    [MWOverlay updateOverlay];
    
}

+ (void) setViewportLineAlpha: (float) value {
    
    viewportLineAlpha = value;
    [MWOverlay updateOverlay];
    
}

+ (void) setBlinkingLineAlpha: (float) value {
    
    blinkingLineAlpha = value;
    [MWOverlay updateOverlay];
    
}

+ (void) setBlinkingSpeed: (float) value {
    
    blinkingSpeed = value;
    [MWOverlay updateOverlay];
    
}

+ (void) setViewportLineRGBColor: (int) value {
    
    viewportLineColor = value;
    [MWOverlay updateOverlay];
    
}

+ (void) setBlinkingLineRGBColor: (int) value {
    
    blinkingLineColor = value;
    [MWOverlay updateOverlay];
    
}

+ (void) setViewportLineUIColor: (UIColor*) value {
    
    CGColorRef color = [value CGColor];
    
    int numComponents = CGColorGetNumberOfComponents(color);
    
    if (numComponents >= 3)
    {
        const CGFloat *components = CGColorGetComponents(color);
        CGFloat red = components[0];
        CGFloat green = components[1];
        CGFloat blue = components[2];
        
        int intColor = (((int) (red * 255)) << 16) + (((int) (green * 255)) << 8) + ((int) (blue * 255)) ;
        
        viewportLineColor = intColor;
        [MWOverlay updateOverlay];
        
    }
}

+ (void) setBlinkingLineUIColor: (UIColor*) value {
    
    CGColorRef color = [value CGColor];
    
    int numComponents = CGColorGetNumberOfComponents(color);
    
    if (numComponents >= 3)
    {
        const CGFloat *components = CGColorGetComponents(color);
        CGFloat red = components[0];
        CGFloat green = components[1];
        CGFloat blue = components[2];
        
        int intColor = (((int) (red * 255)) << 16) + (((int) (green * 255)) << 8) + ((int) (blue * 255)) ;
        
        blinkingLineColor = intColor;
        [MWOverlay updateOverlay];
        
    }
}




@end
