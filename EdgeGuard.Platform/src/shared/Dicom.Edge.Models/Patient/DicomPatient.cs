namespace Dicom.Edge.Models.Patient
{
    /// <summary>
    /// Represents a patient with comprehensive DICOM demographics and administrative information.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Patient information in DICOM is defined in the Patient Module (C.7.1.1) of the DICOM standard.
    /// This class captures all relevant patient demographics for medical imaging workflows,
    /// including Modality Worklist integration and regulatory compliance.
    /// </para>
    /// <para>
    /// <strong>Privacy and Security:</strong>
    /// <list type="bullet">
    ///   <item><description>Contains PHI (Protected Health Information)</description></item>
    ///   <item><description>Must be handled according to HIPAA, GDPR, and local regulations</description></item>
    ///   <item><description>Consider encryption at rest and in transit</description></item>
    ///   <item><description>Implement access control and audit logging</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>DICOM Tag Mapping:</strong>
    /// <code>
    /// PatientId          → (0010,0020)
    /// PatientName        → (0010,0010)
    /// BirthDate          → (0010,0030)
    /// Sex                → (0010,0040)
    /// PatientAge         → (0010,1010)
    /// AccessionNumber    → (0008,0050)
    /// ReferringPhysician → (0008,0090)
    /// InstitutionName    → (0008,0080)
    /// </code>
    /// </para>
    /// </remarks>
    public class DicomPatient
    {
        // ==================== Core Patient Identifiers ====================

        /// <summary>
        /// Gets or sets the Patient ID (0010,0020).
        /// </summary>
        /// <remarks>
        /// <para>
        /// Primary identifier assigned by the hospital/facility.
        /// <strong>Important:</strong> May not be globally unique - different facilities may use the same IDs.
        /// </para>
        /// <para>
        /// For enterprise systems spanning multiple facilities, consider using a composite key:
        /// <code>
        /// string GlobalId = $"{InstitutionName}_{PatientId}";
        /// </code>
        /// </para>
        /// <para>
        /// <strong>Privacy Note:</strong> Consider tokenization or pseudonymization for data-sharing scenarios.
        /// </para>
        /// </remarks>
        public string PatientId { get; set; } = default!;

        /// <summary>
        /// Gets or sets the Patient Name (0010,0010) in DICOM PN format.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <strong>DICOM PN Format:</strong> "FamilyName^GivenName^MiddleName^Prefix^Suffix"
        /// </para>
        /// <para>
        /// <strong>Examples:</strong>
        /// <list type="bullet">
        ///   <item><description>"Doe^John^A" → John A Doe</description></item>
        ///   <item><description>"Smith^Jane" → Jane Smith</description></item>
        ///   <item><description>"van der Berg^Hans^Willem^Dr" → Dr Hans Willem van der Berg</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// <strong>Privacy:</strong> PHI - handle with appropriate security measures.
        /// </para>
        /// </remarks>
        public string PatientName { get; set; } = default!;

        /// <summary>
        /// Gets or sets the Patient Birth Date (0010,0030).
        /// </summary>
        /// <remarks>
        /// <para>
        /// Optional but recommended for patient identification and age calculation.
        /// Stored as nullable to handle cases where birth date is unknown or not provided.
        /// </para>
        /// <para>
        /// <strong>Privacy Note:</strong> Birth date is PHI - some privacy-preserving systems
        /// only store birth year or age ranges instead of full date.
        /// </para>
        /// </remarks>
        public DateTime? BirthDate { get; set; }

        /// <summary>
        /// Gets or sets the Patient Sex (0010,0040).
        /// </summary>
        /// <remarks>
        /// <para>
        /// <strong>DICOM Defined Terms:</strong>
        /// <list type="bullet">
        ///   <item><description>"M" - Male</description></item>
        ///   <item><description>"F" - Female</description></item>
        ///   <item><description>"O" - Other</description></item>
        ///   <item><description>"" (empty) - Unknown</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// Single character as per DICOM standard.
        /// </para>
        /// </remarks>
        public string Sex { get; set; } = default!;

        // ==================== Additional Demographics ====================

        /// <summary>
        /// Gets or sets the Patient Age (0010,1010) as a DICOM Age String.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <strong>DICOM Age String Format:</strong> nnnD (days), nnnW (weeks), nnnM (months), nnnY (years)
        /// </para>
        /// <para>
        /// <strong>Examples:</strong>
        /// <list type="bullet">
        ///   <item><description>"042Y" - 42 years old</description></item>
        ///   <item><description>"003M" - 3 months old</description></item>
        ///   <item><description>"010D" - 10 days old</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// Typically calculated from BirthDate at study time, but may be provided directly
        /// for privacy-preserving scenarios where exact birth date is not available.
        /// </para>
        /// </remarks>
        public string? PatientAge { get; set; }

        /// <summary>
        /// Gets or sets the Patient Weight in kilograms.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Optional but important for certain modalities:
        /// <list type="bullet">
        ///   <item><description>CT: Radiation dose calculations</description></item>
        ///   <item><description>Nuclear Medicine: Tracer dose calculations</description></item>
        ///   <item><description>Contrast injection protocols</description></item>
        /// </list>
        /// </para>
        /// </remarks>
        public double? PatientWeightKg { get; set; }

        /// <summary>
        /// Gets or sets the Patient Height in meters.
        /// </summary>
        /// <remarks>
        /// Optional. Used with weight for BMI calculations and dosing protocols.
        /// </remarks>
        public double? PatientHeightM { get; set; }

        // ==================== Study Context (Enterprise Extensions) ====================

        /// <summary>
        /// Gets or sets the Accession Number (0008,0050) for the associated study.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Hospital/RIS identifier that links imaging study to the clinical order.
        /// <strong>Key for Worklist (MWL) integration.</strong>
        /// </para>
        /// <para>
        /// Use cases:
        /// <list type="bullet">
        ///   <item><description>Correlate study with original order</description></item>
        ///   <item><description>Billing and insurance verification</description></item>
        ///   <item><description>Workflow tracking</description></item>
        /// </list>
        /// </para>
        /// </remarks>
        public string? AccessionNumber { get; set; }

        /// <summary>
        /// Gets or sets the Referring Physician Name (0008,0090).
        /// </summary>
        /// <remarks>
        /// <para>
        /// Name of physician who referred/ordered the imaging study.
        /// Format follows DICOM PN (Person Name) structure: "FamilyName^GivenName"
        /// </para>
        /// <para>
        /// Important for:
        /// <list type="bullet">
        ///   <item><description>Results reporting (send to referring physician)</description></item>
        ///   <item><description>Regulatory compliance</description></item>
        ///   <item><description>Billing workflows</description></item>
        /// </list>
        /// </para>
        /// </remarks>
        public string? ReferringPhysician { get; set; }

        /// <summary>
        /// Gets or sets the Institution Name (0008,0080) where the study was performed.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Name of hospital, clinic, or imaging center.
        /// Critical for multi-facility enterprises to:
        /// <list type="bullet">
        ///   <item><description>Disambiguate patient IDs</description></item>
        ///   <item><description>Route studies correctly</description></item>
        ///   <item><description>Generate facility-specific reports</description></item>
        /// </list>
        /// </para>
        /// </remarks>
        public string? InstitutionName { get; set; }

        /// <summary>
        /// Gets or sets the Medical Record Number (hospital-specific).
        /// </summary>
        /// <remarks>
        /// <para>
        /// Alternative patient identifier used in some facilities.
        /// May be different from PatientId.
        /// </para>
        /// <para>
        /// Useful for:
        /// <list type="bullet">
        ///   <item><description>Integration with hospital EMR/HIS systems</description></item>
        ///   <item><description>Cross-referencing with billing systems</description></item>
        /// </list>
        /// </para>
        /// </remarks>
        public string? MedicalRecordNumber { get; set; }

        // ==================== Additional Clinical Information ====================

        /// <summary>
        /// Gets or sets the patient phone number extracted from PID-13 / PID-14.
        /// </summary>
        public string? PhoneNumber { get; set; }

        /// <summary>
        /// Gets or sets the patient email extracted from PID-13 repetitions.
        /// </summary>
        public string? Email { get; set; }

        /// <summary>
        /// Gets or sets patient allergies for safety protocols.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Free text field. Common allergies to capture for imaging:
        /// <list type="bullet">
        ///   <item><description>Iodinated contrast</description></item>
        ///   <item><description>Gadolinium (MRI contrast)</description></item>
        ///   <item><description>Latex</description></item>
        ///   <item><description>Medications used in imaging protocols</description></item>
        /// </list>
        /// </para>
        /// </remarks>
        public string? Allergies { get; set; }

        /// <summary>
        /// Gets or sets additional patient comments or special instructions.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Free text for:
        /// <list type="bullet">
        ///   <item><description>Special handling requirements</description></item>
        ///   <item><description>Patient mobility restrictions</description></item>
        ///   <item><description>Communication needs (language, hearing, etc.)</description></item>
        /// </list>
        /// </para>
        /// </remarks>
        public string? Comments { get; set; }

        // ==================== Computed Properties ====================

        /// <summary>
        /// Gets the calculated age in years based on BirthDate.
        /// </summary>
        /// <remarks>
        /// Returns null if BirthDate is not set.
        /// Calculates age as of today.
        /// </remarks>
        public int? AgeYears
        {
            get
            {
                if (!BirthDate.HasValue) return null;

                var today = DateTime.Today;
                var age = today.Year - BirthDate.Value.Year;

                // Adjust if birthday hasn't occurred this year
                if (BirthDate.Value.Date > today.AddYears(-age))
                    age--;

                return age;
            }
        }

        /// <summary>
        /// Gets the formatted display name (converts DICOM PN format to readable format).
        /// </summary>
        /// <remarks>
        /// <para>
        /// Converts "LastName^FirstName^MiddleName" to "FirstName MiddleName LastName"
        /// </para>
        /// <para>
        /// <strong>Example:</strong>
        /// "Doe^John^A" → "John A Doe"
        /// </para>
        /// </remarks>
        public string DisplayName
        {
            get
            {
                if (string.IsNullOrWhiteSpace(PatientName))
                    return "Unknown Patient";

                var parts = PatientName.Split('^');
                if (parts.Length == 1)
                    return parts[0]; // No formatting needed

                // Reorder: Given Middle Family
                var formatted = parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1])
                    ? parts[1] // Given name
                    : "";

                if (parts.Length > 2 && !string.IsNullOrWhiteSpace(parts[2]))
                    formatted += " " + parts[2]; // Middle name

                if (parts.Length > 0 && !string.IsNullOrWhiteSpace(parts[0]))
                    formatted += " " + parts[0]; // Family name

                return formatted.Trim();
            }
        }

        /// <summary>
        /// Gets the Body Mass Index (BMI) if weight and height are available.
        /// </summary>
        /// <remarks>
        /// <para>
        /// BMI = weight(kg) / height(m)²
        /// </para>
        /// <para>
        /// Returns null if either weight or height is not set.
        /// </para>
        /// </remarks>
        public double? BodyMassIndex
        {
            get
            {
                if (!PatientWeightKg.HasValue || !PatientHeightM.HasValue || PatientHeightM.Value == 0)
                    return null;

                return PatientWeightKg.Value / (PatientHeightM.Value * PatientHeightM.Value);
            }
        }
    }
}
