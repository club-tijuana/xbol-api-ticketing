using EntityDTO = XBOL.Ticketing.Core.DTO.BundleEventScheduleResponseDTO;
using EntityModel = XBOL.Ticketing.Core.Model.BundleEventSchedule;

namespace XBOL.Ticketing.Core.Mappers
{
    public static class BundleEventScheduleMapper
    {
        public static List<EntityDTO> ToDto(this IList<EntityModel> entities)
            => [.. entities.Select(x => x.ToDto())];

        public static EntityDTO ToDto(this EntityModel entity)
        {
            return new EntityDTO
            {
                BundleId = entity.BundleId,
                EventScheduleId = entity.EventScheduleId,
                SortOrder = entity.SortOrder,
                EventSchedule = new XBOL.Ticketing.Core.DTO.EventScheduleSummaryDTO
                {
                    Id = entity.EventSchedule.Id,
                    StartDateTime = entity.EventSchedule.StartDateTime,
                    EndDateTime = entity.EventSchedule.EndDateTime,
                    ExternalEventKey = entity.EventSchedule.ExternalEventKey,
                    GameCategory = (Commons.Enums.GameCategory)entity.EventSchedule.GameCategory,
                    Status = entity.EventSchedule.Status
                }
            };
        }
    }
}
