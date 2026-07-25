namespace AppBase.Common;

public static class DateTimeAddons
{
    public static DateTime PreviousWorkDay(DateTime date)
    {
        do
        {
            date = date.AddDays(-1);
        }
        while (IsHoliday(date) || IsWeekend(date));

        return date;
    }

    private static bool IsWeekend(DateTime date)
    {
        return date.DayOfWeek == DayOfWeek.Saturday ||
               date.DayOfWeek == DayOfWeek.Sunday;
    }


    private readonly static HashSet<DateTime> _holidayList = new HashSet<DateTime>
{
    // 2021
    new DateTime(2021,12,25), // Christmas Day
    new DateTime(2021,12,26), // Boxing Day
    
    // 2022
    new DateTime(2022,1,1),   // New Year's Day
    new DateTime(2022,1,6),   // Epiphany (Three Kings Day)
    new DateTime(2022,4,17),  // Easter Sunday
    new DateTime(2022,4,18),  // Easter Monday
    new DateTime(2022,5,1),   // Labour Day
    new DateTime(2022,5,3),   // Constitution Day (May 3rd)
    new DateTime(2022,6,5),   // Whit Sunday (Pentecost)
    new DateTime(2022,6,16),  // Corpus Christi
    new DateTime(2022,8,15),  // Assumption of Mary
    new DateTime(2022,11,1),  // All Saints' Day
    new DateTime(2022,11,11), // Independence Day
    new DateTime(2022,12,25), // Christmas Day
    new DateTime(2022,12,26), // Boxing Day
    
    // 2023
    new DateTime(2023,1,1),   // New Year's Day
    new DateTime(2023,1,6),   // Epiphany (Three Kings Day)
    new DateTime(2023,4,9),   // Easter Sunday
    new DateTime(2023,4,10),  // Easter Monday
    new DateTime(2023,5,1),   // Labour Day
    new DateTime(2023,5,3),   // Constitution Day (May 3rd)
    new DateTime(2023,5,28),  // Whit Sunday (Pentecost)
    new DateTime(2023,6,8),   // Corpus Christi
    new DateTime(2023,8,15),  // Assumption of Mary
    new DateTime(2023,11,1),  // All Saints' Day
    new DateTime(2023,11,11), // Independence Day
    new DateTime(2023,12,25), // Christmas Day
    new DateTime(2023,12,26), // Boxing Day
    
    // 2024
    new DateTime(2024,1,1),   // New Year's Day
    new DateTime(2024,1,6),   // Epiphany (Three Kings Day)
    new DateTime(2024,3,31),  // Easter Sunday
    new DateTime(2024,4,1),   // Easter Monday
    new DateTime(2024,5,1),   // Labour Day
    new DateTime(2024,5,3),   // Constitution Day (May 3rd)
    new DateTime(2024,5,19),  // Whit Sunday (Pentecost)
    new DateTime(2024,5,30),  // Corpus Christi
    new DateTime(2024,8,15),  // Assumption of Mary
    new DateTime(2024,11,1),  // All Saints' Day
    new DateTime(2024,11,11), // Independence Day
    new DateTime(2024,12,25), // Christmas Day
    new DateTime(2024,12,26), // Boxing Day
    
    // 2025
    new DateTime(2025,1,1),   // New Year's Day
    new DateTime(2025,1,6),   // Epiphany (Three Kings Day)
    new DateTime(2025,4,20),  // Easter Sunday
    new DateTime(2025,4,21),  // Easter Monday
    new DateTime(2025,5,1),   // Labour Day
    new DateTime(2025,5,3),   // Constitution Day (May 3rd)
    new DateTime(2025,6,8),   // Whit Sunday (Pentecost)
    new DateTime(2025,6,19),  // Corpus Christi
    new DateTime(2025,8,15),  // Assumption of Mary
    new DateTime(2025,11,1),  // All Saints' Day
    new DateTime(2025,11,11), // Independence Day
    new DateTime(2025,12,25), // Christmas Day
    new DateTime(2025,12,26), // Boxing Day
    
    // 2026
    new DateTime(2026,1,1),   // New Year's Day
    new DateTime(2026,1,6),   // Epiphany (Three Kings Day)
    new DateTime(2026,4,5),   // Easter Sunday
    new DateTime(2026,4,6),   // Easter Monday
    new DateTime(2026,5,1),   // Labour Day
    new DateTime(2026,5,3),   // Constitution Day (May 3rd)
    new DateTime(2026,5,24),  // Whit Sunday (Pentecost)
    new DateTime(2026,6,4),   // Corpus Christi
    new DateTime(2026,8,15),  // Assumption of Mary
    new DateTime(2026,11,1),  // All Saints' Day
    new DateTime(2026,11,11), // Independence Day
    new DateTime(2026,12,25), // Christmas Day
    new DateTime(2026,12,26), // Boxing Day
    
    // 2027
    new DateTime(2027,1,1),   // New Year's Day
    new DateTime(2027,1,6),   // Epiphany (Three Kings Day)
    new DateTime(2027,3,28),  // Easter Sunday
    new DateTime(2027,3,29),  // Easter Monday
    new DateTime(2027,5,1),   // Labour Day
    new DateTime(2027,5,3),   // Constitution Day (May 3rd)
    new DateTime(2027,5,16),  // Whit Sunday (Pentecost)
    new DateTime(2027,5,27),  // Corpus Christi
    new DateTime(2027,8,15),  // Assumption of Mary
    new DateTime(2027,11,1),  // All Saints' Day
    new DateTime(2027,11,11), // Independence Day
    new DateTime(2027,12,25), // Christmas Day
    new DateTime(2027,12,26), // Boxing Day
    
    // 2028
    new DateTime(2028,1,1),   // New Year's Day
    new DateTime(2028,1,6),   // Epiphany (Three Kings Day)
    new DateTime(2028,4,16),  // Easter Sunday
    new DateTime(2028,4,17),  // Easter Monday
    new DateTime(2028,5,1),   // Labour Day
    new DateTime(2028,5,3),   // Constitution Day (May 3rd)
    new DateTime(2028,6,4),   // Whit Sunday (Pentecost)
    new DateTime(2028,6,15),  // Corpus Christi
    new DateTime(2028,8,15),  // Assumption of Mary
    new DateTime(2028,11,1),  // All Saints' Day
    new DateTime(2028,11,11), // Independence Day
    new DateTime(2028,12,25), // Christmas Day
    new DateTime(2028,12,26)  // Boxing Day
};

    private static bool IsHoliday(DateTime date)
    {
        return _holidayList.Contains(date);
    }
}
