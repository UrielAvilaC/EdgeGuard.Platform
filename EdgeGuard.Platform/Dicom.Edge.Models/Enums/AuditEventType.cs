namespace Dicom.Edge.Models.Enums
{
    /// <summary>
    /// Types of audit events tracked in the system.
    /// </summary>
    public enum AuditEventType
    {
        // Study lifecycle
        StudyReceived = 0,
        StudyCompleted = 1,
        StudySent = 2,
        StudyFailed = 3,
        StudyArchived = 4,
        StudyDeleted = 5,
        
        // Association events
        AssociationRequested = 10,
        AssociationAccepted = 11,
        AssociationRejected = 12,
        AssociationAborted = 13,
        
        // System events
        SystemStarted = 20,
        SystemStopped = 21,
        ConfigurationChanged = 22,
        
        // Security events
        AuthenticationSuccess = 30,
        AuthenticationFailure = 31,
        AuthorizationFailure = 32,
        UnauthorizedAccess = 33,
        
        // Data access
        PatientDataAccessed = 40,
        StudyViewed = 41,
        ReportGenerated = 42
    }
}
