namespace Dicom.Edge.Contracts.Errors
{
    /// <summary>
    /// Data transfer object for transfer error information.
    /// </summary>
    public class TransferErrorDto
    {
        public string Id { get; set; } = default!;
        public string? StudyInstanceUid { get; set; }
        public string ErrorType { get; set; } = default!;
        public string ErrorMessage { get; set; } = default!;
        public DateTime OccurredAt { get; set; }
        public int RetryAttempt { get; set; }
        public bool IsResolved { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public string? ResolutionNotes { get; set; }
        public string? EdgeNodeId { get; set; }
        public bool IsRetryable { get; set; }
    }
}
