using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using DateTimeExtensions.WorkingDays;
using DateTimeExtensions.WorkingDays.CultureStrategies;

namespace DateTimeExtensions.Tests
{
    [TestFixture]
    public class KRHolidaysTests
    {
        [Test]
        public void can_get_strateryies()
        {
            var dateTimeCulture = new WorkingDayCultureInfo("ko-KR");
            var holidaysStrategy = dateTimeCulture.LocateHolidayStrategy(dateTimeCulture.Name, null);
            Assert.AreEqual(typeof(KO_KRHolidayStrategy), holidaysStrategy.GetType());
            var workingDaysStrategy = dateTimeCulture.LocateWorkingDayOfWeekStrategy(dateTimeCulture.Name, null);
            Assert.AreEqual(typeof(DefaultWorkingDayOfWeekStrategy), workingDaysStrategy.GetType());
        }

        [Test]
        public void holiday_days_span()
        {
            var dateTimeCulture = new WorkingDayCultureInfo("ko-KR");
            var holiday = KO_KRHolidayStrategy.Seolnal;
            var startDay = holiday.GetInstance(2015);

            //Seol 2015
            DateTime day = new DateTime(2015, 2, 18);
            do
            {
                Assert.IsFalse(dateTimeCulture.IsWorkingDay(day), day.ToString() + " shouldn't be a working day");
                day = day.AddDays(1);
            } while (day <= new DateTime(2015, 2, 20));

            //Seol 2016 (with substitute holiday)
            day = new DateTime(2016, 2, 7);
            do
            {
                Assert.IsFalse(dateTimeCulture.IsWorkingDay(day), day.ToString() + " shouldn't be a working day");
                day = day.AddDays(1);
            } while (day <= new DateTime(2015, 2, 10));

            //Chuseok 2015 (with substitute holiday)
            day = new DateTime(2015, 9, 26);
            do
            {
                Assert.IsFalse(dateTimeCulture.IsWorkingDay(day), day.ToString() + " shouldn't be a working day");
                day = day.AddDays(1);
            } while (day <= new DateTime(2015, 9, 29));
        }

        [Test]
        public void can_identify_Seolnal()
        {
            var holiday = KO_KRHolidayStrategy.Seolnal;
            var dateOnGregorian = new DateTime(2015, 2, 19);
            TestHoliday(holiday, dateOnGregorian);
        }

        [Test]
        public void can_identify_SeokgaTansilil()
        {
            var holiday = KO_KRHolidayStrategy.SeokgaTansinil;
            var dateOnGregorian = new DateTime(2015, 5, 25);
            TestHoliday(holiday, dateOnGregorian);
        }

        [Test]
        public void can_identify_Chuseok()
        {
            var holiday = KO_KRHolidayStrategy.Chuseok;
            var dateOnGregorian = new DateTime(2015, 9, 27);
            TestHoliday(holiday, dateOnGregorian);
        }

        [Test]
        public void can_identify_Jeheonjeol()
        {
            var holiday = KO_KRHolidayStrategy.Jeheonjeol;
            TestHoliday(holiday, new DateTime(2026, 7, 17));
            TestHoliday(holiday, new DateTime(2007, 7, 17));
            Assert.IsFalse(holiday.GetInstance(2025).HasValue);

            var dateTimeCulture = new WorkingDayCultureInfo("ko-KR");
            Assert.IsTrue(dateTimeCulture.IsWorkingDay(new DateTime(2025, 7, 17)));
            Assert.IsFalse(dateTimeCulture.IsWorkingDay(new DateTime(2026, 7, 17)));
        }

        [TestCase(2021, 8, 16)] // Gwangbokjeol on Sunday
        [TestCase(2021, 10, 4)] // Gaecheonjeol on Sunday
        [TestCase(2021, 10, 11)] // Hangulnal on Saturday
        [TestCase(2023, 5, 29)] // SeokgaTansinil on Saturday
        [TestCase(2025, 3, 3)] // Samiljeol on Saturday
        [TestCase(2026, 3, 2)] // Samiljeol on Sunday
        [TestCase(2026, 5, 25)] // SeokgaTansinil on Sunday
        [TestCase(2026, 8, 17)] // Gwangbokjeol on Saturday
        [TestCase(2026, 10, 5)] // Gaecheonjeol on Saturday
        [TestCase(2027, 5, 3)] // Labour Day on Saturday
        [TestCase(2027, 7, 19)] // Jeheonjeol on Saturday
        [TestCase(2027, 12, 27)] // Christmas on Saturday
        public void substitute_holiday_for_a_holiday_on_a_weekend(int year, int month, int day)
        {
            var dateTimeCulture = new WorkingDayCultureInfo("ko-KR");
            Assert.IsFalse(dateTimeCulture.IsWorkingDay(new DateTime(year, month, day)));
        }

        [Test]
        public void no_weekend_substitute_holiday_before_the_rule_applies()
        {
            var dateTimeCulture = new WorkingDayCultureInfo("ko-KR");
            Assert.IsTrue(dateTimeCulture.IsWorkingDay(new DateTime(2020, 10, 5))); // Gaecheonjeol 2020 on Saturday
            Assert.IsTrue(dateTimeCulture.IsWorkingDay(new DateTime(2022, 12, 26))); // Christmas 2022 on Sunday
        }

        [Test]
        public void chuseok_overlapping_gaecheonjeol_gets_one_substitute_holiday()
        {
            var dateTimeCulture = new WorkingDayCultureInfo("ko-KR");
            // Chuseok 2028 is Oct. 2-4 and Oct. 3 is also Gaecheonjeol
            var day = new DateTime(2028, 10, 2);
            do
            {
                Assert.IsFalse(dateTimeCulture.IsWorkingDay(day), day.ToString() + " shouldn't be a working day");
                day = day.AddDays(1);
            } while (day <= new DateTime(2028, 10, 5));
            Assert.IsTrue(dateTimeCulture.IsWorkingDay(new DateTime(2028, 10, 6)));
        }

        private void TestHoliday(NamedDay holiday, DateTime dateOnGregorian)
        {
            var holidayInstance = holiday.GetInstance(dateOnGregorian.Year);
            Assert.IsTrue(holidayInstance.HasValue);
            Assert.AreEqual(dateOnGregorian, holidayInstance.Value);
            Assert.IsTrue(holiday.IsInstanceOf(dateOnGregorian));
        }
    }
}