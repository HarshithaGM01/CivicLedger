using CivicLedger.Domain;

namespace CivicLedger.Application;

public sealed record CreateGrantRequest(
    string Name,
    string Department,
    string FundingSource,
    decimal AwardedAmount,
    DateOnly StartDate,
    DateOnly EndDate);

public sealed record AddExpenseRequest(
    decimal Amount,
    string Category,
    string Vendor,
    DateOnly IncurredOn);

public sealed record ExpenseResponse(
    Guid Id,
    decimal Amount,
    string Category,
    string Vendor,
    DateOnly IncurredOn);

public sealed record AuditResponse(
    Guid Id,
    string Action,
    string Details,
    DateTime CreatedAtUtc);

public sealed record GrantResponse(
    Guid Id,
    string Name,
    string Department,
    string FundingSource,
    decimal AwardedAmount,
    decimal SpentAmount,
    decimal RemainingAmount,
    decimal UtilizationPercent,
    DateOnly StartDate,
    DateOnly EndDate,
    GrantStatus Status,
    IReadOnlyCollection<ExpenseResponse> Expenses,
    IReadOnlyCollection<AuditResponse> AuditEntries);

public sealed record DashboardResponse(
    int TotalGrants,
    int ActiveGrants,
    int AtRiskGrants,
    decimal TotalAwarded,
    decimal TotalSpent,
    decimal UtilizationPercent,
    IReadOnlyCollection<DepartmentSummary> Departments);

public sealed record DepartmentSummary(
    string Department,
    int GrantCount,
    decimal Awarded,
    decimal Spent);

public sealed record RiskAssessment(
    int Score,
    string Level,
    IReadOnlyCollection<string> Reasons);
