using VideoEdit.Domain.Models;

namespace VideoEdit.Application.Services;

public sealed class MediaJobValidator
{
    public ValidationResult Validate(MediaJobRequest request)
    {
        var result = ValidationResult.Success();

        if (request.InputPaths.Count == 0)
        {
            result.Errors.Add("Debe seleccionar al menos un archivo de entrada.");
        }

        foreach (var inputPath in request.InputPaths.Where(static path => !string.IsNullOrWhiteSpace(path)))
        {
            if (!File.Exists(inputPath))
            {
                result.Errors.Add($"No existe el archivo de entrada: {inputPath}");
            }
        }

        if (string.IsNullOrWhiteSpace(request.OutputPath))
        {
            result.Errors.Add("Debe indicar un archivo de salida.");
        }

        if (!string.IsNullOrWhiteSpace(request.OutputPath) && request.InputPaths.Any(path =>
                string.Equals(Path.GetFullPath(path), Path.GetFullPath(request.OutputPath), StringComparison.OrdinalIgnoreCase)))
        {
            result.Errors.Add("El archivo de salida no puede coincidir con la entrada.");
        }

        ValidateTimeRange(request, result);
        ValidateContainerCompatibility(request, result);

        if (request.JobType == MediaJobType.Concat && request.InputPaths.Count < 2)
        {
            result.Errors.Add("La concatenación requiere al menos dos archivos.");
        }

        return result;
    }

    private static void ValidateTimeRange(MediaJobRequest request, ValidationResult result)
    {
        if (request.JobType != MediaJobType.TrimVideo || request.TimeRange is null)
        {
            return;
        }

        if (request.TimeRange.Start is null)
        {
            result.Errors.Add("Debe indicar un tiempo inicial.");
        }

        if (request.TimeRange.End is not null && request.TimeRange.Start is not null && request.TimeRange.End <= request.TimeRange.Start)
        {
            result.Errors.Add("El tiempo final debe ser mayor que el inicial.");
        }

        if (request.TimeRange.Duration is not null && request.TimeRange.Duration <= TimeSpan.Zero)
        {
            result.Errors.Add("La duración debe ser mayor que cero.");
        }
    }

    private static void ValidateContainerCompatibility(MediaJobRequest request, ValidationResult result)
    {
        var extension = request.ContainerExtension?.TrimStart('.').ToLowerInvariant()
                        ?? Path.GetExtension(request.OutputPath).TrimStart('.').ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(extension))
        {
            return;
        }

        if (extension is "mp3" && !string.IsNullOrWhiteSpace(request.VideoCodec))
        {
            result.Errors.Add("El contenedor MP3 no admite video.");
        }

        if (extension is "mp4" && string.Equals(request.AudioCodec, "libopus", StringComparison.OrdinalIgnoreCase))
        {
            result.Errors.Add("MP4 no es el contenedor recomendado para audio Opus.");
        }
    }
}
