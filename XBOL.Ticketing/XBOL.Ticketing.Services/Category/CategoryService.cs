using XBOL.Ticketing.Core.Commons.Enums;

namespace XBOL.Ticketing.Services.Category
{
    public class CategoryService
    {
        public List<string> GetCategoryNames()
        {
            return Enum.GetNames<EventCategory>().ToList();
        }
    }
}
