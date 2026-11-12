using SeldonVigil.Api.Modules.CostIntelligence.Domain.Entities;
using SeldonVigil.Api.Modules.CostIntelligence.Domain.ValueObjects;
using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;
using SeldonVigil.SharedKernel.Infrastructure;
using Moq;
using Xunit;
using FluentAssertions;

namespace SeldonVigil.UnitTests.CostIntelligence.Domain.Entities;

public class CostBudgetTests
{
    private readonly Mock<IDateTimeProvider> _dateTimeProviderMock;

    public CostBudgetTests()
    {
        _dateTimeProviderMock = new Mock<IDateTimeProvider>();
        _ = _dateTimeProviderMock.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);
    }

    [Fact]
    public void CreateWithValidParametersReturnsSuccess()
    {
        // Arrange
        var tenantId = TenantId.New();
        var scopeId = Guid.NewGuid();
        var budgetAmount = new Money(1000m, "USD");
        var alertThresholds = new List<decimal> { 0.5m, 0.75m, 0.9m };

        // Act
        var result = CostBudget.Create(
            tenantId,
            "Test Budget",
            BudgetScopeType.Tenant,
            scopeId,
            budgetAmount,
            BudgetPeriod.Monthly,
            alertThresholds,
            EnforcementType.Soft,
            Guid.NewGuid(),
            _dateTimeProviderMock.Object);

        // Assert
        _ = result.IsSuccess.Should().BeTrue();
        _ = result.Value!.Name.Should().Be("Test Budget");
        _ = result.Value.BudgetAmount.Should().Be(budgetAmount);
        _ = result.Value.CurrentSpend.Amount.Should().Be(0);
        _ = result.Value.AlertThresholds.Should().HaveCount(3);
    }

    [Fact]
    public void CreateWithInvalidNameReturnsFailure()
    {
        // Arrange
        var tenantId = TenantId.New();
        var scopeId = Guid.NewGuid();
        var budgetAmount = new Money(1000m, "USD");

        // Act
        var result = CostBudget.Create(
            tenantId,
            "", // Invalid: empty
            BudgetScopeType.Tenant,
            scopeId,
            budgetAmount,
            BudgetPeriod.Monthly,
            new List<decimal>(),
            EnforcementType.Soft,
            Guid.NewGuid(),
            _dateTimeProviderMock.Object);

        // Assert
        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error!.Code.Should().Be("Budget.InvalidName");
    }

    [Fact]
    public void CreateWithNameTooLongReturnsFailure()
    {
        // Arrange
        var tenantId = TenantId.New();
        var scopeId = Guid.NewGuid();
        var budgetAmount = new Money(1000m, "USD");
        var longName = new string('a', 256);

        // Act
        var result = CostBudget.Create(
            tenantId,
            longName,
            BudgetScopeType.Tenant,
            scopeId,
            budgetAmount,
            BudgetPeriod.Monthly,
            new List<decimal>(),
            EnforcementType.Soft,
            Guid.NewGuid(),
            _dateTimeProviderMock.Object);

        // Assert
        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error!.Code.Should().Be("Budget.InvalidName");
    }

    [Fact]
    public void CreateWithInvalidAmountReturnsFailure()
    {
        // Arrange
        var tenantId = TenantId.New();
        var scopeId = Guid.NewGuid();
        var budgetAmount = new Money(0m, "USD"); // Invalid: zero

        // Act
        var result = CostBudget.Create(
            tenantId,
            "Test Budget",
            BudgetScopeType.Tenant,
            scopeId,
            budgetAmount,
            BudgetPeriod.Monthly,
            new List<decimal>(),
            EnforcementType.Soft,
            Guid.NewGuid(),
            _dateTimeProviderMock.Object);

        // Assert
        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error!.Code.Should().Be("Budget.InvalidAmount");
    }

    [Fact]
    public void CreateWithInvalidThresholdsReturnsFailure()
    {
        // Arrange
        var tenantId = TenantId.New();
        var scopeId = Guid.NewGuid();
        var budgetAmount = new Money(1000m, "USD");
        var invalidThresholds = new List<decimal> { 1.5m }; // Invalid: > 1

        // Act
        var result = CostBudget.Create(
            tenantId,
            "Test Budget",
            BudgetScopeType.Tenant,
            scopeId,
            budgetAmount,
            BudgetPeriod.Monthly,
            invalidThresholds,
            EnforcementType.Soft,
            Guid.NewGuid(),
            _dateTimeProviderMock.Object);

        // Assert
        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error!.Code.Should().Be("Budget.InvalidThresholds");
    }

    [Fact]
    public void RecordSpendWithMatchingCurrencyIncreasesCurrentSpend()
    {
        // Arrange
        var tenantId = TenantId.New();
        var scopeId = Guid.NewGuid();
        var budgetAmount = new Money(1000m, "USD");
        var budget = CostBudget.Create(
            tenantId,
            "Test Budget",
            BudgetScopeType.Tenant,
            scopeId,
            budgetAmount,
            BudgetPeriod.Monthly,
            new List<decimal>(),
            EnforcementType.Soft,
            Guid.NewGuid(),
            _dateTimeProviderMock.Object).Value!;
        var spendAmount = new Money(100m, "USD");

        // Act
        var result = budget.RecordSpend(spendAmount, _dateTimeProviderMock.Object);

        // Assert
        _ = result.IsSuccess.Should().BeTrue();
        _ = budget.CurrentSpend.Amount.Should().Be(100m);
    }

    [Fact]
    public void RecordSpendWithCurrencyMismatchReturnsFailure()
    {
        // Arrange
        var tenantId = TenantId.New();
        var scopeId = Guid.NewGuid();
        var budgetAmount = new Money(1000m, "USD");
        var budget = CostBudget.Create(
            tenantId,
            "Test Budget",
            BudgetScopeType.Tenant,
            scopeId,
            budgetAmount,
            BudgetPeriod.Monthly,
            new List<decimal>(),
            EnforcementType.Soft,
            Guid.NewGuid(),
            _dateTimeProviderMock.Object).Value!;
        var spendAmount = new Money(100m, "EUR"); // Different currency

        // Act
        var result = budget.RecordSpend(spendAmount, _dateTimeProviderMock.Object);

        // Assert
        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error!.Code.Should().Be("Budget.CurrencyMismatch");
    }

    [Fact]
    public void RecordSpendWhenThresholdBreachedReturnsBreachedThreshold()
    {
        // Arrange
        var tenantId = TenantId.New();
        var scopeId = Guid.NewGuid();
        var budgetAmount = new Money(1000m, "USD");
        var alertThresholds = new List<decimal> { 0.5m };
        var budget = CostBudget.Create(
            tenantId,
            "Test Budget",
            BudgetScopeType.Tenant,
            scopeId,
            budgetAmount,
            BudgetPeriod.Monthly,
            alertThresholds,
            EnforcementType.Soft,
            Guid.NewGuid(),
            _dateTimeProviderMock.Object).Value!;
        var spendAmount = new Money(600m, "USD"); // 60% utilization

        // Act
        var result = budget.RecordSpend(spendAmount, _dateTimeProviderMock.Object);

        // Assert
        _ = result.IsSuccess.Should().BeTrue();
        _ = result.Value.ThresholdBreached.Should().BeTrue();
        _ = result.Value.BreachedThreshold.Should().Be(0.5m);
    }

    [Fact]
    public void RecordSpendWhenThresholdNotBreachedReturnsFalse()
    {
        // Arrange
        var tenantId = TenantId.New();
        var scopeId = Guid.NewGuid();
        var budgetAmount = new Money(1000m, "USD");
        var alertThresholds = new List<decimal> { 0.5m };
        var budget = CostBudget.Create(
            tenantId,
            "Test Budget",
            BudgetScopeType.Tenant,
            scopeId,
            budgetAmount,
            BudgetPeriod.Monthly,
            alertThresholds,
            EnforcementType.Soft,
            Guid.NewGuid(),
            _dateTimeProviderMock.Object).Value!;
        var spendAmount = new Money(400m, "USD"); // 40% utilization

        // Act
        var result = budget.RecordSpend(spendAmount, _dateTimeProviderMock.Object);

        // Assert
        _ = result.IsSuccess.Should().BeTrue();
        _ = result.Value.ThresholdBreached.Should().BeFalse();
        _ = result.Value.BreachedThreshold.Should().BeNull();
    }

    [Fact]
    public void ResetPeriodResetsCurrentSpendToZero()
    {
        // Arrange
        var tenantId = TenantId.New();
        var scopeId = Guid.NewGuid();
        var budgetAmount = new Money(1000m, "USD");
        var budget = CostBudget.Create(
            tenantId,
            "Test Budget",
            BudgetScopeType.Tenant,
            scopeId,
            budgetAmount,
            BudgetPeriod.Monthly,
            new List<decimal>(),
            EnforcementType.Soft,
            Guid.NewGuid(),
            _dateTimeProviderMock.Object).Value!;
        _ = budget.RecordSpend(new Money(500m, "USD"), _dateTimeProviderMock.Object);

        // Act
        budget.ResetPeriod(_dateTimeProviderMock.Object);

        // Assert
        _ = budget.CurrentSpend.Amount.Should().Be(0);
        _ = budget.LastResetAt.Should().NotBe(default);
    }

    [Fact]
    public void ShouldResetWithDailyPeriodAndOneDayPassedReturnsTrue()
    {
        // Arrange
        var tenantId = TenantId.New();
        var scopeId = Guid.NewGuid();
        var budgetAmount = new Money(1000m, "USD");
        var yesterday = DateTime.UtcNow.AddDays(-1);
        _ = _dateTimeProviderMock.Setup(x => x.UtcNow).Returns(yesterday);
        var budget = CostBudget.Create(
            tenantId,
            "Test Budget",
            BudgetScopeType.Tenant,
            scopeId,
            budgetAmount,
            BudgetPeriod.Daily,
            new List<decimal>(),
            EnforcementType.Soft,
            Guid.NewGuid(),
            _dateTimeProviderMock.Object).Value!;
        _ = _dateTimeProviderMock.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);

        // Act
        var result = budget.ShouldReset(_dateTimeProviderMock.Object);

        // Assert
        _ = result.Should().BeTrue();
    }

    [Fact]
    public void ShouldResetWithDailyPeriodAndLessThanOneDayPassedReturnsFalse()
    {
        // Arrange
        var tenantId = TenantId.New();
        var scopeId = Guid.NewGuid();
        var budgetAmount = new Money(1000m, "USD");
        var budget = CostBudget.Create(
            tenantId,
            "Test Budget",
            BudgetScopeType.Tenant,
            scopeId,
            budgetAmount,
            BudgetPeriod.Daily,
            new List<decimal>(),
            EnforcementType.Soft,
            Guid.NewGuid(),
            _dateTimeProviderMock.Object).Value!;

        // Act
        var result = budget.ShouldReset(_dateTimeProviderMock.Object);

        // Assert
        _ = result.Should().BeFalse();
    }

    [Fact]
    public void ShouldResetWithMonthlyPeriodAndThirtyDaysPassedReturnsTrue()
    {
        // Arrange
        var tenantId = TenantId.New();
        var scopeId = Guid.NewGuid();
        var budgetAmount = new Money(1000m, "USD");
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
        _ = _dateTimeProviderMock.Setup(x => x.UtcNow).Returns(thirtyDaysAgo);
        var budget = CostBudget.Create(
            tenantId,
            "Test Budget",
            BudgetScopeType.Tenant,
            scopeId,
            budgetAmount,
            BudgetPeriod.Monthly,
            new List<decimal>(),
            EnforcementType.Soft,
            Guid.NewGuid(),
            _dateTimeProviderMock.Object).Value!;
        _ = _dateTimeProviderMock.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);

        // Act
        var result = budget.ShouldReset(_dateTimeProviderMock.Object);

        // Assert
        _ = result.Should().BeTrue();
    }
}
