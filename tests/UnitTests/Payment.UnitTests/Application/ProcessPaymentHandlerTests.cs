using FluentAssertions;
using Moq;
using BuildingBlocks.SharedContracts.Abstractions;
using Payment.Application.Commands.ProcessPayment;
using Payment.Domain.Aggregates;
using Payment.Domain.Enumerations;

namespace Payment.UnitTests.Application;

public class ProcessPaymentHandlerTests
{
    private readonly Mock<IPaymentTransactionRepository> _repositoryMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly ProcessPaymentHandler _handler;

    public ProcessPaymentHandlerTests()
    {
        _handler = new ProcessPaymentHandler(_repositoryMock.Object, _uowMock.Object);
    }

    [Fact]
    public async Task Handle_CreatesTransactionAndMarksCompleted()
    {
        var command = new ProcessPaymentInternalCommand(
            Guid.NewGuid(), Guid.NewGuid(), 100m, "buyer-1");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _repositoryMock.Verify(r => r.Add(It.IsAny<PaymentTransaction>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Handle_PersistsTransactionWithCorrectFields()
    {
        PaymentTransaction? captured = null;
        _repositoryMock.Setup(r => r.Add(It.IsAny<PaymentTransaction>()))
            .Callback<PaymentTransaction>(t => captured = t);

        var orderId = Guid.NewGuid();
        var command = new ProcessPaymentInternalCommand(
            Guid.NewGuid(), orderId, 75.50m, "buyer-2");

        await _handler.Handle(command, CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.OrderId.Should().Be(orderId);
        captured.BuyerId.Should().Be("buyer-2");
        captured.Amount.Should().Be(75.50m);
        captured.Status.Should().Be(PaymentStatus.Completed);
        captured.TransactionId.Should().StartWith("txn_");
    }
}
