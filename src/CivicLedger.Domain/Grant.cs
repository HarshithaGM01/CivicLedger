namespace CivicLedger.Domain;

public sealed class Grant
{
    private readonly List<Expense> _expenses = [];
    private readonly List<AuditEntry> _auditEntries = [];

    private Grant()
    {
    }

    public Grant(
        string name,
        string department,
        string fundingSource,
        decimal awardedAmount,
        DateOnly startDate,
        DateOnly endDate)
    {
        if (awardedAmount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(awardedAmount), "Awarded amount must be greater than zero.");
        }

        if (endDate < startDate)
        {
            throw new ArgumentException("End date must be on or after the start date.", nameof(endDate));
        }

        Id = Guid.NewGuid();
        Name = RequireText(name, nameof(name), 140);
        Department = RequireText(department, nameof(department), 100);
        FundingSource = RequireText(fundingSource, nameof(fundingSource), 120);
        AwardedAmount = decimal.Round(awardedAmount, 2);
        StartDate = startDate;
        EndDate = endDate;
        Status = GrantStatus.Draft;
        CreatedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
        RecordAudit("Grant created", $"{Name} was created with an award of {AwardedAmount:C}.");
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Department { get; private set; } = string.Empty;
    public string FundingSource { get; private set; } = string.Empty;
    public decimal AwardedAmount { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public GrantStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public IReadOnlyCollection<Expense> Expenses => _expenses.AsReadOnly();
    public IReadOnlyCollection<AuditEntry> AuditEntries => _auditEntries.AsReadOnly();
    public decimal SpentAmount => _expenses.Sum(expense => expense.Amount);
    public decimal RemainingAmount => AwardedAmount - SpentAmount;
    public decimal UtilizationPercent => AwardedAmount == 0
        ? 0
        : decimal.Round(SpentAmount / AwardedAmount * 100, 1);

    public void Activate()
    {
        if (Status != GrantStatus.Draft)
        {
            throw new InvalidOperationException("Only draft grants can be activated.");
        }

        Status = GrantStatus.Active;
        Touch();
        RecordAudit("Grant activated", "The grant is now available for expense reporting.");
    }

    public Expense AddExpense(decimal amount, string category, string vendor, DateOnly incurredOn)
    {
        if (Status is GrantStatus.Draft or GrantStatus.Completed or GrantStatus.Closed)
        {
            throw new InvalidOperationException("Expenses can only be added to active or at-risk grants.");
        }

        if (incurredOn < StartDate || incurredOn > EndDate)
        {
            throw new ArgumentOutOfRangeException(nameof(incurredOn), "Expense date must fall within the grant period.");
        }

        if (amount > RemainingAmount)
        {
            throw new InvalidOperationException("The expense would exceed the remaining grant budget.");
        }

        var expense = new Expense(Id, amount, category, vendor, incurredOn);
        _expenses.Add(expense);
        Touch();
        RecordAudit("Expense recorded", $"{expense.Vendor}: {expense.Amount:C} in {expense.Category}.");
        return expense;
    }

    public void ApplyRiskStatus(bool isAtRisk, string explanation)
    {
        if (Status is GrantStatus.Completed or GrantStatus.Closed or GrantStatus.Draft)
        {
            return;
        }

        var nextStatus = isAtRisk ? GrantStatus.AtRisk : GrantStatus.Active;
        if (Status == nextStatus)
        {
            return;
        }

        Status = nextStatus;
        Touch();
        RecordAudit("Risk status updated", explanation);
    }

    private void Touch() => UpdatedAtUtc = DateTime.UtcNow;

    private void RecordAudit(string action, string details) =>
        _auditEntries.Add(new AuditEntry(Id, action, details));

    private static string RequireText(string value, string parameterName, int maximumLength)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException("A value is required.", parameterName);
        }

        if (normalized.Length > maximumLength)
        {
            throw new ArgumentException($"The value cannot exceed {maximumLength} characters.", parameterName);
        }

        return normalized;
    }
}
