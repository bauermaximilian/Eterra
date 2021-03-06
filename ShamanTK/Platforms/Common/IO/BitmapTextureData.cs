﻿/* 
 * ShamanTK
 * A toolkit for creating multimedia applications.
 * Copyright (C) 2020, Maximilian Bauer (contact@lengo.cc)
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 * 
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

using ShamanTK.Common;
using ShamanTK.IO;
using System;
using System.Drawing.Imaging;
using System.IO;

namespace ShamanTK.Platforms.Common.IO
{
    public class BitmapTextureData : TextureData
    {
        private System.Drawing.Bitmap bitmap;
        private BitmapData bitmapData;

        private readonly int paddingWidth;

        public override Pointer PixelData { get; }

        public BitmapTextureData(System.Drawing.Bitmap bitmap)
            : base(new Size(bitmap != null ? bitmap.Width : 1,
                  bitmap != null ? bitmap.Height : 1))
        {
            this.bitmap = bitmap ??
                throw new ArgumentNullException(nameof(bitmap));
            bitmapData = bitmap.LockBits(new System.Drawing.Rectangle(0, 0,
                Size.Width, Size.Height), ImageLockMode.ReadWrite,
                bitmap.PixelFormat);

            if (bitmap.PixelFormat == PixelFormat.Format32bppArgb)
            {
                PixelData = new Pointer(bitmapData.Scan0, typeof(Color.BGRA));
                paddingWidth = Math.Max(0, Math.Abs(bitmapData.Stride) / 4
                    - Size.Width);
            }
            else if (bitmap.PixelFormat == PixelFormat.Format24bppRgb)
            {
                PixelData = new Pointer(bitmapData.Scan0, typeof(Color.BGR));
                paddingWidth = Math.Max(0, Math.Abs(bitmapData.Stride) / 3
                    - Size.Width);
            }
            else
            {
                bitmap.UnlockBits(bitmapData);
                bitmapData = null;
            }
        }

        public override int GetPixelIndex(int tx, int ty)
        {
            if (tx < 0 || tx >= Size.Width)
                throw new ArgumentOutOfRangeException(nameof(tx));
            if (ty < 0 || ty >= Size.Height)
                throw new ArgumentOutOfRangeException(nameof(ty));

            return ty * (Size.Width + paddingWidth) + tx;
        }

        public override void GetPixelPosition(int index, out int tx,
            out int ty)
        {
            if (index < 0 || index >= PixelCount)
                throw new ArgumentOutOfRangeException(nameof(index));
            tx = index % (Size.Width + paddingWidth);
            ty = index / (Size.Width + paddingWidth);
        }

        public override Color[] GetRegion(int tx, int ty, int width, 
            int height)
        {
            AssertValidTextureSection(tx, ty, width, height);
            Color[] pixels = new Color[width * height];

            int i = 0;
            for (int y = ty; y < (ty + height); y++)
            {
                for (int x = tx; x < (tx + width); x++)
                {
                    var pixel = bitmap.GetPixel(x, y);
                    pixels[i++] = 
                        new Color(pixel.R, pixel.G, pixel.B, pixel.A);
                }
            }

            return pixels;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && bitmap != null)
            {
                if (bitmapData != null)
                {
                    bitmap.UnlockBits(bitmapData);
                    bitmapData = null;
                }

                bitmap.Dispose();
                bitmap = null;
            }
        }

        /// <summary>
        /// Imports a <see cref="BitmapTextureData"/> instance from a 
        /// <see cref="Stream"/>. Supports all file formats supported by
        /// <see cref="System.Drawing.Image.FromStream(Stream)"/>.
        /// </summary>
        /// <param name="stream">
        /// The source image file stream.
        /// </param>
        /// <returns>
        /// A new <see cref="TextureData"/> instance.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Is thrown when <paramref name="stream"/> is null.
        /// </exception>
        /// <exception cref="FormatException">
        /// Is thrown when <paramref name="stream"/> doesn't provide a valid
        /// or supported image file.
        /// </exception>
        public static TextureData FromStream(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            try
            {
                return new BitmapTextureData((System.Drawing.Bitmap)
                    System.Drawing.Image.FromStream(stream));
            }
            catch (Exception exc)
            {
                throw new FormatException("The specified stream couldn't be " +
                    "imported as image.", exc);
            }            
        }
    }
}
