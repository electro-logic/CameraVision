// Author: Leonardo Tazzini (http://electro-logic.blogspot.it)

using System;
using System.Threading.Tasks;

namespace CameraVision
{
    /// <summary>
    /// Algorithms that works on byte array of raw bayer pattern
    /// </summary>
    public static class BayerAlgoritms
    {
        /// <summary>
        /// Simple Bayer to Gray conversion. Algorithm not recommended for production.
        /// </summary>
        public static int[] GrayRaw(byte[] imageRaw, int imageWidth, int imageHeight)
        {
            int[] imageColor = new int[imageWidth * imageHeight];
            Parallel.For(0, imageHeight, (y) =>
            {
                for (int x = 0; x < imageWidth; x++)
                {
                    int pixelIndex = y * imageWidth + x;
                    byte color = imageRaw[pixelIndex];
                    imageColor[pixelIndex] = BitConverter.ToInt32(new byte[] { color, color, color, 255 }, 0);
                }
            });
            return imageColor;
        }

        /// <summary>
        /// Didactic implementation to view Bayer pattern. Algorithm not recommended for production.
        /// </summary>
        public static int[] ColorRaw(byte[] imageRaw, int imageWidth, int imageHeight)
        {
            int[] imageColor = new int[imageWidth * imageHeight];
            Parallel.For(0, imageHeight, (y) =>
            {
                for (int x = 0; x < imageWidth; x++)
                {
                    int pixelIndex = y * imageWidth + x;
                    byte color = imageRaw[pixelIndex];
                    bool rowEven = (y % 2) == 0;
                    bool colEven = (x % 2) == 0;
                    byte r = 0, g = 0, b = 0;
                    if ((rowEven) && (colEven))
                    {
                        r = 0;g = 0;b = color;
                    }
                    if ((!rowEven) && (colEven))
                    {
                        r = 0; g = color; b = 0;                        
                    }
                    if ((rowEven) && (!colEven))
                    {
                        r = 0; g = color; b = 0;                        
                    }
                    if ((!rowEven) && (!colEven))
                    {
                        r = color; g = 0; b = 0;
                    }
                    imageColor[pixelIndex] = BitConverter.ToInt32(new byte[] { b, g, r, 255 }, 0);
                }
            });
            return imageColor;
        }

        /// <summary>
        /// Demosaicize a RAW image (8 bit BGGR pattern) into a BGR32 image.
        /// Low-performance and simple interpolation pattern. Algorithm not recommended for production.
        /// </summary>
        public static int[] Demosaic(byte[] imageRaw, int imageWidth, int imageHeight)
        {
            int[] imageColor = new int[imageWidth * imageHeight];
            Parallel.For(0, imageHeight, (y) =>
            {
                for (int x = 0; x < imageWidth; x++)
                {
                    int pixelIndex = y * imageWidth + x;
                    byte color = imageRaw[pixelIndex];
                    byte r = 0, g = 0, b = 0;
                    if (((x > 0) && (x < imageWidth - 1)) && ((y > 0) && (y < imageHeight - 1)))
                    {
                        int g1 = 0, g2 = 0, g3 = 0, g4 = 0;
                        int r1 = 0, r2 = 0, r3 = 0, r4 = 0;
                        int b1 = 0, b2 = 0, b3 = 0, b4 = 0;
                        bool rowEven = ((y % 2) == 0);
                        bool colEven = ((x % 2) == 0);
                        if ((rowEven) && (colEven))
                        {
                            g1 = imageRaw[y * imageWidth + (x - 1)];
                            g2 = imageRaw[y * imageWidth + (x + 1)];
                            g3 = imageRaw[(y + 1) * imageWidth + x];
                            g4 = imageRaw[(y - 1) * imageWidth + x];
                            r1 = imageRaw[(y - 1) * imageWidth + (x - 1)];
                            r2 = imageRaw[(y + 1) * imageWidth + (x - 1)];
                            r3 = imageRaw[(y - 1) * imageWidth + (x + 1)];
                            r4 = imageRaw[(y + 1) * imageWidth + (x + 1)];
                            r = (byte)Math.Round((r1 + r2 + r3 + r4) / 4.0);
                            g = (byte)Math.Round((g1 + g2 + g3 + g4) / 4.0);
                            b = color;
                        }
                        if ((!rowEven) && (colEven))
                        {
                            r1 = imageRaw[y * imageWidth + (x - 1)];
                            r2 = imageRaw[y * imageWidth + (x + 1)];
                            b1 = imageRaw[(y - 1) * imageWidth + x];
                            b2 = imageRaw[(y + 1) * imageWidth + x];
                            r = (byte)Math.Round((r1 + r2) / 2.0);
                            g = color;
                            b = (byte)Math.Round((b1 + b2) / 2.0);
                        }
                        if ((rowEven) && (!colEven))
                        {
                            b1 = imageRaw[y * imageWidth + (x - 1)];
                            b2 = imageRaw[y * imageWidth + (x + 1)];
                            r1 = imageRaw[(y - 1) * imageWidth + x];
                            r2 = imageRaw[(y + 1) * imageWidth + x];
                            r = (byte)Math.Round((r1 + r2) / 2.0);
                            g = color;
                            b = (byte)Math.Round((b1 + b2) / 2.0);
                        }
                        if ((!rowEven) && (!colEven))
                        {
                            g1 = imageRaw[y * imageWidth + (x - 1)];
                            g2 = imageRaw[y * imageWidth + (x + 1)];
                            g3 = imageRaw[(y - 1) * imageWidth + x];
                            g4 = imageRaw[(y + 1) * imageWidth + x];
                            b1 = imageRaw[(y - 1) * imageWidth + (x - 1)];
                            b2 = imageRaw[(y + 1) * imageWidth + (x - 1)];
                            b3 = imageRaw[(y - 1) * imageWidth + (x + 1)];
                            b4 = imageRaw[(y + 1) * imageWidth + (x + 1)];
                            r = color;
                            g = (byte)Math.Round((g1 + g2 + g3 + g4) / 4.0);
                            b = (byte)Math.Round((b1 + b2 + b3 + b4) / 4.0);
                        }
                    }
                    imageColor[pixelIndex] = BitConverter.ToInt32(new byte[] { b, g, r, 255 }, 0);
                }
            });
            return imageColor;
        }

        /*
        /// <summary>
        /// Demosaicize a RAW image (10 bit BGGR pattern) into a RGBA64 image.
        /// Low-performance and simple interpolation pattern. Algorithm not recommended for production.
        /// </summary>
        public static UInt64[] Demosaic(UInt16[] imageRaw, int imageWidth, int imageHeight)
        {
            UInt64[] imageColor = new UInt64[imageWidth * imageHeight];
            Parallel.For(0, imageHeight, (y) =>
            {
                for (int x = 0; x < imageWidth; x++)
                {
                    int pixelIndex = y * imageWidth + x;
                    UInt16 color = imageRaw[pixelIndex];
                    UInt16 r = 0, g = 0, b = 0;
                    if (((x > 0) && (x < imageWidth - 1)) && ((y > 0) && (y < imageHeight - 1)))
                    {
                        UInt16 g1 = 0, g2 = 0, g3 = 0, g4 = 0;
                        UInt16 r1 = 0, r2 = 0, r3 = 0, r4 = 0;
                        UInt16 b1 = 0, b2 = 0, b3 = 0, b4 = 0;
                        bool rowEven = ((y % 2) == 0);
                        bool colEven = ((x % 2) == 0);
                        if ((rowEven) && (colEven))
                        {
                            g1 = imageRaw[y * imageWidth + (x - 1)];
                            g2 = imageRaw[y * imageWidth + (x + 1)];
                            g3 = imageRaw[(y + 1) * imageWidth + x];
                            g4 = imageRaw[(y - 1) * imageWidth + x];
                            r1 = imageRaw[(y - 1) * imageWidth + (x - 1)];
                            r2 = imageRaw[(y + 1) * imageWidth + (x - 1)];
                            r3 = imageRaw[(y - 1) * imageWidth + (x + 1)];
                            r4 = imageRaw[(y + 1) * imageWidth + (x + 1)];                            
                            r = (UInt16)((r1 + r2 + r3 + r4) / 4);
                            g = (UInt16)((g1 + g2 + g3 + g4) / 4);
                            b = color;
                        }
                        if ((!rowEven) && (colEven))
                        {
                            r1 = imageRaw[y * imageWidth + (x - 1)];
                            r2 = imageRaw[y * imageWidth + (x + 1)];
                            b1 = imageRaw[(y - 1) * imageWidth + x];
                            b2 = imageRaw[(y + 1) * imageWidth + x];
                            r = (UInt16)((r1 + r2) / 2);
                            g = color;
                            b = (UInt16)((b1 + b2) / 2);
                        }
                        if ((rowEven) && (!colEven))
                        {
                            b1 = imageRaw[y * imageWidth + (x - 1)];
                            b2 = imageRaw[y * imageWidth + (x + 1)];
                            r1 = imageRaw[(y - 1) * imageWidth + x];
                            r2 = imageRaw[(y + 1) * imageWidth + x];
                            r = (UInt16)((r1 + r2) / 2);
                            g = color;
                            b = (UInt16)((b1 + b2) / 2);
                        }
                        if ((!rowEven) && (!colEven))
                        {
                            g1 = imageRaw[y * imageWidth + (x - 1)];
                            g2 = imageRaw[y * imageWidth + (x + 1)];
                            g3 = imageRaw[(y - 1) * imageWidth + x];
                            g4 = imageRaw[(y + 1) * imageWidth + x];
                            b1 = imageRaw[(y - 1) * imageWidth + (x - 1)];
                            b2 = imageRaw[(y + 1) * imageWidth + (x - 1)];
                            b3 = imageRaw[(y - 1) * imageWidth + (x + 1)];
                            b4 = imageRaw[(y + 1) * imageWidth + (x + 1)];
                            r = color;
                            g = (UInt16)((g1 + g2 + g3 + g4) / 4.0);
                            b = (UInt16)((b1 + b2 + b3 + b4) / 4.0);
                        }
                    }
                    //RGBA64
                    //imageColor[pixelIndex] = (UInt64)UInt16.MaxValue | ((UInt64)b << 16) | ((UInt64)g << 32) | ((UInt64)r << 48);
                    imageColor[pixelIndex] = (UInt64)r | ((UInt64)g << 16) | ((UInt64)b << 32) | ((UInt64)UInt16.MaxValue << 48);
                }
            });
            return imageColor;
        }
        */
    }
}
