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
using System.Collections.Generic;
using IText.IO.Source;
using IText.IO.Util;

namespace  IText.IO.Font.Otf {
    public class OtfReadCommon {
        public static int[] ReadUShortArray(RandomAccessFileOrArray rf, int size, int location) {
            var ret = new int[size];
            for (var k = 0; k < size; ++k) {
                var offset = rf.ReadUnsignedShort();
                ret[k] = offset == 0 ? offset : offset + location;
            }
            return ret;
        }

        public static int[] ReadUShortArray(RandomAccessFileOrArray rf, int size) {
            return ReadUShortArray(rf, size, 0);
        }

        public static void ReadCoverages(RandomAccessFileOrArray rf, int[] locations, IList<ICollection<int>> coverage
            ) {
            foreach (var location in locations) {
                coverage.Add(new HashSet<int>(ReadCoverageFormat(rf, location)));
            }
        }

        public static IList<int> ReadCoverageFormat(RandomAccessFileOrArray rf, int coverageLocation) {
            rf.Seek(coverageLocation);
            int coverageFormat = rf.ReadShort();
            IList<int> glyphIds;
            if (coverageFormat == 1) {
                var glyphCount = rf.ReadUnsignedShort();
                glyphIds = new List<int>(glyphCount);
                for (var i = 0; i < glyphCount; i++) {
                    var coverageGlyphId = rf.ReadUnsignedShort();
                    glyphIds.Add(coverageGlyphId);
                }
            }
            else {
                if (coverageFormat == 2) {
                    var rangeCount = rf.ReadUnsignedShort();
                    glyphIds = new List<int>();
                    for (var i = 0; i < rangeCount; i++) {
                        ReadRangeRecord(rf, glyphIds);
                    }
                }
                else {
                    throw new NotSupportedException(MessageFormatUtil.Format("Invalid coverage format: {0}", coverageFormat));
                }
            }
            return JavaCollectionsUtil.UnmodifiableList(glyphIds);
        }

        private static void ReadRangeRecord(RandomAccessFileOrArray rf, IList<int> glyphIds) {
            var startGlyphId = rf.ReadUnsignedShort();
            var endGlyphId = rf.ReadUnsignedShort();
            int startCoverageIndex = rf.ReadShort();
            for (var glyphId = startGlyphId; glyphId <= endGlyphId; glyphId++) {
                glyphIds.Add(glyphId);
            }
        }

        public static GposValueRecord ReadGposValueRecord(OpenTypeFontTableReader tableReader, int mask) {
            var vr = new GposValueRecord();
            if ((mask & 0x0001) != 0) {
                vr.XPlacement = tableReader.rf.ReadShort() * 1000 / tableReader.GetUnitsPerEm();
            }
            if ((mask & 0x0002) != 0) {
                vr.YPlacement = tableReader.rf.ReadShort() * 1000 / tableReader.GetUnitsPerEm();
            }
            if ((mask & 0x0004) != 0) {
                vr.XAdvance = tableReader.rf.ReadShort() * 1000 / tableReader.GetUnitsPerEm();
            }
            if ((mask & 0x0008) != 0) {
                vr.YAdvance = tableReader.rf.ReadShort() * 1000 / tableReader.GetUnitsPerEm();
            }
            if ((mask & 0x0010) != 0) {
                tableReader.rf.Skip(2);
            }
            if ((mask & 0x0020) != 0) {
                tableReader.rf.Skip(2);
            }
            if ((mask & 0x0040) != 0) {
                tableReader.rf.Skip(2);
            }
            if ((mask & 0x0080) != 0) {
                tableReader.rf.Skip(2);
            }
            return vr;
        }

        public static GposAnchor ReadGposAnchor(OpenTypeFontTableReader tableReader, int location) {
            if (location == 0) {
                return null;
            }
            tableReader.rf.Seek(location);
            var format = tableReader.rf.ReadUnsignedShort();
            GposAnchor t = null;
            switch (format) {
                default: {
                    t = new GposAnchor();
                    t.XCoordinate = tableReader.rf.ReadShort() * 1000 / tableReader.GetUnitsPerEm();
                    t.YCoordinate = tableReader.rf.ReadShort() * 1000 / tableReader.GetUnitsPerEm();
                    break;
                }
            }
            return t;
        }

        public static IList<OtfMarkRecord> ReadMarkArray(OpenTypeFontTableReader tableReader, int location) {
            tableReader.rf.Seek(location);
            var markCount = tableReader.rf.ReadUnsignedShort();
            var classes = new int[markCount];
            var locations = new int[markCount];
            for (var k = 0; k < markCount; ++k) {
                classes[k] = tableReader.rf.ReadUnsignedShort();
                var offset = tableReader.rf.ReadUnsignedShort();
                locations[k] = location + offset;
            }
            IList<OtfMarkRecord> marks = new List<OtfMarkRecord>();
            for (var k = 0; k < markCount; ++k) {
                var rec = new OtfMarkRecord();
                rec.markClass = classes[k];
                rec.anchor = ReadGposAnchor(tableReader, locations[k]);
                marks.Add(rec);
            }
            return marks;
        }

        public static SubstLookupRecord[] ReadSubstLookupRecords(RandomAccessFileOrArray rf, int substCount) {
            var substLookUpRecords = new SubstLookupRecord[substCount];
            for (var i = 0; i < substCount; ++i) {
                var slr = new SubstLookupRecord();
                slr.sequenceIndex = rf.ReadUnsignedShort();
                slr.lookupListIndex = rf.ReadUnsignedShort();
                substLookUpRecords[i] = slr;
            }
            return substLookUpRecords;
        }

        public static PosLookupRecord[] ReadPosLookupRecords(RandomAccessFileOrArray rf, int recordCount) {
            var posLookUpRecords = new PosLookupRecord[recordCount];
            for (var i = 0; i < recordCount; ++i) {
                var lookupRecord = new PosLookupRecord();
                lookupRecord.sequenceIndex = rf.ReadUnsignedShort();
                lookupRecord.lookupListIndex = rf.ReadUnsignedShort();
                posLookUpRecords[i] = lookupRecord;
            }
            return posLookUpRecords;
        }

        public static GposAnchor[] ReadAnchorArray(OpenTypeFontTableReader tableReader, int[] locations, int left, 
            int right) {
            var anchors = new GposAnchor[right - left];
            for (var i = left; i < right; i++) {
                anchors[i - left] = ReadGposAnchor(tableReader, locations[i]);
            }
            return anchors;
        }

        public static IList<GposAnchor[]> ReadBaseArray(OpenTypeFontTableReader tableReader, int classCount, int location
            ) {
            IList<GposAnchor[]> baseArray = new List<GposAnchor[]>();
            tableReader.rf.Seek(location);
            var baseCount = tableReader.rf.ReadUnsignedShort();
            var anchorLocations = ReadUShortArray(tableReader.rf, baseCount * classCount, location);
            var idx = 0;
            for (var k = 0; k < baseCount; ++k) {
                baseArray.Add(ReadAnchorArray(tableReader, anchorLocations, idx, idx + classCount));
                idx += classCount;
            }
            return baseArray;
        }

        public static IList<IList<GposAnchor[]>> ReadLigatureArray(OpenTypeFontTableReader tableReader, int classCount
            , int location) {
            IList<IList<GposAnchor[]>> ligatureArray = new List<IList<GposAnchor[]>>();
            tableReader.rf.Seek(location);
            var ligatureCount = tableReader.rf.ReadUnsignedShort();
            var ligatureAttachLocations = ReadUShortArray(tableReader.rf, ligatureCount, location);
            for (var liga = 0; liga < ligatureCount; ++liga) {
                var ligatureAttachLocation = ligatureAttachLocations[liga];
                IList<GposAnchor[]> ligatureAttach = new List<GposAnchor[]>();
                tableReader.rf.Seek(ligatureAttachLocation);
                var componentCount = tableReader.rf.ReadUnsignedShort();
                var componentRecordsLocation = ReadUShortArray(tableReader.rf, classCount * componentCount, ligatureAttachLocation
                    );
                var idx = 0;
                for (var k = 0; k < componentCount; ++k) {
                    ligatureAttach.Add(ReadAnchorArray(tableReader, componentRecordsLocation, idx, idx + classCount));
                    idx += classCount;
                }
                ligatureArray.Add(ligatureAttach);
            }
            return ligatureArray;
        }
    }
}
