using System.Linq.Expressions;
using XBOL.Ticketing.Core.Commons.Enums;
using XBOL.Ticketing.Core.DTO;
using XBOL.Ticketing.Core.DTO.Requests;
using XBOL.Ticketing.Core.DTO.Responses;
using XBOL.Ticketing.Core.Mappers;
using XBOL.Ticketing.Data.Abstractions;

namespace XBOL.Ticketing.Services.Bundle
{
    public class BundlePassService(IBundlePassRepository repository)
    {
        protected IBundlePassRepository Repository { get; set; } = repository;

        public async Task<PagedResponse<BundlePassDTO>> GetPagedAsync(BundlePassQueryParams queryParams)
        {
            var searchTerm = queryParams.SearchTerm?.Trim().ToLower() ?? "";

            var passes = Repository.Get(
                filter: BuildFilter(queryParams, searchTerm),
                orderBy: q => q.OrderBy(bp => bp.Id),
                pageSize: queryParams.PageSize,
                currentPage: queryParams.Page
            ).ToList();

            var totalCount = Repository.Get().Count();

            return new PagedResponse<BundlePassDTO>
            {
                Items = passes.ToDto(),
                TotalCount = totalCount,
                Page = queryParams.Page,
                PageSize = queryParams.PageSize
            };
        }

        public async Task<BundlePassDTO?> GetByIdAsync(long id)
        {
            var pass = await Repository.GetByIdAsync(id);
            return pass?.ToDto();
        }

        public async Task<BundlePassDTO> CreateAsync(BundlePassCreateRequest request, Guid userId)
        {
            var now = DateTimeOffset.UtcNow;
            var pass = new Core.Model.BundlePass
            {
                BundleId = request.BundleId,
                ClientId = request.ClientId,
                UserId = request.UserId,
                BundleSeatId = request.BundleSeatId,
                TrackingCode = Guid.NewGuid().ToString("N")[..16].ToUpperInvariant(),
                PrivateToken = Guid.NewGuid().ToString(),
                BundlePassType = request.BundlePassType,
                Status = BundlePassStatus.Active,
                IsDigital = request.IsDigital,
                Price = request.Price,
                PurchasedAt = now,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedBy = userId,
                UpdatedBy = userId
            };

            await Repository.InsertAsync(pass);
            await Repository.CommitAsync();
            return pass.ToDto();
        }

        public async Task<BundlePassDTO?> UpdateAsync(long id, BundlePassUpdateRequest request, Guid userId)
        {
            var pass = await Repository.GetByIdAsync(id);
            if (pass is null) { return null; }

            if (request.Status is not null) { pass.Status = request.Status.Value; }
            if (request.SuspendedReason is not null) { pass.SuspendedReason = request.SuspendedReason; }
            if (request.SuspendedOtherReason is not null) { pass.SuspendedOtherReason = request.SuspendedOtherReason; }
            if (request.Price is not null) { pass.Price = request.Price.Value; }
            if (request.BundleSeatId is not null) { pass.BundleSeatId = request.BundleSeatId; }
            if (request.IsDigital is not null) { pass.IsDigital = request.IsDigital.Value; }

            pass.UpdatedAt = DateTimeOffset.UtcNow;
            pass.UpdatedBy = userId;

            await Repository.UpdateAsync(pass);
            return pass.ToDto();
        }

        public async Task<bool> DeleteAsync(long id)
        {
            var pass = await Repository.GetByIdAsync(id);
            if (pass is null) { return false; }

            await Repository.HardDeleteAsync(pass);
            return true;
        }

        private static Expression<Func<Core.Model.BundlePass, bool>>? BuildFilter(
            BundlePassQueryParams queryParams, string searchTerm)
        {
            if (!queryParams.BundleId.HasValue && queryParams.Status is null && string.IsNullOrEmpty(searchTerm))
            {
                return null;
            }

            return bp =>
                (!queryParams.BundleId.HasValue || bp.BundleId == queryParams.BundleId.Value) &&
                (!queryParams.Status.HasValue || bp.Status == queryParams.Status.Value) &&
                (string.IsNullOrEmpty(searchTerm) || bp.TrackingCode.ToLower().Contains(searchTerm));
        }
    }
}
