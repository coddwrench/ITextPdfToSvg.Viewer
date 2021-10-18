/*

This file is part of the iText (R) project.
Copyright (c) 1998-2021 iText Group NV
Authors: Bruno Lowagie, Paulo Soares, et al.

This program is free software; you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License version 3
as published by the Free Software Foundation with the addition of the
following permission added to Section 15 as permitted in Section 7(a):
FOR ANY PART OF THE COVERED WORK IN WHICH THE COPYRIGHT IS OWNED BY
ITEXT GROUP. ITEXT GROUP DISCLAIMS THE WARRANTY OF NON INFRINGEMENT
OF THIRD PARTY RIGHTS

This program is distributed in the hope that it will be useful, but
WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
or FITNESS FOR A PARTICULAR PURPOSE.
See the GNU Affero General Public License for more details.
You should have received a copy of the GNU Affero General Public License
along with this program; if not, see http://www.gnu.org/licenses or write to
the Free Software Foundation, Inc., 51 Franklin Street, Fifth Floor,
Boston, MA, 02110-1301 USA, or download the license from the following URL:
http://itextpdf.com/terms-of-use/

The interactive user interfaces in modified source and object code versions
of this program must display Appropriate Legal Notices, as required under
Section 5 of the GNU Affero General Public License.

In accordance with Section 7(b) of the GNU Affero General Public License,
a covered work must retain the producer line in every PDF that is created
or manipulated using iText.

You can be released from the requirements of the license by purchasing
a commercial license. Buying such a license is mandatory as soon as you
develop commercial activities involving the iText software without
disclosing the source code of your own applications.
These activities include: offering paid services to customers as an ASP,
serving PDFs on the fly in a web application, shipping iText with a closed
source product.

For more information, please contact iText Software Corp. at this
address: sales@itextpdf.com
*/

using System;
using IText.IO;
using IText.Kernel.Pdf.Colorspace;
using IText.Logger;

namespace IText.Kernel.Colors {
    /// <summary>Color space to specify shades of gray color.</summary>
    public class DeviceGray : Color {
        /// <summary>Predefined white DeviceGray color.</summary>
        public static readonly DeviceGray WHITE = new DeviceGray(1f);

        /// <summary>Predefined gray DeviceGray color.</summary>
        public static readonly DeviceGray GRAY = new DeviceGray(.5f);

        /// <summary>Predefined black DeviceGray color.</summary>
        public static readonly DeviceGray BLACK = new DeviceGray();

        /// <summary>Creates DeviceGray color by given grayscale.</summary>
        /// <remarks>
        /// Creates DeviceGray color by given grayscale.
        /// The grayscale is considered to be in [0, 1] interval, if not,
        /// the grayscale will be considered as 1 (when grayscale's value is bigger than 1)
        /// or 0 (when grayscale's value is less than 0).
        /// </remarks>
        /// <param name="value">the grayscale value</param>
        public DeviceGray(float value)
            : base(new PdfDeviceCs.Gray(), new[] { value > 1 ? 1 : (value > 0 ? value : 0) }) {
            if (value > 1 || value < 0) {
                var LOGGER = LogManager.GetLogger(typeof(DeviceGray));
                LOGGER.Warn(LogMessageConstant.COLORANT_INTENSITIES_INVALID);
            }
        }

        /// <summary>Creates DeviceGray color with grayscale value initialised as zero.</summary>
        public DeviceGray()
            : this(0f) {
        }

        /// <summary>
        /// Returns
        /// <see cref="DeviceGray">DeviceGray</see>
        /// color which is lighter than given one
        /// </summary>
        /// <param name="grayColor">the DeviceGray color to be made lighter</param>
        /// <returns>lighter color</returns>
        public static DeviceGray MakeLighter(DeviceGray grayColor) {
            var v = grayColor.GetColorValue()[0];
            if (v == 0f) {
                return new DeviceGray(0.3f);
            }
            var multiplier = Math.Min(1f, v + 0.33f) / v;
            return new DeviceGray(v * multiplier);
        }

        /// <summary>
        /// Returns
        /// <see cref="DeviceGray">DeviceGray</see>
        /// color which is darker than given one
        /// </summary>
        /// <param name="grayColor">the DeviceGray color to be made darker</param>
        /// <returns>darker color</returns>
        public static DeviceGray MakeDarker(DeviceGray grayColor) {
            var v = grayColor.GetColorValue()[0];
            var multiplier = Math.Max(0f, (v - 0.33f) / v);
            return new DeviceGray(v * multiplier);
        }
    }
}
