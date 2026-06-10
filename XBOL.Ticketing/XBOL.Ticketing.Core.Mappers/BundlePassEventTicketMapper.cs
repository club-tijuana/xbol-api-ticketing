using EntityDTO = XBOL.Ticketing.Core.DTO.BundlePassEventTicketDTO;
using EntityModel = XBOL.Ticketing.Core.Model.BundlePassEventTicket;

namespace XBOL.Ticketing.Core.Mappers
{
    public static class BundlePassEventTicketMapper
    {
        public static List<EntityDTO> ToDto(this IList<EntityModel> entities)
            => [.. entities.Select(x => x.ToDto())];

        public static EntityDTO ToDto(this EntityModel entity)
        {
            return new EntityDTO
            {
                Id = entity.Id,
                BundlePassId = entity.BundlePassId,
                TicketId = entity.TicketId
            };
        }
    }
}
