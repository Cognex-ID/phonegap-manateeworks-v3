/*
 
 Version 1.0
  
 The MWOverlay class serves to greatly simplify the addition of a dynamic viewfinder (similar to the one implemented 
 in Manatee Works Barcode Scanners application) to your own application.
 Minimum setup assumes:
 - Make sure you are using latest BarcodeHelper.cs in your app
 - Add WriteableBitmapEx class to the priject (http://writeablebitmapex.codeplex.com/)
 1. Add MWOverlay.cs files to your project;
 2. Put MWOverlay.
 * (canvas); after initializing the camera; Assumes that VideoBrush is child of Canvas named 'canvas'
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
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Microsoft.Phone.Controls;
using System.Windows;

namespace BarcodeScanners
{
    class MWOverlay
    {
        private static Image viewportLayer;
        private static Image lineLayer;
        private static Canvas currentCanvas;
        private static Storyboard blinkingStoryboard;
        private static DispatcherTimer checkTimer;

        public static bool isViewportVisible = true;
        public static bool isBlinkingLineVisible = true;

        public static float viewportLineWidth = 4.0f;
        public static float blinkingLineWidth = 1.0f;
        public static float viewportAlpha = 0.5f;
        public static float viewportLineAlpha = 0.5f;
        public static float blinkingLineAlpha = 1.0f;
        public static float blinkingSpeed = 0.25f;
        public static int viewportLineColor = 0xff0000;
        public static int blinkingLineColor = 0xff0000;

        private static int lastOrientation = -1;
        private static float lastLeft = -1;
        private static float lastTop = -1;
        private static float lastWidth = -1;
        private static float lastHeight = -1;
        private static float lastBLinkingSpeed = -1;

        public static void addOverlay(Canvas canvas)
        {
            if (viewportLayer != null)
            {
                removeOverlay();
            }

            viewportLayer = new Image();
            lineLayer = new Image();

            currentCanvas = canvas;

            UIElementCollection children = canvas.Children;

            canvas.Children.Add(viewportLayer);
            canvas.Children.Add(lineLayer);

            for (int i = 0; i < canvas.Children.Count - 2; i++)
            {
                System.Windows.UIElement element = children.ElementAt(0);
                    
                canvas.Children.Remove(element);
                canvas.Children.Add(element);

            }           

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
            if (checkTimer != null) { 
                checkTimer.Stop();
                checkTimer = null;
            }
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
            blinkingAnimation.Duration = new System.Windows.Duration(TimeSpan.FromSeconds(blinkingSpeed));
            blinkingAnimation.From = 0;
            blinkingAnimation.To = 1;


            blinkingStoryboard = new Storyboard();
            blinkingStoryboard.Duration = new System.Windows.Duration(TimeSpan.FromDays(10));
            blinkingStoryboard.Children.Add(blinkingAnimation);

            Storyboard.SetTarget(blinkingAnimation, lineLayer);
            Storyboard.SetTargetProperty(blinkingAnimation, new System.Windows.PropertyPath("(Opacity)"));

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

        private static System.Windows.Media.Color colorFromAlphaAndInt(float alpha, int intColor)
        {

            int intAlpha = (int)(alpha * 255);
            int r = intColor >> 16;
            int g = (intColor >> 8) & 0xff;
            int b = intColor & 0xff;

            return System.Windows.Media.Color.FromArgb((byte)intAlpha, (byte)r, (byte)g, (byte)b);


        }

        private static void updateOverlay()
        {

            Windows.Foundation.Rect unionRect = BarcodeHelper.MWBgetScanningRect(0);
            int orientation = BarcodeLib.Scanner.MWBgetDirection();

            int width = (int)currentCanvas.ActualWidth;
            int height = (int)currentCanvas.ActualHeight;

            if (width <= 0 || height == 0)
            {
                DispatcherTimer updateDelayed = new DispatcherTimer();
                updateDelayed.Interval = TimeSpan.FromSeconds(0.2);
                updateDelayed.Tick += delegate
                {

                    updateOverlay();
                    updateDelayed.Stop();
                };
                updateDelayed.Start();
                return;
            }

            

            PageOrientation currentOrientation = (((PhoneApplicationFrame)Application.Current.RootVisual).Content as PhoneApplicationPage).Orientation;

          if ((currentOrientation & PageOrientation.LandscapeRight) == (PageOrientation.LandscapeRight))
            {
                unionRect = new Windows.Foundation.Rect(100 - unionRect.Right, 100 -unionRect.Bottom, unionRect.Width, unionRect.Height);
            }
            else if ((currentOrientation & PageOrientation.PortraitUp) == (PageOrientation.PortraitUp))
            {
                unionRect = new Windows.Foundation.Rect(100 - unionRect.Top - unionRect.Height, unionRect.Left, unionRect.Height,unionRect.Width);
            }

           

            int rectLeft = (int)((float)unionRect.Left * width / 100.0);
            int rectTop = (int)((float)unionRect.Top * height / 100.0);
            int rectWidth = (int)((float)unionRect.Width * width / 100.0);
            int rectHeight = (int)((float)unionRect.Height * height / 100.0);
            int rectRight = (int)((float)unionRect.Right * width / 100.0);
            int rectBottom = (int)((float)unionRect.Bottom * height / 100.0);




      


            if (isViewportVisible)
            {

                viewportLayer.Visibility = System.Windows.Visibility.Visible;

                var bitmapviewport = new WriteableBitmap(width, height);
                bitmapviewport.FillRectangle(0, 0, width, height, colorFromAlphaAndInt(viewportAlpha, 0));

                int lineWidth2 = (int)(viewportLineWidth / 2.0);

                bitmapviewport.FillRectangle(rectLeft - lineWidth2, rectTop - lineWidth2, rectRight + lineWidth2, rectBottom + lineWidth2, colorFromAlphaAndInt(viewportLineAlpha, viewportLineColor));

                bitmapviewport.FillRectangle(rectLeft, rectTop, rectRight, rectBottom, System.Windows.Media.Color.FromArgb(0, 0, 0, 0));
                
                


                viewportLayer.Source = bitmapviewport;
            }
            else
            {
                viewportLayer.Visibility = System.Windows.Visibility.Collapsed;
            }


            if (isBlinkingLineVisible)
            {

                lineLayer.Visibility = System.Windows.Visibility.Visible;

                addAnimation();
               
                if (width < height)
                {

                    double pos1f = Math.Log(BarcodeLib.Scanner.MWB_SCANDIRECTION_HORIZONTAL) / Math.Log(2);
                    double pos2f = Math.Log(BarcodeLib.Scanner.MWB_SCANDIRECTION_VERTICAL) / Math.Log(2);

                    int pos1 = (int)(pos1f + 0.01);
                    int pos2 = (int)(pos2f + 0.01);

                    int bit1 = (orientation >> pos1) & 1;// bit at pos1
                    int bit2 = (orientation >> pos2) & 1;// bit at pos2
                    int mask = (bit2 << pos1) | (bit1 << pos2);
                    orientation = orientation & 0xc;
                    orientation = orientation | mask;

                }

               

                var bitmapLine = new WriteableBitmap(width, height);
                int lineWidth2 = (int)(blinkingLineWidth / 2.0);

                if (((orientation & BarcodeLib.Scanner.MWB_SCANDIRECTION_HORIZONTAL) > 0) || ((orientation & BarcodeLib.Scanner.MWB_SCANDIRECTION_OMNI) > 0))
                {
                  
                    bitmapLine.FillRectangle(rectLeft, rectTop + rectHeight / 2 - lineWidth2, rectRight, rectTop + rectHeight / 2 + lineWidth2, colorFromAlphaAndInt(blinkingLineAlpha, blinkingLineColor));
                    
                }

                if (((orientation & BarcodeLib.Scanner.MWB_SCANDIRECTION_VERTICAL) > 0) || ((orientation & BarcodeLib.Scanner.MWB_SCANDIRECTION_OMNI) > 0))
                {
                    bitmapLine.FillRectangle(rectLeft + rectWidth / 2 - lineWidth2, rectTop, rectLeft + rectWidth / 2 + lineWidth2, rectBottom, colorFromAlphaAndInt(blinkingLineAlpha, blinkingLineColor));
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
                lineLayer.Visibility = System.Windows.Visibility.Collapsed;
                removeAnimation();
            }

        }


        private static void checkChange()
        {

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

            if (isBlinkingLineVisible != (lineLayer.Visibility == System.Windows.Visibility.Visible))
            {
                updateOverlay();
            }

            if (isViewportVisible != (viewportLayer.Visibility == System.Windows.Visibility.Visible))
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
