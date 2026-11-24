#region License

// 
// Copyright (c) 2011-2012, João Matos Silva <kappy@acydburne.com.pt>
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//   http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// 

#endregion

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using DateTimeExtensions.Common;

namespace DateTimeExtensions.WorkingDays.CultureStrategies
{
    [Locale("ko-KR")]
    public class KO_KRHolidayStrategy : HolidayStrategyBase, IObservancesStrategy
    {
        private static readonly Calendar KoreanLunisolarCalendar = new KoreanLunisolarCalendar();

        public KO_KRHolidayStrategy()
        {
            this.InnerObservances.AddHoliday(GlobalHolidays.NewYear);
            this.InnerObservances.AddHoliday(Samiljeol);
            this.InnerObservances.AddHoliday(GlobalHolidays.InternationalWorkersDay);
            this.InnerObservances.AddHoliday(SeokgaTansinil);
            this.InnerObservances.AddHoliday(Hyeonchungil);
            this.InnerObservances.AddHoliday(Jeheonjeol);
            this.InnerObservances.AddHoliday(Gwangbokjeol);
            this.InnerObservances.AddHoliday(Gaecheonjeol);
            this.InnerObservances.AddHoliday(Hangulnal);
            this.InnerObservances.AddHoliday(ChristianHolidays.Christmas);

            // Add these later for substitute holiday rule application when overlapped with other holidays.
            this.InnerObservances.AddHoliday(Seolnal);
            this.InnerObservances.AddHoliday(Chuseok);
            this.InnerObservances.AddHoliday(Eorininal);
        }

        protected override IDictionary<DateTime, Observance> BuildObservancesMap(int year)
        {
            IDictionary<DateTime, Observance> holidayMap = new Dictionary<DateTime, Observance>();
            foreach (var innerHoliday in InnerObservances)
            {
                var date = innerHoliday.CalendarDay.GetInstance(year);
                if (date.HasValue)
                {
                    if (innerHoliday.CalendarDay == Seolnal || innerHoliday.CalendarDay == Chuseok) {
                        DateTime[] holidays = new DateTime[] { 
                            date.Value.AddDays(-1),
                            date.Value, 
                            date.Value.AddDays(1)
                        };
                        AddSubstituteHoliday(holidayMap, holidays, innerHoliday.CalendarDay);
                        foreach (DateTime holiday in holidays)
                        {
                            // A day that is already another holiday (e.g. Gaecheonjeol during Chuseok 2028) keeps it;
                            // the overlap was compensated with a substitute holiday above.
                            if (!holidayMap.ContainsKey(holiday))
                            {
                                holidayMap.Add(holiday, new Observance(new NamedDay(innerHoliday.CalendarDay.Name, new FixedDayResolver(holiday.Month, holiday.Day)), true));
                            }
                        }
                    }
                    else if (innerHoliday.CalendarDay == Eorininal)
                    {
                        // Special substitute holiday rule for Eorininal(May 5, children's day)
                        // effective since Oct. 29, 2013.
                        DateTime childrensDay = date.Value;
                        Observance overlappedHoliday = null;
                        while (childrensDay.DayOfWeek == DayOfWeek.Saturday ||
                                childrensDay.DayOfWeek == DayOfWeek.Sunday ||
                                holidayMap.TryGetValue(childrensDay, out overlappedHoliday))
                        {
                            childrensDay = childrensDay.AddDays(1);
                        }
                        holidayMap.Add(childrensDay, new Observance(new NamedDay(innerHoliday.CalendarDay.Name, new FixedDayResolver(childrensDay.Month, childrensDay.Day)), true));
                    }
                    else if (!holidayMap.ContainsKey(date.Value))
                    {
                        holidayMap.Add(date.Value, innerHoliday);
                    }
                }
            }
            AddWeekendSubstituteHolidays(holidayMap, year);
            return holidayMap;
        }

        // Holidays that get a substitute holiday when they fall on a Saturday or Sunday or on another holiday,
        // and the first year the rule applies to them:
        // Samiljeol, Gwangbokjeol, Gaecheonjeol and Hangulnal since Aug. 2021,
        // SeokgaTansinil and Christmas since May 2023, Labour Day and Jeheonjeol since 2026.
        private static readonly (NamedDay Holiday, int SinceYear)[] WeekendSubstituteHolidays =
        {
            (Samiljeol, 2021),
            (GlobalHolidays.InternationalWorkersDay, 2026),
            (SeokgaTansinil, 2023),
            (Jeheonjeol, 2026),
            (Gwangbokjeol, 2021),
            (Gaecheonjeol, 2021),
            (Hangulnal, 2021),
            (ChristianHolidays.Christmas, 2023),
        };

        // The substitute holiday is the first day after the holiday that is neither a weekend day nor a holiday.
        private static void AddWeekendSubstituteHolidays(IDictionary<DateTime, Observance> holidayMap, int year)
        {
            foreach (var (holiday, sinceYear) in WeekendSubstituteHolidays)
            {
                if (year < sinceYear)
                {
                    continue;
                }
                var date = holiday.GetInstance(year);
                if (!date.HasValue)
                {
                    continue;
                }
                Observance overlappedHoliday;
                if (IsWeekend(date.Value) ||
                    holidayMap.TryGetValue(date.Value, out overlappedHoliday) && overlappedHoliday.CalendarDay != holiday)
                {
                    DateTime substituteDay = date.Value.AddDays(1);
                    while (IsWeekend(substituteDay) || holidayMap.ContainsKey(substituteDay))
                    {
                        substituteDay = substituteDay.AddDays(1);
                    }
                    holidayMap.Add(substituteDay, new Observance(new NamedDay(holiday.Name, new FixedDayResolver(substituteDay.Month, substituteDay.Day)), true));
                }
            }
        }

        private static bool IsWeekend(DateTime date)
        {
            return date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday;
        }

        // Special substitute holiday rule for Seol(lunisolar new year) and Chuseok(15th of 8th lunisolar month)
        // effective since Oct. 29, 2013.
        private void AddSubstituteHoliday(IDictionary<DateTime, Observance> holidayMap, DateTime[] dates, NamedDay holiday)
        {
            foreach (DateTime date in dates)
            {
                Observance overlappedHoliday = null;
                if (date.DayOfWeek == DayOfWeek.Sunday || 
                    holidayMap.TryGetValue(date, out overlappedHoliday) && overlappedHoliday.CalendarDay != holiday)
                {
                    DateTime substituteDay = dates.Last().AddDays(1);
                    while (substituteDay.DayOfWeek == DayOfWeek.Sunday || holidayMap.ContainsKey(substituteDay))
                    {
                        substituteDay = substituteDay.AddDays(1);
                    }
                    holidayMap.Add(substituteDay, new Observance(new NamedDay(holiday.Name, new FixedDayResolver(substituteDay.Month, substituteDay.Day)), true));
                }
            }
        }

        private static NamedDay seolnal;

        public static NamedDay Seolnal
        {
            get
            {
                if (seolnal == null)
                {
                    seolnal = new NamedDay("Seolnal", new FixedDayResolver(1, 1, KoreanLunisolarCalendar));
                }
                return seolnal;
            }
        }

        private static NamedDay samiljeol;

        public static NamedDay Samiljeol
        {
            get
            {
                if (samiljeol == null)
                {
                    samiljeol = new NamedDay("Samiljeol", new FixedDayResolver(3, 1));
                }
                return samiljeol;
            }
        }

        private static NamedDay eorininal;

        public static NamedDay Eorininal
        {
            get
            {
                if (eorininal == null)
                {
                    eorininal = new NamedDay("Eorininal", new FixedDayResolver(5, 5));
                }
                return eorininal;
            }
        }

        private static NamedDay seokgatansinil;

        public static NamedDay SeokgaTansinil
        {
            get
            {
                if (seokgatansinil == null)
                {
                    seokgatansinil = new NamedDay("SeokgaTansinil", new FixedDayResolver(4, 8, KoreanLunisolarCalendar));
                }
                return seokgatansinil;
            }
        }

        private static NamedDay hyeonchungil;

        public static NamedDay Hyeonchungil
        {
            get
            {
                if (hyeonchungil == null)
                {
                    hyeonchungil = new NamedDay("Hyeonchungil", new FixedDayResolver(6, 6));
                }
                return hyeonchungil;
            }
        }

        private static NamedDay jeheonjeol;

        // Constitution Day (July 17): a public holiday until 2007, and again since 2026.
        public static NamedDay Jeheonjeol
        {
            get
            {
                if (jeheonjeol == null)
                {
                    jeheonjeol = new NamedDay("Jeheonjeol", new YearDependantDayResolver(year => year <= 2007 || year >= 2026, new FixedDayResolver(7, 17)));
                }
                return jeheonjeol;
            }
        }

        private static NamedDay gwangbokjeol;

        public static NamedDay Gwangbokjeol
        {
            get
            {
                if (gwangbokjeol == null)
                {
                    gwangbokjeol = new NamedDay("Gwangbokjeol", new FixedDayResolver(8, 15));
                }
                return gwangbokjeol;
            }
        }

        private static NamedDay chuseok;

        public static NamedDay Chuseok
        {
            get
            {
                if (chuseok == null)
                {
                    chuseok = new NamedDay("Chuseok", new FixedDayResolver(8, 15, KoreanLunisolarCalendar));
                }
                return chuseok;
            }
        }

        private static NamedDay gaecheonjeol;

        public static NamedDay Gaecheonjeol
        {
            get
            {
                if (gaecheonjeol == null)
                {
                    gaecheonjeol = new NamedDay("Gaecheonjeol", new FixedDayResolver(10, 3));
                }
                return gaecheonjeol;
            }
        }

        private static NamedDay hangulnal;

        public static NamedDay Hangulnal
        {
            get
            {
                if (hangulnal == null)
                {
                    hangulnal = new NamedDay("Hangulnal", new FixedDayResolver(10, 9));
                }
                return hangulnal;
            }
        }
    }
}
