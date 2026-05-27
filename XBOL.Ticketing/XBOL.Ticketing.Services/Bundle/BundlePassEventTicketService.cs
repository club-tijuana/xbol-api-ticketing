using XBOL.Ticketing.Core.DTO;
using XBOL.Ticketing.Core.DTO.Requests;
using XBOL.Ticketing.Core.Mappers;
using XBOL.Ticketing.Core.Model;
using XBOL.Ticketing.Data.Abstractions;

namespace XBOL.Ticketing.Services.Bundle
{
    public class BundlePassEventTicketService(
        IBundlePassEventTicketRepository bundlePassEventTicketRepository,
        IBundlePassRepository bundlePassRepository,
        ITicketRepository ticketRepository)
    {
        public async Task<List<BundlePassEventTicketDTO>> GetByPassAsync(long bundlePassId)
        {
            var pass = await bundlePassRepository.GetByIdAsync(bundlePassId)
                ?? throw new KeyNotFoundException($"BundlePass {bundlePassId} not found.");

            var entries = await GetAllForPassAsync(bundlePassId);
            return entries.ToDto();
        }

        public async Task<List<BundlePassEventTicketDTO>> AddAsync(
            long bundlePassId, BundlePassEventTicketAddRequest request)
        {
            var pass = await bundlePassRepository.GetByIdAsync(bundlePassId)
                ?? throw new KeyNotFoundException($"BundlePass {bundlePassId} not found.");

            foreach (var ticketId in request.TicketIds)
            {
                var ticket = await ticketRepository.GetByIdAsync(ticketId)
                    ?? throw new KeyNotFoundException($"Ticket {ticketId} not found.");

                var existing = await FindByTicketIdAsync(ticketId);
                if (existing is not null)
                {
                    throw new InvalidOperationException(
                        $"Ticket {ticketId} is already linked to a BundlePassEventTicket.");
                }

                await bundlePassEventTicketRepository.InsertAsync(new BundlePassEventTicket
                {
                    BundlePassId = bundlePassId,
                    TicketId = ticketId
                });
            }

            await bundlePassEventTicketRepository.CommitAsync();

            var entries = await GetAllForPassAsync(bundlePassId);
            return entries.ToDto();
        }

        public async Task<int> RemoveAsync(long bundlePassId, BundlePassEventTicketRemoveRequest request)
        {
            var pass = await bundlePassRepository.GetByIdAsync(bundlePassId)
                ?? throw new KeyNotFoundException($"BundlePass {bundlePassId} not found.");

            var removed = 0;
            foreach (var ticketId in request.TicketIds)
            {
                var entry = await FindByPassAndTicketAsync(bundlePassId, ticketId);
                if (entry is not null)
                {
                    bundlePassEventTicketRepository.HardDelete(entry);
                    removed++;
                }
            }

            if (removed > 0)
            {
                await bundlePassEventTicketRepository.CommitAsync();
            }

            return removed;
        }

        private async Task<List<BundlePassEventTicket>> GetAllForPassAsync(long bundlePassId)
        {
            return bundlePassEventTicketRepository
                .Get(filter: bpet => bpet.BundlePassId == bundlePassId)
                .ToList();
        }

        private async Task<BundlePassEventTicket?> FindByPassAndTicketAsync(long bundlePassId, long ticketId)
        {
            return bundlePassEventTicketRepository
                .Get(filter: bpet => bpet.BundlePassId == bundlePassId && bpet.TicketId == ticketId)
                .FirstOrDefault();
        }

        private async Task<BundlePassEventTicket?> FindByTicketIdAsync(long ticketId)
        {
            return bundlePassEventTicketRepository
                .Get(filter: bpet => bpet.TicketId == ticketId)
                .FirstOrDefault();
        }
    }
}
