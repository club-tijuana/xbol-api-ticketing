using System.Linq.Expressions;
using FluentAssertions;
using NSubstitute;
using XBOL.Ticketing.Core.DTO.Requests;
using XBOL.Ticketing.Core.Model;
using XBOL.Ticketing.Data.Abstractions;
using XBOL.Ticketing.Services.Bundle;

namespace XBOL.Ticketing.Tests.Services;

public class BundlePassEventTicketServiceTests
{
    private readonly IBundlePassEventTicketRepository _bpetRepo = Substitute.For<IBundlePassEventTicketRepository>();
    private readonly IBundlePassRepository _passRepo = Substitute.For<IBundlePassRepository>();
    private readonly ITicketRepository _ticketRepo = Substitute.For<ITicketRepository>();
    private readonly BundlePassEventTicketService _sut;

    public BundlePassEventTicketServiceTests()
    {
        _sut = new BundlePassEventTicketService(_bpetRepo, _passRepo, _ticketRepo);
    }

    [Fact]
    public async Task AddAsync_TicketAlreadyLinkedGlobally_Throws()
    {
        _passRepo.GetByIdAsync(1).Returns(new BundlePass { Id = 1 });
        _ticketRepo.GetByIdAsync(10).Returns(new Core.Model.Ticket { Id = 10 });
        _bpetRepo.Get(filter: Arg.Any<Expression<Func<BundlePassEventTicket, bool>>>())
            .Returns(new List<BundlePassEventTicket>
            {
                new() { TicketId = 10, BundlePassId = 99 }
            }.AsQueryable());

        var act = () => _sut.AddAsync(1, new BundlePassEventTicketAddRequest { TicketIds = [10] });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already linked*");
    }

    [Fact]
    public async Task AddAsync_DifferentTicketsBothFree_BothLinked()
    {
        _passRepo.GetByIdAsync(1).Returns(new BundlePass { Id = 1 });
        _ticketRepo.GetByIdAsync(10).Returns(new Core.Model.Ticket { Id = 10 });
        _ticketRepo.GetByIdAsync(20).Returns(new Core.Model.Ticket { Id = 20 });
        _bpetRepo.Get(filter: Arg.Any<Expression<Func<BundlePassEventTicket, bool>>>())
            .Returns(Enumerable.Empty<BundlePassEventTicket>().AsQueryable());

        await _sut.AddAsync(1, new BundlePassEventTicketAddRequest { TicketIds = [10, 20] });

        await _bpetRepo.Received(2).InsertAsync(Arg.Any<BundlePassEventTicket>());
    }

    [Fact]
    public async Task AddAsync_InsertsValidTicketsBeforeHittingInvalidOne()
    {
        _passRepo.GetByIdAsync(1).Returns(new BundlePass { Id = 1 });
        _ticketRepo.GetByIdAsync(10).Returns(new Core.Model.Ticket { Id = 10 });
        _ticketRepo.GetByIdAsync(999).Returns((Core.Model.Ticket?)null);
        _bpetRepo.Get(filter: Arg.Any<Expression<Func<BundlePassEventTicket, bool>>>())
            .Returns(Enumerable.Empty<BundlePassEventTicket>().AsQueryable());

        var act = () => _sut.AddAsync(1, new BundlePassEventTicketAddRequest { TicketIds = [10, 999] });

        await act.Should().ThrowAsync<KeyNotFoundException>();
        await _bpetRepo.Received(1).InsertAsync(Arg.Is<BundlePassEventTicket>(e => e.TicketId == 10));
    }

    [Fact]
    public async Task RemoveAsync_OnlyCountsActuallyRemovedEntries()
    {
        var entry10 = new BundlePassEventTicket { BundlePassId = 1, TicketId = 10 };
        _passRepo.GetByIdAsync(1).Returns(new BundlePass { Id = 1 });
        _bpetRepo.Get(filter: Arg.Any<Expression<Func<BundlePassEventTicket, bool>>>())
            .Returns(new List<BundlePassEventTicket> { entry10 }.AsQueryable());

        var removed = await _sut.RemoveAsync(1,
            new BundlePassEventTicketRemoveRequest { TicketIds = [10] });

        removed.Should().Be(1);
        _bpetRepo.Received(1).HardDelete(entry10);
    }

    [Fact]
    public async Task RemoveAsync_NothingRemoved_DoesNotCommit()
    {
        _passRepo.GetByIdAsync(1).Returns(new BundlePass { Id = 1 });
        _bpetRepo.Get(filter: Arg.Any<Expression<Func<BundlePassEventTicket, bool>>>())
            .Returns(Enumerable.Empty<BundlePassEventTicket>().AsQueryable());

        await _sut.RemoveAsync(1, new BundlePassEventTicketRemoveRequest { TicketIds = [99] });

        await _bpetRepo.DidNotReceive().CommitAsync();
    }
}
