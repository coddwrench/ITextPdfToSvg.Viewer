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
using System.Globalization;
using IText.IO.Source;
using IText.IO.Util;

namespace IText.Kernel.Pdf {
    public class PdfNumber : PdfPrimitiveObject {
        private double value;

        private bool isDouble;

        public PdfNumber(double value)
        {
            SetValue(value);
        }

        public PdfNumber(int value)
        {
            SetValue(value);
        }

        public PdfNumber(byte[] content)
            : base(content) {
            isDouble = true;
            value = double.NaN;
        }

        private PdfNumber()
        {
        }

        public override byte GetObjectType() {
            return NUMBER;
        }

        public virtual double GetValue() {
            if (double.IsNaN(value)) {
                GenerateValue();
            }
            return value;
        }

        public virtual double DoubleValue() {
            return GetValue();
        }

        public virtual float FloatValue() {
            return (float)GetValue();
        }

        public virtual long LongValue() {
            return (long)GetValue();
        }

        public virtual int IntValue() {
            return (int)GetValue();
        }

        public virtual void SetValue(int value) {
            this.value = value;
            isDouble = false;
            content = null;
        }

        public virtual void SetValue(double value) {
            this.value = value;
            isDouble = true;
            content = null;
        }

        public virtual void Increment() {
            SetValue(++value);
        }

        public virtual void Decrement() {
            SetValue(--value);
        }

        public override string ToString()
        {
	        if (content != null) {
                return JavaUtil.GetStringForBytes(content, EncodingUtil.ISO_8859_1);
            }

	        if (isDouble) {
		        return JavaUtil.GetStringForBytes(ByteUtils.GetIsoBytes(GetValue()), EncodingUtil.ISO_8859_1
		        );
	        }

	        return JavaUtil.GetStringForBytes(ByteUtils.GetIsoBytes(IntValue()), EncodingUtil.ISO_8859_1
	        );
        }

        public override bool Equals(object o) {
            if (this == o) {
                return true;
            }
            if (o == null || GetType() != o.GetType()) {
                return false;
            }
            return JavaUtil.DoubleCompare(((PdfNumber)o).GetValue(), GetValue()) == 0;
        }

        /// <summary>Checks if string representation of the value contains decimal point.</summary>
        /// <returns>true if contains so the number must be real not integer</returns>
        public virtual bool HasDecimalPoint() {
            return ToString().Contains(".");
        }

        public override int GetHashCode() {
            var hash = JavaUtil.DoubleToLongBits(GetValue());
            return (int)(hash ^ ((long)(((ulong)hash) >> 32)));
        }

        protected internal override PdfObject NewInstance() {
            return new PdfNumber();
        }

        protected internal virtual bool IsDoubleNumber() {
            return isDouble;
        }

        protected internal override void GenerateContent() {
            if (isDouble) {
                content = ByteUtils.GetIsoBytes(value);
            }
            else {
                content = ByteUtils.GetIsoBytes((int)value);
            }
        }

        protected internal virtual void GenerateValue() {
            try {
                value = double.Parse(JavaUtil.GetStringForBytes(content, EncodingUtil.ISO_8859_1
                    ), CultureInfo.InvariantCulture);
            }
            catch (FormatException) {
                value = double.NaN;
            }
            isDouble = true;
        }

        protected internal override void CopyContent(PdfObject from, PdfDocument document) {
            base.CopyContent(from, document);
            var number = (PdfNumber)from;
            value = number.value;
            isDouble = number.isDouble;
        }
    }
}
