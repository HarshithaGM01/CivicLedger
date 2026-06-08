namespace CivicLedger.Domain;

public sealed class Expense
{
    private Expense()
    {
    }

    public Expense(Guid grantId, decimal amount, string category, string vendor, DateOnly incurredOn)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Expense amount must be greater than zero.");
        }

        Id = Guid.NewGuid();
        GrantId = grantId;
        Amount = decimal.Round(amount, 2);
        Category = RequireText(category, nameof(category), 80);
        Vendor = RequireText(vendor, nameof(vendor), 120);
        IncurredOn = incurredOn;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid GrantId { get; private set; }
    public decimal Amount { get; private set; }
    public string Category { get; private set; } = string.Empty;
    public string Vendor { get; private set; } = string.Empty;
    public DateOnly IncurredOn { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

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
