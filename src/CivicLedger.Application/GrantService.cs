using CivicLedger.Domain;

namespace CivicLedger.Application;

public sealed class GrantService(
    IGrantRepository repository,
    RiskAssessmentService riskAssessmentService)
{
    public async Task<IReadOnlyCollection<GrantResponse>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var grants = await repository.GetAllAsync(cancellationToken);
        return grants
            .OrderBy(grant => grant.EndDate)
            .Select(Map)
            .ToArray();
    }

    public async Task<GrantResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var grant = await repository.GetByIdAsync(id, cancellationToken);
        return grant is null ? null : Map(grant);
    }

    public async Task<GrantResponse> CreateAsync(
        CreateGrantRequest request,
        CancellationToken cancellationToken = default)
    {
        var grant = new Grant(
            request.Name,
            request.Department,
            request.FundingSource,
            request.AwardedAmount,
            request.StartDate,
            request.EndDate);

        await repository.AddAsync(grant, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return Map(grant);
    }

    public async Task<GrantResponse?> ActivateAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var grant = await repository.GetByIdAsync(id, cancellationToken);
        if (grant is null)
        {
            return null;
        }

        grant.Activate();
        await repository.SaveChangesAsync(cancellationToken);
        return Map(grant);
    }

    public async Task<GrantResponse?> AddExpenseAsync(
        Guid id,
        AddExpenseRequest request,
        CancellationToken cancellationToken = default)
    {
        var grant = await repository.GetByIdAsync(id, cancellationToken);
        if (grant is null)
        {
            return null;
        }

        grant.AddExpense(request.Amount, request.Category, request.Vendor, request.IncurredOn);
        var risk = riskAssessmentService.Assess(grant, DateOnly.FromDateTime(DateTime.UtcNow));
        grant.ApplyRiskStatus(risk.Score >= 60, string.Join(" ", risk.Reasons));
        await repository.SaveChangesAsync(cancellationToken);
        return Map(grant);
    }

    public async Task<RiskAssessment?> AssessRiskAsync(
        Guid id,
        DateOnly today,
        CancellationToken cancellationToken = default)
    {
        var grant = await repository.GetByIdAsync(id, cancellationToken);
        return grant is null ? null : riskAssessmentService.Assess(grant, today);
    }

    public async Task<DashboardResponse> GetDashboardAsync(
        CancellationToken cancellationToken = default)
    {
        var grants = await repository.GetAllAsync(cancellationToken);
        var totalAwarded = grants.Sum(grant => grant.AwardedAmount);
        var totalSpent = grants.Sum(grant => grant.SpentAmount);

        var departments = grants
            .GroupBy(grant => grant.Department)
            .Select(group => new DepartmentSummary(
                group.Key,
                group.Count(),
                group.Sum(grant => grant.AwardedAmount),
                group.Sum(grant => grant.SpentAmount)))
            .OrderByDescending(summary => summary.Awarded)
            .ToArray();

        return new DashboardResponse(
            grants.Count,
            grants.Count(grant => grant.Status == GrantStatus.Active),
            grants.Count(grant => grant.Status == GrantStatus.AtRisk),
            totalAwarded,
            totalSpent,
            totalAwarded == 0 ? 0 : decimal.Round(totalSpent / totalAwarded * 100, 1),
            departments);
    }

    private static GrantResponse Map(Grant grant) =>
        new(
            grant.Id,
            grant.Name,
            grant.Department,
            grant.FundingSource,
            grant.AwardedAmount,
            grant.SpentAmount,
            grant.RemainingAmount,
            grant.UtilizationPercent,
            grant.StartDate,
            grant.EndDate,
            grant.Status,
            grant.Expenses
                .OrderByDescending(expense => expense.IncurredOn)
                .Select(expense => new ExpenseResponse(
                    expense.Id,
                    expense.Amount,
                    expense.Category,
                    expense.Vendor,
                    expense.IncurredOn))
                .ToArray(),
            grant.AuditEntries
                .OrderByDescending(entry => entry.CreatedAtUtc)
                .Select(entry => new AuditResponse(
                    entry.Id,
                    entry.Action,
                    entry.Details,
                    entry.CreatedAtUtc))
                .ToArray());
}
