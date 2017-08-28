// Author: Leonardo Tazzini (http://electro-logic.blogspot.it)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CameraVision
{
    /// <summary>
    /// Functions to work with HSB color space
    /// </summary>
    public static class ColorExtension
    {
        public static float GetHue(this System.Windows.Media.Color c)
        {
            var color = System.Drawing.Color.FromArgb(c.A, c.R, c.G, c.B);
            return color.GetHue();
        }

        public static float GetBrightness(this System.Windows.Media.Color c)
        {
            var color = System.Drawing.Color.FromArgb(c.A, c.R, c.G, c.B);
            return color.GetBrightness();
        }

        public static float GetSaturation(this System.Windows.Media.Color c)
        {
            var color = System.Drawing.Color.FromArgb(c.A, c.R, c.G, c.B);
            return color.GetSaturation();
        }
    }
}
