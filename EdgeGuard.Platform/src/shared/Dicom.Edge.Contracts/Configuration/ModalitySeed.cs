namespace Dicom.Edge.Contracts.Configuration;

/// <summary>A single canonical modality catalog seed row.</summary>
public sealed record ModalitySeedEntry(string Code, string DisplayName, bool IsSupported, int SortOrder);

/// <summary>
/// Canonical, shared seed for the modality reference catalog. Used by BOTH the Hub and
/// the Edge Node so the set of valid codes and their <c>IsSupported</c> flags stay identical
/// across the fleet. The rule: only image-producing modalities are <c>IsSupported = true</c>;
/// non-image objects (reports, presentation states, waveforms, most RT objects) are
/// <c>IsSupported = false</c>. Seeding is idempotent (upsert by <see cref="ModalitySeedEntry.Code"/>).
/// </summary>
public static class ModalitySeed
{
    /// <summary>The complete proposed catalog. Codes are DICOM Defined Terms (PS3.3 / PS3.16).</summary>
    public static IReadOnlyList<ModalitySeedEntry> All { get; } =
    [
        // ── Image modalities — IsSupported = true ───────────────────────────────
        new("CR",      "Radiografía Computarizada",                    true, 10),
        new("CT",      "Tomografía Computarizada",                     true, 20),
        new("DX",      "Radiografía Digital",                          true, 30),
        new("IO",      "Radiografía Intraoral",                        true, 40),
        new("MG",      "Mamografía",                                   true, 50),
        new("MR",      "Resonancia Magnética",                         true, 60),
        new("NM",      "Medicina Nuclear",                             true, 70),
        new("PT",      "Tomografía por Emisión de Positrones (PET)",   true, 80),
        new("US",      "Ultrasonido",                                  true, 90),
        new("XA",      "Angiografía por Rayos X",                      true, 100),
        new("RF",      "Radiofluoroscopía",                            true, 110),
        new("RG",      "Radiografía Convencional",                     true, 120),
        new("PX",      "Radiografía Panorámica",                       true, 130),
        new("ES",      "Endoscopía",                                   true, 140),
        new("XC",      "Fotografía con Cámara Externa",                true, 150),
        new("OP",      "Fotografía Oftálmica",                         true, 160),
        new("OPT",     "Tomografía Oftálmica (OCT)",                   true, 170),
        new("SM",      "Microscopía de Portaobjetos",                 true, 180),
        new("GM",      "Microscopía General",                          true, 190),
        new("IVUS",    "Ultrasonido Intravascular",                    true, 200),
        new("BDUS",    "Densitometría Ósea por Ultrasonido",          true, 210),
        new("BMD",     "Densitometría Ósea (Rayos X)",                 true, 220),
        new("DG",      "Diafanografía",                                true, 230),
        new("TG",      "Termografía",                                  true, 240),
        new("RTIMAGE", "Imagen de Radioterapia",                       true, 250),

        // ── Non-image modalities — IsSupported = false ──────────────────────────
        new("SR",       "Documento de Reporte Estructurado",           false, 500),
        new("KO",       "Selección de Objeto Clave",                   false, 510),
        new("PR",       "Estado de Presentación",                      false, 520),
        new("AU",       "Audio",                                       false, 530),
        new("DOC",      "Documento (PDF/CDA encapsulado)",             false, 540),
        new("FID",      "Marcadores Fiduciales",                       false, 550),
        new("REG",      "Registro (Transformación Espacial)",          false, 560),
        new("SEG",      "Segmentación",                                false, 570),
        new("RWV",      "Mapeo de Valores del Mundo Real",             false, 580),
        new("PLAN",     "Plan",                                        false, 590),
        new("RTSTRUCT", "Conjunto de Estructuras de Radioterapia",     false, 600),
        new("RTPLAN",   "Plan de Radioterapia",                        false, 610),
        new("RTDOSE",   "Dosis de Radioterapia",                       false, 620),
        new("RTRECORD", "Registro de Tratamiento de Radioterapia",     false, 630),
        new("ECG",      "Electrocardiografía",                         false, 640),
        new("EPS",      "Electrofisiología Cardíaca",                  false, 650),
        new("HD",       "Forma de Onda Hemodinámica",                  false, 660),
        new("RESP",     "Forma de Onda Respiratoria",                  false, 670),
        new("HC",       "Copia Impresa",                               false, 680),
        new("OT",       "Otro",                                        false, 690),

        // ── Borderline (measurement devices) — IsSupported = false by default ───
        new("OAM",      "Mediciones Axiales Oftálmicas",               false, 900),
        new("KER",      "Queratometría",                               false, 910),
        new("SRF",      "Refracción Subjetiva",                        false, 920),
        new("LEN",      "Lensometría",                                 false, 930),
        new("VA",       "Agudeza Visual",                              false, 940),
    ];
}
