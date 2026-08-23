/**
 * Lectura del almacenamiento de un nodo, en un solo lugar.
 *
 * Tres pantallas muestran este dato y antes cada una repetía la cuenta. Con la
 * lógica de hoy —usado es la suma de dos cosas, el porcentaje solo existe si hay
 * cuota, y «sin datos» es un estado propio— tres copias serían tres formas de
 * equivocarse distinto.
 */

/** Lo mínimo que necesita esta lectura; sirve tanto para Node como para DashboardNode. */
export interface StorageFields {
  storageLimitMb: number | null;
  storageDicomMb: number | null;
  storageDatabaseMb: number | null;
  storageVolumeFreeMb?: number | null;
  storageVolumeTotalMb?: number | null;
  storageMeasuredAt?: string | null;
}

export interface StorageReading {
  /** false cuando el nodo nunca reportó. No es lo mismo que estar vacío. */
  readonly hasData: boolean;
  /** DICOM + base de datos, que es lo que se contrasta contra la cuota. */
  readonly usedMb: number;
  readonly dicomMb: number;
  readonly databaseMb: number;
  /** null = sin cuota configurada. */
  readonly limitMb: number | null;
  /** null cuando no hay datos o no hay cuota: sin ambas cosas no hay porcentaje. */
  readonly percent: number | null;
  readonly measuredAt: string | null;
}

export function readStorage(node: StorageFields): StorageReading {
  // La medición llega junta: si falta el peso DICOM, el nodo no ha reportado.
  const hasData = node.storageDicomMb !== null && node.storageDicomMb !== undefined;

  const dicomMb = node.storageDicomMb ?? 0;
  const databaseMb = node.storageDatabaseMb ?? 0;
  const usedMb = dicomMb + databaseMb;
  const limitMb = node.storageLimitMb && node.storageLimitMb > 0 ? node.storageLimitMb : null;

  return {
    hasData,
    usedMb,
    dicomMb,
    databaseMb,
    limitMb,
    percent: hasData && limitMb ? Math.round((usedMb / limitMb) * 100) : null,
    measuredAt: node.storageMeasuredAt ?? null,
  };
}

/** Color de la barra según qué tan cerca está de la cuota. */
export function storageBarClass(percent: number | null): string {
  if (percent === null) return 'bg-gray-300 dark:bg-gray-600';
  if (percent > 90) return 'bg-red-500';
  if (percent > 70) return 'bg-amber-500';
  return 'bg-emerald-500';
}

/** Formatea MB con la unidad que corresponda al tamaño. */
export function formatMb(mb: number | null): string {
  if (mb === null || mb === undefined) return '—';
  if (mb >= 1024 * 1024) return `${(mb / (1024 * 1024)).toFixed(1)} TB`;
  if (mb >= 1024) return `${(mb / 1024).toFixed(1)} GB`;
  return `${Math.round(mb)} MB`;
}

/**
 * Texto de la lectura para una tabla: los MB usados, y contra qué, si hay cuota.
 */
export function storageSummary(reading: StorageReading): string {
  if (!reading.hasData) return 'Sin datos';
  if (reading.limitMb === null) return formatMb(reading.usedMb);
  return `${formatMb(reading.usedMb)} / ${formatMb(reading.limitMb)}`;
}
