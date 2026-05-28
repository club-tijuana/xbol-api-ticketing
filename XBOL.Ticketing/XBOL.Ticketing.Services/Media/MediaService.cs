using Microsoft.EntityFrameworkCore;
using XBOL.Ticketing.Core.Commons.Enums;
using XBOL.Ticketing.Core.DTO.Responses;
using XBOL.Ticketing.Data.Repositories.Media;

namespace XBOL.Ticketing.Services.Media
{
    public class MediaService(MediaRepository _mediaRepository)
    {
        public async Task<List<MediaResponse>> GetProductMediaAsync(long referenceId, SaleType referenceType)
        {
            return await _mediaRepository
                            .Get()
                            .AsNoTracking()
                            .Where(m => m.ReferenceId == referenceId && m.ReferenceType == referenceType)
                            .Where(x => x.DeletedAt == null)
                            .Select(m => new MediaResponse
                            {
                                Id = m.Id,
                                ReferenceId = m.ReferenceId,
                                ReferenceType = m.ReferenceType,
                                Title = m.FileName,
                                ImageBase64 = Convert.ToBase64String(m.Content),
                                ContentType = m.ContentType,
                                Url = m.Url,
                                MediaType = m.MediaType,
                                Order = m.Order
                            }).ToListAsync();
        }

        public async Task<List<MediaResponse>> GetProductMediaByMediaTypeAsync(long referenceId, SaleType referenceType, MediaType mediaType)
        {
            return await _mediaRepository
                            .Get()
                            .AsNoTracking()
                            .Where(m => m.ReferenceId == referenceId && m.ReferenceType == referenceType && m.MediaType == mediaType)
                            .Where(x => x.DeletedAt == null)
                            .Select(m => new MediaResponse
                            {
                                Id = m.Id,
                                ReferenceId = m.ReferenceId,
                                ReferenceType = m.ReferenceType,
                                Title = m.FileName,
                                ImageBase64 = Convert.ToBase64String(m.Content),
                                ContentType = m.ContentType,
                                Url = m.Url,
                                MediaType = m.MediaType,
                                Order = m.Order
                            }).ToListAsync();
        }
    }
}
