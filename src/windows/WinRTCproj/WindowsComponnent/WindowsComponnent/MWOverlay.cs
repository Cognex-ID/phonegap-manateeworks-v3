/*
 
 Version 1.0
  
 The MWOverlay class serves to greatly simplify the addition of a dynamic viewfinder (similar to the one implemented 
 in Manatee Works Barcode Scanners application) to your own application.
 Minimum setup assumes:
 - Make sure you are using latest BarcodeHelper.cs in your app
 - Add WriteableBitmapEx class to the priject (http://writeablebitmapex.codeplex.com/)
 1. Add MWOverlay.cs files to your project;
 2. Put MWOverlay.addOverlay(canvas); after initializing the camera; Assumes that VideoBrush is child of Canvas named 'canvas'
 3. Put MWOverlay.removeOverlay(); on leaving the scanning page;
 
 If all steps are done correctly, you should be able to see a default red viewfinder with a blinking line, capable
 of updating itself automatically after changing any of the scanning parameters (scanning direction, scanning rectangles 
 and active barcode symbologies).
 The appearance of the viewfinder and the blinking line can be further customized by changing colors, line width, transparencies
 and similar, by setting the following properties:
 
    MWOverlay.isViewportVisible;
    MWOverlay.isBlinkingLineVisible;
    MWOverlay.viewportLineWidth;
    MWOverlay.blinkingLineWidth;
    MWOverlay.viewportAlpha;
    MWOverlay.viewportLineAlpha;
    MWOverlay.blinkingLineAlpha;
    MWOverlay.blinkingSpeed;
    MWOverlay.viewportLineColor;
    MWOverlay.blinkingLineColor;
  
 Note: Due to the limitation of WritableBitmapEx, it's not possible to use custom width for slope lines, so they will be 1px width
 regardless of blinkingLineWidth param.

 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Media.Animation;
using WindowsComponnent;
using Windows.UI;
using BarcodeLib;

//using System.Windows.Threading;


namespace WindowsComponnent
{
    public sealed class MWOverlay
    {
        private static Image viewportLayer;
        private static Image lineLayer;
        private static Windows.UI.Xaml.Controls.Canvas currentCanvas;
        private static Storyboard blinkingStoryboard;
        private static DispatcherTimer checkTimer;
        private static float cameraAR;


        private static bool isViewportVisible = true;

        private static bool isBlinkingLineVisible  = true;

        private static float viewportLineWidth  = 4.0f;
        private static float blinkingLineWidth  = 1.0f;
        private static float viewportAlpha = 0.5f;
        private static float viewportLineAlpha  = 0.5f;
        private static float blinkingLineAlpha = 1.0f;
        private static float blinkingSpeed = 0.25f;
        private static int viewportLineColor  = 0xff0000;
        private static int blinkingLineColor = 0xff0000;

       

        private static int lastOrientation = -1;
        private static float lastLeft = -1;
        private static float lastTop = -1;
        private static float lastWidth = -1;
        private static float lastHeight = -1;
        private static float lastBLinkingSpeed = -1;




        public static void addOverlay(Windows.UI.Xaml.Controls.Canvas canvas, float cameraAspectRatio)
        {
            cameraAR = cameraAspectRatio;

            if (viewportLayer != null)
            {
                removeOverlay();

            }

            viewportLayer = new Image();
            lineLayer = new Image();

            currentCanvas = canvas;
            canvas.Children.Add(viewportLayer);
            canvas.Children.Add(lineLayer);
            updateOverlay();
            addAnimation();

            checkTimer = new DispatcherTimer();
            checkTimer.Interval = TimeSpan.FromSeconds(0.2);
            checkTimer.Tick += delegate
            {
         
                checkChange();

            };
            checkTimer.Start();
          

        }

        public static void removeOverlay()
        {
            checkTimer.Stop();
            checkTimer = null;
            removeAnimation();
            if (viewportLayer != null)
            {
                currentCanvas.Children.Remove(viewportLayer);
                currentCanvas.Children.Remove(lineLayer);
                viewportLayer = null;
                lineLayer = null;
            }

        }

        private static void addAnimation()
        {
            DoubleAnimation blinkingAnimation = new DoubleAnimation();
            blinkingAnimation.AutoReverse = true;
            blinkingAnimation.RepeatBehavior = RepeatBehavior.Forever;
            blinkingAnimation.Duration = new Duration(TimeSpan.FromSeconds(blinkingSpeed));
            blinkingAnimation.From = 0;
            blinkingAnimation.To = 1;

            blinkingStoryboard = new Storyboard();
            blinkingStoryboard.Duration = new Duration(TimeSpan.FromDays(10));
            blinkingStoryboard.Children.Add(blinkingAnimation);

            Storyboard.SetTarget(blinkingAnimation, lineLayer);
            Storyboard.SetTargetProperty(blinkingAnimation, "(Opacity)");

            blinkingStoryboard.Begin();
        }

        private static void removeAnimation()
        {
            if (blinkingStoryboard != null)
            {
                blinkingStoryboard.Stop();
                blinkingStoryboard = null;
            }
           
            
        }

        private static Windows.UI.Color colorFromAlphaAndInt(float alpha, int intColor)
        {

            int intAlpha = (int)(alpha * 255);
            int r = intColor >> 16;
            int g = (intColor >> 8) & 0xff;
            int b = intColor & 0xff;

            return Windows.UI.Color.FromArgb((byte)intAlpha, (byte)r, (byte)g, (byte)b);


        }

        private static void updateOverlay()
        {

            Windows.Foundation.Rect unionRect = BarcodeHelper.MWBgetScanningRect(0);
            int orientation = BarcodeLib.Scanner.MWBgetDirection();

            int width = (int)currentCanvas.Width;
            int height = (int)currentCanvas.Height;

            float canvasAR = (float)height / width;
            int left = 0;
            int top = 0;

            if (canvasAR > cameraAR)
            {
                height = (int)(cameraAR * width);
                top = ((int)currentCanvas.Height - height) / 2;
            }
            else
            {
                width = (int)(height / cameraAR);
                left = ((int)currentCanvas.Width - width) / 2;
            }

            if (width <= 0)
                width = 800;

            if (height <= 0)
                height = 480;


            int rectLeft = (int)((float)unionRect.Left * width / 100.0) + left;
            int rectTop = (int)((float)unionRect.Top * height / 100.0) + top;
            int rectWidth = (int)((float)unionRect.Width * width / 100.0);
            int rectHeight = (int)((float)unionRect.Height * height / 100.0);
            int rectRight = (int)((float)unionRect.Right * width / 100.0) + left;
            int rectBottom = (int)((float)unionRect.Bottom * height / 100.0) + top;

            if (isViewportVisible)
            {

                viewportLayer.Visibility = Visibility.Visible;

                var bitmapviewport = new WriteableBitmap((int)currentCanvas.Width, (int)currentCanvas.Height);
                bitmapviewport.FillRectangle(0, 0, (int)currentCanvas.Width, (int)currentCanvas.Height, colorFromAlphaAndInt(viewportAlpha, 0));
                int lineWidth2 = (int)(viewportLineWidth / 2.0);

                bitmapviewport.FillRectangle(rectLeft - lineWidth2, rectTop - lineWidth2, rectRight + lineWidth2, rectBottom + lineWidth2, colorFromAlphaAndInt(viewportLineAlpha, viewportLineColor));
                bitmapviewport.FillRectangle(rectLeft, rectTop, rectRight, rectBottom, Color.FromArgb(0,0, 0, 0));
                
                


                viewportLayer.Source = bitmapviewport;
            }
            else
            {
                viewportLayer.Visibility = Visibility.Collapsed;
            }


            if (isBlinkingLineVisible)
            {

                lineLayer.Visibility = Visibility.Visible;

                addAnimation();



                var bitmapLine = new WriteableBitmap((int)currentCanvas.Width, (int)currentCanvas.Height);
                int lineWidth2 = (int)(blinkingLineWidth / 2.0);
                int widthAddon = 0;
                if (lineWidth2 == 0)
                {
                    widthAddon = 1;
                }

                if (((orientation & BarcodeLib.Scanner.MWB_SCANDIRECTION_HORIZONTAL) > 0) || ((orientation & BarcodeLib.Scanner.MWB_SCANDIRECTION_OMNI) > 0))
                {
                  
                    bitmapLine.FillRectangle(rectLeft, rectTop + rectHeight / 2 - lineWidth2, rectRight, rectTop + rectHeight / 2 + lineWidth2 + widthAddon, colorFromAlphaAndInt(blinkingLineAlpha, blinkingLineColor));
                    
                }

                if (((orientation & BarcodeLib.Scanner.MWB_SCANDIRECTION_VERTICAL) > 0) || ((orientation & BarcodeLib.Scanner.MWB_SCANDIRECTION_OMNI) > 0))
                {
                    bitmapLine.FillRectangle(rectLeft + rectWidth / 2 - lineWidth2, rectTop, rectLeft + rectWidth / 2 + lineWidth2 + widthAddon, rectBottom, colorFromAlphaAndInt(blinkingLineAlpha, blinkingLineColor));
                }

                if ((orientation & BarcodeLib.Scanner.MWB_SCANDIRECTION_OMNI) > 0)
                {

                    bitmapLine.DrawLine(rectLeft, rectTop, rectRight, rectBottom, colorFromAlphaAndInt(blinkingLineAlpha, blinkingLineColor));
                    bitmapLine.DrawLine(rectLeft, rectBottom, rectRight, rectTop, colorFromAlphaAndInt(blinkingLineAlpha, blinkingLineColor));

                }

                lineLayer.Source = bitmapLine;
            }
            else
            {
                lineLayer.Visibility = Visibility.Collapsed;
                removeAnimation();
            }

        }


        private static void checkChange()
        {

            if (lineLayer == null || viewportLayer == null)
                return;

            
            Windows.Foundation.Rect frame = BarcodeHelper.MWBgetScanningRect(0);
            int orientation = BarcodeLib.Scanner.MWBgetDirection();

            if (orientation != lastOrientation || frame.Left != lastLeft || frame.Top != lastTop || frame.Width != lastWidth || frame.Height != lastHeight)
            {

                updateOverlay();
            }

            if (lastBLinkingSpeed != blinkingSpeed)
            {
                removeAnimation();
                addAnimation();
            }

            if (isBlinkingLineVisible != (lineLayer.Visibility == Visibility.Visible))
            {
                updateOverlay();
            }

            if (isViewportVisible != (viewportLayer.Visibility == Visibility.Visible))
            {
                updateOverlay();
            }

            lastOrientation = orientation;
            lastLeft = (float) frame.Left;
            lastTop = (float) frame.Top;
            lastWidth = (float) frame.Width;
            lastHeight = (float) frame.Height;
            lastBLinkingSpeed = blinkingSpeed;

        }

    }
}
