using Dicom.Edge.Abstractions.Persistence;
using Dicom.Edge.Contracts.Hub;
using Dicom.Edge.Hub.Domain.Aggregates.Patients;
using Dicom.Edge.Hub.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Dicom.Edge.Hub.Application.CsvServices;

public interface ICsvImportService
{
    Task<ImportResultDto> ImportPatientsAsync(Stream csvStream, CancellationToken ct = default);
}

public sealed class CsvImportService(
    IPatientRepository patientRepository,
    IUnitOfWork unitOfWork,
    ILogger<CsvImportService> logger) : ICsvImportService
{
    public async Task<ImportResultDto> ImportPatientsAsync(Stream csvStream, CancellationToken ct = default)
    {
        using var reader = new StreamReader(csvStream);
        var rows = new List<ImportRowResult>();
        var header = await reader.ReadLineAsync(ct);
        if (string.IsNullOrWhiteSpace(header))
            return new ImportResultDto { TotalRecords = 0, SuccessCount = 0, ErrorCount = 0, Rows = rows };

        var rowNumber = 1;
        var successCount = 0;
        var errorCount = 0;

        while (!reader.EndOfStream)
        {
            rowNumber++;
            var line = await reader.ReadLineAsync(ct);
            if (string.IsNullOrWhiteSpace(line)) continue;

            try
            {
                var fields = ParseCsvLine(line);
                if (fields.Length < 3)
                {
                    rows.Add(new ImportRowResult { RowNumber = rowNumber, Status = "Error", Error = "Minimum 3 columns required: PatientDicomId, PatientName, BirthDate" });
                    errorCount++;
                    continue;
                }

                var dicomId = fields[0].Trim();
                var name = fields[1].Trim();

                if (string.IsNullOrWhiteSpace(dicomId) || string.IsNullOrWhiteSpace(name))
                {
                    rows.Add(new ImportRowResult { RowNumber = rowNumber, Status = "Error", Identifier = dicomId, Error = "PatientDicomId and PatientName are required" });
                    errorCount++;
                    continue;
                }

                var existing = await patientRepository.GetByPatientDicomIdAsync(dicomId, ct);
                if (existing is not null)
                {
                    rows.Add(new ImportRowResult { RowNumber = rowNumber, Status = "Skipped", Identifier = dicomId, Error = "Patient already exists" });
                    continue;
                }

                DateOnly? birthDate = null;
                if (fields.Length > 2 && DateOnly.TryParseExact(fields[2].Trim(), "yyyyMMdd", out var bd))
                    birthDate = bd;

                var sex = fields.Length > 3 ? fields[3].Trim() : null;
                var phone = fields.Length > 4 ? fields[4].Trim() : null;
                var email = fields.Length > 5 ? fields[5].Trim() : null;

                var patient = Patient.Create(
                    PatientIdentifier.Create(dicomId),
                    name,
                    birthDate,
                    sex,
                    phoneNumber: phone,
                    email: email);

                await patientRepository.AddAsync(patient, ct);
                rows.Add(new ImportRowResult { RowNumber = rowNumber, Status = "Imported", Identifier = dicomId });
                successCount++;
            }
            catch (Exception ex)
            {
                rows.Add(new ImportRowResult { RowNumber = rowNumber, Status = "Error", Error = ex.Message });
                errorCount++;
            }
        }

        if (successCount > 0)
            await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Patient CSV import: {Success} imported, {Errors} errors, {Total} total",
            successCount, errorCount, rowNumber - 1);

        return new ImportResultDto
        {
            TotalRecords = rowNumber - 1,
            SuccessCount = successCount,
            ErrorCount = errorCount,
            Rows = rows
        };
    }

    private static string[] ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var current = new System.Text.StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (inQuotes)
            {
                if (c == '"' && i + 1 < line.Length && line[i + 1] == '"') { current.Append('"'); i++; }
                else if (c == '"') inQuotes = false;
                else current.Append(c);
            }
            else
            {
                if (c == '"') inQuotes = true;
                else if (c == ',') { fields.Add(current.ToString()); current.Clear(); }
                else current.Append(c);
            }
        }
        fields.Add(current.ToString());
        return [.. fields];
    }
}
