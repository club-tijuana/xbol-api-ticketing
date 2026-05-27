using EntityDTO = XBOL.Ticketing.Core.DTO.BundlePassDTO;
using EntityModel = XBOL.Ticketing.Core.Model.BundlePass;

namespace XBOL.Ticketing.Core.Mappers
{
    public static class BundlePassMapper
    {
        public static List<EntityDTO> ToDto(this IList<EntityModel> entities)
            => [.. entities.Select(x => x.ToDto())];

        public static EntityDTO ToDto(this EntityModel entity)
        {
            return new EntityDTO
            {
                Id = entity.Id,
                BundleId = entity.BundleId,
                ClientId = entity.ClientId,
                UserId = entity.UserId,
                BundleSeatId = entity.BundleSeatId,
                TrackingCode = entity.TrackingCode,
                PrivateToken = entity.PrivateToken,
                BundlePassType = entity.BundlePassType,
                Status = entity.Status,
                SuspendedReason = entity.SuspendedReason,
                SuspendedOtherReason = entity.SuspendedOtherReason,
                IsDigital = entity.IsDigital,
                Price = entity.Price,
                PurchasedAt = entity.PurchasedAt,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            };
        }

        public static List<EntityModel> ToModel(this IList<EntityDTO> entities)
            => [.. entities.Select(x => x.ToModel())];

        public static EntityModel ToModel(this EntityDTO entity)
        {
            return new EntityModel
            {
                Id = entity.Id,
                BundleId = entity.BundleId,
                ClientId = entity.ClientId,
                UserId = entity.UserId,
                BundleSeatId = entity.BundleSeatId,
                TrackingCode = entity.TrackingCode,
                PrivateToken = entity.PrivateToken,
                BundlePassType = entity.BundlePassType,
                Status = entity.Status,
                SuspendedReason = entity.SuspendedReason,
                SuspendedOtherReason = entity.SuspendedOtherReason,
                IsDigital = entity.IsDigital,
                Price = entity.Price,
                PurchasedAt = entity.PurchasedAt,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            };
        }
    }
}
