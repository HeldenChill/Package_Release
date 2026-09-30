using Hung.Analytics.Tracking;
using NUnit.Framework;

namespace Hung.Analytics.Tests.Tracking
{
    public class SessionRulesTests
    {
        static TrackingHarness New() => new TrackingHarness(_ => new IRule[]
        {
            new UserPropertiesRule(), new OpenAppRule(), new LoginDayRule(), new FtuTimeplayRule()
        });

        [Test]
        public void OpenApp_FtuThenPlain_BackgroundDoesNothing()
        {
            var h = New().Launch();
            Assert.Contains("ftu_open_app", h.Names);
            h.Clear();
            h.Background(10);
            Assert.IsEmpty(h.Names);
            h.Kill(1);
            Assert.Contains("open_app", h.Names);
        }

        [Test]
        public void UserProperties_SetOnColdStartAndProgress()
        {
            var h = New().Launch();
            Assert.AreEqual("1", h.Backend.UserProperties["ftu"]);
            Assert.AreEqual("1", h.Backend.UserProperties["days_since_install"]);
            h.Do(Fact.Progress(4));
            Assert.AreEqual("4", h.Backend.UserProperties["current_stage"]);
            h.Kill(24 * 60);
            Assert.AreEqual("0", h.Backend.UserProperties["ftu"]);
            Assert.AreEqual("2", h.Backend.UserProperties["days_since_install"]);
        }

        [Test]
        public void LoginDay_OncePerCalendarDay_SkippedDaysStaySkipped()
        {
            var h = New().Launch();                 // Monday, FTU
            Assert.IsFalse(h.Names.Contains("login_day"));
            h.Kill(24 * 60);                        // Tuesday
            Assert.AreEqual(2, h.Last("login_day").Parameters["n"]);
            h.Clear();
            h.Kill(60);                             // still Tuesday
            Assert.IsFalse(h.Names.Contains("login_day"));
            h.Kill(3 * 24 * 60);                    // Friday
            Assert.AreEqual(5, h.Last("login_day").Parameters["n"]);
        }

        [Test]
        public void FtuTimeplay_FiresOnCrossingMarks_OnlyInFtu()
        {
            var h = New().Launch();
            h.Play(4);
            Assert.IsFalse(h.Names.Contains("ftu_timeplay"));
            h.Play(2);
            Assert.AreEqual(5, h.Last("ftu_timeplay").Parameters["min"]);
            h.Clear();
            h.Play(10);                             // 6 -> 16 crosses 10 and 15
            Assert.AreEqual(2, h.Names.FindAll(n => n == "ftu_timeplay").Count);
            h.Kill(1);
            h.Clear();
            h.Play(60);
            Assert.IsEmpty(h.Names.FindAll(n => n.Contains("timeplay")));
        }
    }
}
