using System.Text.RegularExpressions;

namespace Smart_Stay.Helpers
{
    public static class PricePeriodHelper
    {
        private static readonly string[] Allowed = { "per night", "per week", "per month" };

        public static string Get(string? description)
        {
            var m = Regex.Match(description ?? "", @"\[PricePeriod:(.+?)\]");
            return m.Success && Allowed.Contains(m.Groups[1].Value)
                ? m.Groups[1].Value
                : "per month";
        }
    }
}