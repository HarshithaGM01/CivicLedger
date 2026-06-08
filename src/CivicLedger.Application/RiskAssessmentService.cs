using CivicLedger.Domain;

namespace CivicLedger.Application;

public sealed class RiskAssessmentService
{
    public RiskAssessment Assess(Grant grant, DateOnly today)
    {
        var score = 0;
        var reasons = new List<string>();
        var totalDays = Math.Max(1, grant.EndDate.DayNumber - grant.StartDate.DayNumber);
        var elapsedDays = Math.Clamp(today.DayNumber - grant.StartDate.DayNumber, 0, totalDays);
        var elapsedPercent = (decimal)elapsedDays / totalDays * 100;

        if (grant.UtilizationPercent >= 90 && elapsedPercent < 75)
        {
            score += 45;
            reasons.Add("More than 90% of the budget is used before 75% of the grant period has elapsed.");
        }
        else if (grant.UtilizationPercent >= 75 && elapsedPercent < 50)
        {
            score += 30;
            reasons.Add("Spending is progressing substantially faster than the grant timeline.");
        }

        if (grant.EndDate.DayNumber - today.DayNumber <= 30 && grant.RemainingAmount > grant.AwardedAmount * 0.4m)
        {
            score += 35;
            reasons.Add("More than 40% of the award remains with 30 days or fewer before the end date.");
        }

        var largestExpense = grant.Expenses.OrderByDescending(expense => expense.Amount).FirstOrDefault();
        if (largestExpense is not null && largestExpense.Amount > grant.AwardedAmount * 0.35m)
        {
            score += 25;
            reasons.Add("A single expense represents more than 35% of the total award.");
        }

        score = Math.Min(score, 100);
        var level = score switch
        {
            >= 60 => "High",
            >= 30 => "Medium",
            _ => "Low"
        };

        if (reasons.Count == 0)
        {
            reasons.Add("Spending and timeline indicators are within configured thresholds.");
        }

        return new RiskAssessment(score, level, reasons);
    }
}
