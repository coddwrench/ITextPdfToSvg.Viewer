//Copyright (c) 2006, Adobe Systems Incorporated
//All rights reserved.
//
//        Redistribution and use in source and binary forms, with or without
//        modification, are permitted provided that the following conditions are met:
//        1. Redistributions of source code must retain the above copyright
//        notice, this list of conditions and the following disclaimer.
//        2. Redistributions in binary form must reproduce the above copyright
//        notice, this list of conditions and the following disclaimer in the
//        documentation and/or other materials provided with the distribution.
//        3. All advertising materials mentioning features or use of this software
//        must display the following acknowledgement:
//        This product includes software developed by the Adobe Systems Incorporated.
//        4. Neither the name of the Adobe Systems Incorporated nor the
//        names of its contributors may be used to endorse or promote products
//        derived from this software without specific prior written permission.
//
//        THIS SOFTWARE IS PROVIDED BY ADOBE SYSTEMS INCORPORATED ''AS IS'' AND ANY
//        EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
//        WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
//        DISCLAIMED. IN NO EVENT SHALL ADOBE SYSTEMS INCORPORATED BE LIABLE FOR ANY
//        DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
//        (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
//        LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
//        ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
//        (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
//        SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
//
//        http://www.adobe.com/devnet/xmp/library/eula-xmp-library-java.html

using System;

namespace IText.Kernel.XMP.Impl
{
	/// <summary>The implementation of <code>XMPDateTime</code>.</summary>
	/// <remarks>
	/// The implementation of <code>XMPDateTime</code>. Internally a <code>calendar</code> is used
	/// plus an additional nano seconds field, because <code>Calendar</code> supports only milli
	/// seconds. The <code>nanoSeconds</code> convers only the resolution beyond a milli second.
	/// </remarks>
	/// <since>16.02.2006</since>
	public class XMPDateTimeImpl : XMPDateTime
	{
		private int year;

		private int month;

		private int day;

		private int hour;

		private int minute;

		private int second;

        /// <summary>Use NO time zone as default</summary>

        private TimeZoneInfo timeZone;

        /// <summary>The nano seconds take micro and nano seconds, while the milli seconds are in the calendar.
        /// 	</summary>
        private int nanoSeconds;

		private bool hasDate;

		private bool hasTime;

		private bool hasTimeZone;

		/// <summary>
		/// Creates an <code>XMPDateTime</code>-instance with the current time in the default time
		/// zone.
		/// </summary>
		public XMPDateTimeImpl() : this(new XMPCalendar())
		{
		}

		/// <summary>
		/// Creates an <code>XMPDateTime</code>-instance from a calendar.
		/// </summary>
		/// <param name="calendar"> a <code>Calendar</code> </param>
		public XMPDateTimeImpl(XMPCalendar calendar) {
			// extract the date and timezone from the calendar provided
			var date = calendar.GetDateTime();
            var zone = calendar.GetTimeZone();

            year = date.Year;
			month = date.Month + 1; // cal is from 0..12
			day = date.Day;
			hour = date.Hour;
			minute = date.Minute;
			second = date.Second;
			nanoSeconds = date.Millisecond * 1000000;
			timeZone = zone;

			// object contains all date components
			hasDate = hasTime = hasTimeZone = true;
		}

        /// <summary>
        /// Creates an <code>XMPDateTime</code>-instance from 
        /// a <code>Date</code> and a <code>TimeZoneInfo</code>.
        /// </summary>
        /// <param name="date"> a date describing an absolute point in time </param>
        /// <param name="timeZone"> a TimeZoneInfo how to interpret the date </param>
        public XMPDateTimeImpl(DateTime date, TimeZoneInfo timeZone) {
            year = date.Year;
			month = date.Month + 1; // cal is from 0..12
			day = date.Day;
			hour = date.Hour;
			minute = date.Minute;
			second = date.Second;
			nanoSeconds = date.Millisecond * 1000000;
			this.timeZone = timeZone;

			// object contains all date components
			hasDate = hasTime = hasTimeZone = true;
		}

		/// <summary>Creates an <code>XMPDateTime</code>-instance from an ISO 8601 string.</summary>
		/// <param name="strValue">an ISO 8601 string</param>
		public XMPDateTimeImpl(string strValue)
		{
			ISO8601Converter.Parse(strValue, this);
		}

		/// <seealso cref="XMPDateTime.GetYear()"/>
		public virtual int GetYear()
		{
			return year;
		}

		/// <seealso cref="XMPDateTime.SetYear(int)"/>
		public virtual void SetYear(int year)
		{
			this.year = Math.Min(Math.Abs(year), 9999);
			hasDate = true;
		}

		/// <seealso cref="XMPDateTime.GetMonth()"/>
		public virtual int GetMonth()
		{
			return month;
		}

		/// <seealso cref="XMPDateTime.SetMonth(int)"/>
		public virtual void SetMonth(int month)
		{
			if (month < 1)
			{
				this.month = 1;
			}
			else
			{
				if (month > 12)
				{
					this.month = 12;
				}
				else
				{
					this.month = month;
				}
			}
			hasDate = true;
		}

		/// <seealso cref="XMPDateTime.GetDay()"/>
		public virtual int GetDay()
		{
			return day;
		}

		/// <seealso cref="XMPDateTime.SetDay(int)"/>
		public virtual void SetDay(int day)
		{
			if (day < 1)
			{
				this.day = 1;
			}
			else
			{
				if (day > 31)
				{
					this.day = 31;
				}
				else
				{
					this.day = day;
				}
			}
			hasDate = true;
		}

		/// <seealso cref="XMPDateTime.GetHour()"/>
		public virtual int GetHour()
		{
			return hour;
		}

		/// <seealso cref="XMPDateTime.SetHour(int)"/>
		public virtual void SetHour(int hour)
		{
			this.hour = Math.Min(Math.Abs(hour), 23);
			hasTime = true;
		}

		/// <seealso cref="XMPDateTime.GetMinute()"/>
		public virtual int GetMinute()
		{
			return minute;
		}

		/// <seealso cref="XMPDateTime.SetMinute(int)"/>
		public virtual void SetMinute(int minute)
		{
			this.minute = Math.Min(Math.Abs(minute), 59);
			hasTime = true;
		}

		/// <seealso cref="XMPDateTime.GetSecond()"/>
		public virtual int GetSecond()
		{
			return second;
		}

		/// <seealso cref="XMPDateTime.SetSecond(int)"/>
		public virtual void SetSecond(int second)
		{
			this.second = Math.Min(Math.Abs(second), 59);
			hasTime = true;
		}

		/// <seealso cref="XMPDateTime.GetNanoSecond()"/>
		public virtual int GetNanoSecond()
		{
			return nanoSeconds;
		}

		/// <seealso cref="XMPDateTime.SetNanoSecond(int)"/>
		public virtual void SetNanoSecond(int nanoSecond)
		{
			nanoSeconds = nanoSecond;
			hasTime = true;
		}

		/// <seealso cref="System.IComparable{T}.CompareTo(System.Object)"/>
		public virtual int CompareTo(object dt)
		{
			var d =  (long) (DateTime.Now - DateTime.MinValue).TotalMilliseconds  - ((XMPDateTime) dt).GetCalendar().GetTimeInMillis();
			if (d != 0) {
				return Math.Sign(d);
			}
			// if millis are equal, compare nanoseconds
			d = nanoSeconds - ((XMPDateTime) dt).GetNanoSecond();
			return Math.Sign(d);
		}

        /// <seealso cref="XMPDateTime.GetTimeZone()"/>
        public virtual TimeZoneInfo GetTimeZone()
		{
			return timeZone;
		}

        /// <seealso cref="XMPDateTime.SetTimeZone(TimeZoneInfo)"
        /// 	/>
        public virtual void SetTimeZone(TimeZoneInfo timeZone)
		{
			this.timeZone = timeZone;
			hasTime = true;
			hasTimeZone = true;
		}

        /// <seealso cref="XMPDateTime.HasDate()"/>
        public virtual bool HasDate()
		{
			return hasDate;
		}

		/// <seealso cref="XMPDateTime.HasTime()"/>
		public virtual bool HasTime()
		{
			return hasTime;
		}

		/// <seealso cref="XMPDateTime.HasTimeZone()"/>
		public virtual bool HasTimeZone()
		{
			return hasTimeZone;
		}

		/// <seealso cref="XMPDateTime.GetCalendar()"/>
		public virtual XMPCalendar GetCalendar()
		{
            TimeZoneInfo tz;
			if (hasTimeZone) {
				tz = timeZone;
			} else {
				tz = TimeZoneInfo.Local;
			}
            return new XMPCalendar(new DateTime(year, month - 1, day, hour, minute, second, nanoSeconds / 1000000), tz);
		}

	    /// <seealso cref="XMPDateTime.GetISO8601String()"/>
		public virtual string GetIso8601String()
		{
			return ISO8601Converter.Render(this);
		}

		/// <returns>Returns the ISO string representation.</returns>
		public override string ToString()
		{
			return GetIso8601String();
		}
	}
}
