using EntityDTO = XBOL.Ticketing.Core.DTO.Role;
using EntityModel = XBOL.Ticketing.Core.Model.Role;

namespace XBOL.Ticketing.Core.Mappers
{
    public static class RoleMapper
    {
        public static List<EntityDTO> ToDto(this IList<EntityModel> entities) => entities.Select(x => x.ToDto()).ToList();

        public static EntityDTO ToDto(this EntityModel entity)
        {
            return new EntityDTO
            {
                Id = entity.Id,
                Name = entity.Name ?? string.Empty
            };
        }

        public static List<EntityModel> ToModel(this IList<EntityDTO> entities) => entities.Select(x => x.ToModel()).ToList();

        public static EntityModel ToModel(this EntityDTO entity)
        {
            return new EntityModel
            {
                Id = entity.Id,
                Name = entity.Name
            };
        }
    }
}