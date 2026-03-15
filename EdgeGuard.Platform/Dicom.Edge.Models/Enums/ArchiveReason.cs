namespace Dicom.Edge.Models.Enums
{
    /// <summary>
    /// Reason why a study was archived.
    /// </summary>
    public enum ArchiveReason
    {
        /// <summary>
        /// Study was successfully sent to all destinations and can be archived.
        /// </summary>
        SentSuccessfully = 0,
        
        /// <summary>
        /// Study reached retention policy age limit.
        /// </summary>
        RetentionPolicy = 1,
        
        /// <summary>
        /// Administrator manually archived the study.
        /// </summary>
        ManualArchive = 2,
        
        /// <summary>
        /// Storage capacity reached threshold and older studies are being archived.
        /// </summary>
        StorageFull = 3,
        
        /// <summary>
        /// Study deemed duplicate and archived.
        /// </summary>
        Duplicate = 4,
        
        /// <summary>
        /// Study failed quality checks and was archived for review.
        /// </summary>
        QualityFailed = 5
    }
}
