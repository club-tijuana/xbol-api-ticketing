using XBOL.Ticketing.Core.Commons.Enums;
using XBOL.Ticketing.Core.DTO.Responses;
using XBOL.Ticketing.Core.Model;

namespace XBOL.Ticketing.Data.Queries;

public static class MediaQueryExtensions
{
    public static IQueryable<Media> AvailableBlobMedia(this IQueryable<Media> query)
    {
        return query.Where(media =>
            media.DeletedAt == null
            && media.BlobAsset.Status == BlobAssetStatus.Available);
    }

    public static IQueryable<Media> ForReference(
        this IQueryable<Media> query,
        long referenceId,
        SaleType referenceType)
    {
        return query.Where(media =>
            media.ReferenceId == referenceId
            && media.ReferenceType == referenceType);
    }

    public static IQueryable<MediaResponse> SelectMediaResponse(this IQueryable<Media> query)
    {
        return query.Select(media => new MediaResponse
        {
            Id = media.Id,
            ReferenceId = media.ReferenceId,
            ReferenceType = media.ReferenceType,
            Title = media.BlobAsset.FileName,
            FileName = media.BlobAsset.FileName,
            ContentType = media.BlobAsset.ContentType,
            Url = media.BlobAsset.Url,
            MediaType = media.MediaType,
            Order = media.Order
        });
    }
}
