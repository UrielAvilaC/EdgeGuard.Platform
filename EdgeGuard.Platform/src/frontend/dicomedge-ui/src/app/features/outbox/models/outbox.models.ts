export interface OutboxActivity {
  id: string;
  /** 'NodeSync' | 'Notification' */
  category: string;
  topicId: string;
  topicName: string;
  target?: string | null;
  status: string;
  attempts: number;
  nextAttemptAt?: string | null;
  lastError?: string | null;
  createdAt: string;
  processedAt?: string | null;
}

export interface OutboxTopic {
  id: string;
  category: string;
  displayName: string;
  description?: string | null;
  enabled: boolean;
  defaultMaxAttempts: number;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface OutboxEntryChanged {
  category: string;
  id: string;
  topicId: string;
  status: string;
  attempts: number;
  error?: string | null;
}

export type OutboxCategoryFilter = '' | 'NodeSync' | 'Notification';

/** The REST action route segment for a given category. */
export function storeForCategory(category: string): 'node' | 'notifications' {
  return category === 'NodeSync' ? 'node' : 'notifications';
}
