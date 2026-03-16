namespace Dicom.Edge.Common.Errors
{
    /// <summary>
    /// Centralized error codes for the EdgeGuard platform.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Error codes follow the format: CATEGORY_NNN
    /// Categories: STORAGE, DICOM, NETWORK, TRANSFER, CONFIG, QUEUE, AUTH, VALIDATION
    /// </para>
    /// </remarks>
    public static class ErrorCodes
    {
        // ==================== General Errors ====================
        public const string Validation = "VALIDATION_ERROR";
        public const string NotFound = "NOT_FOUND";
        public const string Unauthorized = "UNAUTHORIZED";
        public const string Conflict = "CONFLICT";
        public const string Internal = "INTERNAL_ERROR";

        // ==================== Storage Errors (STORAGE_xxx) ====================
        public const string StorageFull = "STORAGE_001";
        public const string StorageUnavailable = "STORAGE_002";
        public const string FileNotFound = "STORAGE_003";
        public const string FileCorrupted = "STORAGE_004";
        public const string FileAlreadyExists = "STORAGE_005";
        public const string InsufficientSpace = "STORAGE_006";
        public const string StorageAccessDenied = "STORAGE_007";
        public const string StoragePathInvalid = "STORAGE_008";
        public const string ArchiveFailure = "STORAGE_009";
        public const string DeleteFailure = "STORAGE_010";

        // ==================== DICOM Protocol Errors (DICOM_xxx) ====================
        public const string DicomAssociationRejected = "DICOM_001";
        public const string DicomInvalidData = "DICOM_002";
        public const string DicomUnsupportedSopClass = "DICOM_003";
        public const string DicomInvalidTag = "DICOM_004";
        public const string DicomParseError = "DICOM_005";
        public const string DicomAssociationAborted = "DICOM_006";
        public const string DicomTimeout = "DICOM_007";
        public const string DicomInvalidTransferSyntax = "DICOM_008";
        public const string DicomMaxConnectionsReached = "DICOM_009";
        public const string DicomUnauthorizedAeTitle = "DICOM_010";

        // ==================== Network Errors (NETWORK_xxx) ====================
        public const string NetworkTimeout = "NETWORK_001";
        public const string NetworkConnectionFailed = "NETWORK_002";
        public const string NetworkUnreachable = "NETWORK_003";
        public const string NetworkDnsResolutionFailed = "NETWORK_004";
        public const string NetworkConnectionLost = "NETWORK_005";
        public const string NetworkSslError = "NETWORK_006";
        public const string NetworkBandwidthExceeded = "NETWORK_007";

        // ==================== Transfer Errors (TRANSFER_xxx) ====================
        public const string TransferFailed = "TRANSFER_001";
        public const string TransferTimeout = "TRANSFER_002";
        public const string TransferCancelled = "TRANSFER_003";
        public const string TransferPartialFailure = "TRANSFER_004";
        public const string TransferRetryExceeded = "TRANSFER_005";
        public const string TransferDestinationUnavailable = "TRANSFER_006";
        public const string TransferIntegrityCheckFailed = "TRANSFER_007";

        // ==================== Configuration Errors (CONFIG_xxx) ====================
        public const string ConfigurationInvalid = "CONFIG_001";
        public const string ConfigurationNotFound = "CONFIG_002";
        public const string ConfigurationMissing = "CONFIG_003";
        public const string ConfigurationParseError = "CONFIG_004";
        public const string ConfigurationValidationFailed = "CONFIG_005";
        public const string ConfigurationAccessDenied = "CONFIG_006";

        // ==================== Queue Errors (QUEUE_xxx) ====================
        public const string QueueFull = "QUEUE_001";
        public const string QueueEmpty = "QUEUE_002";
        public const string QueueItemNotFound = "QUEUE_003";
        public const string QueueDeadLetter = "QUEUE_004";
        public const string QueueProcessingError = "QUEUE_005";
        public const string QueueLockTimeout = "QUEUE_006";

        // ==================== Authentication/Authorization (AUTH_xxx) ====================
        public const string AuthInvalidCredentials = "AUTH_001";
        public const string AuthTokenExpired = "AUTH_002";
        public const string AuthTokenInvalid = "AUTH_003";
        public const string AuthInsufficientPermissions = "AUTH_004";
        public const string AuthAccountLocked = "AUTH_005";
        public const string AuthApiKeyInvalid = "AUTH_006";
        public const string AuthSessionExpired = "AUTH_007";
        public const string AuthCertificateInvalid = "AUTH_008";

        // ==================== Database Errors (DB_xxx) ====================
        public const string DatabaseConnectionFailed = "DB_001";
        public const string DatabaseTimeout = "DB_002";
        public const string DatabaseConstraintViolation = "DB_003";
        public const string DatabaseDeadlock = "DB_004";
        public const string DatabaseMigrationFailed = "DB_005";

        // ==================== Business Logic Errors (BL_xxx) ====================
        public const string StudyAlreadyExists = "BL_001";
        public const string StudyIncomplete = "BL_002";
        public const string StudyNotReceived = "BL_003";
        public const string InvalidStudyState = "BL_004";
        public const string RoutingRuleNotFound = "BL_005";
        public const string ModalityNotAuthorized = "BL_006";
        public const string RetentionPolicyViolation = "BL_007";

        // ==================== External Service Errors (EXT_xxx) ====================
        public const string ExternalServiceUnavailable = "EXT_001";
        public const string ExternalServiceTimeout = "EXT_002";
        public const string ExternalServiceError = "EXT_003";
        public const string HubUnavailable = "EXT_004";
        public const string PacsUnavailable = "EXT_005";

        // ==================== Audit & Compliance (AUDIT_xxx) ====================
        public const string AuditLogFailed = "AUDIT_001";
        public const string ComplianceViolation = "AUDIT_002";
        public const string UnauthorizedDataAccess = "AUDIT_003";
    }
}

