using Microsoft.EntityFrameworkCore;
using XBOL.Ticketing.Core.Commons.Enums;
using XBOL.Ticketing.Core.DTO.Responses;
using XBOL.Ticketing.Data.Queries;
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
                            .ForReference(referenceId, referenceType)
                            .AvailableBlobMedia()
                            .OrderBy(m => m.Order)
                            .ThenBy(m => m.Id)
                            .SelectMediaResponse()
                            .ToListAsync();
        }

        public async Task<List<MediaResponse>> GetProductMediaByMediaTypeAsync(long referenceId, SaleType referenceType, MediaType mediaType)
        {
            return await _mediaRepository
                            .Get()
                            .AsNoTracking()
                            .ForReference(referenceId, referenceType)
                            .Where(m => m.MediaType == mediaType)
                            .AvailableBlobMedia()
                            .OrderBy(m => m.Order)
                            .ThenBy(m => m.Id)
                            .SelectMediaResponse()
                            .ToListAsync();
        }
    }
}
